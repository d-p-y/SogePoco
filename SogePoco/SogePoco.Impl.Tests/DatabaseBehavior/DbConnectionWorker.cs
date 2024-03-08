using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public class DbConnectionWorker : ThreadedWorker<DbConnection> {
    public DbConnectionWorker(
        DbConnection connToOpen,
        ISourceBlock<Func<DbConnection, DbAnswer>?> request, 
        ITargetBlock<DbAnswer> reply, ILogger logger) 
        : base(connToOpen, 
            x => x.State == ConnectionState.Open, 
            x => x.Open(), 
            request, reply, logger) { }
}
