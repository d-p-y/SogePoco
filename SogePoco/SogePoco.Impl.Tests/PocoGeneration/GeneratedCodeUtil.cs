using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Compiler;
using SogePoco.Impl.Tests.Connections;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.Model;
using SogePoco.Impl.Tests.Schemas;
using Xunit;
using DefaultableColumnShouldInsert = System.Func<(System.Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue),bool>;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public record GeneratedCodeResult(Assembly Asm, object DbConn, TestData? TestData, Type FooT, Type ChildOfFooT, Type TableWithCompositePkT) {
        
    public static void ForwardLastSqlOfDatabaseClassInstanceIntoLogger(object rawDbConn, ILogger logger) {
        logger.LogDebug($"LastSqlText={((dynamic)rawDbConn).LastSqlText}" );

        var result = (System.Data.Common.DbParameter[]?)((dynamic)rawDbConn).LastSqlParams switch {
            null => "null",
            var raw => raw
                .Select(x => $"(paramName={x.ParameterName},valueType={(x.Value?.GetType().FullName ?? "null")},value={x.Value})")
                .ConcatenateUsingSep(",\n    ") };

        logger.LogDebug($"LastSqlParams={result}");
    }

    public void ForwardLastSqlToLogger(ILogger logger) => ForwardLastSqlOfDatabaseClassInstanceIntoLogger(DbConn, logger);
        
    public void ApplyZeroOrException(ZeroOrException strategy) {
        Action<string> action;
            
        switch(strategy) {
            case ZeroOrException.CustomException:
                action = msg => throw new SomeException(msg);
                ((dynamic)DbConn).OnRowsAffectedExpectedToBeExactlyOne = action;
                break;
            case ZeroOrException.DefaultException:
                //do nothing, available by default
                break;
                    
            case ZeroOrException.Zero:
                action = msg => {};
                ((dynamic)DbConn).OnRowsAffectedExpectedToBeExactlyOne = action;
                break;
                
            default: throw new Exception("unsupported ZeroOrException");
        }
    }

}

public class GeneratedCodeUtil {
    public static ILogger? Logger;
        
    public static Sln DumpToDisk(string projectName, ISet<SimpleNamedFile> sources) {
        var csproj = new Csproj(
            projectName, sources.ToList(), EmbeddedResourceCollection.Empty, 
            NugetPackageCollection.ForGeneratedPocos, CsprojCollection.Empty);
            
        var sln = new Sln(ProcessExec.HandyTempDirLocation, projectName, csproj.AsSingletonCollection());
            
        Logger?.LogDebug($"{nameof(GeneratedCodeUtil)}->{nameof(DumpToDisk)} project={projectName} sources={sources} to solution={sln.SlnFullPath}");
        return sln;
    }

    public static async Task<GeneratedCodeResult> BuildGeneratedCode(
            SingleServingDbConnection rawDbConn, 
            ITestingSchema testingSchema, 
            GeneratorOptions opt,
            ISqlParamNamingStrategy naming, 
            ICodeConvention convention, 
            IDatabaseDotnetDataMapperGenerator mapper,
            SchemaDataOp op,
            DefaultableColumnShouldInsert databaseClassArgument,
            Action<ISet<SimpleNamedFile>> forwardFiles) {
                
        using var compiler = new InMemoryCompiler();
        
        var dbSchema = DbSchema.CreateOf(rawDbConn.DbConn, testingSchema.GetAsSyntheticModel());
        var richModel = PocoClassesGenerator.BuildRichModel(
            dbSchema, opt, convention, mapper);
            
        var sources = ClassesGenerator.GeneratePocosAndDatabaseClasses(richModel, naming, opt, mapper);

        forwardFiles(sources);
                
        var asm = compiler.CompileToAssembly("PoCos.dll",
                sources,
                dependantAssemblies:new [] {
                    typeof(DbConnection).Assembly,
                    typeof(DbCommand).Assembly, 
                    typeof(Component).Assembly,
                    typeof(System.Runtime.Serialization.ISerializable).Assembly,
                    typeof(Enumerable).Assembly,
                    rawDbConn.DbConn.GetType().Assembly,
                    typeof(SogePoco.Common.Query).Assembly
                })
            .ToAssembly();
           
        await testingSchema.CreateSchema(rawDbConn.DbConn);

        var testData = op switch {
            SchemaDataOp.CreateSchemaAndPopulateData => await testingSchema.CreateData(rawDbConn.DbConn),
            SchemaDataOp.CreateSchemaOnly => null,
            _ => throw new Exception($"unsupported {nameof(SchemaDataOp)}") 
        };
            
        var dbConn = asm.CreateInstance(opt.DatabaseClassFullName, rawDbConn.DbConn, databaseClassArgument);
            
        Assert.NotNull(dbConn);
            
        var fooT = asm.GetTypeOrFail("SogePoco.Pocos.Foo");                        
        var childOfFooT = asm.GetTypeOrFail("SogePoco.Pocos.ChildOfFoo");            
        var tableWithCompositePkT = asm.GetTypeOrFail("SogePoco.Pocos.TableWithCompositePk");
                        
        return new GeneratedCodeResult(asm, dbConn!, testData, fooT!, childOfFooT!, tableWithCompositePkT!);
    }
}