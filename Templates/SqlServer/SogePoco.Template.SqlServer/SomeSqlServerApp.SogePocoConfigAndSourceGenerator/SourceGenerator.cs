using Microsoft.CodeAnalysis;
using SogePoco.Impl.CodeGen;

namespace SomeSqlServerApp.SogePocoConfigAndSourceGenerator;

[Generator]
public class SourceGenerator : BaseSourceGenerator {
    public SourceGenerator() : base(new SogePocoSqlServerConfig()) { }
}
