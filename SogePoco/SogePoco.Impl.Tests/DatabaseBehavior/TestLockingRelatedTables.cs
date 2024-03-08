using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

/// <summary>
/// Shortly: Sql Server make assumptions to guess whether `select`-ion of rows in transaction implies (b)locking or not.
///      MS docs discourage manual influence of locking using query hints e.g. select ... from table with (rowlock, holdlock). It is advised to use different isolation level instead.
///
/// Sql Server is *complicated*.  It has following isolation levels:
///   read uncommitted,
///   read committed [without snapshot],
///   repeatable read,
///   serializable,
///   snapshot,
///   read committed [with] snapshot
/// 
/// By default, Sql Server uses pessimistic concurrency and doesn't use MVCC/row versioning. Last two levels above, use MVCC/row versioning transitioning it into optimistic concurrency. 
/// https://learn.microsoft.com/en-us/previous-versions/tn-archive/cc546518(v=technet.10)?redirectedfrom=MSDN
/// https://www.red-gate.com/simple-talk/databases/sql-server/t-sql-programming-sql-server/row-versioning-concurrency-in-sql-server/
///
///Tests in this class and in <see cref="T:TestLockingUnrelatedTables"/> verify how databases behave on 'logical conflict' defined as follows:
///    when user in transaction does `select` of some row, they see it is in a state such-and-such hence they do some modification to another table in db.
///    should database assume that mere `select` implicates that my transaction depends on state of `select`-ed row?
///    Postgres doesn't make this assumption and depends on explicit locking (select ... for ...).
///    Sql Server instead seems to (b)lock in some cases (=when primary key of selected row is used in mutation OR one uses 'repeatable read' or 'serializable) and not (b)lock when tables are unrelated. 
///
/// This class tests related tables (equivalent of header-detail scenario where `Foo` and `ChildOfFoo` play those roles).
/// Concurrently one transaction attempts to change 'header' and another only verifies 'header' state to add 'detail'.
/// Class <see cref="T:TestLockingUnrelatedTables"/> does exactly the same operations but using tables not referencing each other (`Foo` and `TableWithCompositePk`)
/// </summary>
public class TestLockingRelatedTables : BaseTest {
    public TestLockingRelatedTables(ITestOutputHelper outputHelper) : base(outputHelper) { }

    Task<DbConnectionWorkerController> CreateThreadedWorker(DbConnection conn) => 
        WorkerController.CreateDbConnectionWorker(_loggerProvider, conn);
    
    [Theory]
	[MemberData(nameof(AllValuesOf_DbToTest))]
    public async Task TestWorkerWorks(DbToTest dbToTest) {
        if (dbToTest == DbToTest.Sqlite) {
			return; //skip test because of inability to have empty member data (possible when skipping via environment)
		}
                
        using var sut = await SystemUnderTestFactory.Create(dbToTest);

        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fstSession = await CreateThreadedWorker(fstConn);
        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var sndSession = await CreateThreadedWorker(sndConn);
        
        //
        // errors are reported
        //
        await fstSession.RequestAndAssertReply(
            _ => throw new Exception("test"),
            x => Assert.True(x is DbAnswer.Error));
        
        await sndSession.RequestAndAssertReply(
            _ => throw new Exception("test"),
            x => Assert.True(x is DbAnswer.Error));
        
        //
        // query result is passed
        //
        await sndSession.RequestAndAssertReply(
            dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = "select 'abc'";
                var result = dbCmd.ExecuteScalar();
                return new DbAnswer.OkSingleValue(result);
            },
            x => Assert.Equal("abc", x is DbAnswer.OkSingleValue {} y 
                ? y.Val 
                : throw new Exception($"not {nameof(DbAnswer.OkSingleValue)}")));
        
        //
        // quitting works
        //
        await fstSession.RequestShutdownAndAssertCompliance();
        await sndSession.RequestShutdownAndAssertCompliance();
    }
    
    //note #1: Postgres uses snapshot by default and doesn't assume that 'select' implies locking
    //         unless explicit 'select ... for ...' is requested
    //note #2: no need for "read uncommitted" https://www.postgresql.org/docs/current/sql-set-transaction.html
    [Theory]
    [InlineData("read committed", true)]
    [InlineData("read committed", false)]
    [InlineData("repeatable read", true)]
    [InlineData("repeatable read", false)]
    [InlineData("serializable", true)]
    [InlineData("serializable", false)]
    public async Task InPostgresqlSelectingRowDoesntImplyLocking(string isolationLevel, bool fstCommitsAsFirstOne) {
        const DbToTest dbToTest = DbToTest.Postgresql;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of postgres instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);

        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        using var fstConnRaw = NpgsqlTestingHelper.CreateDisposableForcingDisconnectionOf(fstConn);

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        using var sndConnRaw = NpgsqlTestingHelper.CreateDisposableForcingDisconnectionOf(sndConn);
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where nullable_text = 'nt';";
            Assert.Equal(1L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from child_of_foo";
            Assert.Equal(2L, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        await snd.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        
        //
        // select same row
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            "select id,nullable_text from foo where nullable_text = 'nt';", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        var (sndFooId, sndNullableText) = await snd.RequestSingleRowAndAssertReply(
            "select id,nullable_text from foo where nullable_text = 'nt';", 
             x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );  
        
        Assert.Equal(fstFooId, sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);
        
        //
        // make decision based on the perceived state of Foo
        //
        await snd.RequestAndAssertExecution("update foo set nullable_text = 'zzz' where id = $1;", new object?[] {sndFooId});
        await fst.RequestAndAssertExecution("insert into child_of_foo (foo_id) values ($1);", new object?[] {fstFooId});
        
        //from fst's perspective, Foo is the same as it was since first select...
        var (fstFooId2, fstNullableText2) = await fst.RequestSingleRowAndAssertReply(
            "select id,nullable_text from foo where id = $1;",
            new object?[] {fstFooId},
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        Assert.Equal(fstFooId, fstFooId2);
        Assert.Equal(fstNullableText, fstNullableText2);
        
        //
        // commit
        //
        if (fstCommitsAsFirstOne) {
            await fst.RequestAndAssertExecution("commit transaction;");
            await snd.RequestAndAssertExecution("commit transaction;");    
        } else {
            await snd.RequestAndAssertExecution("commit transaction;");
            await fst.RequestAndAssertExecution("commit transaction;");
        }
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where nullable_text = 'nt';";
            Assert.Equal(0L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from child_of_foo;";
            Assert.Equal(3L, await dbCmd.ExecuteScalarAsync());
        }
    }
    
    
    //note #1: syntax of 'for' clause see "The Locking Clause" in https://www.postgresql.org/docs/current/sql-select.html
    //note #2: no need for "read uncommitted" https://www.postgresql.org/docs/current/sql-set-transaction.html
    //docs https://www.postgresql.org/docs/current/explicit-locking.html
    [Theory]
    //expensive
    [InlineData("read committed", "for update", "for update")]
    [InlineData("repeatable read", "for update", "for update")]
    [InlineData("serializable", "for update", "for update")]
    //less expensive
    [InlineData("read committed", "for no key update", "for no key update")]
    [InlineData("repeatable read", "for no key update", "for no key update")]
    [InlineData("serializable", "for no key update", "for no key update")]
    //cheapest
    [InlineData("read committed", "for share", "for no key update")]
    [InlineData("repeatable read", "for share", "for no key update")]
    [InlineData("serializable", "for share", "for no key update")]
    public async Task InPostgresqlSelectingRowWithForClauseCausesLocking(string isolationLevel, string fstForClause, string sndForClause) {
        //fst wants some row to stay immutable; snd wants to change same row
        const DbToTest dbToTest = DbToTest.Postgresql;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of postgres instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);

        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        using var fstConnRaw = NpgsqlTestingHelper.CreateDisposableForcingDisconnectionOf(fstConn);

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        using var sndConnRaw = NpgsqlTestingHelper.CreateDisposableForcingDisconnectionOf(sndConn);
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where nullable_text = 'nt';";
            Assert.Equal(1L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from child_of_foo";
            Assert.Equal(2L, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        await snd.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        
        //
        // select same row requesting lock
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            $"select id,nullable_text from foo where nullable_text = 'nt' {fstForClause};", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        await snd.RequestSingleRowAndAssertTimeout(
            $"select id,nullable_text from foo where nullable_text = 'nt' {sndForClause};");  
        
        //
        // make decision based on the perceived state of Foo
        //
        await fst.RequestAndAssertExecution("insert into child_of_foo (foo_id) values ($1);", new object?[] {fstFooId});
        await snd.AssertReplyTimeouts(); //snd is still blocked by fst tran
        
        await fst.RequestAndAssertExecution("commit transaction;"); //snd should become unblocked
        var (sndFooId, sndNullableText) = await snd.AssertPendingReplyAsserts(x => {
            Assert.IsType<DbAnswer.OkSingleRow>(x);
            return x switch {
                DbAnswer.OkSingleRow {Row: var y} when
                    y.TryGetValue("id", out var id) && id is int idInt &&
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr),
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")};
        });
        
        Assert.Equal(fstFooId,sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);
        
        await snd.RequestAndAssertExecution("update foo set nullable_text = 'zzz' where id = $1;", new object?[] {sndFooId});
        await snd.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where nullable_text = 'nt';";
            Assert.Equal(0L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from child_of_foo;";
            Assert.Equal(3L, await dbCmd.ExecuteScalarAsync());
        }
    }
    
    ///contrasting behavior to <see cref="M:DatabaseBehavior.TestLockingUnrelatedTables.InSqlServerWhenUsingReadCommittedItDoesntBlockOnPotentialConflicts"/>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task InSqlServerWhenUsingReadCommittedItBlocksOnPotentialConflicts(bool enableSnapshot, bool readCommittedUsesSnapshot) {
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
            dbCmd.CommandText = "select count(*) from ChildOfFoo";
            Assert.Equal(2, await dbCmd.ExecuteScalarAsync());
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
        
        //fst will be blocked because snd implicitly locked Foo row above
        await fst.RequestAndAssertTimeout("insert into ChildOfFoo (FooId) values (@p0);",
            new object?[] {new SqlParameter("p0", fstFooId)});
        
        await snd.RequestAndAssertExecution("commit transaction;"); //fst should become unblocked
        await fst.AssertPendingReplyAsserts(x => Assert.IsType<DbAnswer.Ok>(x));
        
        await fst.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from ChildOfFoo;";
            Assert.Equal(3, await dbCmd.ExecuteScalarAsync());
        }
    }
    
    ///contrasting behavior to <see cref="M:DatabaseBehavior.TestLockingUnrelatedTables.InSqlServerWhenUsingSnapshotItDoesntBlockOrRaiseErrorOnPotentialConflict"/>
    [Fact]
    public async Task InSqlServerWhenUsingSnapshotItDoesntBlockOnConflictButThrowsErrorAndAbortsTransaction() {
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
            dbCmd.CommandText = "select count(*) from ChildOfFoo";
            Assert.Equal(2, await dbCmd.ExecuteScalarAsync());
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
        
        //fst will be blocked because snd implicitly locked Foo row above
        await fst.RequestAndAssertTimeout("insert into ChildOfFoo (FooId) values (@p0);",
            new object?[] {new SqlParameter("p0", fstFooId)});
        
        await snd.RequestAndAssertExecution("commit transaction;"); //fst should become unblocked
        await fst.AssertPendingReplyAsserts(x => {
            var err = Assert.IsType<DbAnswer.Error>(x);
            Assert.Contains("Snapshot isolation transaction aborted", err.Msg);
            return err;
        });
        
        await fst.RequestScalarAndAssertReply("select @@TRANCOUNT;", x => {
            Assert.Equal(0, x);
            return Task.CompletedTask;
        });
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from ChildOfFoo;";
            Assert.Equal(2, await dbCmd.ExecuteScalarAsync()); //insert was reverted
        }
    }
    
    ///same behavior as in related tables <see cref="M:DatabaseBehavior.TestLockingUnrelatedTables.InSqlServerSelectingRowInHigherIsolationLevelImpliesSharedLocking"/>
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
            dbCmd.CommandText = "select count(*) from ChildOfFoo";
            Assert.Equal(2, await dbCmd.ExecuteScalarAsync());
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
        
        await fst.RequestAndAssertExecution("insert into ChildOfFoo (FooId) values (@p0);",
            new object?[] {new SqlParameter("p0", fstFooId)});
        await snd.AssertReplyTimeouts(); //snd is still blocked by fst tran
        
        await fst.RequestAndAssertExecution("commit transaction;"); //fst should become unblocked
        
        await snd.AssertPendingReplyAsserts(x => Assert.IsType<DbAnswer.Ok>(x));
        await snd.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where NullableText = 'nt';";
            Assert.Equal(0, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from ChildOfFoo;";
            Assert.Equal(3, await dbCmd.ExecuteScalarAsync());
        }
    }
    
    [Theory]
    [InlineData("read committed")]
    [InlineData("repeatable read")]
    [InlineData("serializable")]
    public async Task InPostgresSelectForShareIsSimilarToImplicitLockingOfSqlServerInNonSnapshotMode(
            string isolationLevel) {
        
        //fst wants some row to stay immutable; snd wants to change same row
        const DbToTest dbToTest = DbToTest.Postgresql;

        if (!DbToTestUtil.GetAllToBeTested().Contains(dbToTest)) {
            return; //skip test because of unavailability of sql server instance
        }
        
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);
        
        await using var fstConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var fst = await CreateThreadedWorker(fstConn);
        await fst.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");

        await using var sndConn = sut.DbConn.CreateAndOpenAnotherConnection();
        await using var snd = await CreateThreadedWorker(sndConn);
        await snd.RequestAndAssertExecution($"set transaction isolation level {isolationLevel};");
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where Nullable_Text = 'nt';";
            Assert.Equal(1L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Child_Of_Foo";
            Assert.Equal(2L, await dbCmd.ExecuteScalarAsync());
        }
        
        //
        // begin tran
        //
        await fst.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        await snd.RequestAndAssertExecution($"begin transaction isolation level {isolationLevel};");
        
        //
        // select same row requesting lock
        //
        var (fstFooId, fstNullableText) = await fst.RequestSingleRowAndAssertReply(
            $"select id,nullable_text from foo where nullable_text = 'nt' for share;", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")}
            );
        
        var (sndFooId, sndNullableText) = await snd.RequestSingleRowAndAssertReply(
            "select id,nullable_text from foo where nullable_text = 'nt' for share;", 
            x => x switch { 
                var y when 
                    y.TryGetValue("id", out var id) && id is int idInt && 
                    y.TryGetValue("nullable_text", out var nt) && nt is string ntStr => (idInt, ntStr), 
                _ => throw new Exception($"unexpected {nameof(DbAnswer.OkSingleRow)} content, got {x}")});  
        
        Assert.Equal(fstFooId, sndFooId);
        Assert.Equal(fstNullableText, sndNullableText);
        
        //snd will be blocked because fst implicitly locked Foo row 
        await snd.RequestAndAssertTimeout("update foo set nullable_text = 'zzz' where id = $1;", 
            new object?[] {sndFooId});
        
        await fst.RequestAndAssertExecution("insert into Child_Of_Foo (Foo_Id) values ($1);",
            new object?[] {fstFooId});
        await snd.AssertReplyTimeouts(); //snd is still blocked by fst tran
        
        await fst.RequestAndAssertExecution("commit transaction;"); //fst should become unblocked
        
        await snd.AssertPendingReplyAsserts(x => Assert.IsType<DbAnswer.Ok>(x));
        await snd.RequestAndAssertExecution("commit transaction;");
        
        //
        // both foo update AND child_of_foo inserts are persisted
        //
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Foo where nullable_text = 'nt';";
            Assert.Equal(0L, await dbCmd.ExecuteScalarAsync());
        }
        
        {
            await using var dbCmd = sut.DbConn.DbConn.CreateCommand();
            dbCmd.CommandText = "select count(*) from Child_Of_Foo;";
            Assert.Equal(3L, await dbCmd.ExecuteScalarAsync());
        }
    }
}
