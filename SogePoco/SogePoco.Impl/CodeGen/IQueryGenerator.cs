using Microsoft.CodeAnalysis;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public interface IQueryGenerator {
    void OnElement(Compilation c, SyntaxNode syntaxNode);
    ISet<SimpleNamedFile> GenerateFiles(PocoSchema metaData);
}