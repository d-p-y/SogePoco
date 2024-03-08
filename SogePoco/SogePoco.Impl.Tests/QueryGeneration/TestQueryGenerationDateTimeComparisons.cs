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

public class TestQueryGenerationDateTimeComparisons : BaseTest {
	public TestQueryGenerationDateTimeComparisons(ITestOutputHelper outputHelper) : base(outputHelper) {}
        
	public enum ExpectedToFindBack {
		NineteenEightyFour,
		TwentyTwenty,
		FstDecOf2K,
		FstDecOf2KAndNineteenEightyFour,
		FstDecOf2KAndTwentyTwenty,
		NotNull,
		Null
	}
		
	public static IEnumerable<object[]> AllValuesFor_NullableDateTimeFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "new DateTime(2000, 12, 1)", ExpectedToFindBack.FstDecOf2K},
			new object[] {x, "==", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.NineteenEightyFour},
			new object[] {x, "!=", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.FstDecOf2KAndTwentyTwenty},
			new object[] {x, "<",  "new DateTime(2020, 6, 12, 20, 21, 22)", ExpectedToFindBack.FstDecOf2KAndNineteenEightyFour},
			new object[] {x, "<=", "new DateTime(2020, 6, 12, 20, 21, 22)", ExpectedToFindBack.NotNull},
			new object[] {x, ">",  "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.FstDecOf2KAndTwentyTwenty},
			new object[] {x, ">=", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.NotNull},
			new object[] {x, "==", "null", ExpectedToFindBack.Null},
			new object[] {x, "!=", "null", ExpectedToFindBack.NotNull} });
        

	[Theory]
	[MemberData(nameof(AllValuesFor_NullableDateTimeFieldComparisonToLiteral))]
	public async Task NullableDateTimeFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NullableDateTimeFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NullableDateTime {csOper} {csCompVal});		
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
				((dynamic)foo1).NullableDateTime = new DateTime(1984, 1, 2, 13,   5,  5);
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableDateTime = new DateTime(2020, 6, 12, 20, 21, 22);
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableDateTime = new DateTime(2000, 12, 1);
				await dbInstance.Insert(foo3);
			        
				var foo4 = fooT.CreateInstanceOrFail();
				((dynamic)foo4).NullableDateTime = null;
				await dbInstance.Insert(foo4);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.NineteenEightyFour => new[] {foo1},
						ExpectedToFindBack.TwentyTwenty => new[] {foo2},
						ExpectedToFindBack.FstDecOf2K => new[] {foo3},
						ExpectedToFindBack.FstDecOf2KAndNineteenEightyFour => new[] {foo1, foo3},
						ExpectedToFindBack.FstDecOf2KAndTwentyTwenty => new[] {foo3, foo2},
						ExpectedToFindBack.NotNull => new[] {foo1, foo3, foo2},
						ExpectedToFindBack.Null => new[] {foo4},
						_ => throw new ArgumentOutOfRangeException(nameof(expToFind), expToFind, null)
					}).ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
		
        
	public static IEnumerable<object[]> AllValuesFor_NotNullableDateTimeFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "new DateTime(2000, 12, 1)", ExpectedToFindBack.FstDecOf2K},
			new object[] {x, "==", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.NineteenEightyFour},
			new object[] {x, "!=", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.FstDecOf2KAndTwentyTwenty},
			new object[] {x, "<",  "new DateTime(2020, 6, 12, 20, 21, 22)", ExpectedToFindBack.FstDecOf2KAndNineteenEightyFour},
			new object[] {x, "<=", "new DateTime(2020, 6, 12, 20, 21, 22)", ExpectedToFindBack.NotNull},
			new object[] {x, ">",  "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.FstDecOf2KAndTwentyTwenty},
			new object[] {x, ">=", "new DateTime(1984, 1, 2, 13,   5,  5)", ExpectedToFindBack.NotNull} });
        
        
	[Theory]
	[MemberData(nameof(AllValuesFor_NotNullableDateTimeFieldComparisonToLiteral))]
	public async Task NotNullableDateTimeFieldComparisonToLiteral( 
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NotNullableDateTimeFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NotNullableDateTime {csOper} {csCompVal});		
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
				((dynamic)foo1).NotNullableDateTime = new DateTime(1984, 1, 2, 13,   5,  5);
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NotNullableDateTime = new DateTime(2020, 6, 12, 20, 21, 22);
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NotNullableDateTime = new DateTime(2000, 12, 1);
				await dbInstance.Insert(foo3);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.NineteenEightyFour => new[] {foo1},
						ExpectedToFindBack.TwentyTwenty => new[] {foo2},
						ExpectedToFindBack.FstDecOf2K => new[] {foo3},
						ExpectedToFindBack.FstDecOf2KAndNineteenEightyFour => new[] {foo1, foo3},
						ExpectedToFindBack.FstDecOf2KAndTwentyTwenty => new[] {foo3, foo2},
						ExpectedToFindBack.NotNull => new[] {foo1, foo3, foo2},
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