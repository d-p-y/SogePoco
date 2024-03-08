using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Connections;
using SogePoco.Impl.Tests.Schemas;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public enum DbToTest {
    Sqlite,
    Postgresql,
    SqlServer
}

public static class DbToTestUtil {
    public static IEnumerable<DbToTest> GetAllToBeTested() {
        if (Environment.GetEnvironmentVariable("TEST_SKIP_SQLITE") is null) {
            yield return DbToTest.Sqlite;
        }
            
        if (Environment.GetEnvironmentVariable("TEST_SKIP_POSTGRES") is null) {
            yield return DbToTest.Postgresql;
        }
            
        if (Environment.GetEnvironmentVariable("TEST_SKIP_SQLSERVER") is null) {
            yield return DbToTest.SqlServer;
        }
    }
}
    
public static class SystemUnderTestFactory {
    public static async Task<SystemUnderTest> Create(DbToTest db) =>
        db switch {
            DbToTest.Postgresql => await CreatePostgresql(),
            DbToTest.Sqlite => await CreateSqlite(),
            DbToTest.SqlServer => await CreateSqlServer(true),
            _ => throw new ArgumentOutOfRangeException(nameof(db), db, null) };

    public static async Task<SystemUnderTest> CreateSqlite() {
        var conn = await SingleServingSqliteConnection.Build();
            
        return new(new DefaultCodeConvention(), conn, new SqliteTestingSchema(), new SqliteMapper(), 
            new SqliteNaming(), new SqliteSchemaExtractor(), x => Convert.ToInt64(x));
    }

    public static async Task<SystemUnderTest> CreatePostgresql() {
        var conn = await SingleServingPostgresqlConnection.Build();
            
        return new(new DefaultCodeConvention(), conn, new PostgresqlTestingSchema(), new PostgresqlMapper(),
            new PostgresqlNaming(), new PostgresqlSchemaExtractor(), x => x);
    }

    public static async Task<SystemUnderTest> CreateSqlServer(bool withArraysSupport) {
        var extraSchemaQueries = 
            withArraysSupport ? new List<string> {
                    "CREATE TYPE ArrayOfInt AS TABLE(V int NULL)",
                    "CREATE TYPE ArrayOfString AS TABLE(V nvarchar(255) NULL)" }
                : new List<string>();
            
        Func<DotnetTypeDescr,SqlServerUserDataTypeInfo?> userDataTypeInfoForArrayParameter =
            withArraysSupport 
                ? x => x.NamespaceAndGenericClassName switch {
                    "int[]" => new SqlServerUserDataTypeInfo("ArrayOfInt", "V"),
                    "int?[]" => new SqlServerUserDataTypeInfo("ArrayOfInt", "V"),
                    "string[]" => new SqlServerUserDataTypeInfo("ArrayOfString", "V"),
                    "string?[]" => new SqlServerUserDataTypeInfo("ArrayOfString", "V"),
                    _ => null}
                : _ => null;
            
        var conn = await SingleServingSqlServerConnection.Build(extraSchemaQueries:extraSchemaQueries);
            
        return new (new DefaultCodeConvention(), conn, new SqlServerTestingSchema(), 
            new SqlServerMapper(userDataTypeInfoForArrayParameter), new SqlServerNaming(), 
            new SqlServerSchemaExtractor(), x =>x);
    }
}