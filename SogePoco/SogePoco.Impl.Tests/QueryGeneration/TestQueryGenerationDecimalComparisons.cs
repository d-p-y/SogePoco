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

public class TestQueryGenerationDecimalComparisons : BaseTest {
	public TestQueryGenerationDecimalComparisons(ITestOutputHelper outputHelper) : base(outputHelper) {}
        
	public enum ExpectedToFindBack {
		OneAndHalf,
		TwoPointNine,
		NotNull,
		Null
	}

	public static IEnumerable<object[]> AllValuesFor_NullableDecimalFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "1.5m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "!=", "2.9m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "<", "2.9m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "<=", "2.9m", ExpectedToFindBack.NotNull},
			new object[] {x, ">", "1.5m", ExpectedToFindBack.TwoPointNine},
			new object[] {x, ">=", "1.5m", ExpectedToFindBack.NotNull},
			new object[] {x, "==", "null", ExpectedToFindBack.Null},
			new object[] {x, "!=", "null", ExpectedToFindBack.NotNull} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NullableDecimalFieldComparisonToLiteral))]
	public async Task NullableDecimalFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NullableDecimalFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NullableDecimal {csOper} {csCompVal});		
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
				((dynamic)foo1).NullableDecimal = 1.5m;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableDecimal = 2.9m;
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableDecimal = null;
				await dbInstance.Insert(foo3);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.OneAndHalf => new[] {foo1},
						ExpectedToFindBack.TwoPointNine => new[] {foo2},
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
		
        
	public static IEnumerable<object[]> AllValuesFor_NotNullableDecimalFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "1.5m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "!=", "2.9m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "<", "2.9m", ExpectedToFindBack.OneAndHalf},
			new object[] {x, "<=", "2.9m", ExpectedToFindBack.NotNull},
			new object[] {x, ">", "1.5m", ExpectedToFindBack.TwoPointNine},
			new object[] {x, ">=", "1.5m", ExpectedToFindBack.NotNull} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NotNullableDecimalFieldComparisonToLiteral))]
	public async Task NotNullableDecimalFieldComparisonToLiteral( 
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NotNullableDecimalFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{		
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NotNullableDecimal {csOper} {csCompVal});		
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
				((dynamic)foo1).NotNullableDecimal = 1.5m;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NotNullableDecimal = 2.9m;
				await dbInstance.Insert(foo2);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.OneAndHalf => new[] {foo1},
						ExpectedToFindBack.TwoPointNine => new[] {foo2},
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