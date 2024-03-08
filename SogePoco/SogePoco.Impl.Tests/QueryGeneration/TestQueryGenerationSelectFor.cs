using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.DatabaseBehavior;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.QueryGeneration;

public class TestQueryGenerationPostgresSelectFor : BaseTest {
	public TestQueryGenerationPostgresSelectFor(ITestOutputHelper outputHelper) : base(outputHelper) => 
		RawWorkerControllerExtensions.Logger = _loggerProvider.CreateLogger(nameof(RawWorkerControllerExtensions));

	Task<RawWorkerController<GeneratedDatabaseClassHelper>> CreateWorker(GeneratedDatabaseClassHelper conn) => 
        WorkerController.CreateRawWorker(
	        _loggerProvider, conn, 
	        x => x.GetDbConnectionOf().State == ConnectionState.Open, 
	        x => x.GetDbConnectionOf().Open());
    
    [Fact]
    public async Task PostgresForUpdateActuallyCausesLockingInCompetingTransaction() {
        if (!DbToTestUtil.GetAllToBeTested().Contains(DbToTest.Postgresql)) {
            return; //skip test because of unavailability of postgres instance
        }

        var dbToTest = DbToTest.Postgresql; 
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
            sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
            nameof(PostgresForUpdateActuallyCausesLockingInCompetingTransaction),
            sut,
            opt,
            QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
            $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	PostgresQuery
		.From<Foo>()
		.Where( f => f.NullableInt == 1024)
		.ForUpdate();		
}}", 
            onElement:generator.OnElement,
            generateCode:generator.GenerateFiles,
            postCompilationAssertions:async asm => {
                var fooTypeName = "SogePoco.Pocos.Foo";
                var fooT = asm.GetTypeOrFail(fooTypeName);
	            
                var dbInstanceFst = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.CreateAndOpenAnotherConnection(), DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                await using var dbConnFst = await CreateWorker(dbInstanceFst);
                
                var dbInstanceSnd = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.CreateAndOpenAnotherConnection(), DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                await using var dbConnSnd = await CreateWorker(dbInstanceSnd);
                
                var dbConn = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                
                var foo1 = fooT.CreateInstanceOrFail();
                ((dynamic)foo1).NullableInt = 1024;
                await dbConn.Insert(foo1);
		        
                var foo2 = fooT.CreateInstanceOrFail();
                ((dynamic)foo2).NullableInt = 512;
                await dbConn.Insert(foo2);
		        
                var foo3 = fooT.CreateInstanceOrFail();
                ((dynamic)foo3).NullableInt = 2;
                await dbConn.Insert(foo3);
		        
                var foo4 = fooT.CreateInstanceOrFail();
                ((dynamic)foo4).NullableInt = 8;
                await dbConn.Insert(foo4);
		        
                var foo5 = fooT.CreateInstanceOrFail();
                ((dynamic)foo5).NullableInt = 1;
                await dbConn.Insert(foo5);
                
                var expectedRefTbls = new [] {foo1}.ToPropertyNameAndValueDict();

                //
                // begin tran
                //
                await dbConnFst.RequestAndAssertExecution(x => {
	                var conn = x.GetDbConnectionOf();
	                using var dbCmd = conn.CreateCommand();
	                dbCmd.CommandText = "begin transaction;";
	                dbCmd.ExecuteNonQuery();
                });
                
                await dbConnSnd.RequestAndAssertExecution(x => {
	                var conn = x.GetDbConnectionOf();
	                using var dbCmd = conn.CreateCommand();
	                dbCmd.CommandText = "begin transaction;";
	                dbCmd.ExecuteNonQuery();
                });
                
                //
                // fst locks out snd
                //
                var actual1 = await dbConnFst.RequestAndAssertReplyAndReturn(x => {
	                var dbExtensions = x.BuildExtensionsHelper();

	                var rawResult = TestingHacks.RunAsyncAsSync(
		                () => dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT));
		            return rawResult.ToPropertyNameAndValueDict();
                });
                AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actual1);
				
				var actual2 = await dbConnSnd.RequestAndAssertTimeout(x => {
	                var dbExtensions = x.BuildExtensionsHelper();

	                var rawResult = TestingHacks.RunAsyncAsSync(
		                () => dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT));
		            return new DbAnswer.OkSingleValue(rawResult.ToPropertyNameAndValueDict());
                });

				//fst commits unblocking snd
				await dbConnFst.RequestAndAssertExecution(x => {
	                var conn = x.GetDbConnectionOf();
	                using var dbCmd = conn.CreateCommand();
	                dbCmd.CommandText = "commit transaction;";
	                dbCmd.ExecuteNonQuery();
                });
                
				//snd returns
				await dbConnSnd.AssertPendingReplyAsserts(x => {
					var v = Assert.IsType<DbAnswer.OkSingleValue>(x);
					AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, 
						v.Val as IEnumerable<IDictionary<string, object?>> ?? throw new InvalidOperationException());

					return Task.CompletedTask;
				});
            });
    }
    
    [Fact]
    public async Task PostgresForShareActuallyDoesntLockInCompetingTransaction() {
        if (!DbToTestUtil.GetAllToBeTested().Contains(DbToTest.Postgresql)) {
            return; //skip test because of unavailability of postgres instance
        }

        var dbToTest = DbToTest.Postgresql; 
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
            sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
            nameof(PostgresForShareActuallyDoesntLockInCompetingTransaction),
            sut,
            opt,
            QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
            $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	PostgresQuery
		.From<Foo>()
		.Where( f => f.NullableInt == 1024)
		.ForShare();		
}}", 
            onElement:generator.OnElement,
            generateCode:generator.GenerateFiles,
            postCompilationAssertions:async asm => {
                var fooTypeName = "SogePoco.Pocos.Foo";
                var fooT = asm.GetTypeOrFail(fooTypeName);
	            
                var dbInstanceFst = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.CreateAndOpenAnotherConnection(), DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                await using var dbConnFst = await CreateWorker(dbInstanceFst);
                
                var dbInstanceSnd = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.CreateAndOpenAnotherConnection(), DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                await using var dbConnSnd = await CreateWorker(dbInstanceSnd);
                
                var dbConn = GeneratedDatabaseClassHelper.CreateInstance(
                    Logger, asm, opt, sut.DbConn.DbConn, DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert());
                
                var foo1 = fooT.CreateInstanceOrFail();
                ((dynamic)foo1).NullableInt = 1024;
                await dbConn.Insert(foo1);
		        
                var foo2 = fooT.CreateInstanceOrFail();
                ((dynamic)foo2).NullableInt = 512;
                await dbConn.Insert(foo2);
		        
                var foo3 = fooT.CreateInstanceOrFail();
                ((dynamic)foo3).NullableInt = 2;
                await dbConn.Insert(foo3);
		        
                var foo4 = fooT.CreateInstanceOrFail();
                ((dynamic)foo4).NullableInt = 8;
                await dbConn.Insert(foo4);
		        
                var foo5 = fooT.CreateInstanceOrFail();
                ((dynamic)foo5).NullableInt = 1;
                await dbConn.Insert(foo5);
                
                var expectedRefTbls = new [] {foo1}.ToPropertyNameAndValueDict();

                //
                // begin tran
                //
                await dbConnFst.RequestAndAssertExecution(x => {
	                var conn = x.GetDbConnectionOf();
	                using var dbCmd = conn.CreateCommand();
	                dbCmd.CommandText = "begin transaction;";
	                dbCmd.ExecuteNonQuery();
                });
                
                await dbConnSnd.RequestAndAssertExecution(x => {
	                var conn = x.GetDbConnectionOf();
	                using var dbCmd = conn.CreateCommand();
	                dbCmd.CommandText = "begin transaction;";
	                dbCmd.ExecuteNonQuery();
                });
                
                //
                // fst locks out snd
                //
                var actual1 = await dbConnFst.RequestAndAssertReplyAndReturn(x => {
	                var dbExtensions = x.BuildExtensionsHelper();

	                var rawResult = TestingHacks.RunAsyncAsSync(
		                () => dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT));
		            return rawResult.ToPropertyNameAndValueDict();
                });
                AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actual1);
				
				var actual2 = await dbConnSnd.RequestAndAssertReplyAndReturn(x => {
	                var dbExtensions = x.BuildExtensionsHelper();

	                var rawResult = TestingHacks.RunAsyncAsSync(
		                () => dbExtensions.ExecuteGeneratedQuery("GetMatchingFoo", fooT));
		            return rawResult.ToPropertyNameAndValueDict();
                });
				AssertUtil.AssertSameEntitiesColl(Logger, null, expectedRefTbls, actual2);
            });
    }
    
    //this one is not reliable as query may look like: select col1,col2 from tablename -- for select;
    [Theory]
    [InlineData(".ForUpdate()", @"\s+for\s+update\s*;\s*?$")]
    [InlineData(".ForShare()", @"\s+for\s+share\s*;\s*?$")]
    [InlineData(".ForNoKeyUpdate()", @"\s+for\s+no\s+key\s+update\s*;\s*?$")]
    [InlineData(".WithForClause(\"update nowait\")", @"\s+for\s+update\s+nowait\s*;\s*?$")]
    public async Task PostgresForClauseIsAppendedToQuery(string queryForMethodInvocation, string expectedLastQueryRegexp) {
        if (!DbToTestUtil.GetAllToBeTested().Contains(DbToTest.Postgresql)) {
            return; //skip test because of unavailability of postgres instance
        }

        var dbToTest = DbToTest.Postgresql; 
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        
        var opt = new GeneratorOptions();
        var generator = new DefaultQueryGenerator(
            sut.DbConn.AdoDbConnectionFullClassName, sut.MapperGenerator, opt, sut.Naming);

        await QueryGeneratorTestUtil.GenerateCompileAndAssert(
            nameof(PostgresForClauseIsAppendedToQuery),
            sut,
            opt,
            QueryGeneratorTestUtil.QueryRegistrationApiSnippet,
            $@"
[GenerateQueries]
class Second {{
public void GetMatchingFoo() =>	
	PostgresQuery
		.From<Foo>()
		{queryForMethodInvocation}
		.Where( f => f.NullableInt == 1024);		
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

                Assert.Matches(
	                new Regex(expectedLastQueryRegexp, RegexOptions.IgnoreCase),
	                dbExtensions.GetLastSqlText());
            });
    }
}
