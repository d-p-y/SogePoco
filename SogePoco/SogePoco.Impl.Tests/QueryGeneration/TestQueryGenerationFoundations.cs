using System;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.Model;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using SogePoco.Impl.UsingMsBuild;
using Xunit;
using Xunit.Abstractions;
using SogePoco.Common;
	
namespace SogePoco.Impl.Tests.QueryGeneration; 

public class TestQueryGenerationFoundations : BaseTest {
	public TestQueryGenerationFoundations(ITestOutputHelper outputHelper) : base(outputHelper) { }
		
	[Fact]
	public async Task AbleToVisitCode() {
		var testProj = new Csproj(
			"some.project.name", 
			new [] {new SimpleNamedFile("someclass.cs", @"
using System; 
namespace testproj.requestsquery { 
	public class Class1 {} 
} ") }, EmbeddedResourceCollection.Empty, NugetPackageCollection.Empty, CsprojCollection.Empty);

		var testSln = new Sln(ProcessExec.HandyTempDirLocation, $"solution.for.test.{nameof(AbleToVisitCode)}", new[] {testProj});
	        
		using var cleanup = new OnFinallyAction(() => testSln.RemoveFromDisk());

		var richModel = new PocoSchema(string.Empty, string.Empty, Array.Empty<SqlTableForCodGen>());
            
		var generator = await QueryGenerationClasses.CreateFromMsbuild(testSln.SlnFullPath, richModel);
            
		var classesCount = 0;
            
		generator.Process(
			visitor:(_,syntaxNode) => {
				Logger.Log(LogLevel.Debug, $"syntaxNode {syntaxNode.GetType().FullName}");
				if (syntaxNode is ClassDeclarationSyntax cds) {
					classesCount++;
				}
			}, 
			generator:_ => new HashSet<SimpleNamedFile>());
            
		Assert.Equal(1, classesCount);
            
		cleanup.EnableInvokeActionInFinally();
	}
        
	[Fact]
	public async Task AbleToVisitGeneratedPocoCode() {
		var dbToTest = DbToTest.Sqlite;
		using var sut = await SystemUnderTestFactory.Create(dbToTest);
		var opt = new GeneratorOptions();
	        
		var dbSchema = DbSchema.CreateOf(sut.DbConn.DbConn, sut.TestingSchema.GetAsSyntheticModel());
		var richModel = PocoClassesGenerator.BuildRichModel(
			dbSchema, opt, sut.CodeConvention, sut.MapperGenerator);
	        
		var sources = ClassesGenerator.GeneratePocosAndDatabaseClasses(
			richModel,    
			sut.Naming,
			opt,
			sut.MapperGenerator);

		var pocosProj = new Csproj(
			"generated.pocos", sources.ToList(), EmbeddedResourceCollection.Empty, 
			NugetPackageCollection.ForGeneratedPocos, CsprojCollection.Empty);
	        
		var sln = new Sln(ProcessExec.HandyTempDirLocation, nameof(AbleToVisitGeneratedPocoCode), pocosProj.AsSingletonCollection());
		using var cleanup = new OnFinallyAction(() => sln.RemoveFromDisk());
	        
		var pocoSchema = new PocoSchema(string.Empty, string.Empty, Array.Empty<SqlTableForCodGen>());
            
		var generator = await QueryGenerationClasses.CreateFromMsbuild(sln.SlnFullPath, pocoSchema);
            
		var classesCount = 0;
            
		generator.Process(
			visitor:(_,syntaxNode) => {
				Logger.Log(LogLevel.Debug, $"syntaxNode {syntaxNode.GetType().FullName}");
				if (syntaxNode is ClassDeclarationSyntax cds) {
					classesCount++;
				} 
			},
			generator: _ => new HashSet<SimpleNamedFile>());
            
		Assert.Equal(4+2+1 /*pocos + foreign keys in posos + database*/, classesCount);
	        
		cleanup.EnableInvokeActionInFinally();
	}

	[Fact]
	public async Task FindsQueryGeneratorRequests() {
		var generateQueryAttributeNames = new [] {
			GenerateQueriesAttribute.FullName, GenerateQueriesAttribute.ShortName
		};
		var found = 0;
		using var sut = await SystemUnderTestFactory.Create(DbToTest.Sqlite);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
	        
		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(FindsQueryGeneratorRequests),
			sut,
			new GeneratorOptions(),
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			@"
[SogePoco.Common.GenerateQueries]
class First {
	public void GetFooWithIdAtLeast(int minimumId) => Query.Register((Foo f) => f.Id >= minimumId);		
}

[GenerateQueries]
class Second {
	public void GetFooWithPositiveId() => Query.Register((Foo f) => f.Id > 0);			
}
", 
			onElement:(_,syntaxNode) => {
				if (syntaxNode is ClassDeclarationSyntax mds) {
					var neededAttrs = mds.AttributeLists
						.SelectMany(als => als.Attributes.Where(
							atrSntx => generateQueryAttributeNames.Contains(atrSntx.Name.GetNameAsText())))
						.ToList(); 
			            
					if (!neededAttrs.Any()) {
						return;
					}
			            
					found++;
				} 
			},
			generateCode: _ => new SimpleNamedFile(
				"queries.cs", 
				@"namespace SomeNamespace { public static class SomeClass {} }").AsSingletonSet(),
			postCompilationAssertions:asm => {
				Assert.Equal(2, found);
			        
				var t = asm.GetTypeOrFail("SomeNamespace.SomeClass");
			        
				return Task.CompletedTask;
			});
	}
        
	[Theory]
	[InlineData(@"
[GenerateQueries]
class Second {
	public void GetFooWithPositiveId() => Query.Register((Foo f) => false);		
}
")]
	[InlineData(@"
[GenerateQueries]
class Second {
	public void GetFooWithPositiveId() { Query.Register((Foo f) => false);	}		
}
")]
	public async Task ParsesAndGeneratesWorkingCode_FindsQueriesDefinedAsLambdaAndStatement(string csCode) {
		using var sut = await SystemUnderTestFactory.Create(DbToTest.Sqlite);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
		var _ = await sut.TestingSchema.CreateData(sut.DbConn.DbConn);
	        
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(ParsesAndGeneratesWorkingCode_FindsQueriesDefinedAsLambdaAndStatement),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			csCode, 
			onElement:generator.OnElement,
			generateCode:generator.GenerateFiles,
			postCompilationAssertions:async asm => {
				var fooTypeName = "SogePoco.Pocos.Foo";
				var fooT = asm.GetTypeOrFail(fooTypeName);
		            
				var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
					Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
		            
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetFooWithPositiveId", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();
					
				AssertUtil.AssertSameEntitiesColl(Logger, "Id", Array.Empty<IDictionary<string,object?>>(), actualFoos);
			});
	}

	//representative sample from:
	//   SogePoco.Common.Consts.ReservedLiteralsInQueryGenerator
	//   SogePoco.Common.Consts.ReservedNamesInQueryGenerator
	public static IEnumerable<object[]> RepresentativeNamesToBeSanitized => 
		new []{"iCol","x","itm0"}.Select(x => new object[]{x}).ToList();

	[Theory]
	[MemberData(nameof(RepresentativeNamesToBeSanitized))]
	public async Task QuotesParameterNamesCollidingWithInternalVariableNames(string varName) {
		var csCode = $@"
[GenerateQueries]
class Second {{
	public void GetFooWithPositiveId(int {varName}) => Query.Register((Foo self) => self.NullableInt == {varName});		
}}";
		
		using var sut = await SystemUnderTestFactory.Create(DbToTest.Sqlite);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
		var _ = await sut.TestingSchema.CreateData(sut.DbConn.DbConn);
	        
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(ParsesAndGeneratesWorkingCode_FindsQueriesDefinedAsLambdaAndStatement),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			csCode, 
			onElement:generator.OnElement,
			generateCode:generator.GenerateFiles,
			postCompilationAssertions:async asm => {
				var fooTypeName = "SogePoco.Pocos.Foo";
				var fooT = asm.GetTypeOrFail(fooTypeName);
		            
				var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
					Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
		            
				var dbExtensions = dbInstance.BuildExtensionsHelper();

				var foo1 = fooT.CreateInstanceOrFail();
				((dynamic)foo1).NullableInt = 42;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableInt = 100;
				await dbInstance.Insert(foo2);
			    
				var expectedFoos = new[] {foo1}.ToPropertyNameAndValueDict();
			            
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQueryWithArgs("GetFooWithPositiveId", fooT, new object?[]{42}))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();
					
				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
}