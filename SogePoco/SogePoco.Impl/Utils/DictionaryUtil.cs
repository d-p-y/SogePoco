using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.Utils; 

public static class DictionaryUtil {
    public static IDictionary<K,V> BuildFromManyFailOnDuplicateKey<K,V>(params IDictionary<K,V>[] inp) {
        var result = new Dictionary<K, V>();

        inp.ForEach(x => {
            x.ForEach(y => result.Add(y.Key, y.Value)); });
            
        return result;
    }

    public static IDictionary<K, V> Empty<K, V>() => new Dictionary<K, V>();
}