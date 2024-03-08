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

public class TestQueryGenerationStringComparisons : BaseTest {
	public TestQueryGenerationStringComparisons(ITestOutputHelper outputHelper) : base(outputHelper) {}
        
	public enum ExpectedToFindBack {
		Foo,
		Bar,
		NotNull,
		Null
	}
		
	public static IEnumerable<object[]> AllValuesFor_NullableStringFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "\"foo\"", ExpectedToFindBack.Foo},
			new object[] {x, "==", "\"bar\"", ExpectedToFindBack.Bar},
			new object[] {x, "!=", "\"bar\"", ExpectedToFindBack.Foo},
			new object[] {x, "==", "null", ExpectedToFindBack.Null},
			new object[] {x, "is", "null", ExpectedToFindBack.Null},
			new object[] {x, "!=", "null", ExpectedToFindBack.NotNull},
			new object[] {x, "is", "not null", ExpectedToFindBack.NotNull},
			new object[] {x, "is", "{}", ExpectedToFindBack.NotNull}});

	[Theory]
	[MemberData(nameof(AllValuesFor_NullableStringFieldComparisonToLiteral))]
	public async Task NullableStringFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NullableStringFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
namespace SogePoco.Common {{
	[GenerateQueries]
	class Second {{
		public void GetMatchingFoo() =>
			Query.Register((Foo f) => f.NullableText {csOper} {csCompVal});		
	}}
}}", 
			onElement:generator.OnElement,
			generateCode:generator.GenerateFiles,
			postCompilationAssertions:async asm => {
				var fooTypeName = "SogePoco.Pocos.Foo";
				var fooT = asm.GetTypeOrFail(fooTypeName);
		            
				var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
					Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
		            
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var foo1 = fooT.CreateInstanceOrFail();
				((dynamic)foo1).NullableText = "foo";
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableText = "bar";
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableText = null;
				await dbInstance.Insert(foo3);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.Foo => new[] {foo1},
						ExpectedToFindBack.Bar => new[] {foo2},
						ExpectedToFindBack.NotNull => new[] {foo1, foo2},
						ExpectedToFindBack.Null => new[] {foo3},
						_ => throw new ArgumentOutOfRangeException(nameof(expToFind), expToFind, null)
					}).ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
		
	public static IEnumerable<object[]> AllValuesFor_NotNullableStringFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "\"foo\"", ExpectedToFindBack.Foo},
			new object[] {x, "==", "\"bar\"", ExpectedToFindBack.Bar},
			new object[] {x, "!=", "\"bar\"", ExpectedToFindBack.Foo} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NotNullableStringFieldComparisonToLiteral))]
	public async Task NotNullableStringFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NotNullableStringFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
namespace SogePoco.Common {{
	[GenerateQueries]	
	class Second {{
		public void GetMatchingFoo() =>
			Query.Register((Foo f) => f.NotNullableText {csOper} {csCompVal});		
	}}
}}", 
			onElement:generator.OnElement,
			generateCode:generator.GenerateFiles,
			postCompilationAssertions:async asm => {
				var fooTypeName = "SogePoco.Pocos.Foo";
				var fooT = asm.GetTypeOrFail(fooTypeName);
		            
				var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
					Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
		            
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var foo1 = fooT.CreateInstanceOrFail();
				((dynamic)foo1).NotNullableText = "foo";
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NotNullableText = "bar";
				await dbInstance.Insert(foo2);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.Foo => new[] {foo1},
						ExpectedToFindBack.Bar => new[] {foo2},
						ExpectedToFindBack.NotNull => new[] {foo1, foo2},
						_ => throw new ArgumentOutOfRangeException(nameof(expToFind), expToFind, null)
					}).ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
}