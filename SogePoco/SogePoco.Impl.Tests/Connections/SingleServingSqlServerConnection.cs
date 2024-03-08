using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Tests.Connections; 

/// <summary>dispose not guaranteed, for testing purposes only</summary>
public class SingleServingSqlServerConnection : SingleServingDbConnection {
    private SqlConnection _rawDbConn;
    private readonly string _connStr,_targetDbName;
    public DbConnection DbConn => _rawDbConn;
    public string InitialConnectionString => _connStr;
    public string TargetDatabaseName => _targetDbName;
    
    private SingleServingSqlServerConnection(string targetDbName) {
        _connStr = 
            Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTIONSTRING") 
            ??
            @"Data Source=127.0.0.1,14332;Initial Catalog=master;Trusted_Connection=False;User id=sa;Password=1234_some_sa_PASSWD;Connection Timeout=2;TrustServerCertificate=True";

        _targetDbName = targetDbName;
        _rawDbConn = new SqlConnection(_connStr);
        _rawDbConn.Open();
    }

    public DbConnection CreateAndOpenAnotherConnection() {
        var res = new SqlConnection(InitialConnectionString);
        res.Open();
        res.ChangeDatabase(TargetDatabaseName);
        return res;
    }

    public async static Task<SingleServingSqlServerConnection> Build(
            string testDatabaseName = "sogepoco_tester_db", 
            IReadOnlyCollection<string>? extraSchemaQueries = null) {
            
        var result = new SingleServingSqlServerConnection(testDatabaseName);
                
        var naming = new SqlServerNaming();

        //select DATABASEPROPERTY ( 'sogepoco_tester_db', 'IsAutoClose')
        await result.DbConn.ExecuteNonQueryAsync(naming, $"IF DB_ID('{testDatabaseName}') IS NOT NULL begin alter DATABASE {testDatabaseName} set auto_close off end");
        await result.DbConn.ExecuteNonQueryAsync(naming, $"IF DB_ID('{testDatabaseName}') IS NOT NULL begin drop database {testDatabaseName} end"); //unsafe sql building
        await result.DbConn.ExecuteNonQueryAsync(naming, $"create database {testDatabaseName}"); //unsafe sql building
        await result.DbConn.ExecuteNonQueryAsync(naming, $"alter DATABASE {testDatabaseName} set auto_close off"); //unsafe sql building
        await result.DbConn.ExecuteNonQueryAsync(naming, $"use {testDatabaseName}"); //unsafe sql building

        if (extraSchemaQueries is { } queries) {
            foreach (var query in queries) {
                await result.DbConn.ExecuteNonQueryAsync(naming, query); 
            }
        }
            
        return result;
    }

    public void Dispose() => _rawDbConn.Dispose();
}