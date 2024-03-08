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

public class TestQueryGenerationLogicalClauses : BaseTest {
	public TestQueryGenerationLogicalClauses(ITestOutputHelper outputHelper) : base(outputHelper) {}

	public enum ExpectedToFindBack {
		One,
		Two,
		Three,
		Four,
		Five
	}
        
	public static IEnumerable<object[]> AllValuesFor_Comparisons 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object[] {x, "f.NullableInt == 1 || f.NullableInt == 2", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two}},
			new object[] {x, "(f.NullableInt == 1 || f.NullableInt == 2)", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two}},
			new object[] {x, "f.NullableInt == 1 || f.NullableInt == 2 || f.NullableInt == 3", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two, ExpectedToFindBack.Three}},
			new object[] {x, "f.NullableInt == 1 || (f.NullableInt == 2 || f.NullableInt == 3)", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two, ExpectedToFindBack.Three}},
			new object[] {x, "(f.NullableInt == 1 || (f.NullableInt == 2 || f.NullableInt == 3))", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two, ExpectedToFindBack.Three}},
			new object[] {x, "!(f.NullableInt == 1 || !(f.NullableInt == 2 || f.NullableInt == 3))", new [] {ExpectedToFindBack.Two, ExpectedToFindBack.Three}},
			new object[] {x, "f.NullableInt <= 1 && f.NullableInt <= 2 || f.NullableInt >= 3 && f.NullableInt >= 4", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Four, ExpectedToFindBack.Five}},
			new object[] {x, "f.NullableInt <= 1 && (f.NullableInt <= 2 || f.NullableInt >= 3) && f.NullableInt >= 4", new object[0] },
			new object[] {x, "f.NullableInt > 1 && f.NullableInt <= 2", new [] {ExpectedToFindBack.Two}},
			new object[] {x, "f.NullableInt > 1 && f.NullableInt <= 3 && f.NullableInt == 2", new [] {ExpectedToFindBack.Two}},
			new object[] {x, "f.NullableInt > 1 || f.NullableInt >= 2 && f.NullableInt < 3", new [] {ExpectedToFindBack.Two, ExpectedToFindBack.Three, ExpectedToFindBack.Four, ExpectedToFindBack.Five}},
			new object[] {x, "f.NullableInt > 1 || (f.NullableInt >= 2 && f.NullableInt < 3)", new [] {ExpectedToFindBack.Two, ExpectedToFindBack.Three, ExpectedToFindBack.Four, ExpectedToFindBack.Five}},
			new object[] {x, "(f.NullableInt > 1 || f.NullableInt >= 2) && f.NullableInt < 3", new [] {ExpectedToFindBack.Two}},
			new object[] {x, "((f.NullableInt > 1 || f.NullableInt >= 2) && f.NullableInt < 3)", new [] {ExpectedToFindBack.Two}},
			new object[] {x, "(f.NullableInt == 1 || f.NullableInt == 2) && f.NullableInt <= 3", new [] {ExpectedToFindBack.One, ExpectedToFindBack.Two}},
			new object[] {x, "!(f.NullableInt == 1 || f.NullableInt == 2) && f.NullableInt <= 3", new [] {ExpectedToFindBack.Three}} });
		
	[Theory]
	[MemberData(nameof(AllValuesFor_Comparisons))]
	public async Task Comparisons(
		DbToTest dbTest, string csBody, ExpectedToFindBack[] expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(Comparisons),
			sut,
			opt,
			QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
			$@"	
[GenerateQueries]
class Second {{
	public void GetMatchingFoo() =>	Query.Register((Foo f) => {csBody});		
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
				((dynamic)foo1).NullableInt = 1;
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableInt = 2;
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableInt = 3;
				await dbInstance.Insert(foo3);
			        
				var foo4 = fooT.CreateInstanceOrFail();
				((dynamic)foo4).NullableInt = 4;
				await dbInstance.Insert(foo4);
			        
				var foo5 = fooT.CreateInstanceOrFail();
				((dynamic)foo5).NullableInt = 5;
				await dbInstance.Insert(foo5);
			        
				var expectedFoos = 
					expToFind.SelectMany(x => x switch {
						ExpectedToFindBack.One => new[] {foo1},
						ExpectedToFindBack.Two => new[] {foo2},
						ExpectedToFindBack.Three => new[] {foo3},
						ExpectedToFindBack.Four => new[] {foo4},
						ExpectedToFindBack.Five => new[] {foo5},
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