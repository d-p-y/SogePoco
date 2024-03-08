using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Tests.Extensions;

namespace SogePoco.Impl.Tests.QueryGeneration; 

public class GeneratedDatabaseExtensionsClassHelper {
    private readonly GeneratedDatabaseClassHelper _delegatesTo;
    private readonly Type _generatedQueryClass;

    private GeneratedDatabaseExtensionsClassHelper(GeneratedDatabaseClassHelper delegatesTo, Type generatedQueryClass) {
        _delegatesTo = delegatesTo;
        _generatedQueryClass = generatedQueryClass;
    }
		
    public static GeneratedDatabaseExtensionsClassHelper BuildFor(
        GeneratedDatabaseClassHelper delegatesTo, Assembly asm, GeneratorOptions opt) {
        var generatedQueryClass = asm.GetTypeOrFail($"{opt.DatabaseClassNameSpace}.{opt.DatabaseClassSimpleName}Extensions");
            
        return new GeneratedDatabaseExtensionsClassHelper(delegatesTo, generatedQueryClass!);
    }

    public string GetLastSqlText() => ((dynamic) _delegatesTo.WrappedInstance).LastSqlText;

    public Task<List<object?>> ExecuteGeneratedQuery(string generatedQueryName, Type returningInstancesOf) =>
        ExecuteGeneratedQueryWithArgs(generatedQueryName, returningInstancesOf, args:Array.Empty<object>());

    public async Task<List<(object?,object?)>> ExecuteGeneratedQuery(string generatedQueryName, Type returningTupleArg0, Type returningTupleArg1) =>
        (await ExecuteGeneratedQueryWithArgs(
            generatedQueryName, typeof(ValueTuple<,>).MakeGenericType(returningTupleArg0, returningTupleArg1),
            Array.Empty<object>()))
        .Select(x => 
            x switch {
                ITuple {Length:2} t => (t[0], t[1]),
                var y => throw new Exception($"expected to get Tuple2 but got something else {y}")
            })
        .ToList();

    public async Task<List<(object?,object?,object?)>> ExecuteGeneratedQuery(
        string generatedQueryName, Type returningTupleArg0, Type returningTupleArg1, Type returningTupleArg2) =>
        (await ExecuteGeneratedQueryWithArgs(
            generatedQueryName, typeof(ValueTuple<,,>).MakeGenericType(returningTupleArg0, returningTupleArg1, returningTupleArg2),
            Array.Empty<object>()))
        .Select(x => 
            x switch {
                ITuple {Length:3} t => (t[0], t[1], t[2]),
                var y => throw new Exception($"expected to get Tuple3 but got something else {y}")
            })
        .ToList();
    public async Task<List<(object?,object?,object?,object?)>> ExecuteGeneratedQuery(
        string generatedQueryName, Type returningTupleArg0, Type returningTupleArg1, Type returningTupleArg2, Type returningTupleArg3) =>
        (await ExecuteGeneratedQueryWithArgs(
            generatedQueryName, typeof(ValueTuple<,,,>).MakeGenericType(
                returningTupleArg0, returningTupleArg1, returningTupleArg2, returningTupleArg3),
            Array.Empty<object>()))
        .Select(x => 
            x switch {
                ITuple {Length:4} t => (t[0], t[1], t[2], t[3]),
                var y => throw new Exception($"expected to get Tuple4 but got something else {y}")
            })
        .ToList();
        
    public async Task<List<object?>> ExecuteGeneratedQueryWithArgs(
        string generatedQueryName, Type returningInstancesOf, object?[] args) {
            
        var generatedQueryMethod = 
            _generatedQueryClass.GetMethod(generatedQueryName, BindingFlags.Static|BindingFlags.Public)
            ?? throw new Exception($"could not find generated method {generatedQueryName}. Query generation skipped?");
            
        args = _delegatesTo.WrappedInstance
            .AsSingletonCollection()
            .Concat(args)
            .ToArray();
            
        var rawPocos = 
            generatedQueryMethod.Invoke(null, args)
            ?? throw new Exception("bug: executed query method returned null");
	        
        return await rawPocos
            .AsIAsyncEnumerableOfObject(returningInstancesOf)
            .ToListAsync();
    }
}