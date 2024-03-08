using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.QueryGeneration; 

public class TestQueryGenerationSelect : BaseTest {
    public TestQueryGenerationSelect(ITestOutputHelper outputHelper) : base(outputHelper) {}
	

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenLeftJoinThenWhereSelectTupleOfSecondAndThird(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenWhereSelectTupleOfSecondAndThird),
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
		.Where( (cof,f,cof2) => f.NullableInt == 2)
		.Select((cof,f,cof2) => (f,cof2) );		
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
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo2,null),
				        (foo2,cof1)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT, cofT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenLeftJoinThenWhereSelectTupleOfFirstAndLast(DbToTest dbToTest) {
		using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenWhereSelectTupleOfFirstAndLast),
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
		.Where((f,cof,rtwcp) => cof.Id > 0 && rtwcp.Val == ""b"" && f.NullableInt == 2)
		.Select((f,cof,rtwcp) => (f, rtwcp));
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
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo2,refTbl2)
			        }
			      .TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("FetchData", fooT, refTblT))
						.OrderBy(x => ((dynamic)x).Item1.Id )
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenLeftJoinThenWhereSelectSecondOnePoco(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenLeftJoinThenWhereSelectSecondOnePoco),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
    Query.From<ReferencesTableWithCompositePk>()
         .LeftJoin( rtwcp => rtwcp.ForeignKeys.Foo_by_FooId)
         .Where( (rtwcp,f) => f == null || f.NullableInt == 2)
         .Select( (rtwcp,f) => f);		
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
		        
		        var expectedRefTbls = new[] {null, foo2}
			        .ToPropertyNameAndValueDict();

		        var defaultIdValue = ((dynamic) fooT.CreateInstanceOrFail()).Id;
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.OrderBy(x => x == null ? defaultIdValue : ((dynamic)x).Id )
						.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenJoinThenWhereSelectSecondOnePoco(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenWhereSelectSecondOnePoco),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
    Query.From<ReferencesTableWithCompositePk>()
         .Join( rtwcp => rtwcp.ForeignKeys.Foo_by_FooId)
         .Where( (rtwcp,f) => f.NullableInt == 2 && rtwcp.Id > 0)
         .Select( (rtwcp,f) => f);		
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
		        
		        var expectedRefTbls = new[] {foo2}
			        .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.OrderBy(x => ((dynamic)x!).Id )
						.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromThenJoinThenWhereSelectFirstOnePoco(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromThenJoinThenWhereSelectFirstOnePoco),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
    Query.From<ReferencesTableWithCompositePk>()
         .Join( rtwcp => rtwcp.ForeignKeys.Foo_by_FooId)
         .Where( (rtwcp,f) => f.NullableInt == 2)
         .Select( (rtwcp,f) => rtwcp);		
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
		        
		        var expectedRefTbls = new[] {refTbl2}
			        .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", refTblT))
						.OrderBy(x => ((dynamic)x!).Id )
						.ToPropertyNameAndValueDict();

				AssertUtil.AssertSameEntitiesColl(Logger, "Id", expectedRefTbls, actualRefTbls);
	        });
    }
}