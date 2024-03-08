using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Tests.QueryGeneration;
using Xunit;

namespace SogePoco.Impl.Tests.DatabaseBehavior;

public static class RawWorkerControllerExtensions {
	public static ILogger? Logger;
	
	public static Task RequestAndAssertExecution(
			this RawWorkerController<GeneratedDatabaseClassHelper> self, Action<GeneratedDatabaseClassHelper> body) {
		
        Logger?.LogDebug($"{nameof(RequestAndAssertExecution)}(body) entering");
        return self.RequestAndAssertReply(dbConn => {
                body(dbConn);
                return new DbAnswer.Ok();
            },
            x => Assert.IsType<DbAnswer.Ok>(x));
    }
	
	public static Task<IEnumerable<IDictionary<string, object?>>> RequestAndAssertReplyAndReturn(
			this RawWorkerController<GeneratedDatabaseClassHelper> self, 
			Func<GeneratedDatabaseClassHelper,IEnumerable<IDictionary<string, object?>>> body) {
		
        Logger?.LogDebug($"{nameof(RequestAndAssertReplyAndReturn)}(body) entering");
        return self.RequestAndAssertReplyAndReturn(dbConn => 
		        new DbAnswer.OkSingleValue(body(dbConn)),
            x => {
	            var res = Assert.IsType<DbAnswer.OkSingleValue>(x);
	            return res.Val as IEnumerable<IDictionary<string, object?>> ?? throw new Exception("expected to get collection of dicts but got something else");
            });
    }
}
