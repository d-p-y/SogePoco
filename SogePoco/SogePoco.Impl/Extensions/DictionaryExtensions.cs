namespace SogePoco.Impl.Extensions; 

public static class DictionaryExtensions {
    public static V GetValueOrInvoke<K,V>(this IDictionary<K,V> self, K needed, Func<V> otherwise) =>
        !self.TryGetValue(needed, out var result) ? otherwise() : result;
}