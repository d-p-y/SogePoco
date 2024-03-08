using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Compiler;
using SogePoco.Impl.Tests.Model;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.UsingMsBuild;

namespace SogePoco.Impl.Tests.QueryGeneration; 

public static class QueryGeneratorTestUtil {
		
    public static string QueryRegistrationApiSnippet => @"
using System;
using System.Linq;
using SogePoco.Pocos;
using SogePoco.Common;
";
	  
    public static async Task GenerateCompileAndAssert(
            string testName,
            SystemUnderTest sut,
            GeneratorOptions opt,
            string queryRegistrationApiCs,
            string queriesCs,
            Action<Compilation, SyntaxNode> onElement,
            Func<PocoSchema, ISet<SimpleNamedFile>> generateCode,
            Func<Assembly,Task> postCompilationAssertions,
            bool forceSchemaRegeneration=false) {
	            
        using var cleanup = new OnFinallyAction();

        //for quicker tests
        var rawModel = 
            forceSchemaRegeneration
            ? await DbSchema.CreateOf(sut.DbConn.DbConn, sut.SchemaExtractor) 
            : DbSchema.CreateOf(sut.DbConn.DbConn, sut.TestingSchema.GetAsSyntheticModel());
        
        var rawSchemaFile = PocoClassesGenerator.SerializeDbSchema(rawModel);

        var richModel = PocoClassesGenerator.BuildRichModel(rawModel, opt, sut.CodeConvention, sut.MapperGenerator);
        var pocoMetadataFile = PocoClassesGenerator.SerializePocosMetadata(richModel);
	        
        var pocosCsFiles = ClassesGenerator.GeneratePocosAndDatabaseClasses(
            richModel,
            sut.Naming,
            opt,
            sut.MapperGenerator);
	        
        var pocosProj = new Csproj(
            "pocos", pocosCsFiles.ToList(), 
            new []{ rawSchemaFile, pocoMetadataFile},
            NugetPackageCollection.ForGeneratedPocos, CsprojCollection.Empty);
	        
        var sln = new Sln(
            ProcessExec.HandyTempDirLocation, 
            testName, 
            new []{pocosProj});
	        
        cleanup.Add(sln.RemoveFromDisk);

        var apiAndQueryRequestCsFile = new SimpleNamedFile(
            "queries.cs",
            queryRegistrationApiCs + queriesCs);
	        
        var requestsForQueriesProj = new Csproj(
            "requests.for.queries", apiAndQueryRequestCsFile.AsSingletonCollection(), 
            EmbeddedResourceCollection.Empty, NugetPackageCollection.Empty, pocosProj.AsSingletonCollection());
	        
        sln.Add(requestsForQueriesProj);
	        
        using var compiler = new InMemoryCompiler();
	        
        var _ = compiler
            .CompileToAssembly(
                "PoCos_pre.dll",
                pocosCsFiles.ToHashSet().Also(x => x.Add(apiAndQueryRequestCsFile)),
                dependantAssemblies:new [] {
                    typeof(DbConnection).Assembly,
                    typeof(DbCommand).Assembly, 
                    typeof(Component).Assembly,
                    typeof(System.Runtime.Serialization.ISerializable).Assembly,
                    typeof(Enumerable).Assembly,
                    sut.DbConn.DbConn.GetType().Assembly,
                    typeof(SogePoco.Common.Query).Assembly
                })
            .ToAssembly();
	        
        var pocoMetadata = PocoClassesGenerator.DeserializePocosMetadataOrFail(pocoMetadataFile); //TODO make it properly taken from csproj
           
        var generator = await QueryGenerationClasses.CreateFromMsbuild(sln.SlnFullPath, pocoMetadata);
            
        var queriesCsFiles = generator.Process(onElement, generateCode);
            
        var generatedQueriesCsproj = new Csproj(
            "generated.queries", queriesCsFiles.ToList(), 
            EmbeddedResourceCollection.Empty, NugetPackageCollection.ForGeneratedQueries, 
            pocosProj.AsSingletonCollection());
            
        sln.Add(generatedQueriesCsproj);
            
        var queriesAssembly = compiler
            .CompileToAssembly(
                "PoCos_post.dll",
                pocosCsFiles.ToHashSet()
                    .Also(x => x.Add(apiAndQueryRequestCsFile))
                    .Also(x => queriesCsFiles.ForEach(f => x.Add(f)) ),
                dependantAssemblies:new [] {
                    typeof(DbConnection).Assembly,
                    typeof(DbCommand).Assembly, 
                    typeof(Component).Assembly,
                    typeof(System.Runtime.Serialization.ISerializable).Assembly,
                    typeof(Enumerable).Assembly,
                    sut.DbConn.DbConn.GetType().Assembly,
                    typeof(SogePoco.Common.Query).Assembly
                })
            .ToAssembly();
	        
        await postCompilationAssertions(queriesAssembly);
            
        cleanup.EnableInvokeActionInFinally();
    }
        
}