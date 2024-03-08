using System;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public class RawWorker<T> : ThreadedWorker<T> where T:IDisposable {
    public RawWorker(
        T connToOpen,
        Func<T,bool> isOpen, 
        Action<T> doOpen,
        ISourceBlock<Func<T, DbAnswer>?> request, 
        ITargetBlock<DbAnswer> reply, ILogger logger) 
        : base(connToOpen, isOpen, doOpen, request, reply, logger) { }
}
