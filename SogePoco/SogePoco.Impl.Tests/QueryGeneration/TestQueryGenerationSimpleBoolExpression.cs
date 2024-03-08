using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.QueryGeneration; 

public class TestQueryGenerationSimpleBoolExpression : BaseTest {
	public TestQueryGenerationSimpleBoolExpression(ITestOutputHelper outputHelper) : base(outputHelper) {}
       
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task ParsesAndGeneratesWorkingCode_ParameterlessSingleTableFetchAll(DbToTest dbTest) {
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(ParsesAndGeneratesWorkingCode_ParameterlessSingleTableFetchAll),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			@"
using SogePoco.Common;

[GenerateQueries]
class Second {		
	public void GetFooWithPositiveId() =>Query.Register((Foo f) => true);		
}
", 
			onElement:generator.OnElement,
			generateCode:generator.GenerateFiles,
			postCompilationAssertions:async asm => {
				var fooTypeName = "SogePoco.Pocos.Foo";
				var fooT = asm.GetTypeOrFail(fooTypeName);
		            
				var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
					Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
		            
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var foo1 = fooT.CreateInstanceOrFail();
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				await dbInstance.Insert(foo2);
			        
				var expectedFoos = new[] {foo1, foo2}.ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetFooWithPositiveId", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
        
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task ParsesAndGeneratesWorkingCode_ParameterlessSingleTableFetchNone(DbToTest dbToTest) {
		using var sut = await SystemUnderTestFactory.Create(dbToTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
		var _ = await sut.TestingSchema.CreateData(sut.DbConn.DbConn);
	        
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(ParsesAndGeneratesWorkingCode_ParameterlessSingleTableFetchNone),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			@"
using SogePoco.Common;

namespace Irrelevant {
	[GenerateQueries]
	class Second {
		public void GetFooWithPositiveId() => Query.Register((Foo f) => false);	
	}
}", 
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
}