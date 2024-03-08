using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SogePoco.Common;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Tests.Extensions;
using Xunit;

namespace SogePoco.Impl.Tests.QueryGeneration; 

public class GeneratedDatabaseClassHelper : IDisposable {
    private readonly ILogger _logger;
    private readonly Assembly _asm;
    private readonly GeneratorOptions _opt;
    private readonly object _dbClassInstance;

    public object WrappedInstance => _dbClassInstance;
	
    private GeneratedDatabaseClassHelper(ILogger logger, Assembly asm, GeneratorOptions opt, object dbClassInstance) {
        _logger = logger;
        _asm = asm;
        _opt = opt;
        _dbClassInstance = dbClassInstance;
    }

    public static GeneratedDatabaseClassHelper CreateInstance(
            ILogger logger, Assembly asm, GeneratorOptions opt, DbConnection dbConn, 
            Func<(Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue), bool> defaultsStrategy) {
			
        var dbClassInstance = asm.CreateInstance(opt.DatabaseClassFullName, dbConn, defaultsStrategy);
        Assert.NotNull(dbClassInstance);

        return new GeneratedDatabaseClassHelper(logger, asm, opt, dbClassInstance!);
    }

    public async Task Insert(object pocoInstance, string? customTypeName = null) {
        var pocoInstanceTypeName = 
            customTypeName 
            ?? pocoInstance.GetType().FullName
            ?? throw new Exception("could not infer needed poco type name");
			
        var insertMethod = 
            _dbClassInstance
                .GetType()
                .GetMethods(BindingFlags.Public|BindingFlags.Instance)
                .SingleOrDefault(x => 
                    x.Name == "Insert" && 
                    x.GetParameters().FirstOrDefault()?.ParameterType.FullName is {} tn &&
                    tn.Contains(pocoInstanceTypeName))
            ?? throw new Exception("bug: could not find Insert method with one parameter");

        var result = 
            insertMethod.Invoke(_dbClassInstance, new [] {pocoInstance})
            ?? throw new Exception("bug: Insert method returned null");

        await (dynamic)result;
    }

    public GeneratedDatabaseExtensionsClassHelper BuildExtensionsHelper() =>
        GeneratedDatabaseExtensionsClassHelper.BuildFor(this, _asm, _opt);

    public void Dispose() {
        if (_dbClassInstance is IDisposable x) {
            try {
                x.Dispose();
            } catch (Exception ex) {
                _logger.LogDebug("there was problem during disposal of inner dbconnection {ex}", ex);
            }
            
        }
    }
    
    public DbConnection GetDbConnectionOf() {
	    var sogePocoDatabase = WrappedInstance;
	    var fld = sogePocoDatabase.GetType().GetField(Consts.GeneratedDatabaseClassDbConnFieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
	    return (fld.GetValue(sogePocoDatabase) as DbConnection) 
	           ?? throw new Exception("unable to extract raw dbconn from sogepoco instance");
    }
}