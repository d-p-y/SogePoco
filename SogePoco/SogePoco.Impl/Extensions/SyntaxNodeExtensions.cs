using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SogePoco.Common;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Extensions; 

public static class SyntaxNodeExtensions {
    public static bool IsRequestingGenerateQueries(this SyntaxNode self, IConfiguration cfg) {
        var result = self is ClassDeclarationSyntax c &&
               c.AttributeLists.Any(al =>
                   al.Attributes.Any(a => a.Name.GetNameAsText().EndsWith(GenerateQueriesAttribute.ShortName)));

        cfg.Logger?.LogDebug($"{nameof(IsRequestingGenerateQueries)} node={self} result={result}");
        return result;
    }
    
    public static bool IsRequestingGenerateDatabaseClassAndPocos(this SyntaxNode self, IConfiguration cfg) {
        var result = self is ClassDeclarationSyntax c &&
               c.AttributeLists.Any(al =>
                   al.Attributes.Any(a => a.Name.GetNameAsText().EndsWith(GenerateDatabaseClassAndPocosAttribute.ShortName)));

        cfg.Logger?.LogDebug($"{nameof(IsRequestingGenerateDatabaseClassAndPocos)} node={self} result={result}");
        return result;
    }
}
