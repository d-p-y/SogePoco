using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SogePoco.Impl.Extensions; 

public static class QualifiedNameSyntaxExtensions {
    public static string GetFullName(this QualifiedNameSyntax self) {
        var result = "";
            
        if (self.Left is IdentifierNameSyntax lins) {
            result += lins.Identifier.Text;
        } else if (self.Left is QualifiedNameSyntax lqns) {
            result += GetFullName(lqns);
        }

        result += self.DotToken.Text;
            
        result += self.Right.Identifier.Text;
        
        return result;
    }
}