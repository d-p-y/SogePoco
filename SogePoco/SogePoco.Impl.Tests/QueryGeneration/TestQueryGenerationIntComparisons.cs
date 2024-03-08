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

public class TestQueryGenerationIntComparisons : BaseTest {
	public TestQueryGenerationIntComparisons(ITestOutputHelper outputHelper) : base(outputHelper) {}

	public enum ExpectedToFindBack {
		FortyTwo,
		OneHundred,
		NotNull,
		Null
	}

	public static IEnumerable<object[]> AllValuesFor_NullableIntFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "42", ExpectedToFindBack.FortyTwo},
			new object[] {x, "!=", "42", ExpectedToFindBack.OneHundred},
			new object[] {x, "==", "100", ExpectedToFindBack.OneHundred},
			new object[] {x, "<", "100", ExpectedToFindBack.FortyTwo},
			new object[] {x, "<=", "100", ExpectedToFindBack.NotNull},
			new object[] {x, ">", "42", ExpectedToFindBack.OneHundred},
			new object[] {x, ">=", "42", ExpectedToFindBack.NotNull},
			new object[] {x, "==", "null", ExpectedToFindBack.Null},
			new object[] {x, "!=", "null", ExpectedToFindBack.NotNull}
		});

	[Theory]
	[MemberData(nameof(AllValuesFor_NullableIntFieldComparisonToLiteral))]
	public async Task NullableIntFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NullableIntFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{	
	public void GetMatchingFoo() =>		Query.Register((Foo f) => f.NullableInt {csOper} {csCompVal});		
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
				((dynamic)foo1).NullableInt = 42;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableInt = 100;
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableInt = null;
				await dbInstance.Insert(foo3);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.FortyTwo => new[] {foo1},
						ExpectedToFindBack.OneHundred => new[] {foo2},
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
		
		
		
	public static IEnumerable<object[]> AllValuesFor_NotNullableIntFieldComparisonToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "42", ExpectedToFindBack.FortyTwo},
			new object[] {x, "!=", "42", ExpectedToFindBack.OneHundred},
			new object[] {x, "==", "100", ExpectedToFindBack.OneHundred},
			new object[] {x, "<", "100", ExpectedToFindBack.FortyTwo},
			new object[] {x, "<=", "100", ExpectedToFindBack.NotNull},
			new object[] {x, ">", "42", ExpectedToFindBack.OneHundred},
			new object[] {x, ">=", "42", ExpectedToFindBack.NotNull} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NotNullableIntFieldComparisonToLiteral))]
	public async Task NotNullableIntFieldComparisonToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NotNullableIntFieldComparisonToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NotNullableInt {csOper} {csCompVal});		
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
				((dynamic)foo1).NotNullableInt = 42;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NotNullableInt = 100;
				await dbInstance.Insert(foo2);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.FortyTwo => new[] {foo1},
						ExpectedToFindBack.OneHundred => new[] {foo2},
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