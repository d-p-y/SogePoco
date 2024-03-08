using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Model;
using SogePoco.Impl.Tests.Compiler;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests; 

public class TestsInMemoryCompilation : BaseTest {
    public TestsInMemoryCompilation(ITestOutputHelper outputHelper) : base(outputHelper) { }
        
    [Fact]
    public void CompiledInMemoryDllIsInvokable() {
        using var compiler = new InMemoryCompiler();
        var asm = compiler.CompileToAssembly("PocoClasses", 
            new HashSet<SimpleNamedFile> {
                new SimpleNamedFile("file.cs", @"
    namespace PocoClasses { 
        public class Foo {  
            public override string ToString() => ""hello"";
        } 
    }") }).ToAssembly();
        var t = asm.GetType("PocoClasses.Foo");
            
        Assert.NotNull(t);
        var tInstance = Activator.CreateInstance(t!);
            
        Assert.NotNull(tInstance);
        Assert.Equal("hello", tInstance!.ToString());
    }
        
    [Fact]
    public void CompileInMemoryInterdependentDllsAreInvokable() {
        using var compiler = new InMemoryCompiler();
        var asmBase = compiler.CompileToAssembly("Base.dll", 
            new HashSet<SimpleNamedFile> {
                new SimpleNamedFile("file.cs", @"
namespace Base { 
    public class Foo {  
        public override string ToString() => ""hello"";
    } 
}") });

        Logger.Log(LogLevel.Debug, "compiled 1st");

        var asmRequiringBase = compiler.CompileToAssembly("RequiringBase.dll", 
            new HashSet<SimpleNamedFile> {
                new SimpleNamedFile("file.cs", @"
namespace RequiringBase { 
    public class Bar {
        public Base.Foo P { get; } = new Base.Foo();   
        public override string ToString() => ""worked"";
    } 
}") }, inMemoryAssemblies:new []{asmBase});

        Logger.Log(LogLevel.Debug, "compiled 2nd");
            
        {
            var a = asmBase.ToAssembly();
                
            Assert.NotNull(a);
                
            var tInstance = a.CreateInstance("Base.Foo");
            Assert.NotNull(tInstance);    
                
            Assert.Equal("hello", tInstance!.ToString());
        }
            
        {
            var a = asmRequiringBase.ToAssembly();

            var tInstance = a.CreateInstance("RequiringBase.Bar");
            
            Assert.NotNull(tInstance);
            Assert.Equal("worked", tInstance!.ToString());    
        }
    }
}