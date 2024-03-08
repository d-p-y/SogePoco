using System;
using System.Data;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public enum ZeroOrException {
    Zero,
    DefaultException,
    CustomException
}

public static class ZeroOrExceptionExtensions {
    public static void ReactOnActualUpdateBehavior(this ZeroOrException strategy, ILogger logger, (object? Value, Exception? Ex) result) {
        logger.Log(LogLevel.Debug, $"ReactOnActualUpdateBehavior got{result}");
            
        switch (strategy) {
            case ZeroOrException.Zero:
                Assert.Equal(0L, Convert.ToInt64(result.Value));
                Assert.Null(result.Ex);
                break;
                
            case ZeroOrException.CustomException:
                Assert.Null(result.Value);
                Assert.NotNull(result.Ex);
                Assert.Equal(typeof(SomeException), result.Ex!.GetType());
                break;
                
            case ZeroOrException.DefaultException:
                Assert.Null(result.Value);
                Assert.NotNull(result.Ex);
                Assert.Equal(typeof(DBConcurrencyException), result.Ex!.GetType());
                break;
                
            default: throw new ArgumentException("unsupported "+nameof(ZeroOrException));
        }            
    }
}