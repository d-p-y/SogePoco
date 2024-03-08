using System.Collections.Generic;
using System.Linq;

namespace SogePoco.Impl.Tests.Extensions; 

public static class EnumerableExtensions {
        public static List<Dictionary<string, object?>> ToPropertyNameAndValueDict(this IEnumerable<object?> self, bool autoSkipForeignKeys = true) =>
            self.Select(x => 
                    x == null 
                    ? new Dictionary<string, object?>() 
                    : ObjectExtensions.ItemToPropertyNameAndValueDict(x, autoSkipForeignKeys:autoSkipForeignKeys))
                .ToList();
        
        public static IReadOnlyList<IReadOnlyList<Dictionary<string, object?>>> TupleToPropertyNameAndValueDict(
            this IEnumerable<(object?,object?)> self, bool autoSkipForeignKeys = true) =>
            self.Select( x =>
                    new [] {
                        x.Item1 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item1, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item2 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item2, autoSkipForeignKeys:autoSkipForeignKeys)
                    })
                .ToList();
        
        public static IReadOnlyList<IReadOnlyList<Dictionary<string, object?>>> TupleToPropertyNameAndValueDict(
            this IEnumerable<(object?,object?,object?)> self, bool autoSkipForeignKeys = true) =>
            self.Select( x =>
                    new [] {
                        x.Item1 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item1, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item2 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item2, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item3 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item3, autoSkipForeignKeys:autoSkipForeignKeys)
                    })
                .ToList();
        
        public static IReadOnlyList<IReadOnlyList<Dictionary<string, object?>>> TupleToPropertyNameAndValueDict(
            this IEnumerable<(object?,object?,object?,object?)> self, bool autoSkipForeignKeys = true) =>
            self.Select( x =>
                    new [] {
                        x.Item1 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item1, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item2 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item2, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item3 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item3, autoSkipForeignKeys:autoSkipForeignKeys),
                        x.Item4 == null 
                        ? new Dictionary<string, object?>() 
                        : ObjectExtensions.ItemToPropertyNameAndValueDict(x.Item4, autoSkipForeignKeys:autoSkipForeignKeys)
                    })
                .ToList();
}
