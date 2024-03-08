using Microsoft.CodeAnalysis;
using SogePoco.Impl.CodeGen;

namespace SomeSqliteApp.SogePocoConfigAndSourceGenerator;

[Generator]
public class SourceGenerator : BaseSourceGenerator {
    public SourceGenerator() : base(new SogePocoSqliteConfig()) { }
}
