using System.Reflection;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.CodeGen; 

public record DotnetTypeDescr(
    string? NameSpace, string BaseDotnetClassName, bool? MaybeIsNullable, 
    IReadOnlyCollection<DotnetTypeDescr> GenericArgs, bool IsArray) {
        
    public bool IsNullable => 
        MaybeIsNullable.HasValue && MaybeIsNullable.Value || NameSpace == "System" && BaseDotnetClassName == "Nullable";

    public string NamespaceAndClassNameAndMaybeArray =>
        (NameSpace != null ? $"{NameSpace}." : "") +
        BaseDotnetClassName + 
        (IsArray ? "[]" : string.Empty);

    public string ArrayItemType => IsArray
        ? ((NameSpace != null ? $"{NameSpace}." : "") + BaseDotnetClassName)
        : throw new Exception("type is not an array");
        
    public string NamespaceAndGenericClassName =>
        GenericArgs.Any() ? NamespaceAndGenericClassNameGeneric : NamespaceAndGenericClassNameNonGeneric; 

    private string NamespaceAndGenericClassNameGeneric =>
        (NameSpace != null ? $"{NameSpace}." : "") + 
        BaseDotnetClassName +
        $"<{GenericArgs.Select(a => a.NamespaceAndGenericClassName).ConcatenateUsingComma()}>" + 
        (IsArray ? "[]" : string.Empty);

    private string NamespaceAndGenericClassNameNonGeneric =>
        (NameSpace != null ? $"{NameSpace}." : "") + 
        BaseDotnetClassName +
        (IsArray ? "[]" : string.Empty) +
        MaybeIsNullable switch {
            null => "",
            var b => b.Value ? "?" : ""};

    private static readonly IDictionary<string,string> PrimitiveFullNameToAlias = new Dictionary<string,string> {
        {"System.Boolean", "bool"},
        {"System.Boolean[]", "bool[]"},
        {"System.Boolean?", "bool?"},
        {"System.Boolean?[]", "bool?[]"},
        {"System.Int32", "int"},
        {"System.Int32[]", "int[]"},
        {"System.Int32?", "int?"},
        {"System.Int32?[]", "int?[]"},
        {"System.Int64", "long"},
        {"System.Int64[]", "long[]"},
        {"System.Int64?", "long?"},
        {"System.Int64?[]", "long?[]"},
        {"System.Decimal", "decimal"},
        {"System.Decimal[]", "decimal[]"},
        {"System.Decimal?", "decimal?"},
        {"System.Decimal?[]", "decimal?[]"},
        {"System.String", "string"},
        {"System.String[]", "string[]"},
        {"System.String?", "string?"},
        {"System.String?[]", "string?[]"}
    };
        
    private static readonly Assembly PrimitivesAssembly = typeof(int).Assembly; //safe assumption for the moment...
            
    public static DotnetTypeDescr CreateOf(string classNameMaybeWithNamespace, bool nullable) {
        var iNsEnd = classNameMaybeWithNamespace.LastIndexOf(".", StringComparison.InvariantCulture);

        var isArray = classNameMaybeWithNamespace.EndsWith("[]");

        if (isArray) {
            classNameMaybeWithNamespace = classNameMaybeWithNamespace.Substring(0, classNameMaybeWithNamespace.Length - 2);
        }
        
        DotnetTypeDescr result = iNsEnd < 0
            ? new(
                null,
                classNameMaybeWithNamespace,
                nullable,
                Array.Empty<DotnetTypeDescr>(),
                isArray)
            : new(
                classNameMaybeWithNamespace.Substring(0, iNsEnd),
                classNameMaybeWithNamespace.Substring(iNsEnd + 1),
                nullable,
                Array.Empty<DotnetTypeDescr>(),
                isArray);

        return result.AsSimplified();
    }

    public static DotnetTypeDescr CreateOf(Type inp) {
        var isArray = inp.IsArray;

        if (isArray) {
            inp = 
                inp.GetElementType() 
                ?? throw new Exception("array type but GetElementType() returned null");
        }
            
        return (inp switch {
            _ when inp.IsGenericType => new DotnetTypeDescr(
                inp.Namespace,
                inp.Name,
                null,
                inp.GenericTypeArguments
                    .Select(x => DotnetTypeDescr.CreateOf(x))
                    .ToArray(),
                IsArray: isArray),
            _ => new DotnetTypeDescr(
                inp.Namespace, inp.Name, false, Array.Empty<DotnetTypeDescr>(),
                IsArray: isArray)
        }).AsSimplified();
    }

    private DotnetTypeDescr AsSimplified() =>
        NamespaceAndClassNameAndMaybeArray switch {
            "System.Nullable" => //example: System.Nullable<int>
                CreateOf(
                    CreateOf(GenericArgs.Single().NamespaceAndClassNameAndMaybeArray, nullable:false)
                        .NamespaceAndClassNameAndMaybeArray, 
                    nullable:true),
            "System.Nullable[]" => //example: System.Nullable<int>[] 
                CreateOf(
                    CreateOf(
                            GenericArgs.Single().NamespaceAndClassNameAndMaybeArray, nullable:true)
                        .NamespaceAndClassNameAndMaybeArray, 
                    nullable:false),
            var x when PrimitiveFullNameToAlias.TryGetValue(x, out var shortName) => CreateOf(shortName, x.EndsWith("?")),
            _ => this
        };

    public static DotnetTypeDescr CreateOfBuiltinType(
        List<string> usings, 
        string? containingNamespace, 
        string typeName, 
        IReadOnlyCollection<string>? genericArgs,
        bool isArray) {

        var genericArgs2 = genericArgs switch {
            null => new List<DotnetTypeDescr>(),
            var x => x.Select(genericArg => 
                    GetBuiltinFullTypeNameOrNull(usings, genericArg) switch {
                        null => throw new Exception("could not identify namespace for type's generic argument"),
                        var (ns, tn) => CreateOf(ns == null ? tn : $"{ns}.{tn}", nullable:false)})
                .ToList() };
            
        var result = containingNamespace switch {
            null => GetBuiltinFullTypeNameOrNull(usings, typeName) switch {
                null => throw new Exception("could not identify namespace for type utilizing 'global namespace'"),
                var (ns, tn) => new DotnetTypeDescr(ns, tn, null, genericArgs2, isArray)},
            var ns => new DotnetTypeDescr(ns, typeName, null, genericArgs2, isArray)
        };

        return result.AsSimplified();
    }
        
    private static (string? ns, string className)? GetBuiltinFullTypeNameOrNull(IReadOnlyCollection<string> namespaces, string className) {
        if (PrimitiveFullNameToAlias.Values.Contains(className)) {
            return (null, className);
        }

        if (PrimitiveFullNameToAlias.TryGetValue(className, out var alias)) {
            return (null, alias);
        }
            
        if (PrimitivesAssembly.GetType(className) != null) {
            return (null, className);
        }
            
        return namespaces.FirstOrDefault(ns => PrimitivesAssembly.GetType($"{ns}.{className}") != null) switch {
            null => null,
            var ns => (ns, className)};
    }
}