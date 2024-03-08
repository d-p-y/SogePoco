using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Npgsql;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Tests.Extensions; 

public static class DbConnectionExtensions {
    public static async Task<bool> IsInTransaction(this DbConnection conn, DbTransaction? tran) =>
        conn switch {
            NpgsqlConnection c => IsInTransactionNpgsql(c),
            SqlConnection c => Convert.ToInt32(await c.ExecuteScalarAsync(tran, new SqlServerNaming(),
                @"select cast(case @@TRANCOUNT when 0 then 0 else 1 end	as bit)")) > 0,
            //http://www.sqlite.org/c3ref/get_autocommit.html
            SqliteConnection c => IsInTransactionSqlite(c),
            _ => throw new Exception("unsupported DbConnection")
        };

    private static bool IsInTransactionSqlite(SqliteConnection c) =>
        SQLitePCL.raw.sqlite3_get_autocommit(c.Handle) == 0;
        
    private static bool IsInTransactionNpgsql(NpgsqlConnection c) {
        //https://dba.stackexchange.com/questions/208363/how-to-check-if-the-current-connection-is-in-a-transaction
        //but won't work because of https://github.com/npgsql/npgsql/issues/1307
        //using following hack as workaround

        var connectorFld = 
            c.GetType().GetProperty("Connector", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception("bug: could not get 'Connector' property");
        var connector = connectorFld.GetValue(c);

        var inTransactionFld = 
            connector?.GetType().GetProperty("InTransaction", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception("bug: could not get 'InTransaction' property");
        var inTransaction = inTransactionFld.GetValue(connector)
                            ?? throw new Exception("bug: could not get 'InTransaction' property value");
            
        return (bool)inTransaction;
    }
}