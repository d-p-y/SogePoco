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

public class TestQueryGenerationJoins : BaseTest {
	public TestQueryGenerationJoins(ITestOutputHelper outputHelper) : base(outputHelper) {}
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenInvertedJoinThenInvertedJoinWhere(DbToTest dbToTest) {
		using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenInvertedJoinThenInvertedJoinWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        @"
[GenerateQueries]
class Second {
public void FetchData() =>	
	Query
		.From<Foo>()
		.Join((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Join((ReferencesTableWithCompositePk rtwcp) => rtwcp.ForeignKeys.Foo_by_FooId, (f,_) => f)
		.Where((f,cof,rtwcp) => cof.Id > 0 && rtwcp.Val == ""b"" && f.NullableInt == 2);
}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
				var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
				var refTblT = asm.GetTypeOrFail(refTblTypeName);

	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	            
	            var foo0 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo0).NullableInt = 0;
		        await dbInstance.Insert(foo0);
		        
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1;
		        await dbInstance.Insert(foo1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 2;
		        await dbInstance.Insert(foo2);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);

				var refTbl1 = refTblT.CreateInstanceOrFail();
				((dynamic)refTbl1).Val = "a";
				await dbInstance.Insert(refTbl1);

				var refTbl2 = refTblT.CreateInstanceOrFail();
				((dynamic)refTbl2).Val = "b";
				((dynamic)refTbl2).FooId = ((dynamic)foo2).Id;
				await dbInstance.Insert(refTbl2);
		        
		        var expectedRefTbls = new (object?,object?,object?)[] {
				        (foo2,cof2,refTbl2)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("FetchData", fooT, cofT, refTblT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenInvertedLeftJoinThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenInvertedJoinThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void FetchData() =>	
	Query
		.From<Foo>()
		.LeftJoin((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Where( (f,cof) => (cof == null || cof.Id > 0) && f.Id > 0);
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1;
		        await dbInstance.Insert(foo1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 2;
		        await dbInstance.Insert(foo2);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo1,cof1), 
				        (foo2,cof2)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("FetchData", fooT, cofT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenInvertedJoinThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenInvertedJoinThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void FetchData() =>	
	Query
		.From<Foo>()
		.Join((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Where( (f,cof) => f.NullableInt == 2 && cof.Id > 0);
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1;
		        await dbInstance.Insert(foo1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 2;
		        await dbInstance.Insert(foo2);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo2,cof2)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("FetchData", fooT, cofT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	
	public static IEnumerable<object[]> AllValuesOf_DbToTestAndIsNotNull =>
		DbToTestUtil.GetAllToBeTested().SelectMany(db => 
			new [] {
				"!= null",
				"is not null",
				"is {}",
				"is {} _",
				"is {} z"
			}.Select(nullCmp => new object[] {nullCmp, db}));

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTestAndIsNotNull))]
	public async Task FromThenLeftJoinThenWhereUtilizingNullablePoco_IsNotNull(string nullCmprsn, DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenWhereUtilizingNullablePoco_IsNotNull),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ReferencesTableWithCompositePk>()
		.LeftJoin(rtwcp => rtwcp.ForeignKeys.TableWithCompositePk_by_ParentIdParentYear)
		.Where( (rtwcp,twcp) => twcp {nullCmprsn} && twcp.Id > 0);
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var twcpTypeName = "SogePoco.Pocos.TableWithCompositePk";
		        var twcpT = asm.GetTypeOrFail(twcpTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var twcp1 = twcpT.CreateInstanceOrFail();
		        ((dynamic)twcp1).Id = 1001;
		        ((dynamic)twcp1).Year = 2001;
		        ((dynamic)twcp1).Value = "x";
		        await dbInstance.Insert(twcp1);
		        
		        var twcp2 = twcpT.CreateInstanceOrFail();
		        ((dynamic)twcp2).Id = 1002;
		        ((dynamic)twcp2).Year = 2002;
		        ((dynamic)twcp2).Value = "y";
		        await dbInstance.Insert(twcp2);
		        
		        var refTbl1 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl1).Val = "a";
		        await dbInstance.Insert(refTbl1);
		        
		        var refTbl2 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl2).Val = "b";
		        ((dynamic)refTbl2).ParentId = ((dynamic)twcp1).Id;
		        ((dynamic)refTbl2).ParentYear = ((dynamic)twcp1).Year;
		        await dbInstance.Insert(refTbl2);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (refTbl2,twcp1)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", refTblT, twcpT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	
	
	public static IEnumerable<object[]> AllValuesOf_DbToTestAndIsNull {
            get => DbToTestUtil.GetAllToBeTested().SelectMany(db => 
	            new [] {
		            "== null",
		            "is null"
	            }.Select(nullCmp => new object[] {nullCmp, db})); 
	}
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTestAndIsNull))]
	public async Task FromThenLeftJoinThenWhereUtilizingNullablePoco_IsNull(string nullCmprsn, DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenWhereUtilizingNullablePoco_IsNull),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ReferencesTableWithCompositePk>()
		.LeftJoin(rtwcp => rtwcp.ForeignKeys.TableWithCompositePk_by_ParentIdParentYear)
		.Where( (rtwcp,twcp) => twcp {nullCmprsn} && rtwcp.Id > 0);
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var twcpTypeName = "SogePoco.Pocos.TableWithCompositePk";
		        var twcpT = asm.GetTypeOrFail(twcpTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var twcp1 = twcpT.CreateInstanceOrFail();
		        ((dynamic)twcp1).Id = 1001;
		        ((dynamic)twcp1).Year = 2001;
		        ((dynamic)twcp1).Value = "x";
		        await dbInstance.Insert(twcp1);
		        
		        var twcp2 = twcpT.CreateInstanceOrFail();
		        ((dynamic)twcp2).Id = 1002;
		        ((dynamic)twcp2).Year = 2002;
		        ((dynamic)twcp2).Value = "y";
		        await dbInstance.Insert(twcp2);
		        
		        var refTbl1 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl1).Val = "a";
		        await dbInstance.Insert(refTbl1);
		        
		        var refTbl2 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl2).Val = "b";
		        ((dynamic)refTbl2).ParentId = ((dynamic)twcp1).Id;
		        ((dynamic)refTbl2).ParentYear = ((dynamic)twcp1).Year;
		        await dbInstance.Insert(refTbl2);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (refTbl1, null)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", refTblT, twcpT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenJoinThenLeftJoinThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenLeftJoinThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ChildOfFoo>()
		.Join(cof => cof.ForeignKeys.Foo_by_FooId)		
		.LeftJoin((cof,f) => cof.ForeignKeys.ChildOfFoo_by_SiblingId)				
		.Where( (cof,f,cof2) => f.NullableInt == 2);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
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
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        ((dynamic)cof2).SiblingId = ((dynamic)cof1).Id;
		        await dbInstance.Insert(cof2);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo3).Id;
		        await dbInstance.Insert(cof3);
		        
		        var expectedRefTbls = new (object?,object?,object?)[] {
				        (cof1,foo2,null),
				        (cof2,foo2,cof1)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", cofT, fooT, cofT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenLeftJoinThenJoinThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenJoinThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ChildOfFoo>()
		.LeftJoin(cof => cof.ForeignKeys.ChildOfFoo_by_SiblingId)		
		.Join((cof,cof2) => cof.ForeignKeys.Foo_by_FooId)		
		.Where( (cof,cof2,f) => f.NullableInt == 2);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
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
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        ((dynamic)cof2).SiblingId = ((dynamic)cof1).Id;
		        await dbInstance.Insert(cof2);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo3).Id;
		        await dbInstance.Insert(cof3);
		        
		        var expectedRefTbls = new (object?,object?,object?)[] {
				        (cof1,null,foo2),
				        (cof2,cof1,foo2)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", cofT, cofT, fooT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenJoinThenJoinFromFirstThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenJoinFromFirstThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ChildOfFoo>()
		.Join(cof => cof.ForeignKeys.Foo_by_FooId)
		.Join((cof,f) => cof.ForeignKeys.ChildOfFoo_by_SiblingId)
		.Where( (cof,f,cofParent) => f.NullableInt == 2);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
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
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        ((dynamic)cof2).SiblingId = ((dynamic)cof1).Id;
		        await dbInstance.Insert(cof2);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo4).Id;
		        await dbInstance.Insert(cof3);
		        
		        var expectedRefTbls = new (object?,object?,object?)[] {
						(cof2,foo2,cof1)
					}.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", cofT,fooT,cofT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenJoinThenJoinFromSecondThenWhere(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenJoinFromSecondThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<ChildOfFoo>()
		.Join(cof => cof.ForeignKeys.ChildOfFoo_by_SiblingId)		
		.Join((cof,cof2) => cof2.ForeignKeys.Foo_by_FooId)
		.Where( (cof,cof2,f) => f.NullableInt == 2);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
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
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        ((dynamic)cof2).SiblingId = ((dynamic)cof1).Id;
		        await dbInstance.Insert(cof2);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo4).Id;
		        ((dynamic)cof3).SiblingId = ((dynamic)cof2).Id;
		        await dbInstance.Insert(cof3);
		        
		        var expectedRefTbls = new (object?,object?,object?)[] {
				        (cof3,cof2,foo2)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", cofT, cofT, fooT))
						.OrderBy(x => ((dynamic)x!.Item1!).Id)
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	
	public static IEnumerable<object[]> AllValuesOf_DbToTestAndDifferentParameterTyping =>
		DbToTestUtil.GetAllToBeTested().SelectMany(db => 
			new [] {
				"tbl",
				"(ReferencesTableWithCompositePk tbl)"
			}.Select(typeDec => new object[] {db, typeDec}));

	
	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTestAndDifferentParameterTyping))]
	public async Task FromThenJoinThenWhere(DbToTest dbToTest, string typeDec) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenWhere),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
    Query.From<ReferencesTableWithCompositePk>()
         .Join( {typeDec} => tbl.ForeignKeys.Foo_by_FooId)
         .Where( (rtwcp,f) => f.NullableInt == 2);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        
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
		        
		        var refTbl1 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl1).FooId = ((dynamic)foo1).Id;
		        ((dynamic)refTbl1).ParentYear = 2001;
		        await dbInstance.Insert(refTbl1);

		        var refTbl2 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl2).FooId = ((dynamic)foo2).Id;
		        ((dynamic)refTbl2).ParentYear = 2002;
		        await dbInstance.Insert(refTbl2);

		        var refTbl3 = refTblT.CreateInstanceOrFail();
		        ((dynamic)refTbl3).FooId = ((dynamic)foo3).Id;
		        ((dynamic)refTbl3).ParentYear = 2003;
		        await dbInstance.Insert(refTbl3);

		        var expectedRefTbls =
			        new (object?,object?)[] {
				        (refTbl2, foo2)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", refTblT, fooT))
						.OrderBy(x => ((dynamic)x.Item1!).Id)
						.TupleToPropertyNameAndValueDict();

		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
}
