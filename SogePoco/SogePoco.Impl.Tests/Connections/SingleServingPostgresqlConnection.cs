using System;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Tests.Connections; 

/// <summary>dispose not guaranteed, for testing purposes only</summary>
public class SingleServingPostgresqlConnection : SingleServingDbConnection {
    private NpgsqlConnection _rawDbConn;
    private readonly string _connStr,_targetDbName;
    public DbConnection DbConn => _rawDbConn;
    
    public string InitialConnectionString => _connStr;
    public string TargetDatabaseName => _targetDbName;
    
    public DbConnection CreateAndOpenAnotherConnection() {
        var res = new NpgsqlConnection(InitialConnectionString);
        res.Open();
        res.ChangeDatabase(TargetDatabaseName);
        return res;
    }

    private SingleServingPostgresqlConnection(string targetDatabaseName) {
        _connStr = 
            Environment.GetEnvironmentVariable("TEST_POSTGRESQL_CONNECTIONSTRING")
            ??
            "Host=127.0.0.1;Port=54332;Username=sogepoco_tester_user;Password=sogepoco_tester_passwd;Database=postgres";
        _targetDbName = targetDatabaseName;
        
        _rawDbConn = new NpgsqlConnection(_connStr);
        _rawDbConn.Open();
    }

    public static async Task<SingleServingPostgresqlConnection> Build(string testDatabaseName = "sogepoco_tester_db") {
        var result = new SingleServingPostgresqlConnection(testDatabaseName);
            
        var naming = new PostgresqlNaming();
        var dbDoesntExistRaw = await result.DbConn.ExecuteScalarAsync(naming,
            "SELECT 0 FROM pg_database WHERE datname=:p0", testDatabaseName);
        var dbExists = !(dbDoesntExistRaw is DBNull || dbDoesntExistRaw == null);

        if (dbExists) {
            await result.DbConn.ExecuteNonQueryAsync(naming, $"drop database {testDatabaseName}"); //unsafe sql building
        }

        await result.DbConn.ExecuteNonQueryAsync(naming, $"create database {testDatabaseName}"); //unsafe sql building
        await result.DbConn.ChangeDatabaseAsync(testDatabaseName);
        return result;
    }

    public void Dispose() {
        //This doesn't really close connection. Connection goes back to pool STILL referencing test database (on the server side).
        //Next test will fail because of it (error 55006).
        _rawDbConn.Close();
        _rawDbConn.Dispose();
    }
}