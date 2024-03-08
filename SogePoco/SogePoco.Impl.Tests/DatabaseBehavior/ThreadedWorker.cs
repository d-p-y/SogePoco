using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public abstract class ThreadedWorker<DatabaseT> : IDisposable where DatabaseT : IDisposable  {
    private ISourceBlock<Func<DatabaseT, DbAnswer>?> Request { get; }
    private ITargetBlock<DbAnswer> Reply { get; }

    private readonly DatabaseT _dbConn;
    private readonly Func<DatabaseT, bool> _isOpen;
    private readonly Action<DatabaseT> _openAction;
    private readonly ILogger _logger;

    protected ThreadedWorker(
        DatabaseT connToOpen,
        Func<DatabaseT,bool> isOpen,
        Action<DatabaseT> openAction,
        ISourceBlock<Func<DatabaseT, DbAnswer>?> request, 
        ITargetBlock<DbAnswer> reply,
        ILogger logger) {
        
        _dbConn = connToOpen;
        _isOpen = isOpen;
        _openAction = openAction;
        _logger = logger;
        Request = request;
        Reply = reply;
    }
    
    public void StartProcessingInBackground() {
        _logger.LogDebug($"{nameof(StartProcessingInBackground)} starting");
        var worker = new Thread(() => {
            try {
                ProcessMessagesUnsafe();
            } catch (Exception ex) {
                _logger.LogDebug($"{nameof(StartProcessingInBackground)} dying due to exception {ex}");
            } finally {
                _logger.LogDebug($"{nameof(StartProcessingInBackground)} about to dispose dbConn");
                try {
                    _dbConn.Dispose();
                } catch (Exception) {
                    _logger.LogDebug($"{nameof(StartProcessingInBackground)} ignored problem withing dbConn dispose");
                }
            }
            
        });
        worker.IsBackground = true;
        worker.Start();
        _logger.LogDebug($"{nameof(StartProcessingInBackground)} ending");
    }

    private void ProcessMessagesUnsafe() {
        _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} starting");
        
        try {
            if (!_isOpen(_dbConn)) {
                _openAction(_dbConn);
            }
            Reply.Post(new DbAnswer.Started());
        } catch (Exception ex) {
            _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} got exception while opening connection {ex}");
            
            Reply.Post(new DbAnswer.Error($"error connecting {ex}"));
            Reply.Post(new DbAnswer.Ended());
            return;
        }

        try {
            while (true) {
                var req = Request.Receive();

                if (req == null) {
                    _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} received null");
                    break;
                }

                try {
                    _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} about to call req");
                    var answer = req(_dbConn);
                    Reply.Post(answer);
                    _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} calling req ended successfully");
                } catch (Exception ex) {
                    _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} got exception while calling req");
                    Reply.Post(new DbAnswer.Error($"error executing {ex}"));
                }
            }
        } catch (Exception ex) {
            _logger.LogDebug($"{nameof(ProcessMessagesUnsafe)} got exception causing while to be abandoned");
            Reply.Post(new DbAnswer.Error($"got exception in while {ex}"));
        } finally {
            Reply.Post(new DbAnswer.Ended());        
        }
    }

    public void Dispose() => _dbConn.Dispose();
}
