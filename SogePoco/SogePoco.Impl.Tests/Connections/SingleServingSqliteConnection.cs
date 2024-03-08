using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SogePoco.Impl.Tests.Connections; 

public class SingleServingSqliteConnection : SingleServingDbConnection {
    public static bool ShouldUseInMemory => 
        System.Environment.GetEnvironmentVariable("SQLITE_USE_DISK") == null && !Debugger.IsAttached; 
        
    private SqliteConnection _rawDbConn;
    private readonly string _connStr,_targetDbName;
    public DbConnection DbConn => _rawDbConn;
    public string InitialConnectionString => _connStr;
    public string TargetDatabaseName => _targetDbName;
    
    public DbConnection CreateAndOpenAnotherConnection() {
        var res = new SqliteConnection(InitialConnectionString);
        res.Open();
        return res;
    }

    private SingleServingSqliteConnection(string? targetDbName) {
        string pth;
            
        if (targetDbName != null) {
            var tmpDir = System.Environment.GetEnvironmentVariable("TMPDIR");

            if (tmpDir == null && Directory.Exists("/tmp")) {
                tmpDir = "/tmp";
            }
                
            if (tmpDir == null) {
                throw new ArgumentException("TEMP environment is not available");
            }
                
            pth = Path.Combine(tmpDir, targetDbName);
                
            if (File.Exists(pth)) {
                File.Delete(pth);
            }
        } else {
            pth = ":memory:";
        }

        _connStr = $"Data Source={pth};Cache=Shared";
        _targetDbName = pth;
        _rawDbConn = new SqliteConnection(_connStr);
        _rawDbConn.Open();
    }

    public static Task<SingleServingSqliteConnection> Build(string testDatabaseName = "sogepoco_tester_db") => 
        Task.FromResult(new SingleServingSqliteConnection(ShouldUseInMemory ? null : testDatabaseName));

    public void Dispose() {
        _rawDbConn.Dispose();
    }
}