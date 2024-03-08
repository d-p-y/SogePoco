using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using Npgsql;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Tests.Compiler;
using SogePoco.Impl.Tests.Model;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.UsingMsBuild;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests; 

public abstract class BaseTest {
    private readonly ITestOutputHelper _outputHelper;
    protected readonly ILoggerProvider _loggerProvider;
    protected readonly ILogger Logger;
        
    public static IEnumerable<object[]> AllValuesOf_DbToTest {
        get => DbToTestUtil.GetAllToBeTested().Select(x => new object[] {x}); }

    protected BaseTest(ITestOutputHelper outputHelper) {
        _outputHelper = outputHelper;
        _loggerProvider = new XUnitLoggerProvider(outputHelper, new XUnitLoggerOptions());
        Logger = _loggerProvider.CreateLogger(GetType().FullName ?? nameof(BaseTest));

        LogTestStarting();
        
        GeneratedCodeUtil.Logger = _loggerProvider.CreateLogger(
            typeof(GeneratedCodeUtil).FullName ?? nameof(GeneratedCodeUtil));
        ProcessExec.Logger = _loggerProvider.CreateLogger(
            typeof(ProcessExec).FullName ?? nameof(ProcessExec)); 
        Sln.Logger = _loggerProvider.CreateLogger(
            typeof(Sln).FullName ?? nameof(Sln));
        QueryGenerationClasses.Logger = _loggerProvider.CreateLogger(
            typeof(QueryGenerationClasses).FullName ?? nameof(QueryGenerationClasses));
        InMemoryCompiler.Logger = _loggerProvider.CreateLogger(
            typeof(InMemoryCompiler).FullName ?? nameof(InMemoryCompiler));
        DbConnectionExtensions.Logger = _loggerProvider.CreateLogger(
            typeof(DbConnectionExtensions).FullName ?? nameof(DbConnectionExtensions));
        OnFinallyAction.Logger = _loggerProvider.CreateLogger(
            typeof(OnFinallyAction).FullName ?? nameof(OnFinallyAction));
        
        //really close connections as formerly "closed" connection goes to pool and is implicitly blocking database on the server side (e.g. drop database).
        //Without it will get error 55006
        NpgsqlConnection.ClearAllPools();

        //same behavior in sql server
        SqlConnection.ClearAllPools();
    }

    //source https://github.com/xunit/xunit/issues/416
    private void LogTestStarting() {
        var t = _outputHelper.GetType();
        var fld = t.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);

        var testName =
            fld?.GetValue(_outputHelper) switch {
                ITest {DisplayName:var dn} => $"testclass={GetType().FullName} test={dn}",
                _ => $"testclass={GetType().FullName} test UNKNOWN",
            };

        Logger.LogInformation($"starting test {testName}");
    }
}