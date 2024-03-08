namespace SogePoco.Impl.Extensions;

public static class ReadOnlyListExtensions {
    public static int IndexOf<T>(this IReadOnlyList<T> self, T itm) {
        var idx = 0;
        
        foreach (var x in self) {
            if (object.Equals(x, itm)) {
                return idx;
            }

            idx++;
        }

        return -1;
    }
    
    public static int IndexOfOrFail<T>(this IReadOnlyList<T> self, T itm) =>
        self.IndexOf(itm) switch {
            var x when x >= 0 => x,
            _ => throw new Exception("no such item on list")
        };
    
    public static int? TryIndexOf<T>(this IReadOnlyList<T> self, T itm) =>
        self.IndexOf(itm) switch {
            var x when x >= 0 => x,
            _ => null
        };
}
