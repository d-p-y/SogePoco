using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.QueryGeneration;

public class TestQueryGenerationOrderBy : BaseTest {
    public TestQueryGenerationOrderBy(ITestOutputHelper outputHelper) : base(outputHelper) {}
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromWhereOrderByDesc(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromWhereOrderByDesc),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()						
		.Where( f => f.NullableInt >= 2)
		.OrderByDesc(f => f.NullableInt);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        //
		        // var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        // var cofT = asm.GetTypeOrFail(cofTypeName);
	            
                var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1024;
		        await dbInstance.Insert(foo1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 512;
		        await dbInstance.Insert(foo2);
		        
		        var foo3 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo3).NullableInt = 2;
		        await dbInstance.Insert(foo3);
		        
		        var foo4 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo4).NullableInt = 8;
		        await dbInstance.Insert(foo4);
		        
		        var foo5 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo5).NullableInt = 1;
		        await dbInstance.Insert(foo5);
		        
		        var expectedRefTbls = new [] {foo1,foo2,foo4,foo3}
			      .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.ToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromWhereOrderByAsc(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromWhereOrderByAsc),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()						
		.Where( f => f.NullableInt >= 2)
		.OrderByAsc(f => f.NullableInt);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        //
		        // var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        // var cofT = asm.GetTypeOrFail(cofTypeName);
	            
                var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1024;
		        await dbInstance.Insert(foo1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 512;
		        await dbInstance.Insert(foo2);
		        
		        var foo3 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo3).NullableInt = 2;
		        await dbInstance.Insert(foo3);
		        
		        var foo4 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo4).NullableInt = 8;
		        await dbInstance.Insert(foo4);
		        
		        var foo5 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo5).NullableInt = 1;
		        await dbInstance.Insert(foo5);
		        
		        var expectedRefTbls = new [] {foo3,foo4,foo2,foo1}
			      .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.ToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actualRefTbls);
	        });
    }
	
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromJoinWhereOrderByAscOrderByDesc(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromJoinWhereOrderByAscOrderByDesc),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()
		.Join((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Where( (f,cof) => f.NullableInt >= 2)
		.OrderByAsc((f,cof) => f.NotNullableInt)
		.OrderByDesc((f,cof) => f.NullableInt);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        //
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
                var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NotNullableInt = 5;
		        ((dynamic)foo1).NullableInt = 1024;
		        await dbInstance.Insert(foo1);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NotNullableInt = 5;
		        ((dynamic)foo2).NullableInt = 512;
		        await dbInstance.Insert(foo2);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);
		        
		        var foo3 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo3).NotNullableInt = 3;
		        ((dynamic)foo3).NullableInt = 2;
		        await dbInstance.Insert(foo3);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo3).Id;
		        await dbInstance.Insert(cof3);
		        
		        var foo4 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo4).NotNullableInt = 3;
		        ((dynamic)foo4).NullableInt = 8;
		        await dbInstance.Insert(foo4);
		        
		        var cof4 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof4).FooId = ((dynamic)foo4).Id;
		        await dbInstance.Insert(cof4);
		        
		        var foo5 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo5).NullableInt = 1;
		        await dbInstance.Insert(foo5);
		        
		        var cof5 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof5).FooId = ((dynamic)foo5).Id;
		        await dbInstance.Insert(cof5);
		        
		        var expectedRefTbls = new (object?,object?)[] {
			                         //A,D
					    (foo4,cof4), //3,8
				        (foo3,cof3), //3,2
				        (foo1,cof1), //5,1024
				        (foo2,cof2)  //5,512
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT,cofT))
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromJoinWhereOrderByDesc(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromJoinWhereOrderByDesc),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()
		.Join((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Where( (f,cof) => f.NullableInt >= 2)
		.OrderByDesc((f,cof) => f.NullableInt);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        //
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
                var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1024;
		        await dbInstance.Insert(foo1);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 512;
		        await dbInstance.Insert(foo2);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);
		        
		        var foo3 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo3).NullableInt = 2;
		        await dbInstance.Insert(foo3);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo3).Id;
		        await dbInstance.Insert(cof3);
		        
		        var foo4 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo4).NullableInt = 8;
		        await dbInstance.Insert(foo4);
		        
		        var cof4 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof4).FooId = ((dynamic)foo4).Id;
		        await dbInstance.Insert(cof4);
		        
		        var foo5 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo5).NullableInt = 1;
		        await dbInstance.Insert(foo5);
		        
		        var cof5 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof5).FooId = ((dynamic)foo5).Id;
		        await dbInstance.Insert(cof5);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo1,cof1),
				        (foo2,cof2),
				        (foo4,cof4),
				        (foo3,cof3)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT,cofT))
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromJoinWhereOrderByAsc(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromJoinWhereOrderByAsc),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()
		.Join((ChildOfFoo cof) => cof.ForeignKeys.Foo_by_FooId, f => f)
		.Where( (f,cof) => f.NullableInt >= 2)
		.OrderByAsc((f,cof) => f.NullableInt);		
}}", 
	        onElement:generator.OnElement,
	        generateCode:generator.GenerateFiles,
	        postCompilationAssertions:async asm => {
		        // var refTblTypeName = "SogePoco.Pocos.ReferencesTableWithCompositePk";
		        // var refTblT = asm.GetTypeOrFail(refTblTypeName);
		        //
		        var cofTypeName = "SogePoco.Pocos.ChildOfFoo";
		        var cofT = asm.GetTypeOrFail(cofTypeName);
	            
                var fooTypeName = "SogePoco.Pocos.Foo";
		        var fooT = asm.GetTypeOrFail(fooTypeName);
	            
	            var dbInstance = GeneratedDatabaseClassHelper.CreateInstance(
		            Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
	            
	            var dbExtensions = dbInstance.BuildExtensionsHelper();
	             
		        var foo1 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo1).NullableInt = 1024;
		        await dbInstance.Insert(foo1);
		        
		        var cof1 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof1).FooId = ((dynamic)foo1).Id;
		        await dbInstance.Insert(cof1);
		        
		        var foo2 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo2).NullableInt = 512;
		        await dbInstance.Insert(foo2);
		        
		        var cof2 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof2).FooId = ((dynamic)foo2).Id;
		        await dbInstance.Insert(cof2);
		        
		        var foo3 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo3).NullableInt = 2;
		        await dbInstance.Insert(foo3);
		        
		        var cof3 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof3).FooId = ((dynamic)foo3).Id;
		        await dbInstance.Insert(cof3);
		        
		        var foo4 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo4).NullableInt = 8;
		        await dbInstance.Insert(foo4);
		        
		        var cof4 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof4).FooId = ((dynamic)foo4).Id;
		        await dbInstance.Insert(cof4);
		        
		        var foo5 = fooT.CreateInstanceOrFail();
		        ((dynamic)foo5).NullableInt = 1;
		        await dbInstance.Insert(foo5);
		        
		        var cof5 = cofT.CreateInstanceOrFail();
		        ((dynamic)cof5).FooId = ((dynamic)foo5).Id;
		        await dbInstance.Insert(cof5);
		        
		        var expectedRefTbls = new (object?,object?)[] {
				        (foo3,cof3),
				        (foo4,cof4),
				        (foo2,cof2),
				        (foo1,cof1)
			        }.TupleToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT,cofT))
						.TupleToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, expectedRefTbls, actualRefTbls);
	        });
    }
}
