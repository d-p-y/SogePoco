using System;
using System.Data.Common;
using System.Reflection;

namespace SogePoco.Impl.Tests.Utils;

public static class NpgsqlTestingHelper {
    public static IDisposable CreateDisposableForcingDisconnectionOf(DbConnection conn) {
        var connector = 
            conn.GetType().GetProperty("Connector",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(conn)
            ?? throw new Exception("couldn't get Connector from NpgsqlConnection, likely DbConnection is not of NpgsqlConnection type");
        
        return 
            (IDisposable)(connector.GetType().GetField("_socket",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(connector)
            ?? throw new Exception("couldn't get _socket from NpgsqlConnector"));
    }
}
