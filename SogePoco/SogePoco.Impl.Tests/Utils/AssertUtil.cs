using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Tests.Extensions;
using Xunit;

namespace SogePoco.Impl.Tests.Utils; 

public static class AssertUtil {
    public static void AssertSameEntities(
        ILogger logger,
        string sortByColumn, //TODO likely not needed as I always sort before check. verify
        IDictionary<string, object?> expected,
        IDictionary<string, object?> actual) =>
        AssertSameEntitiesColl(logger, sortByColumn, expected.AsSingletonCollection(), actual.AsSingletonCollection());

    public static void AssertSameEntitiesColl(
        ILogger logger,
        IEnumerable<IEnumerable<IDictionary<string, object?>>> expectedColl,
        IEnumerable<IEnumerable<IDictionary<string, object?>>> actualColl) {

        var expected = expectedColl.ToList();
        var actual = actualColl.ToList();
            
        logger.LogDebug($"Comparing entities collection sizes {expected.Count} =?= {actual.Count}");
        Assert.Equal(expected.Count, actual.Count);
            
        expected.ForEachI( (i,x) => {
            var e = x.ToList();
            var a = actual[i].ToList();
            logger.LogDebug($"Comparing tuple at index {i} {e.Count} =?= {a.Count}");
            AssertSameEntitiesColl(logger, null, e, a);
        });
    }

    public static void AssertSameEntitiesColl(
        ILogger logger,
        string? sortByColumn, 
        IEnumerable<IDictionary<string, object?>> expectedColl, 
        IEnumerable<IDictionary<string, object?>> actualColl) {

        var expected = expectedColl.ToList();
        var actual = actualColl.ToList();
            
        logger.LogDebug($"Comparing entities collection sizes {expected.Count} =?= {actual.Count}");
            
        logger.LogDebug($"Keys in 1st itm of expected {expected.FirstOrDefault()?.Keys.ConcatenateUsingComma()}");
        logger.LogDebug($"Keys in 1st itm of actual {actual.FirstOrDefault()?.Keys.ConcatenateUsingComma()}");

        Assert.Equal(expected.Count, actual.Count);

        if (sortByColumn is { } c) {
            expected = expected.OrderBy(x => x.Keys.Any() ? x[c] : null).ToList();
            actual = actual.OrderBy(x => x.Keys.Any() ? x[c] : null).ToList();
        }
            
        expected
            .Zip(actual)
            .Select(x => (
                First: x.First.Select(x => (x.Key, x.Value)).OrderBy(x => x.Key).ToList(),
                Second: x.Second.Select(x => (x.Key, x.Value)).OrderBy(x => x.Key).ToList() ) )
            .ForEachI((i,x) => {
                logger.LogDebug($"Comparing entity i={i} properties count {x.First.Count} =?= {x.Second.Count}");

                Assert.Equal(
                    x.First.Select(y => y.Key).ToList(), 
                    x.Second.Select(y => y.Key).ToList());
                    
                x.First
                    .Zip(x.Second)
                    .ForEachI((ip, y) => {
                            
                        if (y.First.Value is System.Collections.IEnumerable fvColl &&
                            y.Second.Value is System.Collections.IEnumerable svColl) {
                                
                            logger.LogDebug($"Comparing collectionvalued entity i={i} property={ip} first=({y.First.Key}={y.First.Value} of type {y.First.Value.BuildTypeName()}) =?= second=({y.Second.Key}={y.Second.Value} of type {y.Second.Value.BuildTypeName()})");
                            Assert.Equal(y.First.Key, y.Second.Key);
                            Assert.Equal(y.First.Value?.GetType(), y.Second.Value?.GetType());
                            Assert.Equal(fvColl, svColl);
                        } else {
                            logger.LogDebug($"Comparing noncollectionvalued entity i={i} property={ip} first=({y.First.Key}={y.First.Value} of type {y.First.Value.BuildTypeName()}) =?= second=({y.Second.Key}={y.Second.Value} of type {y.Second.Value.BuildTypeName()})");
                                
                            Assert.Equal(y.First.Key, y.Second.Key);
                            Assert.Equal(y.First.Value?.GetType(), y.Second.Value?.GetType());
                            Assert.Equal(y.First, y.Second);    
                        }
                    }); });
    }
}