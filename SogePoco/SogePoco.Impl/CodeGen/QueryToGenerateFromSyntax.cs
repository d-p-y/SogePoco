using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SogePoco.Common;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.CodeGen; 

public record ParamInfoHolder(string TypeName, string VariableName, string TableNameAlias);

public record OuterParamInfo(DotnetTypeDescr DotnetTypeName, string VariableName, string? DefaultValueCs);
    
public static class TypeSymbolExtensions {
    public static string? ContainingNamespaceSanitized(this ITypeSymbol self) {
        var result = self.ContainingNamespace?.ToString();

        return result == "<global namespace>" ? null : result;
    }
}
    
public static class ParameterSyntaxExtensions {
    public static OuterParamInfo ToSimpleInfo(
        this ParameterSyntax self, Compilation c, CompilationUnitSyntax cus, SyntaxTree st) {
                
        var sm = c.GetSemanticModel(st);
        var ti = sm.GetTypeInfo(self.Type ?? throw new Exception("could not get Type from self (ParameterSyntax)"));

        //handle nullable<primitive>
        var usings = cus.Usings
            .Select(x => x.Name switch{ 
                IdentifierNameSyntax ins => ins.Identifier.Text,
                QualifiedNameSyntax qns => qns.GetFullName(), 
                _ => throw new ArgumentOutOfRangeException("could not simplify usings into simple imports (bug?)")
            }).ToList();
            
        var typeName =
            ti.Type switch {
                null => throw new Exception("parameter type unknown?"),
                INamedTypeSymbol nts => 
                    DotnetTypeDescr.CreateOfBuiltinType(
                        usings:usings,
                        containingNamespace:nts.ContainingNamespaceSanitized(),
                        typeName:nts.Name,
                        genericArgs:nts.TypeArguments
                            .Select(y => {
                                var ns = y.ContainingNamespaceSanitized();
                                return (string.IsNullOrEmpty(ns) ? "" : $"{ns}.") + y.Name; })
                            .ToList(),
                        isArray:false),
                IArrayTypeSymbol ats when ats.ElementType is INamedTypeSymbol ets && ets.ContainingNamespaceSanitized() == "System" && ets.Name == "Nullable" => 
                    DotnetTypeDescr.CreateOfBuiltinType(
                        usings:usings,
                        containingNamespace:ets.TypeArguments
                            .Single()
                            .ContainingNamespaceSanitized(),
                        typeName:ets.TypeArguments
                            .Single().Name + "?",
                        genericArgs: new List<string>(),
                        isArray:true),
                IArrayTypeSymbol ats when ats.ElementType is INamedTypeSymbol ets => 
                    DotnetTypeDescr.CreateOfBuiltinType(
                        usings:usings,
                        containingNamespace:ets.ContainingNamespaceSanitized(),
                        typeName:ets.Name,
                        genericArgs: ets.TypeArguments
                            .Select(y =>
                                y.ContainingNamespaceSanitized().Map(ns => 
                                    (string.IsNullOrEmpty(ns) ? "" : $"{ns}.")) +
                                y.Name + 
                                (ets.ContainingNamespaceSanitized() == "System" && ets.Name == "Nullable" ? "?" : "") )
                            .ToList(),
                        isArray:true),
                IArrayTypeSymbol ats => DotnetTypeDescr.CreateOfBuiltinType(
                    usings:usings,
                    containingNamespace:ats.ElementType.ContainingNamespaceSanitized(),
                    typeName:ats.ElementType.Name,
                    genericArgs:null,
                    isArray:true),
                _ =>throw new Exception("unsupported ITypeSymbol")
            };

        var defaultValueCs = self.Default switch {
            null => null,
            EqualsValueClauseSyntax {Value: LiteralExpressionSyntax les} => les.Token.Value switch {
                null => "null",
                bool b => b ? "true" : "false", //as b.ToString(CultureInfo.InvariantCulture) returns "True" or "False"
                int i => i.ToString(CultureInfo.InvariantCulture),
                decimal d => d.ToString(CultureInfo.InvariantCulture) + "m",
                string s => s.StringAsCsCodeStringValue(),
                _ => throw new Exception("parameter type not supported (yet?)")
            },
            _ => throw new Exception("parameter initializer not supported (yet?)")
        };
            
        return new(typeName, self.Identifier.Text, defaultValueCs);
    }
}

public class TableNameAndAlias {
    public string PocoTypeName;
    public string Alias;
    public bool MayBeNull;
        
    public TableNameAndAlias(string pocoTypeName, string alias, bool mayBeNull) {
        PocoTypeName = pocoTypeName;
        Alias = alias;
        MayBeNull = mayBeNull;
    }
}

public enum AscOrDesc {
    Asc,
    Desc
}
    
public static class AscOrDescUtil {
    public static AscOrDesc? OfMethodName(string methodName) =>
        methodName switch {
            Consts.OrderByDescPocoQueryMethodName => AscOrDesc.Desc,
            Consts.OrderByAscPocoQueryMethodName => AscOrDesc.Asc,
            _ => null
        };
} 

public static class AscOrDescExtensions {
    public static string AsSqlLiteral(this AscOrDesc self) =>
        self switch {
            AscOrDesc.Asc => "ASC",
            _ => "DESC"
        };
}
    
        
public record TableAndAlias(SqlTableForCodGen PocoType, string Alias,bool MayBeNull);
public record JoinedTableInfo(
    JoinType JoinTypeLiteral,
    bool IsInvertedJoin,
    SqlTableForCodGen from, string fromAlias, SqlForeignKeyForCodGen fk, SqlTableForCodGen to, string toAlias) {
        
    //public TableAndAlias AsTableAndAlias() => new(to, toAlias);

    public string GetJoinTypeAsSqlLiteral() => 
        JoinTypeLiteral switch {
            JoinType.Inner => "INNER JOIN",
            JoinType.Left => "LEFT OUTER JOIN",
            _ => throw new ArgumentOutOfRangeException($"unsupported join type {JoinTypeLiteral}")
        };
}

public record QueryToGenerateTreated(
        Compilation Comp, CompilationUnitSyntax CompUnitSyntax, SyntaxTree StxTree, string Name,
        RequestedQuery Body, IReadOnlyCollection<ParameterSyntax> OuterPrms,
        IReadOnlyList<ParamInfoHolder> PocoParameterInfos,
        IReadOnlyList<(ParameterSyntax,OuterParamInfo)> OuterParamWithInfo,
        IReadOnlyList<JoinedTableInfo> JoinTables,
        IReadOnlyList<TableAndAlias> AllTables,
        IReadOnlyList<TableAndAlias> Result,
        IReadOnlyCollection<(TableAndAlias tbl, SqlColumnForCodGen col, AscOrDesc ord)> OrderBy,
        int? TakeItemCount,
        string? PostgresForClause) {

    public TableAndAlias GetNthTable(int tableIdx) => AllTables[tableIdx];

    public IReadOnlyCollection<OuterParamInfo> GetOuterPrmsInfos() => OuterParamWithInfo.Select(x => x.Item2).ToList();
        
    public OuterParamInfo GetOuterParamAsSimpleInfo(ParameterSyntax mthPrm) =>
        OuterParamWithInfo.Where(x => x.Item1 == mthPrm).Select(x => x.Item2).SingleOrDefault()
        ?? throw new Exception($"requested param is not owned by this {nameof(QueryToGenerateFromSyntax)}");

    public ParamInfoHolder? MaybeGetParameterByVariableName(string variableName) =>
        PocoParameterInfos.FirstOrDefault(x => x.VariableName == variableName);
}
    
public record QueryToGenerateFromSyntax(
    Compilation Comp, CompilationUnitSyntax CompUnitSyntax, SyntaxTree StxTree, string Name,
    RequestedQuery Body, IReadOnlyCollection<ParameterSyntax> OuterPrms) {

    public QueryToGenerateTreated ToTreated(GeneratorOptions opt, PocoSchema metaData, Func<string> tableAliasNameBuilder) {
        //consistency check. not needed anymore?
        if (Body.GetWhere() is { } wh) {
            if (wh is WhereExpression.Ples ples) {
                var pocoParams = ples.Les.ParameterList.Parameters;
                    
                if (!pocoParams.Any()) {
                    throw new Exception("expected to have at least one poco 'self' param but have none");
                }

                if (pocoParams.Count != Body.GetTableCount()) {
                    throw new Exception($"expected to have at same amount of poco parameters {pocoParams.Count} as there are tables involved in query {Body.GetTableCount()}");
                }    
            } else if (wh is WhereExpression.Sles) {
                if (1 != Body.GetTableCount()) {
                    throw new Exception($"expected to have at same amount of poco parameters 1 as there are tables involved in query {Body.GetTableCount()}");
                }
            }
        }

        var (allTables, joins) = Body.GetTables(tableAliasNameBuilder, opt, TypeSymbolToPocoTypeName, GetTypeInfoFor, metaData);

        return new QueryToGenerateTreated(Comp, CompUnitSyntax, StxTree, Name, Body, OuterPrms,
            ExtractPocoParameters(allTables),
            BuildOuterPrmsWithInfo(),
            joins,
            allTables,
            Body.GetSelectOf(allTables) 
            ?? allTables 
            ?? throw new Exception("bug - query has no 'first' table"),
            Body.GetOrderBy(allTables),
            TakeItemCount:Body.MaybeTakeItemCount(),
            PostgresForClause:Body.MaybePostgresForClause());
    }

    ITypeSymbol GetTypeInfoFor(SyntaxNode ins) {
        var sm = Comp.GetSemanticModel(StxTree);
        return sm.GetTypeInfo(ins).Type ?? throw new Exception("GetTypeInfo().Type is null");
    }
        
    ITypeSymbol? MaybeGetTypeInfoFor(ParameterSyntax ps) {
        var sm = Comp.GetSemanticModel(StxTree);
        if (ps.Type == null) {
            return null;
        }
                
        return sm.GetTypeInfo(ps.Type).Type;
    }

    ITypeSymbol GetTypeInfoFor(ParameterSyntax ps) {
        var sm = Comp.GetSemanticModel(StxTree);
        var x = ps.Type ?? throw new Exception("parametersyntax type is null");
        return sm.GetTypeInfo(x).Type ?? throw new Exception("GetTypeInfo().Type is null");
    }

    string TypeSymbolToPocoTypeName(ITypeSymbol ti) => ti.ContainingNamespace.ToString() + "." + ti.Name;
        
    private List<ParamInfoHolder> ExtractPocoParameters(IReadOnlyList<TableAndAlias> tables) =>
        (Body.GetWhere() switch {
            null => Array.Empty<ParameterSyntax>(),
            WhereExpression.Sles sles => sles.Les.Parameter.AsSingletonCollection(),
            WhereExpression.Ples ples => ples.Les.ParameterList.Parameters,
            _ => throw new ArgumentOutOfRangeException($"unsupported {nameof(WhereExpression)} type")
        }).SelectI((i,ps) => {
            var ti = MaybeGetTypeInfoFor(ps);
            var typeName = ti != null ? TypeSymbolToPocoTypeName(ti) : tables[i].PocoType.FullClassName;
            return new ParamInfoHolder(typeName, ps.Identifier.Text, tables[i].Alias);
        }).ToList();

    private IReadOnlyList<(ParameterSyntax,OuterParamInfo)> BuildOuterPrmsWithInfo() =>
        OuterPrms
            .Select(x => (x,x.ToSimpleInfo(Comp, CompUnitSyntax, StxTree)))
            .ToList();
}