namespace SogePoco.Impl.Extensions; 

public static class EnumerableExtensions {
        
    //source generator forces netstandard2.0 while ToHashSet() is available only since netstandard2.1
    //https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.tohashset?view=net-6.0
    public static HashSet<T> ToSet<T>(this IEnumerable<T> self) => new (self);

    public static void ForEach<T>(this IEnumerable<T> self, Action<T> a) {
        foreach (var itm in self) {
            a(itm);
        }
    }
        
    public static void ForEachI<T>(this IEnumerable<T> self, Action<int,T> a) {
        var i = 0;
        foreach (var itm in self) {
            a(i++, itm);
        }
    }

    public static IEnumerable<OutT> SelectI<InpT, OutT>(this IEnumerable<InpT> self, Func<int, InpT, OutT> f) {
        var i = 0;
        foreach (var itm in self) {
            yield return f(i++, itm);
        }
    }
        
    public static IEnumerable<OutT> WhereIsOfType<OutT>(this System.Collections.IEnumerable self) => self.OfType<OutT>();
    public static IEnumerable<T> WhereIsNotNull<T>(this IEnumerable<T?> self) => self.Where(x => x != null).Select(x => (T)x!);
        
    public static IEnumerable<T>? TryTake<T>(this IEnumerable<T> self, int count) {
        var result = self.Take(count).ToList();
        return result.Count != count ? null : result;
    }

    public static (T?, IEnumerable<T>) MaybeHeadAndTail<T>(this IEnumerable<T> self) {
        var inp = self.ToList();
        
        return inp.Any()
            ? (inp.FirstOrDefault(), inp.Skip(1))
            : (default(T), Array.Empty<T>());
    }

    public static Tuple<T,T>? FirstAndSecondOrDefault<T>(this IEnumerable<T> self) {
        var inp = self.ToList();

        return inp.Count >= 2
            ? Tuple.Create(inp[0], inp[1])
            : null;
    }
    
    /// <summary>preserves order</summary>
    public static (IEnumerable<T> Matched,IEnumerable<T> Unmatched) Partition<T>(this IEnumerable<T> self, Func<T,bool> matches) {
        //reportedly oLookup doesn't preserve order https://stackoverflow.com/questions/204505/preserving-order-with-linq 
        //return self.ToLookup(matches).Map(x => (x[true], x[false]));

        var intermediate = self.Select(x => (matches:matches(x), item:x)).ToList();

        return (
            intermediate.Where(x => x.matches).Select(x => x.item),
            intermediate.Where(x => !x.matches).Select(x => x.item));
    }

    public static string Concatenate(this IEnumerable<string> self) => string.Join("", self);
    public static string ConcatenateUsingComma(this IEnumerable<string> self) => string.Join(",", self);
    public static string ConcatenateUsingNewLine(this IEnumerable<string> self) => string.Join("\n", self);
    public static string ConcatenateUsingSep(this IEnumerable<string> self, string separator) =>
        string.Join(separator, self);
}