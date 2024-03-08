using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public record RawWorkerController<T>(
    ITargetBlock<Func<T, DbAnswer>?> Request,
    ISourceBlock<DbAnswer> Reply,
    ThreadedWorker<T> Worker,
    ILogger Logger) : IAsyncDisposable where T:IDisposable {
    private bool _ended;
    
    public async ValueTask DisposeAsync() {
        Logger.LogDebug($"{nameof(DisposeAsync)} entering");
        
        if (!_ended) {
            try {
                await RequestShutdownAndAssertCompliance();
            } catch (Exception) {
                Logger.LogDebug($"{nameof(RequestShutdownAndAssertCompliance)} got exception while waiting for shutdown confirmation. ignoring it");
            }
        }

        Worker.Dispose();
    }

    public Task RequestShutdownAndAssertCompliance() {
        Logger.LogDebug($"{nameof(RequestShutdownAndAssertCompliance)} entering");
        
        Request.Post(null);
        
        var answer = Reply.Receive(DbAnswer.DefaultTimeout);
        if (answer is DbAnswer.Ended) {
            _ended = true;
        }

        Assert.IsType<DbAnswer.Ended>(answer);
        return Task.CompletedTask;
    }
    
    public Task RequestAndAssertReply(Func<T, DbAnswer>? func, Action<DbAnswer> assert) {
        Logger.LogDebug($"{nameof(RequestAndAssertReply)}(func,assert action) entering");
        
        Request.Post(func);

        try {
            Logger.LogDebug($"{nameof(RequestAndAssertReply)}(func,assert action) posted, about to call Receive with timeout");
            
            var answer = Reply.Receive(DbAnswer.DefaultTimeout);
            if (answer is DbAnswer.Ended) {
                _ended = true;
            }

            assert(answer);
            Logger.LogDebug($"{nameof(RequestAndAssertReply)}(func,assert action) leaving without timeout");
            return Task.CompletedTask;
        } catch (TimeoutException) {
            Logger.LogDebug($"{nameof(RequestAndAssertReply)}(func,assert action) leaving due to timeout");
            assert(new DbAnswer.Timeouted());
            return Task.CompletedTask;
        } catch (Exception ex) {
            Logger.LogDebug($"{nameof(RequestAndAssertReply)}(func,assert action) got nontimeout exception, propagating it {ex}");
            throw;
        }
    }
    
    public Task<DbAnswer> RequestAndAssertTimeout(Func<T, DbAnswer> func) {
        Logger.LogDebug($"{nameof(RequestAndAssertTimeout)}(func,assert action) entering");
        
        Request.Post(dbConn => func(dbConn));

        DbAnswer? answer;
        try {
            answer = Reply.Receive(DbAnswer.DefaultTimeout);
                
            if (answer is DbAnswer.Ended) {
                _ended = true;
            }
        } catch (TimeoutException) {
            return Task.FromResult<DbAnswer>(new DbAnswer.Timeouted());
        }
        throw new Exception($"not {nameof(DbAnswer.Timeouted)}, got {answer}");
    }
    
    public Task<U> RequestAndAssertReplyAndReturn<U>(Func<T, DbAnswer>? func, Func<DbAnswer,U> assert) {
        Logger.LogDebug($"{nameof(RequestAndAssertReplyAndReturn)}(func,assert func) entering");
        
        Request.Post(func);

        try {
            var answer = Reply.Receive(DbAnswer.DefaultTimeout);

            if (answer is DbAnswer.Ended) {
                _ended = true;
            }

            return Task.FromResult(assert(answer));
        } catch (TimeoutException) {
            return Task.FromResult(assert(new DbAnswer.Timeouted()));
        }
    }
    
    public Task AssertReplyTimeouts() {
        Logger.LogDebug($"{nameof(AssertReplyTimeouts)} entering");
        
        try {
            var answer = Reply.Receive(DbAnswer.DefaultTimeout);

            if (answer is DbAnswer.Ended) {
                _ended = true;
            }

            throw new Exception($"expected timeout but got {answer}");
        } catch (TimeoutException) {
            return Task.CompletedTask;
        }
    }
    
    public Task<U> AssertPendingReplyAsserts<U>(Func<DbAnswer,U> assert) {
        Logger.LogDebug($"{nameof(AssertPendingReplyAsserts)} entering");
        
        var answer = Reply.Receive(DbAnswer.DefaultTimeout);
        if (answer is DbAnswer.Ended) {
            _ended = true;
        }
        Logger.LogDebug($"{nameof(AssertPendingReplyAsserts)} received answer {answer}");
        
        return Task.FromResult(assert(answer));
    }
}
