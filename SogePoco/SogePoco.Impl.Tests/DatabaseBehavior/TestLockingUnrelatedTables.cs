using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.Tests.PocoGeneration;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

/// <summary> same comment as in class <see cref="T:TestLockingRelatedTables"/> </summary>
public class TestLockingUnrelatedTables : BaseTest {
    public TestLockingUnrelatedTables(ITestOutputHelper outputHelper) : base(outputHelper) { }

    Task<DbConnectionWorkerController> CreateThreadedWorker(DbConnection conn) => 
        WorkerController.CreateDbConnectionWorker(_loggerProvider, conn);
    
    ///contrasting behavior to <see cref="M:DatabaseBehavior.TestLockingRelatedTables.InSqlServerWhenUsingReadCommittedItBlocksOnPotentialConflicts"/>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task InSqlServerWhenUsingReadCommittedItDoesntBlockOnPotentialConflicts(bool enableSnapshot, bool readCommittedUsesSnapshot) {
        var isolationLevel = "read committed";
        
        //fst wants some row to stay immutable; snd wants to change same row
        const DbToTest dbToTest = DbToTest.SqlServer;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of sql server instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);

        if (enableSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET ALLOW_SNAPSHOT_ISOLATION ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        if (readCommittedUsesSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET READ_COMMITTED_SNAPSHOT ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        await fst.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        await snd.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution("begin transaction;");
        await snd.RequestAndAssertExecution("begin transaction;");
        
        //
        // select same row requesting lock
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        var (sndFooId, sndNullableText) = await snd.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")});  
        
        Assert.Equal(fstFooId, sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);

        await snd.RequestAndAssertExecution("update foo set NullableText = 'zzz' where id = @p0;", 
            new object?[] {new SqlParameter("p0", sndFooId)});
        
        //fst will not be blocked because reference/foreignkey of Foo implicitly locked by snd is not used by fst
        await fst.RequestAndAssertExecution("insert into TableWithCompositePk (Id,Year,Value) values (@p0,@p1,@p2);",
            new object?[] {new SqlParameter("p0", 1), new SqlParameter("p1", 2001), new SqlParameter("p2", "x")});
        
        await snd.RequestAndAssertExecution("commit transaction;"); 
        await fst.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND TableWithCompositePk inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk;";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync());
        }
    }
    
    ///contrasting behavior to <see cref="M:DatabaseBehavior.TestLockingRelatedTables.InSqlServerWhenUsingSnapshotItDoesntBlockOnConflictButThrowsErrorAndAbortsTransaction"/>
    [Fact]
    public async Task InSqlServerWhenUsingSnapshotItDoesntBlockOrRaiseErrorOnPotentialConflict() {
        var isolationLevel = "snapshot";
        var enableSnapshot = true;
        var readCommittedUsesSnapshot = false;
            
        //fst wants some row to stay immutable; snd wants to change same row
        const DbToTest dbToTest = DbToTest.SqlServer;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of sql server instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);

        if (enableSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET ALLOW_SNAPSHOT_ISOLATION ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        if (readCommittedUsesSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET READ_COMMITTED_SNAPSHOT ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        await fst.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        await snd.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution("begin transaction;");
        await snd.RequestAndAssertExecution("begin transaction;");
        
        //
        // select same row requesting lock
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        var (sndFooId, sndNullableText) = await snd.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")});  
        
        Assert.Equal(fstFooId, sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);

        await snd.RequestAndAssertExecution("update foo set NullableText = 'zzz' where id = @p0;", 
            new object?[] {new SqlParameter("p0", sndFooId)});
        
        //fst will not be blocked because of Foo implicitly locked by snd because tables are unrelated
        await fst.RequestAndAssertExecution("insert into TableWithCompositePk (Id,Year,Value) values (@p0,@p1,@p2);",
            new object?[] {new SqlParameter("p0", 1), new SqlParameter("p1", 2000), new SqlParameter("p2", "abc")});
        
        await snd.RequestAndAssertExecution("commit transaction;");
        await fst.RequestAndAssertExecution("commit transaction;");
        
        await fst.RequestScalarAndAssertReply("select @@TRANCOUNT;", x => {
            Assert.Equal(0, x);
            return Task.CompletedTask;
        });
        
        //
        // both foo update AND TableWithCompositePk inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk;";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync()); //insert was reverted
        }
    }
    
    ///same behavior as in related tables <see cref="M:DatabaseBehavior.TestLockingRelatedTables.InSqlServerSelectingRowInHigherIsolationLevelImpliesSharedLocking"/>
    [Theory]
    [InlineData("repeatable read", false, false)]
    [InlineData("serializable", false, false)]
    public async Task InSqlServerSelectingRowInHigherIsolationLevelImpliesSharedLocking(
            string isolationLevel, bool enableSnapshot, bool readCommittedUsesSnapshot) {
        
        //fst wants some row to stay immutable; snd wants to change same row
        const DbToTest dbToTest = DbToTest.SqlServer;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of sql server instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);

        if (enableSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET ALLOW_SNAPSHOT_ISOLATION ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        if (readCommittedUsesSnapshot) {
            await using var cmd = sut.DbConn.DbConn.CreateCommand();
            cmd.CommandText = $"alter database {sut.DbConn.TargetDatabaseName} SET READ_COMMITTED_SNAPSHOT ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        
        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        await fst.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        await snd.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution("begin transaction;");
        await snd.RequestAndAssertExecution("begin transaction;");
        
        //
        // select same row requesting lock
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        var (sndFooId, sndNullableText) = await snd.RequestSingleRowAndAssertReply(
            "select id,NullableText from foo where NullableText = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("NullableText", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")});  
        
        Assert.Equal(fstFooId, sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);
        
        //snd will be blocked because fst implicitly locked Foo row 
        await snd.RequestAndAssertTimeout("update foo set NullableText = 'zzz' where id = @p0;", 
            new object?[] {new SqlParameter("p0", sndFooId)});
        
        await fst.RequestAndAssertExecution("insert into TableWithCompositePk (Id,Year,Value) values (@p0,@p1,@p2);",
            new object?[] {new SqlParameter("p0", fstFooId), new SqlParameter("p1", 2001), new SqlParameter("p2", "aaa")});
        await snd.AssertReplyTimeouts(); //snd is still blocked by fst tran
        
        await fst.RequestAndAssertExecution("commit transaction;"); //fst should become unblocked
        
        await snd.AssertPendingReplyAsserts(x => Assert.IsType<DbAnswer.Ok>(x));
        await snd.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND TableWithCompositePk inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from TableWithCompositePk;";
            Assert.Equal(1, await dbCmd.ExecuteScalarAsync());
        }
    }
    
}
