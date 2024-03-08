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

public class TestQueryGenerationQueryParameters : BaseTest {
	public TestQueryGenerationQueryParameters(ITestOutputHelper outputHelper) : base(outputHelper) { }

	public enum Exp {
		One,
		Two,
		Three,
		Null
	}

	public enum Syntax {
		CmpNull,
		CmpNotNull,
		Contains
	}
		
	public static IEnumerable<object?[]> AllValuesFor_TestBool 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object?[] {x, Syntax.CmpNotNull, "bool", null,      "NotNullableBool", new object?[] {true}, new []{Exp.One, Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "bool?", null,         "NullableBool", new object?[] {null}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "bool?", null,         "NullableBool", new object?[] {true}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNull, "bool", null,          "NullableBool", new object?[] {true}, new []{Exp.Two}},
	             
			new object?[] {x, Syntax.CmpNotNull, "bool", " = true", "NotNullableBool", new object?[] {Type.Missing}, new []{Exp.One, Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "bool", " = false","NotNullableBool", new object?[] {true}, new []{Exp.One, Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNotNull, "bool", " = true",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNotNull, "bool", " = true",    "NullableBool", new object?[] {false}, new []{Exp.One, Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "bool?", " = true",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNull, "bool?", " = null",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Null}},
	             
			new object?[] {x, Syntax.CmpNull, "System.Nullable<bool>", " = null",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<bool>", " = null",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<Boolean>", " = null",    "NullableBool", new object?[] {Type.Missing}, new []{Exp.Null}},
		});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestBool))]
	public Task TestBool(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) =>
		FieldComparisonToGeneratedFuncParameter(nameof(TestBool), dbTest, nl, pTyp, pDef, fld, genCodePrms, expToFind);

		
		
		
	public static IEnumerable<object?[]> AllValuesFor_TestInt 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object?[] {x, Syntax.CmpNotNull, "int", null,    "NotNullableInt",      new object?[] {10}, new []{Exp.One}},
			new object?[] {x, Syntax.CmpNotNull, "int", null,    "NotNullableInt",      new object?[] {30}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "int?", null,       "NullableInt",      new object?[] {null}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "int?", null,       "NullableInt",      new object?[] {3}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "int", null,        "NullableInt",      new object?[] {3}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNotNull, "int", " = 30", "NotNullableInt",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "int", " = 30", "NotNullableInt",      new object?[] {20}, new []{Exp.Two}},
	             
			new object?[] {x, Syntax.CmpNotNull, "int", " = 3",     "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "int", " = 3",     "NullableInt",      new object?[] {2}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNull, "int?", " = 3",     "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "int?", " = null",  "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Null}},
	             
			new object?[] {x, Syntax.CmpNull, "System.Nullable<int>", " = null",  "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<int>", " = null",  "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<Int32>", " = null",  "NullableInt",      new object?[] {Type.Missing}, new []{Exp.Null}},
		});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestInt))]
	public Task TestInt(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) =>
		FieldComparisonToGeneratedFuncParameter(nameof(TestInt), dbTest, nl, pTyp, pDef, fld, genCodePrms, expToFind);
		
		
		
		
	public static IEnumerable<object?[]> AllValuesFor_TestDecimal 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object?[] {x, Syntax.CmpNotNull, "decimal", null,    "NotNullableDecimal",      new object[] {10.1m}, new []{Exp.One}},
			new object?[] {x, Syntax.CmpNotNull, "decimal", null,    "NotNullableDecimal",      new object[] {30.3m}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "decimal?", null,       "NullableDecimal",      new object?[] {null}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "decimal?", null,       "NullableDecimal",      new object[] {3.3m}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "decimal", null,        "NullableDecimal",      new object[] {3.3m}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNotNull, "decimal", " = 30.3m", "NotNullableDecimal",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "decimal", " = 30.3m", "NotNullableDecimal",      new object?[] {20.2m}, new []{Exp.Two}},
	             
			new object?[] {x, Syntax.CmpNotNull, "decimal", " = 3.3m",     "NullableDecimal",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "decimal", " = 3.3m",     "NullableDecimal",      new object?[] {2.2m}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNull, "decimal?", " = 3.3m",  "NullableDecimal",      new object[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "decimal?", " = null",  "NullableDecimal",      new object[] {Type.Missing}, new []{Exp.Null}},
	             
			new object?[] {x, Syntax.CmpNull, "System.Nullable<decimal>", " = null",  "NullableDecimal",      new object[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<decimal>", " = null",  "NullableDecimal",      new object[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<Decimal>", " = null",  "NullableDecimal",      new object[] {Type.Missing}, new []{Exp.Null}},
		});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestDecimal))]
	public Task TestDecimal(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) =>
		FieldComparisonToGeneratedFuncParameter(nameof(TestDecimal), dbTest, nl, pTyp, pDef, fld, genCodePrms, expToFind);
		
		
		
		
	public static IEnumerable<object?[]> AllValuesFor_TestString 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object?[] {x, Syntax.CmpNotNull, "string", null,            "NotNullableText",      new object?[] {"nn1st"}, new []{Exp.One}},
			new object?[] {x, Syntax.CmpNotNull, "string", null,            "NotNullableText",      new object?[] {"nn3rd"}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "string?", null,            "NullableText",      new object?[] {null}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "string?", null,            "NullableText",      new object?[] {"n3rd"}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "string", null,             "NullableText",      new object?[] {"n3rd"}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "String", null,             "NullableText",      new object?[] {"n3rd"}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "System.String", null,             "NullableText",      new object?[] {"n3rd"}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNotNull, "string", " = @\"nn3rd\"", "NotNullableText",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "string", " = \"nn3rd\"",  "NotNullableText",      new object?[] {"nn2nd"}, new []{Exp.Two}},
	             
			new object?[] {x, Syntax.CmpNotNull, "string", " = \"n3rd\"",      "NullableText",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNotNull, "string", " = \"n3rd\"",      "NullableText",      new object?[] {"n2nd"}, new []{Exp.Two}},
			new object?[] {x, Syntax.CmpNull, "string?", " = \"n3rd\"",   "NullableText",      new object?[] {Type.Missing}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "string?", " = null",       "NullableText",      new object?[] {Type.Missing}, new []{Exp.Null}},
		});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestString))]
	public Task TestString(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) =>
		FieldComparisonToGeneratedFuncParameter(nameof(TestString), dbTest, nl, pTyp, pDef, fld, genCodePrms, expToFind);
		
		
		
		
	public static IEnumerable<object?[]> AllValuesFor_TestDateTime 
		=> DbToTestUtil.GetAllToBeTested().SelectMany(x => new [] {
			new object?[] {x, Syntax.CmpNotNull, "DateTime", null,            "NotNullableDateTime",      new object?[] {new DateTime(2001, 2, 3, 4, 5, 7)}, new []{Exp.One}},
			new object?[] {x, Syntax.CmpNotNull, "System.DateTime", null,     "NotNullableDateTime",      new object?[] {new DateTime(2003, 2, 3, 4, 5, 7)}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "DateTime?", null,            "NullableDateTime",      new object?[] {null}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "DateTime?", null,            "NullableDateTime",      new object?[] {new DateTime(2003, 2, 3, 4, 5, 6)}, new []{Exp.Three}},
			new object?[] {x, Syntax.CmpNull, "DateTime", null,             "NullableDateTime",      new object?[] {new DateTime(2003, 2, 3, 4, 5, 6)}, new []{Exp.Three}},
	             
			new object?[] {x, Syntax.CmpNull, "DateTime?", " = null",       "NullableDateTime",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<DateTime>", " = null",       "NullableDateTime",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "Nullable<System.DateTime>", " = null",       "NullableDateTime",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "System.Nullable<DateTime>", " = null",       "NullableDateTime",      new object?[] {Type.Missing}, new []{Exp.Null}},
			new object?[] {x, Syntax.CmpNull, "System.Nullable<System.DateTime>", " = null",       "NullableDateTime",      new object?[] {Type.Missing}, new []{Exp.Null}}
		});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestDateTime))]
	public Task TestDateTime(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) =>
		FieldComparisonToGeneratedFuncParameter(nameof(TestDateTime), dbTest, nl, pTyp, pDef, fld, genCodePrms, expToFind);


		
		
	public static IEnumerable<object?[]> AllValuesFor_TestArrayOfInt 
		=> DbToTestUtil.GetAllToBeTested()
			.SelectMany(x => new [] {
				new object?[] {x, Syntax.Contains, "int[]", null,            "NotNullableInt",      new object?[] {new int[]{}}, new Exp[]{}},
				new object?[] {x, Syntax.Contains, "int[]", null,            "NotNullableInt",      new object?[] {new int[]{20}}, new []{Exp.Two}},
				new object?[] {x, Syntax.Contains, "int[]", null,            "NotNullableInt",      new object?[] {new int[]{10,20}}, new []{Exp.One, Exp.Two}},
					
				new object?[] {x, Syntax.Contains, "int?[]", null,            "NullableInt",      new object?[] {new int?[]{}}, new Exp[]{}},
				new object?[] {x, Syntax.Contains, "int?[]", null,            "NullableInt",      new object?[] {new int?[]{2}}, new []{Exp.Two}},
				new object?[] {x, Syntax.Contains, "int?[]", null,            "NullableInt",      new object?[] {new int?[]{1,2}}, new []{Exp.One, Exp.Two}}
			});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestArrayOfInt))]
	public async Task TestArrayOfInt(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) {

		if (dbTest == DbToTest.Sqlite) {
			return; //skip test because of inability to have empty member data (possible when skipping via environment)
		}
			
		await FieldComparisonToGeneratedFuncParameter(nameof(TestArrayOfInt), dbTest, nl, pTyp, pDef, fld,
			genCodePrms, expToFind);
	}


	public static IEnumerable<object?[]> AllValuesFor_TestArrayOfString 
		=> DbToTestUtil.GetAllToBeTested()
			.SelectMany(x => new [] {
				new object?[] {x, Syntax.Contains, "string[]", null,            "NotNullableText",      new object?[] {new string?[]{}}, new Exp[]{}},
				new object?[] {x, Syntax.Contains, "string[]", null,            "NotNullableText",      new object?[] {new string?[]{"nn1st"}}, new []{Exp.One}},
				new object?[] {x, Syntax.Contains, "string[]", null,            "NotNullableText",      new object?[] {new string?[]{"nn1st","nn2nd"}}, new []{Exp.One, Exp.Two}},
					
				new object?[] {x, Syntax.Contains, "string?[]", null,            "NullableText",      new object?[] {new string?[]{}}, new Exp[]{}},
				new object?[] {x, Syntax.Contains, "string?[]", null,            "NullableText",      new object?[] {new string?[]{"n1st"}}, new []{Exp.One}},
				new object?[] {x, Syntax.Contains, "string?[]", null,            "NullableText",      new object?[] {new string?[]{"n1st","n2nd"}}, new []{Exp.One, Exp.Two}}
			});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_TestArrayOfString))]
	public async Task TestArrayOfString(
		DbToTest dbTest, Syntax nl, string pTyp, string? pDef, string fld, object?[] genCodePrms, Exp[] expToFind) {

		if (dbTest == DbToTest.Sqlite) {
			//skip test because of inability to have empty member data (possible when skipping via environment)
			return; 
		}
			
		await FieldComparisonToGeneratedFuncParameter(nameof(TestArrayOfString), dbTest, nl, pTyp, pDef, fld,
			genCodePrms, expToFind);
	}


	//TODO TestArrayOfBool bool,bool?
	//TODO TestArrayOfDecimal decimal,decimal?
	//TODO TestArrayOfString
	//TODO TestArrayOfDateTime
		
	private async Task FieldComparisonToGeneratedFuncParameter(
		string testName,
		DbToTest dbTest, Syntax nullability, string csPrmType, string? csPrmDefVal, string csFieldName, 
		object?[] generatedCodeParams, Exp[] expToFind) {
			
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		var csCode = nullability switch {
			Syntax.CmpNotNull => $@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo({csPrmType} p{csPrmDefVal ?? ""}) =>
		Query.Register((Foo f) => f.{csFieldName} == p);
	}}
",
			Syntax.CmpNull => $@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo({csPrmType} p{csPrmDefVal}) =>
		Query.Register((Foo f) => p==null && f.{csFieldName} == null || p!=null && f.{csFieldName} == p);
}}
",
			Syntax.Contains => $@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo({csPrmType} p{csPrmDefVal ?? ""}) =>
		Query.Register((Foo f) => p.Contains(f.{csFieldName}));
}}
",
			_ => throw new ArgumentOutOfRangeException(nameof(nullability), nullability, null)
		};
		        
		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			testName,
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
		            
				using var cleanup = new OnFinallyAction(
					() => GeneratedCodeResult.ForwardLastSqlOfDatabaseClassInstanceIntoLogger(dbInstance.WrappedInstance, Logger));
				cleanup.InvokeActionInFinallyEnabled = true;
					
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var foo1 = fooT.CreateInstanceOrFail();
				((dynamic)foo1).NullableBool = false;
				((dynamic)foo1).NotNullableBool = true;
			        
				((dynamic)foo1).NullableInt = 1;
				((dynamic)foo1).NotNullableInt = 10;
			        
				((dynamic)foo1).NullableDecimal = 1.1m;
				((dynamic)foo1).NotNullableDecimal = 10.1m;
			        
				((dynamic)foo1).NullableText = "n1st";
				((dynamic)foo1).NotNullableText = "nn1st";
			        
				((dynamic)foo1).NullableDateTime = new DateTime(2001, 2, 3, 4, 5, 6);
				((dynamic)foo1).NotNullableDateTime = new DateTime(2001, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableBool = true;
				((dynamic)foo2).NotNullableBool = false;
			        
				((dynamic)foo2).NullableInt = 2;
				((dynamic)foo2).NotNullableInt = 20;
			        
				((dynamic)foo2).NullableDecimal = 2.2m;
				((dynamic)foo2).NotNullableDecimal = 20.2m;
			        
				((dynamic)foo2).NullableText = "n2nd";
				((dynamic)foo2).NotNullableText = "nn2nd";
			        
				((dynamic)foo2).NullableDateTime = new DateTime(2002, 2, 3, 4, 5, 6);
				((dynamic)foo2).NotNullableDateTime = new DateTime(2002, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableBool = false;
				((dynamic)foo3).NotNullableBool = true;
			        
				((dynamic)foo3).NullableInt = 3;
				((dynamic)foo3).NotNullableInt = 30;
			        
				((dynamic)foo3).NullableDecimal = 3.3m;
				((dynamic)foo3).NotNullableDecimal = 30.3m;
			        
				((dynamic)foo3).NullableText = "n3rd";
				((dynamic)foo3).NotNullableText = "nn3rd";
			        
				((dynamic)foo3).NullableDateTime = new DateTime(2003, 2, 3, 4, 5, 6);
				((dynamic)foo3).NotNullableDateTime = new DateTime(2003, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo3);
			        
				var foo4 = fooT.CreateInstanceOrFail();
				await dbInstance.Insert(foo4);
			        
				var expectedFoos = 
					expToFind
						.Select(x => x switch {
							Exp.One => foo1,
							Exp.Two => foo2,
							Exp.Three => foo3,
							Exp.Null => foo4,
							_ => throw new ArgumentOutOfRangeException(nameof(expToFind), expToFind, null)})
						.ToArray()
						.ToPropertyNameAndValueDict();
			        
				var actualFoos = 
					(await dbExtensions.ExecuteGeneratedQueryWithArgs("GetMatchingFoo", fooT, generatedCodeParams))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedFoos, actualFoos);
			});
	}
	
	
	
	public static IEnumerable<object?[]> AllValuesFor_DbToTestAndExpectedCount 
		=> DbToTestUtil.GetAllToBeTested()
			.SelectMany(x => new [] {
				new object?[] {x, 1},
				new object?[] {x, 2}
			});
		
	[Theory]
	[MemberData(nameof(AllValuesFor_DbToTestAndExpectedCount))]
	public async Task ParametersAreRegisteredOnceDespiteMultipleUsage(DbToTest dbTest, int expectedCount) {
		using var sut = await SystemUnderTestFactory.Create(dbTest);
	        
		await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
            
		var opt = new GeneratorOptions();
		var generator = new DefaultQueryGenerator(
			sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

		var csCode = 
			expectedCount switch {
				1 => $@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo(int p, int? q) =>
		Query.Register((Foo f) => f.Id < p || f.Id > p || p != f.NullableInt);
	}}
",
				2 => $@"
[GenerateQueries]
class Second {{
	public void GetMatchingFoo(int p, int? q) =>
		Query.Register((Foo f) => f.Id < p || f.Id > p || p != f.NullableInt || (q == null || q == p));
	}}
",
				_ => throw new Exception("unsupported expected value")
			}
			;
		        
		await QueryGeneratorTestUtil.GenerateCompileAndAssert(
			nameof(ParametersAreRegisteredOnceDespiteMultipleUsage),
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
		            
				using var cleanup = new OnFinallyAction(
					() => GeneratedCodeResult.ForwardLastSqlOfDatabaseClassInstanceIntoLogger(dbInstance.WrappedInstance, Logger));
				cleanup.InvokeActionInFinallyEnabled = true;
					
				var dbExtensions = dbInstance.BuildExtensionsHelper();
		            
				var foo1 = fooT.CreateInstanceOrFail();
				((dynamic)foo1).NullableBool = false;
				((dynamic)foo1).NotNullableBool = true;
			        
				((dynamic)foo1).NullableInt = 1;
				((dynamic)foo1).NotNullableInt = 10;
			        
				((dynamic)foo1).NullableDecimal = 1.1m;
				((dynamic)foo1).NotNullableDecimal = 10.1m;
			        
				((dynamic)foo1).NullableText = "n1st";
				((dynamic)foo1).NotNullableText = "nn1st";
			        
				((dynamic)foo1).NullableDateTime = new DateTime(2001, 2, 3, 4, 5, 6);
				((dynamic)foo1).NotNullableDateTime = new DateTime(2001, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo1);
			        
				var foo2 = fooT.CreateInstanceOrFail();
				((dynamic)foo2).NullableBool = true;
				((dynamic)foo2).NotNullableBool = false;
			        
				((dynamic)foo2).NullableInt = 2;
				((dynamic)foo2).NotNullableInt = 20;
			        
				((dynamic)foo2).NullableDecimal = 2.2m;
				((dynamic)foo2).NotNullableDecimal = 20.2m;
			        
				((dynamic)foo2).NullableText = "n2nd";
				((dynamic)foo2).NotNullableText = "nn2nd";
			        
				((dynamic)foo2).NullableDateTime = new DateTime(2002, 2, 3, 4, 5, 6);
				((dynamic)foo2).NotNullableDateTime = new DateTime(2002, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo2);
			        
				var foo3 = fooT.CreateInstanceOrFail();
				((dynamic)foo3).NullableBool = false;
				((dynamic)foo3).NotNullableBool = true;
			        
				((dynamic)foo3).NullableInt = 3;
				((dynamic)foo3).NotNullableInt = 30;
			        
				((dynamic)foo3).NullableDecimal = 3.3m;
				((dynamic)foo3).NotNullableDecimal = 30.3m;
			        
				((dynamic)foo3).NullableText = "n3rd";
				((dynamic)foo3).NotNullableText = "nn3rd";
			        
				((dynamic)foo3).NullableDateTime = new DateTime(2003, 2, 3, 4, 5, 6);
				((dynamic)foo3).NotNullableDateTime = new DateTime(2003, 2, 3, 4, 5, 7);
				await dbInstance.Insert(foo3);
			        
				var foo4 = fooT.CreateInstanceOrFail();
				await dbInstance.Insert(foo4);
				
				(await dbExtensions.ExecuteGeneratedQueryWithArgs("GetMatchingFoo", fooT, new object?[] {1,2}))
					.OrderBy(x => ((dynamic)x!).Id )
					.ToPropertyNameAndValueDict();

				Assert.Equal(expectedCount, ((dynamic)dbInstance.WrappedInstance).LastSqlParams.Length);
			});
	}
}
