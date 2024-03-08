using System;
using System.Threading;
using System.Threading.Tasks;

namespace SogePoco.Impl.Tests.Utils;

public static class TestingHacks {
	public static T RunAsyncAsSync<T>(Func<Task<T>> asyncFunc) { 
		//https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
		
		var taskF = new TaskFactory(
			CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
		
		return taskF
			.StartNew(asyncFunc)
			.Unwrap()
			.GetAwaiter()
			.GetResult();
	}
}
