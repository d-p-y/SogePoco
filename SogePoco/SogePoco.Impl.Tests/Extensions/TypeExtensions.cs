using System;

namespace SogePoco.Impl.Tests.Extensions; 

public static class TypeExtensions {
    public static object CreateInstanceOrFail(this Type self) {
        var result = self.Assembly.CreateInstance(self.FullName!);
        return result!;
    }
}