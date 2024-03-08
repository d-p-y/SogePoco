using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.QueryGeneration;

public class TestQueryGenerationTake : BaseTest {
    public TestQueryGenerationTake(ITestOutputHelper outputHelper) : base(outputHelper) {}
	
	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromWhereOrderByDescTake2(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromWhereOrderByDescTake2),
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
		.OrderByDesc(f => f.NullableInt)
        .Take(2);		
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
		        
		        var expectedRefTbls = new [] {foo1,foo2}
			      .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.ToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromWhereOrderByDescTake1(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromWhereOrderByDescTake1),
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
		.OrderByDesc(f => f.NullableInt)
        .Take(1);		
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
		        
		        var expectedRefTbls = new [] {foo1}
			      .ToPropertyNameAndValueDict();
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.ToPropertyNameAndValueDict();
		        
		        AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actualRefTbls);
	        });
    }

	[Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
	public async Task FromTake3(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
	        sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
	        nameof(FromTake3),
	        sut,
	        opt,
	        QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
	        $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	Query
		.From<Foo>()						
        .Take(3);		
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
		        
		        var actualRefTbls = 
					(await dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT))
						.ToPropertyNameAndValueDict();
		        
		        Assert.Equal(3, actualRefTbls.Count);
	        });
    }
}
