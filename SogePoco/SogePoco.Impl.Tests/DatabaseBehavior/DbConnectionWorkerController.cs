using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;
using Xunit;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public record DbConnectionWorkerController(
    ITargetBlock<Func<DbConnection, DbAnswer>?> Request, 
    ISourceBlock<DbAnswer> Reply, 
    ThreadedWorker<DbConnection> Worker, 
    ILogger Logger) : RawWorkerController<DbConnection>(Request, Reply, Worker, Logger)
{
    
    public Task RequestAndAssertExecution(Action<DbConnection> body) {
        Logger.LogDebug($"{nameof(RequestAndAssertExecution)}(body) entering");
        return RequestAndAssertReply(dbConn => {
                body(dbConn);
                return new DbAnswer.Ok();
            },
            x => Assert.IsType<DbAnswer.Ok>(x));
    }

    public Task RequestAndAssertTimeout(string sql, IEnumerable<object?>? sqlParms = null) {
        Logger.LogDebug($"{nameof(RequestAndAssertTimeout)}(sql) entering sql={sql}");
        
        return RequestAndAssertReply(dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;
                
                foreach (var sqlPrm in sqlParms ?? Array.Empty<object?>()) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }
                
                dbCmd.ExecuteNonQuery();
                return new DbAnswer.Ok();
            },
            x => Assert.IsType<DbAnswer.Timeouted>(x));
    }

    public Task RequestAndAssertExecution(string sql, IEnumerable<object?>? sqlParms = null) {
        Logger.LogDebug($"{nameof(RequestAndAssertExecution)}(sql) entering sql={sql}");
        
        return RequestAndAssertReply(dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;
                
                foreach (var sqlPrm in sqlParms ?? Array.Empty<object?>()) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }
                
                Logger.LogDebug($"{nameof(RequestAndAssertExecution)} about to ExecuteNonQuery() sql={sql}");
                try {
                    dbCmd.ExecuteNonQuery();
                    Logger.LogDebug($"{nameof(RequestAndAssertExecution)} successfully executed ExecuteNonQuery() sql={sql}");
                } catch (Exception ex) {
                    Logger.LogDebug($"{nameof(RequestAndAssertExecution)} failed to execute ExecuteNonQuery() sql={sql} due to {ex}");
                    throw;
                }
                
                return new DbAnswer.Ok();
            },
            x => Assert.IsType<DbAnswer.Ok>(x));
    }

    public Task RequestSingleRowAndAssertTimeout(string sql) =>
        RequestSingleRowAndAssertTimeout(sql, Array.Empty<object?>());
    
    public async Task RequestSingleRowAndAssertTimeout(string sql, IEnumerable<object?> sqlParms) {
        Logger.LogDebug($"{nameof(RequestSingleRowAndAssertTimeout)}(sql,params) entering; sql={sql}");

        await RequestAndAssertReplyAndReturn(
            dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;

                foreach (var sqlPrm in sqlParms) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }

                using var rdr = dbCmd.ExecuteReader();

                if (!rdr.HasRows || !rdr.Read()) {
                    throw new Exception("expected one row but got none");
                }

                var res = new Dictionary<string, object?>();
                
                for (var iCol = 0; iCol < rdr.FieldCount; iCol++) {
                    res.Add(rdr.GetName(iCol), rdr.GetValue(iCol));
                }
                
                return new DbAnswer.OkSingleRow(res);
            },
            x => {
                if (x is not DbAnswer.Timeouted) {
                    throw new Exception($"not {nameof(DbAnswer.Timeouted)}, got {x}");
                }

                return Task.CompletedTask;
            });
    }
    
    public Task<T> RequestSingleRowAndAssertReply<T>(string sql, Func<IDictionary<string, object?>, T> asserter) => 
        RequestSingleRowAndAssertReply(sql, Array.Empty<object?>(), asserter);

    public Task<T> RequestSingleRowAndAssertReply<T>(
        string sql, IEnumerable<object?> sqlParms, Func<IDictionary<string, object?>, T> asserter) {

        Logger.LogDebug($"{nameof(RequestSingleRowAndAssertReply)}(sql,params) entering; sql={sql}");

        return RequestAndAssertReplyAndReturn(
            dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;

                foreach (var sqlPrm in sqlParms) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }

                using var rdr = dbCmd.ExecuteReader();

                if (!rdr.HasRows || !rdr.Read()) {
                    throw new Exception("expected one row but got none");
                }

                var res = new Dictionary<string, object?>();
                
                for (var iCol = 0; iCol < rdr.FieldCount; iCol++) {
                    res.Add(rdr.GetName(iCol), rdr.GetValue(iCol));
                }
                
                return new DbAnswer.OkSingleRow(res);
            },
            x => asserter(
                x is DbAnswer.OkSingleRow {Row: var res}
                    ? res
                    : throw new Exception($"not {nameof(DbAnswer.OkSingleRow)}, got {x}")));
    }
    
    public Task<T> RequestScalarAndAssertReply<T>(string sql, Func<object?, T> asserter) {
        Logger.LogDebug($"{nameof(RequestScalarAndAssertReply)} entering");
        return RequestScalarAndAssertReply(sql, Array.Empty<object?>(), asserter);
    }
    
    public Task<T> RequestScalarAndAssertReply<T>(string sql, IEnumerable<object?> sqlParms, Func<object?,T> asserter) {
        Logger.LogDebug($"{nameof(RequestScalarAndAssertReply)}(sql,params) entering; sql={sql}");
        
        return RequestAndAssertReplyAndReturn(
            dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;

                foreach (var sqlPrm in sqlParms) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }

                return new DbAnswer.OkSingleValue(dbCmd.ExecuteScalar());
            },
            x => asserter(
                x is DbAnswer.OkSingleValue {Val: var res}
                    ? res
                    : throw new Exception($"not {nameof(DbAnswer.OkSingleValue)}, got {x}")));
    }

    public Task RequestScalarAndAssertTimeout(string sql) => RequestScalarAndAssertTimeout(sql, Array.Empty<object?>());

    public Task RequestScalarAndAssertTimeout(string sql, IEnumerable<object?> sqlParms) {
        Logger.LogDebug($"{nameof(RequestScalarAndAssertTimeout)}(sql,params) entering; sql={sql}");

        return RequestAndAssertReplyAndReturn(
            dbConn => {
                using var dbCmd = dbConn.CreateCommand();
                dbCmd.CommandText = sql;

                foreach (var sqlPrm in sqlParms) {
                    dbCmd.Parameters.Add(
                        sqlPrm switch {
                            DbParameter prm => prm,
                            var prmVal => dbCmd.CreateParameter().Also(x => x.Value = prmVal)
                        });
                }

                return new DbAnswer.OkSingleValue(dbCmd.ExecuteScalar());
            },
            x => {
                if (x is not DbAnswer.Timeouted) {
                    throw new Exception($"not {nameof(DbAnswer.Timeouted)}, got {x}");
                }

                return Task.CompletedTask;
            });
    }
}
