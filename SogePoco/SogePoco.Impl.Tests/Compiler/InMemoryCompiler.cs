using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Tests.Compiler; 

public class InMemoryCompiler : IDisposable {
    public static int InstancesCount = 0;
    public static ILogger? Logger;
    private readonly IDictionary<string,InMemoryAssembly> DllNameToAssembly 
        = new Dictionary<string, InMemoryAssembly>();

    public InMemoryCompiler() {
        if (InstancesCount != 0) {
            throw new Exception(
                $"may only have one instance but already has {InstancesCount}. Dispose others before creating next instance");
        }
        InstancesCount++;
            
        //needed because Activator.CreateInstance*() / Assembly.CreateInstance*() looks for dependencies on disk
        AppDomain.CurrentDomain.AssemblyResolve += Resolver;
    }

    public void Dispose() {
        InstancesCount--;
        AppDomain.CurrentDomain.AssemblyResolve -= Resolver;
    }

    private Assembly? Resolver(object? sender, ResolveEventArgs args) {
        var i = args.Name.ToLower().IndexOf(".dll", StringComparison.InvariantCulture);
                
        var dllName = i < 0 
            ? throw new Exception($"no '.dll' in assembly name {args.Name}") 
            : args.Name.Substring(0, i + 4);
                
        Logger?.Log(LogLevel.Error, $"failure to resolve type {args.Name} in dll=[{dllName}] - trying to provide it");
        if (DllNameToAssembly.TryGetValue(dllName, out var res)) {
            Logger?.Log(LogLevel.Error, $"succeeded to provide {args.Name}");
            return res.ToAssembly();
        }
        Logger?.Log(LogLevel.Error, $"unable to provide {args.Name}");
        return null;
    }

    private IEnumerable<Assembly> GetDependencies(Assembly x) =>
        x.GetReferencedAssemblies()
            .Select(a => Assembly.Load(a));
        
    public InMemoryAssembly CompileToAssembly(
        string assemblyName, 
        ISet<SimpleNamedFile> inp, 
        Assembly[]? dependantAssemblies = null,
        InMemoryAssembly[]? inMemoryAssemblies = null) {
                
        var options = new CSharpParseOptions(LanguageVersion.Latest);
            
        var inputSourceCode = inp
            .Select(x => {
                Logger?.Log(LogLevel.Debug, $"Adding to compilation fileName={x.FileName} content={x.Content}");
                return CSharpSyntaxTree.ParseText(x.Content, options, x.FileName); })
            .ToList();

        var asmDeps =
            (dependantAssemblies ?? new Assembly[0])
            .Concat(new[] {
                typeof(object).Assembly,
                Assembly.GetEntryAssembly()! })
            .ToList();

        //needed to avoid System.Runtime / MarshalByRef errors https://github.com/dotnet/roslyn/issues/49498
        var implicitDeps =
            asmDeps
                .SelectMany(a => GetDependencies(a))
                .ToList();

        var fileBasedAssemblies =
            asmDeps.Concat(implicitDeps)
                .ToHashSet()
                .Select(a => MetadataReference.CreateFromFile(a.Location));

        var inMemDeps = 
            (inMemoryAssemblies ?? new InMemoryAssembly[0])
            .Select(x => x.ToMetadataReference());

        var dependencies = inMemDeps.Concat(fileBasedAssemblies);
            
        var compilation = CSharpCompilation.Create(
            assemblyName,
            inputSourceCode,
            dependencies,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                nullableContextOptions:NullableContextOptions.Enable));

        var ms = new MemoryStream();
        var result = new InMemoryAssembly(ms);
            
        var emitted = compilation.Emit(ms);

        if (emitted.Success) {
            DllNameToAssembly.Add(assemblyName, result);

            return result;
        }

        throw new Exception(
            $"Error compiling assembly={assemblyName} due to: " + 
            string.Join("\n", emitted.Diagnostics));
    }
}