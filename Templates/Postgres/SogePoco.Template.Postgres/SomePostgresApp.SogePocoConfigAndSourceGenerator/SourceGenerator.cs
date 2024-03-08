using Microsoft.CodeAnalysis;
using SogePoco.Impl.CodeGen;

namespace SomePostgresApp.SogePocoConfigAndSourceGenerator;

[Generator]
public class SourceGenerator : BaseSourceGenerator {
    public SourceGenerator() : base(new SogePocoPostgresConfig()) { }
}
