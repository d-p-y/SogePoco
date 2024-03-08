using Microsoft.CodeAnalysis;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public class QueryGeneratorOfDelegates : IQueryGenerator {
    private readonly Action<Compilation,SyntaxNode> _visitor;
    private readonly Func<PocoSchema,ISet<SimpleNamedFile>> _generate;

    public QueryGeneratorOfDelegates(
        Action<Compilation, SyntaxNode> visitor,
        Func<PocoSchema,ISet<SimpleNamedFile>> generate) {
            
        _visitor = visitor;
        _generate = generate;
    }
        
    public void OnElement(Compilation c,SyntaxNode syntaxNode) => 
        _visitor(c, syntaxNode);

    public ISet<SimpleNamedFile> GenerateFiles(PocoSchema metaData) => _generate(metaData);
}