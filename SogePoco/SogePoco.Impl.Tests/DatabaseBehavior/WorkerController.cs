using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public static class WorkerController {
    public static async Task<DbConnectionWorkerController> CreateDbConnectionWorker(
            ILoggerProvider loggerProvider, DbConnection connToOpen) {
        
        var req = new BufferBlock<Func<DbConnection, DbAnswer>?>();
        var resp = new BufferBlock<DbAnswer>();

        var worker = new DbConnectionWorker(
            connToOpen, req, resp, loggerProvider.CreateLogger(nameof(DbConnectionWorker)));
        worker.StartProcessingInBackground();
        var ctrl = new DbConnectionWorkerController(
            req, resp, worker, loggerProvider.CreateLogger(nameof(DbConnectionWorkerController)));
        
        var answer = await ctrl.Reply.ReceiveAsync(DbAnswer.DefaultTimeout);
        Assert.IsType<DbAnswer.Started>(answer);

        return ctrl;
    }
    
    public static async Task<RawWorkerController<T>> CreateRawWorker<T>(ILoggerProvider loggerProvider,
            T connToOpen, Func<T, bool> isOpen, Action<T> doOpen) where T:IDisposable {
        
        var req = new BufferBlock<Func<T, DbAnswer>?>();
        var resp = new BufferBlock<DbAnswer>();

        var worker = new RawWorker<T>(
            connToOpen, isOpen, doOpen, req, resp, 
            loggerProvider.CreateLogger(nameof(RawWorker<T>)));
        worker.StartProcessingInBackground();
        var ctrl = new RawWorkerController<T>(
            req, resp, worker, loggerProvider.CreateLogger(nameof(RawWorkerController<T>)));
        
        var answer = await ctrl.Reply.ReceiveAsync(DbAnswer.DefaultTimeout);
        Assert.IsType<DbAnswer.Started>(answer);

        return ctrl;
    }
}
