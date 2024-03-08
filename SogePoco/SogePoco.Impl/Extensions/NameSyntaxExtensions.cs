using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SogePoco.Impl.Extensions; 

public static class NameSyntaxExtensions {
    public static string GetNameAsText(this NameSyntax self) {
        if (self is GenericNameSyntax gns) {
            return gns.Identifier.Text;
        } 
            
        if (self is IdentifierNameSyntax ins) {
            return ins.Identifier.Text;
        }
				                
        if (self is QualifiedNameSyntax qns) {
            return qns.GetFullName();
        }
            
        throw new Exception("unsupported Name type");
    }
}