namespace SogePoco.Impl.Extensions; 

public static class ObjectExtensions {
    public static O Map<I, O>(this I self, Func<I, O> map) => map(self);
    public static IReadOnlyCollection<T> AsSingletonCollection<T>(this T inp) => new T[] {inp};
    public static IReadOnlyList<T> AsSingletonList<T>(this T inp) => new T[] {inp};
    public static ISet<T> AsSingletonSet<T>(this T inp) => new HashSet<T>().Also(x => x.Add(inp));
        
    /// <summary>similar idea to https://kotlinlang.org/api/latest/jvm/stdlib/kotlin/also.html</summary>
    public static T Also<T>(this T self, Action<T> action) {
        action(self);
        return self;
    }
        
    /// <summary>similar idea to https://kotlinlang.org/api/latest/jvm/stdlib/kotlin/with.html</summary>
    public static O With<I,O>(this I self, Func<I, O> map) => map(self);
    
    public static T IfTrueThenAlso<T>(this T self, Func<bool> whenTrue, Action<T> action) {
        if (whenTrue()) {
            action(self);
        }

        return self;
    }
}