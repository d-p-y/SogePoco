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

public class TestQueryGenerationBoolComparisons : BaseTest {
	public TestQueryGenerationBoolComparisons(ITestOutputHelper outputHelper) : base(outputHelper) {}

	public enum ExpectedToFindBack {
		TrueOne,
		FalseOne,
		NullOne,
		AllNotNull
	}
		
		
	public static IEnumerable<object[]> AllValuesFor_NotNullableBoolFieldComparisonsToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "true", ExpectedToFindBack.TrueOne},
			new object[] {x, "==", "false", ExpectedToFindBack.FalseOne},
			new object[] {x, "!=", "true", ExpectedToFindBack.FalseOne},
			new object[] {x, "!=", "false", ExpectedToFindBack.TrueOne} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NotNullableBoolFieldComparisonsToLiteral))]
	public async Task NotNullableBoolFieldComparisonsToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NotNullableBoolFieldComparisonsToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NotNullableBool {csOper} {csCompVal});		
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
				((dynamic)foo1).NotNullableBool = true;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NotNullableBool = false;
				await dbInstance.Insert(foo2);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.TrueOne => new[] {foo1},
						ExpectedToFindBack.FalseOne => new[] {foo2},
						_ => throw new ArgumentOutOfRangeException(nameof(expToFind), expToFind, null)
					}).ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
		
		
	public static IEnumerable<object[]> AllValuesFor_NullableBoolFieldComparisonsToLiteral 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "==", "true", ExpectedToFindBack.TrueOne},
			new object[] {x, "==", "false", ExpectedToFindBack.FalseOne},
			new object[] {x, "!=", "true", ExpectedToFindBack.FalseOne},
			new object[] {x, "!=", "false", ExpectedToFindBack.TrueOne},
			new object[] {x, "==", "null", ExpectedToFindBack.NullOne},
			new object[] {x, "!=", "null", ExpectedToFindBack.AllNotNull} });

	[Theory]
	[MemberData(nameof(AllValuesFor_NullableBoolFieldComparisonsToLiteral))]
	public async Task NullableBoolFieldComparisonsToLiteral(
		DbToTest dbTest, string csOper, string csCompVal, ExpectedToFindBack expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(NullableBoolFieldComparisonsToLiteral),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"	
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => f.NullableBool {csOper} {csCompVal});		
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
				((dynamic)foo1).NullableBool = true;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableBool = false;
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableBool = null;
				await dbInstance.Insert(foo3);
			        
				var expectedFoos = 
					(expToFind switch {
						ExpectedToFindBack.TrueOne => new[] {foo1},
						ExpectedToFindBack.FalseOne => new[] {foo2},
						ExpectedToFindBack.NullOne => new[] {foo3},
						ExpectedToFindBack.AllNotNull => new[] {foo1, foo2},
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