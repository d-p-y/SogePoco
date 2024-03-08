using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public interface IParameterRequester {
    SqlParamNamingResult Request();
}

public class DefaultParameterRequester : IParameterRequester {
    private readonly ISqlParamNamingStrategy _naming;
    private int _nextParamNo = 0;

    public DefaultParameterRequester(ISqlParamNamingStrategy naming) => _naming = naming;

    public SqlParamNamingResult Request() => _naming.Build(_nextParamNo++);
}

record LiteralSqlProto(string pre, string post) {
    public static LiteralSqlProto CreateEmpty() => new(string.Empty, string.Empty);

    public LiteralSqlProto AddBrackets() => new (pre+" (", ") "+post);

    public LiteralSqlProto AddNot() => new(pre + " NOT ", post);

    public string BuildLiteralSql(string literalSql) => pre + literalSql + post;
}

enum ComparisonOperator {
    NotEqual,
    Equal
}

static class BinaryExpressionSyntaxExtensions {
    public static ComparisonOperator AsComparisonOperator(this BinaryExpressionSyntax self) =>
        self.OperatorToken.Text switch { 
            "!=" => ComparisonOperator.NotEqual,
            "==" => ComparisonOperator.Equal,
            _ => throw new Exception($"{nameof(AsComparisonOperator)} unsupported operator {self.OperatorToken.Text}")
        };
}

static class LiteralExpressionSyntaxExtensions {
    public static ComparisonOperator? PatternAsMaybeComparisonOperator(this PatternSyntax self) =>
        self switch {//
            RecursivePatternSyntax {
                PositionalPatternClause: null,
                PropertyPatternClause: {} prpc, 
            } when prpc.ToString() == "{}" => 
                // allow simple is-not-null pattern only
                ComparisonOperator.NotEqual, 
            ConstantPatternSyntax {
                Expression: LiteralExpressionSyntax { Token.ValueText: "null" },
            } => ComparisonOperator.Equal,
            UnaryPatternSyntax {
                OperatorToken.ValueText: "not",
                Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token.ValueText: "null" }},
            } => ComparisonOperator.NotEqual,
            _ => throw new ArgumentOutOfRangeException($"{nameof(PatternAsMaybeComparisonOperator)} unsupported PatternSyntax")
        };
}

public record QueryBuildingResult(
    string whereClause, 
    IReadOnlyCollection<OuterParamInfo> QueryMethodParams,
    IReadOnlyCollection<SqlParamInfo> SqlParams);
    
public class SyntaxToWhereClauseConverter {
    private readonly QueryToGenerateTreated _root;
    private readonly IDatabaseDotnetDataMapperGenerator _mapper;
    private readonly IParameterRequester _parameterRequester;
    private readonly Func<string, bool> _paramNameIsUnavailable;
    private static readonly string[] NoCsToSqlTranslNeeded = { "<", "<=", ">", ">="};
    private readonly IDictionary<string, SqlParamInfo> _methodParamUsageCache = new Dictionary<string, SqlParamInfo>();
    
    private SyntaxToWhereClauseConverter(
            QueryToGenerateTreated root, IDatabaseDotnetDataMapperGenerator mapper, 
            IParameterRequester parameterRequester, Func<string,bool> paramNameIsUnavailable) {
        
        _root = root;
        _mapper = mapper;
        _parameterRequester = parameterRequester;
        _paramNameIsUnavailable = paramNameIsUnavailable;
    }

    private QueryChunk ConvertMaes(MemberAccessExpressionSyntax self, LiteralSqlProto litSql) {
        if (self.Expression is IdentifierNameSyntax ins) {
            var pocoParam =
                _root.MaybeGetParameterByVariableName(ins.Identifier.Text)
                ?? throw new Exception($"unexpected: MemberAccessExpressionSyntax->IdentifierNameSyntax->Identifier->Text={ins.Identifier.Text} is not lambda's poco parameter reference");

            var tbl = _root.GetNthTable(_root.PocoParameterInfos.IndexOfOrFail(pocoParam));
                
            var prop = 
                tbl.PocoType.SortedColumns.FirstOrDefault(x => x.PocoPropertyName == self.Name.Identifier.Text)
                ?? throw new Exception($"unexpected: MemberAccessExpressionSyntax->Name->Identifier->Text={self.Name.Identifier.Text} is not backed by poco's property name in model");

            return new QueryChunk(
                litSql.BuildLiteralSql($"{_mapper.QuoteSqlIdentifier(pocoParam.TableNameAlias)}.{_mapper.QuoteSqlIdentifier(prop.Col.Name)}"),
                IsNull:false,
                SqlParams:null);
        }
            
        throw new NotImplementedException($"{nameof(ConvertMaes)} syntax not supported (yet?)");
    }

    private QueryChunk ConvertLess(LiteralExpressionSyntax les, LiteralSqlProto litSql, IReadOnlyCollection<BinaryExpressionSyntax> curBeses) =>
        (les.Token.Value switch {
            null => new QueryChunk("NULL", IsNull:true, SqlParams:null),
            bool b when !curBeses.Any() => new QueryChunk(
                b ? "1 = 1" : "0 = 1",
                IsNull:false,
                SqlParams:null),
            bool b => new QueryChunk(
                b ? _mapper.SqlLiteralForTrue : _mapper.SqlLiteralForFalse,
                IsNull:false,
                SqlParams:null),
            int i => _parameterRequester.Request()
                .With(prm => new QueryChunk(
                    prm.UseInSqlSnippet,
                    IsNull: false,
                    SqlParams: 
                    new SqlParamInfo(
                            prm, 
                            DotnetTypeDescr.CreateOf(typeof(int)),
                            i.ToString(CultureInfo.InvariantCulture))
                        .AsSingletonCollection())),
            string s => _parameterRequester.Request()
                .With(prm => new QueryChunk(
                    prm.UseInSqlSnippet,
                    IsNull: false,
                    SqlParams: 
                    new SqlParamInfo(
                            prm, 
                            DotnetTypeDescr.CreateOf(typeof(string)),
                            s.StringAsCsCodeStringValue())
                        .AsSingletonCollection())),
            decimal d => _parameterRequester.Request()
                .With(prm => new QueryChunk(
                    prm.UseInSqlSnippet,
                    IsNull: false,
                    SqlParams: 
                    new SqlParamInfo(
                            prm, 
                            DotnetTypeDescr.CreateOf(typeof(decimal)),
                            d.ToString(CultureInfo.InvariantCulture)+"m")
                        .AsSingletonCollection())),
            _ => throw new NotImplementedException($"{nameof(ConvertLess)} syntax not supported (yet?)") })
        .With(x => x with {LiteralSql = litSql.BuildLiteralSql(x.LiteralSql)});
    
    private QueryChunk ConvertMaesPocoPropertyWithNullComparison(
            ComparisonOperator cmp, LiteralSqlProto litSql, ParamInfoHolder pocoParam, string pocoField) {
        
        var sqlOper = cmp switch { 
            ComparisonOperator.NotEqual => " IS NOT ",
            ComparisonOperator.Equal => " IS ",
            _ => throw new Exception($"{nameof(ConvertBesPocoVarWithNullComparision)} unsupported operator {cmp}")
        };

        var tblAndAlias = _root.GetNthTable(_root.PocoParameterInfos.IndexOfOrFail(pocoParam));
          
        var prop = 
            tblAndAlias.PocoType.SortedColumns.FirstOrDefault(x => x.PocoPropertyName == pocoField)
            ?? throw new Exception($"unexpected: {pocoParam.VariableName} was supposed to have {pocoField} but it is not backed by poco's property name in model. poco type {pocoParam.TypeName}");

        return new QueryChunk(
            litSql.BuildLiteralSql($"{_mapper.QuoteSqlIdentifier(pocoParam.TableNameAlias)}.{_mapper.QuoteSqlIdentifier(prop.Col.Name)} {sqlOper} NULL"),
            IsNull:false,
            SqlParams:null);
    }
    
    private QueryChunk ConvertBesPocoVarWithNullComparision(
            ComparisonOperator cmp, LiteralSqlProto litSql, ParamInfoHolder pocoParam) {
        
        var tblAndAlias = _root.GetNthTable(_root.PocoParameterInfos.IndexOfOrFail(pocoParam));
            
        var sqlOper = cmp switch { 
            ComparisonOperator.NotEqual => " IS NOT ",
            ComparisonOperator.Equal => " IS ",
            _ => throw new Exception($"{nameof(ConvertBesPocoVarWithNullComparision)} unsupported operator {cmp}")
        };

        var pks = tblAndAlias.PocoType.Tbl.GetPrimaryKey().ToList();
            
        return new QueryChunk(
            litSql.BuildLiteralSql(
                pks.Select(c => $"{_mapper.QuoteSqlIdentifier(tblAndAlias.Alias)}.{_mapper.QuoteSqlIdentifier(c.Name)} {sqlOper} NULL")
                    .ConcatenateUsingSep(" AND ")
                    .With(x => pks.Count <= 1 ? x : $"({x})")),
            IsNull: false,
            SqlParams: null);
    }
        
    private QueryChunk ConvertBesOneToOneComparison(BinaryExpressionSyntax bes, LiteralSqlProto litSql, IReadOnlyCollection<BinaryExpressionSyntax> curBeses) {
        var beses = curBeses.ToList();
        beses.Add(bes);
            
        var left = ConvertEs(bes.Left, LiteralSqlProto.CreateEmpty(), beses);
        var right = ConvertEs(bes.Right, LiteralSqlProto.CreateEmpty(), beses);
            
        var sqlOperator = bes.OperatorToken.Text switch {
            "==" when left.IsNull==false && right.IsNull==false => "=",
            "==" => "IS",
            "!=" when left.IsNull==false && right.IsNull==false => "<>",
            "!=" => "IS NOT",
            "||" => "OR",
            "&&" => "AND",
            var x when NoCsToSqlTranslNeeded.Contains(x) => x,
            _ => throw new Exception("don't know how to translate operator")};

        return new QueryChunk(
            litSql.BuildLiteralSql($"{left.LiteralSql} {sqlOperator} {right.LiteralSql}"),
            IsNull: false,
            SqlParams: (left.SqlParams ?? Array.Empty<SqlParamInfo>())
            .Concat(right.SqlParams ?? Array.Empty<SqlParamInfo>())
            .ToList());
    }
        
    private QueryChunk ConvertBes(
        BinaryExpressionSyntax bes, LiteralSqlProto litSql, IReadOnlyCollection<BinaryExpressionSyntax> curBeses) =>
        bes switch {
            {Left: IdentifierNameSyntax { } pocoIns} when
                bes.Right is LiteralExpressionSyntax les && les.Token.Value == null &&
                _root.MaybeGetParameterByVariableName(pocoIns.GetNameAsText()) is { } pocoParam =>
                ConvertBesPocoVarWithNullComparision(bes.AsComparisonOperator(), litSql, pocoParam),
            {Right: IdentifierNameSyntax { } pocoIns} when
                bes.Left is LiteralExpressionSyntax les && les.Token.Value == null &&
                _root.MaybeGetParameterByVariableName(pocoIns.GetNameAsText()) is { } pocoParam =>
                ConvertBesPocoVarWithNullComparision(bes.AsComparisonOperator(), litSql, pocoParam),
            _ => ConvertBesOneToOneComparison(bes, litSql, curBeses)
        };

    private QueryChunk ConvertOcesDateTime(ObjectCreationExpressionSyntax oce, LiteralSqlProto litSql) {
        var dtArgs = oce.ArgumentList?.Arguments
                         .Select(x => x.Expression switch {
                             LiteralExpressionSyntax les when les.Token.Value is int i => i.ToString(CultureInfo.InvariantCulture),
                             _ => throw new NotImplementedException(
                                 $"{nameof(ConvertOcesDateTime)} datetime creation argument is not int - syntax not supported (yet?)")
                         })
                         .ToList() 
                     ?? new List<string>();

        var prm = _parameterRequester.Request();
        return new QueryChunk(
            litSql.BuildLiteralSql(prm.UseInSqlSnippet),
            IsNull: false,
            SqlParams: 
            new SqlParamInfo(
                    prm,
                    DotnetTypeDescr.CreateOf(typeof(DateTime)),
                    $"new System.DateTime({dtArgs.ConcatenateUsingComma()})")
                .AsSingletonCollection());
    }

    private (bool isNew,SqlParamInfo prm) GetOrBuildSqlParameterForMethodParam(string methodParamName) {
        if (_methodParamUsageCache.TryGetValue(methodParamName, out var sqlName)) {
            return (false,sqlName);
        }
               
        var mthPrm = 
            _root.OuterPrms.FirstOrDefault(x => x.Identifier.Value is string s && s == methodParamName)
            ?? throw new Exception($"query uses variable {methodParamName} BUT method's parameter doesn't contain it");

        var dtn = _root.GetOuterParamAsSimpleInfo(mthPrm).DotnetTypeName;
            
        var result = 
            new SqlParamInfo(
                _parameterRequester.Request(),
                dtn,
                methodParamName);
        
        _methodParamUsageCache.Add(methodParamName, result);
        return (true, result);
    }
    
    private QueryChunk ConvertIns(IdentifierNameSyntax ins, LiteralSqlProto litSql) {
        var lookupPrmName = 
            ins.Identifier.Value as string
            ?? throw new Exception("IdentifierNameSyntax->Identifier->Value expected to be string");
              
        var (isNew,prm) = GetOrBuildSqlParameterForMethodParam(lookupPrmName);
         
        return new QueryChunk(
            litSql.BuildLiteralSql(prm.Name.UseInSqlSnippet),
            IsNull: false,
            SqlParams: 
            isNew ? prm.AsSingletonCollection() : null);
    }

    private QueryChunk ConvertIes(
        MemberAccessExpressionSyntax collPrm, MemberAccessExpressionSyntax pocoFldAccess, LiteralSqlProto litSql, IReadOnlyCollection<BinaryExpressionSyntax> curBeses) {
            
        return
            (collPrm.Name, collPrm.Expression) switch {
                (IdentifierNameSyntax methName, IdentifierNameSyntax obj) 
                    when 
                        methName.Identifier.Text == "Contains" && 
                        _root.OuterPrms.FirstOrDefault(x => x.Identifier.Value is string s && s == obj.Identifier.ValueText) is {} mthPrm &&
                        mthPrm.Type?.ToString().EndsWith("[]") == true 
                    => _mapper.GenerateExpressionValueIsContainedInCollection(
                        ConvertMaes(pocoFldAccess, LiteralSqlProto.CreateEmpty()),
                        ConvertIns(obj, LiteralSqlProto.CreateEmpty())),
                _ => throw new Exception("InvocationExpressionSyntax expected to be call on method 'Contains' on collection parameter and one argument a poco field")
            };
    }
         
    private QueryChunk ConvertEs(
        ExpressionSyntax self, LiteralSqlProto litSql, IReadOnlyCollection<BinaryExpressionSyntax> curBeses) => 
        self switch {
            ParenthesizedExpressionSyntax pes => ConvertEs(pes.Expression, litSql.AddBrackets(), curBeses),
            PrefixUnaryExpressionSyntax pues when pues.OperatorToken.ValueText == "!" => ConvertEs(pues.Operand, litSql.AddNot(), curBeses),
            BinaryExpressionSyntax bes => ConvertBes(bes, litSql, curBeses),
            MemberAccessExpressionSyntax maes => ConvertMaes(maes, litSql),
            LiteralExpressionSyntax les => ConvertLess(les, litSql, curBeses),
            ObjectCreationExpressionSyntax oce when oce.Type is IdentifierNameSyntax tins && tins.Identifier.Text == "DateTime" => 
                ConvertOcesDateTime(oce, litSql),
            IdentifierNameSyntax ins => ConvertIns(ins, litSql),
            InvocationExpressionSyntax ies when ies.Expression is MemberAccessExpressionSyntax maes && 
                                                ies.ArgumentList.Arguments.FirstOrDefault()?.Expression is MemberAccessExpressionSyntax pocoFld => 
                ConvertIes(maes, pocoFld, litSql, curBeses),
            
            //poco is comparison-involving-null-here
            IsPatternExpressionSyntax {
                Expression: IdentifierNameSyntax{} pocoIns,
                Pattern: { } ptrn
                } when _root.MaybeGetParameterByVariableName(pocoIns.GetNameAsText()) is { } pocoParam &&
                       ptrn.PatternAsMaybeComparisonOperator() is {} cmp =>
                ConvertBesPocoVarWithNullComparision(cmp, litSql, pocoParam),
            
            //poco.field is ...
            IsPatternExpressionSyntax {
                Expression: MemberAccessExpressionSyntax {
                    Expression: IdentifierNameSyntax{} pocoIns,
                    Name: IdentifierNameSyntax{} colName
                },
                Pattern: { } ptrn
                } when _root.MaybeGetParameterByVariableName(pocoIns.GetNameAsText()) is { } pocoParam &&
                       ptrn.PatternAsMaybeComparisonOperator() is {} cmp =>
                ConvertMaesPocoPropertyWithNullComparison(cmp, litSql, pocoParam, colName.GetNameAsText()),
            
            _ => throw new NotImplementedException($"{nameof(ConvertEs)} syntax not supported (yet?)") };

    private QueryBuildingResult ConvertWhereExpression(WhereExpression? self) =>
        (self switch {
            null => new QueryChunk("", false, Array.Empty<SqlParamInfo>()),
            WhereExpression.Ples {
                Les.ExpressionBody: LiteralExpressionSyntax les
            } => ConvertLess(les, LiteralSqlProto.CreateEmpty(), new List<BinaryExpressionSyntax>()),
                
            WhereExpression.Sles {
                Les.ExpressionBody: LiteralExpressionSyntax les
            } => ConvertLess(les, LiteralSqlProto.CreateEmpty(), new List<BinaryExpressionSyntax>()),
                
            WhereExpression.Ples {
                Les.ExpressionBody: {} eb
            } => ConvertEs(eb, LiteralSqlProto.CreateEmpty(), new List<BinaryExpressionSyntax>()),
                
            WhereExpression.Sles {
                Les.ExpressionBody: {} eb
            } => ConvertEs(eb, LiteralSqlProto.CreateEmpty(), new List<BinaryExpressionSyntax>()),
                
            _ => throw new ArgumentOutOfRangeException($"unsupported {nameof(WhereExpression)} in {nameof(ConvertWhereExpression)}")
        }).ToQueryBuildingResult(_root, _paramNameIsUnavailable);

    public static QueryBuildingResult BuildQuery(
        IParameterRequester parameterRequester, IDatabaseDotnetDataMapperGenerator mapper, 
        QueryToGenerateTreated root, Func<string,bool> paramNameIsUnavailable) =>
            new SyntaxToWhereClauseConverter(root, mapper, parameterRequester, paramNameIsUnavailable)
                .ConvertWhereExpression(root.Body.GetWhere());
}