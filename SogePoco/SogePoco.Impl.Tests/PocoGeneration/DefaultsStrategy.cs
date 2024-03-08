using System;
using SogePoco.Impl.Tests.Extensions;
using DefaultableColumnShouldInsert = System.Func<(System.Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue),bool>;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public enum DefaultsStrategy {
    NeverInsert,
    AlwaysInsert,
    InsertOnlyWhenSeemsNotDotnetDefault
}

public static class DefaultsStrategyExtensions {
    public static DefaultableColumnShouldInsert CreateDefaultableColumnShouldInsert(this DefaultsStrategy self) =>
        self switch {
            DefaultsStrategy.NeverInsert => _ => false,
            DefaultsStrategy.AlwaysInsert => _ => true,
            DefaultsStrategy.InsertOnlyWhenSeemsNotDotnetDefault => x => {
                var res = 
                    x.poco.GetProperty(x.pocoPropertyName)?.PropertyType
                    ?? throw new Exception("bug: problem getting property type out of poco");
                
                var resFullName = res.FullName ?? throw new Exception("bug: problem getting fullname of restype");
                
                object? def;
                if (!res.IsValueType) {
                    def = null;
                } else {
                    def = Activator.CreateInstance(res) ?? x.poco.Assembly.CreateInstance(resFullName, Array.Empty<object>());
                    if (def == null) {
                        throw new Exception($"tried to activate value type {res.FullName} but got null");
                    }
                }

                return def == null && x.pocoPropertyValue != def ||
                       def != null && !def.Equals(x.pocoPropertyValue); },
            _ => throw new NotImplementedException($"unsupported {nameof(DefaultsStrategy)}")
        };
}
