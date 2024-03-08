using System;
using System.Reflection;

namespace SogePoco.Impl.Tests.Extensions; 

public static class AssemblyExtensions {

    public static Type GetTypeOrFail(this Assembly self, string typeName) => 
        self.GetType(typeName) ?? throw new Exception($"could not create type {typeName}");

    public static object? CreateInstance(
        this Assembly self, string fullClassName, params object[] args) =>
        self.CreateInstance(
            fullClassName,
            false, 
            BindingFlags.Public|BindingFlags.Instance,
            null,
            args,
            null, 
            null);
}