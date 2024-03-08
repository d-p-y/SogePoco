using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public class OnFinallyAction : IDisposable {
    public static ILogger? Logger;
    public static bool DefaultCreationValueForInvokeActionInFinallyEnabled { get; set; } = false;

    private readonly List<Action> _onFinallyInvokes = new ();
    public bool InvokeActionInFinallyEnabled { get; set; } = DefaultCreationValueForInvokeActionInFinallyEnabled;

    public OnFinallyAction() {
        Logger?.LogDebug("cleanup created");
    }
        
    public OnFinallyAction(Action onFinally) => Add(onFinally);

    public void Add(Action a) {
        _onFinallyInvokes.Add(a);
        Logger?.LogDebug($"cleanup added action. actionsCount is not {_onFinallyInvokes.Count}");
    }

    public void EnableInvokeActionInFinally() => InvokeActionInFinallyEnabled = true;

    public void Dispose() {
        var skip = 
            !InvokeActionInFinallyEnabled ||
            Debugger.IsAttached ||
            Environment.GetEnvironmentVariable("DISABLE_CLEANUP_AFTER_TESTS") != null;
            
        Logger?.LogDebug($"cleanup dispose actionsCount={_onFinallyInvokes.Count} skip?={skip}");
            
        if (skip) {
            return;
        }
            
        _onFinallyInvokes.ForEach(x => x());
    }
}