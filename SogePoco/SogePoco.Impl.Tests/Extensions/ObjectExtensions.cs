using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SogePoco.Common;
using Xunit;

namespace SogePoco.Impl.Tests.Extensions; 

public static class ObjectExtensions {
    public static IDictionary<K,V> DowncastCollectionToInterface<K,V>(this Dictionary<K,V> self) where K : notnull => self;

    public static async IAsyncEnumerable<object?> AsIAsyncEnumerableOfObject(this object self, Type t) {
        var iasyncEnumerable = typeof(IAsyncEnumerable<>);
            
        var iasyncEnumerableFoo = iasyncEnumerable.MakeGenericType(t);
            
        var iasyncEnumerator = typeof(IAsyncEnumerator<>);
        var iasyncEnumeratorFoo = iasyncEnumerator.MakeGenericType(t);
        Assert.NotNull(iasyncEnumerableFoo);

        var getAsyncEnumerator = iasyncEnumerableFoo!.GetMethod("GetAsyncEnumerator");
        Assert.NotNull(getAsyncEnumerator);
            
        var enumerator = getAsyncEnumerator!.Invoke(self, new object?[]{null});

        var moveNextAsync = iasyncEnumeratorFoo.GetMethod("MoveNextAsync");
        Assert.NotNull(moveNextAsync);
            
        var current = iasyncEnumeratorFoo.GetProperty("Current");
        Assert.NotNull(current);
            
        while (true) {
            var succ = (ValueTask<bool>)moveNextAsync!.Invoke(enumerator, new object?[0])!;

            if (!(await succ)) {
                yield break;
            }

            var val = current!.GetValue(enumerator);
            yield return val;
        }
    }
        
    public static string BuildTypeName(this object? inp) => (inp == null ? "null" : (inp.GetType().FullName) ?? "unknown");

    public static object? GetPropertyValue(this object? self, string propertyName) =>
        self switch {
            null => throw new ArgumentException("cannot get type from null object"),
            var x => x.GetType().GetProperty(propertyName) switch {
                null => throw new ArgumentException($"nonnull object doesn't have property {propertyName}"),
                var y => y.GetValue(x) }};

    public static void SetPropertyValue(this object? self, string propertyName, object? valueForProperty) {
        if (self is null) {
            throw new ArgumentException("cannot get type from null object");
        }

        var t = self.GetType().GetProperty(propertyName);

        if (t is null) {
            throw new ArgumentException($"nonnull object doesn't have property {propertyName}");
        }

        t.SetValue(self, valueForProperty);
    }
        
    public static Dictionary<string, object?> ItemToPropertyNameAndValueDict(this object? x, bool autoSkipForeignKeys = true) =>
        x == null 
            ? new Dictionary<string, object?>() 
            :
            x.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(y => !autoSkipForeignKeys || y.Name != Consts.ForeignKeysPropertyName)
                .Select(p => (p.Name, value: p.GetValue(x)))
                .ToDictionary(k => k.Name, v => v.value);
}