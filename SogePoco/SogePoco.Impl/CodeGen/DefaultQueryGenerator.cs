using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SogePoco.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public record RequestedQuery {
    private RequestedQuery() { }

    public record SingleTableQuery(ParenthesizedLambdaExpressionSyntax Where) : RequestedQuery();

    public record MultiTableQuery(
        IdentifierNameSyntax From, IReadOnlyCollection<JoinExpression> Joins,
        WhereExpression? Where,
        ParenthesizedLambdaExpressionSyntax? Select,
        IReadOnlyCollection<OrderByExpression> OrderBy,
        int? TakeItemCount,
        string? PostgresForClause) : RequestedQuery();

    public int GetTableCount() => this switch {
        SingleTableQuery _ => 1,
        MultiTableQuery fjw => 1 + fjw.Joins.Count,
        _ => throw new ArgumentOutOfRangeException(
            $"{nameof(GetWhere)} unsupported {nameof(RequestedQuery)} instance")
    };

    private ITypeSymbol GetMainTablePocoTypeName(Func<TypeSyntax, ITypeSymbol> typeExtraction) => this switch {
        SingleTableQuery r when r.Where.ParameterList.Parameters.FirstOrDefault() is { } p && p.Type is { } t =>
            typeExtraction(t),
        SingleTableQuery r =>
            throw new Exception(
                $"{nameof(GetMainTablePocoTypeName)} failed because either there were no params or firstParam->Type is null"),
        MultiTableQuery fjw => typeExtraction(fjw.From),
        _ => throw new ArgumentOutOfRangeException(
            $"{nameof(GetWhere)} unsupported {nameof(RequestedQuery)} instance")
    };
        
    public IReadOnlyList<TableAndAlias>? GetSelectOf(IReadOnlyList<TableAndAlias> allTables) => this switch {
        SingleTableQuery _ => null,
            
        //implicitly all involved tables in registration order 
        MultiTableQuery {Select: null } => allTables,
            
        //single table 
        MultiTableQuery {Select: {
                ExpressionBody: IdentifierNameSyntax {
                    Identifier.Text: {} returnedName
                },
                ParameterList.Parameters: {} prms
            } } when
            prms.SelectI((i, x) => (i,x.Identifier.Text == returnedName)).FirstOrDefault(ix => ix.Item2) is {} idx => 
            allTables[idx.i].AsSingletonList(),
            
        //(sub)tuple
        MultiTableQuery {Select: {
                ParameterList.Parameters: {} inpPocos,
                ExpressionBody: TupleExpressionSyntax {
                    Arguments: {} returnPocos
                }
            } } when 
            inpPocos.Select(x => x.Identifier.Text).ToList() is var inpPocosTxt &&
            returnPocos
                .Select(x => x.Expression is IdentifierNameSyntax {} y ? y.Identifier.Text : throw new Exception($"unsupported expression {x.Expression} in Select"))
                .ToList() is var returnPocosTxt
            => 
            !returnPocosTxt.All(retPoco => inpPocosTxt.Contains(retPoco)) 
                ? throw new Exception("element in poco is not originating from lambda argumens")
                : returnPocosTxt.Select(retPoco => allTables[inpPocosTxt.IndexOf(retPoco)]).ToList(),
            
        _ => throw new ArgumentOutOfRangeException(
            $"{nameof(GetSelectOf)} unsupported {nameof(RequestedQuery)} instance")
    };
        
    public WhereExpression? GetWhere() => this switch {
        SingleTableQuery r => new WhereExpression.Ples(r.Where),
        MultiTableQuery fjw => fjw.Where,
        _ => throw new ArgumentOutOfRangeException(
            $"{nameof(GetWhere)} unsupported {nameof(RequestedQuery)} instance")
    };
        
    private record JoinCalcTempRes(JoinType SqlJoinType, string PocoType, string TblAlias, string ForeignKeyName, int? SourceTblIdx);

    public IReadOnlyList<JoinedTableInfo> GetTablesImpl(
        List<TableNameAndAlias> allTables, GeneratorOptions opt,
        Func<ITypeSymbol, string> typeSymbolToPocoTypeName, Func<TypeSyntax, ITypeSymbol> typeExtraction,
        PocoSchema metaData) => this switch {
            
        SingleTableQuery _ => Array.Empty<JoinedTableInfo>(),
        MultiTableQuery fjw => fjw.Joins.SelectI((iTbl, lbd) => {
            var res = lbd switch {
                JoinExpression.SinglePles {
                        JoinType: {} jt,
                        Les.ExpressionBody: MemberAccessExpressionSyntax {
                            Expression: MemberAccessExpressionSyntax {
                                Expression: IdentifierNameSyntax fkOwner,
                                Name: IdentifierNameSyntax fks
                            },
                            Name: {} fkName
                        },
                        Les.ParameterList.Parameters: { } prms
                    } when 
                    fks.GetNameAsText() == Consts.ForeignKeysPropertyName &&
                    fkOwner.GetNameAsText() is {} parameterName &&
                    prms.SelectI((i, x) => (i, x.Identifier.Text == parameterName))
                        .Where(x => x.Item2)
                        .Select(x => x.i)
                        .FirstOrDefault() is {} idx =>
                    new JoinCalcTempRes(
                            jt, 
                            iTbl <= 0 
                                ? typeSymbolToPocoTypeName(typeExtraction(fjw.From)) 
                                : allTables[idx].PocoTypeName, 
                            allTables[iTbl <= 0 ? 0 : idx].Alias,
                            fkName.GetNameAsText(),
                            null)
                        .Also(x => 
                            allTables[idx].PocoTypeName = x.PocoType),
                JoinExpression.SingleSles {
                        JoinType: {} jt,
                        Les.ExpressionBody: MemberAccessExpressionSyntax {
                            Expression: MemberAccessExpressionSyntax {
                                Expression: IdentifierNameSyntax fkOwner,
                                Name: IdentifierNameSyntax fks
                            },
                            Name: {} fkName
                        },
                        Les.Parameter: {} prm
                    } when
                    fks.GetNameAsText() == Consts.ForeignKeysPropertyName &&
                    fkOwner.GetNameAsText() is {} parameterName &&
                    new []{prm}.SelectI((i, x) => (i, x.Identifier.Text == parameterName))
                        .Where(x => x.Item2)
                        .Select(x => x.i)
                        .FirstOrDefault() is {} idx =>
                    new JoinCalcTempRes(
                            jt, 
                            iTbl <= 0 
                                ? typeSymbolToPocoTypeName(typeExtraction(fjw.From)) 
                                : allTables[idx].PocoTypeName, 
                            allTables[iTbl <= 0 ? 0 : idx].Alias,
                            fkName.GetNameAsText(),
                            null)
                        .Also(x => 
                            allTables[idx].PocoTypeName = x.PocoType),
                JoinExpression.InverseJoinToSles {
                        JoinType: {} jt,
                        PlesOf: {
                            ExpressionBody: MemberAccessExpressionSyntax {
                                Name: {} fkName,
                                Expression: MemberAccessExpressionSyntax {
                                    Name: {} fks
                                }
                            },
                            ParameterList.Parameters: {} prms,
                        },
                        SlesTo: {
                            ExpressionBody: IdentifierNameSyntax {
                                Identifier: {ValueText: {} returnedParamName}
                            },
                            Parameter: {
                                Identifier: {ValueText: {} inputParamName}
                            }
                        }
                    } when 
                    fks.GetNameAsText() == Consts.ForeignKeysPropertyName &&
                    prms.SingleOrDefault() is {} prm && 
                    prm.Type is {} prmT &&
                    returnedParamName == inputParamName => 
                    new JoinCalcTempRes(
                        jt, 
                        typeSymbolToPocoTypeName(typeExtraction(prmT)),
                        allTables[iTbl+1].Alias,
                        fkName.GetNameAsText(),
                        0),
                JoinExpression.InverseJoinToPles {
                        JoinType: {} jt,
                        PlesOf: {
                            ExpressionBody: MemberAccessExpressionSyntax {
                                Name: {} fkName,
                                Expression: MemberAccessExpressionSyntax {
                                    Name: {} fks
                                }
                            },
                            ParameterList.Parameters: {} prms1,
                        },
                        PlesTo: {
                            ExpressionBody: IdentifierNameSyntax {
                                Identifier: {ValueText: {} returnedParamName}
                            },
                            ParameterList: { Parameters: {} prms2}
                        } d
                    } when 
                    fks.GetNameAsText() == Consts.ForeignKeysPropertyName &&
                    prms1.SingleOrDefault() is {} prm1 && 
                    prm1.Type is {} prmT &&
                    prms2.SelectI((i, x) => (i, x.Identifier.Text == returnedParamName))
                        .Where(x => x.Item2)
                        .Select(x => x.i)
                        .FirstOrDefault() is {} idx => 
                    new JoinCalcTempRes(
                        jt, 
                        typeSymbolToPocoTypeName(typeExtraction(prmT)),
                        allTables[iTbl+1].Alias,
                        fkName.GetNameAsText(),
                        idx),
                _ => throw new Exception(
                    $"in Join expected arrow lambda such as: tbl => pocoType.{Consts.ForeignKeysPropertyName}.SomeTable_by_SomeField but got something else: {lbd}")
            };

            var pocoType =
                metaData.Tables.SingleOrDefault(x => x.FullClassName == res.PocoType)
                ?? throw new Exception($"could not find pocoType {res.PocoType} in metadata");

            var fk =
                pocoType.SortedForeignKeys.SingleOrDefault(x => x.DotnetFieldName == res.ForeignKeyName)
                ?? throw new Exception(
                    $"could not find foreignKey named {res.ForeignKeyName} in metadata of pocoType {res.PocoType}");

            var typePointedByFk =
                metaData.Tables.SingleOrDefault(x => opt.AreTableNamesTheSame(x.Tbl.Name, fk.Fk.PrimaryKeyTableName))
                ?? throw new Exception(
                    $"could not find typePointedByFk responsible for table {fk.Fk.PrimaryKeyTableName} in metadata");
                
            if (res.SourceTblIdx is { } stIdx) {
                allTables[stIdx].PocoTypeName = typePointedByFk.FullClassName;
                allTables[iTbl+1].PocoTypeName = pocoType.FullClassName;
                allTables[iTbl + 1].MayBeNull = res.SqlJoinType == JoinType.Left;
                return new JoinedTableInfo(res.SqlJoinType, true, typePointedByFk, allTables[stIdx].Alias, fk, pocoType, allTables[iTbl+1].Alias);    
            }
                
            allTables[iTbl+1].PocoTypeName = typePointedByFk.FullClassName;
            allTables[iTbl+1].MayBeNull = res.SqlJoinType == JoinType.Left;
            return new JoinedTableInfo(res.SqlJoinType, false, pocoType, res.TblAlias, fk, typePointedByFk, allTables[iTbl + 1].Alias);
                
        }).ToList(),
        _ => throw new ArgumentOutOfRangeException($"{nameof(GetWhere)} unsupported {nameof(RequestedQuery)} instance")
    };
        
    public (IReadOnlyList<TableAndAlias> AllTables, IReadOnlyList<JoinedTableInfo> OfWhichJoins) GetTables(
        Func<string> tableAliasNameBuilder, GeneratorOptions opt, 
        Func<ITypeSymbol, string> typeSymbolToPocoTypeName, Func<TypeSyntax, ITypeSymbol> typeExtraction,
        PocoSchema metaData) {
            
        var allTables = new List<TableNameAndAlias>();
        Enumerable
            .Range(0, GetTableCount())
            .ForEach(i => allTables.Add(new TableNameAndAlias(
                pocoTypeName:(
                    i == 0 
                        ? typeSymbolToPocoTypeName(GetMainTablePocoTypeName(typeExtraction)) 
                        : string.Empty //initially...
                ), 
                alias:tableAliasNameBuilder(),
                mayBeNull:false //initially...
            )));
            
        var joins = GetTablesImpl(allTables, opt, typeSymbolToPocoTypeName, typeExtraction, metaData);
        var allTablesResult = 
            allTables
                .Select(x => 
                    new TableAndAlias(
                        metaData.Tables.SingleOrDefault(y => y.FullClassName == x.PocoTypeName)
                        ?? throw new Exception($"could not find type {x.PocoTypeName} in metadata"),
                        x.Alias,
                        x.MayBeNull))
                .ToList();
            
        return (allTablesResult,joins);
    }

    private (TableAndAlias tbl, SqlColumnForCodGen col, AscOrDesc ord) GetOrderByOne(
        OrderByExpression item, IReadOnlyList<TableAndAlias> allTables) =>
        item switch {
            OrderByExpression.Sles {
                    Dir: {} dir,
                    Les: {
                        ExpressionBody: MemberAccessExpressionSyntax {
                            Name.Identifier.Text: {} pocoPropName,
                            Expression: IdentifierNameSyntax {
                                Identifier.Text: {} pocoOwnerName
                            }
                        },
                        Parameter.Identifier.Text: {} pocoParam
                    }} when pocoParam == pocoOwnerName && 
                            allTables[0].PocoType.SortedColumns.FirstOrDefault(c => c.PocoPropertyName == pocoPropName) is {} col
                => (allTables[0], col, dir),
            OrderByExpression.Ples {
                    Dir: {} dir,
                    Les: {
                        ExpressionBody: MemberAccessExpressionSyntax {
                            Name.Identifier.Text: {} pocoPropName,
                            Expression: IdentifierNameSyntax {
                                Identifier.Text: {} pocoOwnerName
                            }
                        },
                        ParameterList.Parameters: {} allPocos
                    }} when allPocos.Select(a => a.Identifier.Text).ToList() is {} allPocosTxt &&
                            allPocosTxt.TryIndexOf(pocoOwnerName) is {} pocoIdx &&  
                            allTables[pocoIdx].PocoType.SortedColumns.FirstOrDefault(c => c.PocoPropertyName == pocoPropName) is {} col
                => (allTables[pocoIdx], col, dir),
            _ =>  throw new ArgumentOutOfRangeException(
                $"{nameof(GetOrderByOne)} unsupported {nameof(OrderByExpression)} instance")
        };
        
    public IReadOnlyCollection<(TableAndAlias tbl, SqlColumnForCodGen col, AscOrDesc ord)> GetOrderBy(IReadOnlyList<TableAndAlias> allTables) =>
        this switch {
            SingleTableQuery _ => Array.Empty<(TableAndAlias tbl, SqlColumnForCodGen col, AscOrDesc ord)>(),
            MultiTableQuery {OrderBy: var x} => x.Select(y => GetOrderByOne(y, allTables)).ToList(),
            _ => throw new ArgumentOutOfRangeException(
                $"{nameof(GetOrderBy)} unsupported {nameof(RequestedQuery)} instance")
        };

    public int? MaybeTakeItemCount() => this switch {
        SingleTableQuery r => null,
        MultiTableQuery fjw => fjw.TakeItemCount,
        _ => throw new ArgumentOutOfRangeException($"{nameof(MaybeTakeItemCount)} unsupported {nameof(RequestedQuery)} instance")
    };
    
    public string? MaybePostgresForClause() => this switch {
        SingleTableQuery r => null,
        MultiTableQuery fjw => fjw.PostgresForClause,
        _ => throw new ArgumentOutOfRangeException($"{nameof(MaybePostgresForClause)} unsupported {nameof(RequestedQuery)} instance")
    };
}

public enum JoinType {
    Inner,
    Left
}

public static class JoinTypeUtil {
    public static JoinType? OfMethodName(string methodName) =>
        methodName switch {
            Consts.InnerJoinPocoQueryMethodName => JoinType.Inner,
            Consts.LeftJoinPocoQueryMethodName => JoinType.Left,
            _ => null
        };
}

public class PostgresForClause {
    //&& args.SingleOrDefault()?.Expression is LiteralExpressionSyntax {Token.Value: int itemCount}
    public static string? OfMethodName(string methodName, ArgumentSyntax? arg) =>
        methodName switch {
            Consts.PostgresForUpdateQueryMethodName => "FOR UPDATE",
            Consts.PostgresForShareQueryMethodName => "FOR SHARE",
            Consts.PostgresForNoKeyUpdateQueryMethodName => "FOR NO KEY UPDATE",
            Consts.PostgresWithForClauseQueryMethodName => arg?.Expression switch {
                 LiteralExpressionSyntax {Token.Value: string content} => $"FOR {content}",
                 _ => throw new Exception($"unexpected argument of {Consts.PostgresWithForClauseQueryMethodName}, expected string literal but got {arg?.Expression}")
            },
            _ => null
        };
}
    
public record WhereExpression {
    private WhereExpression() {}

    public record Sles(SimpleLambdaExpressionSyntax Les) : WhereExpression();
    public record Ples(ParenthesizedLambdaExpressionSyntax Les) : WhereExpression();
}
    
public record OrderByExpression {
    private OrderByExpression() {}

    public record Sles(SimpleLambdaExpressionSyntax Les, AscOrDesc Dir) : OrderByExpression();
    public record Ples(ParenthesizedLambdaExpressionSyntax Les, AscOrDesc Dir) : OrderByExpression();
}
        
public record JoinExpression {
    private JoinExpression() {}

    public record SingleSles(SimpleLambdaExpressionSyntax Les, JoinType JoinType) : JoinExpression();
    public record SinglePles(ParenthesizedLambdaExpressionSyntax Les, JoinType JoinType) : JoinExpression();
        
    public record InverseJoinToSles(ParenthesizedLambdaExpressionSyntax PlesOf, SimpleLambdaExpressionSyntax SlesTo, JoinType JoinType) : JoinExpression();
    public record InverseJoinToPles(ParenthesizedLambdaExpressionSyntax PlesOf, ParenthesizedLambdaExpressionSyntax PlesTo, JoinType JoinType) : JoinExpression();
}
    
public class DefaultQueryGenerator : IQueryGenerator {
    private readonly IDatabaseDotnetDataMapperGenerator _mapper;
    private readonly GeneratorOptions _opt;
    private readonly ISqlParamNamingStrategy _naming;
    private readonly List<QueryToGenerateFromSyntax> _queries = new(); 
    private readonly string _adoDbConnectionFullClassName;
        
    public DefaultQueryGenerator(
        string adoDbConnectionFullClassName,
        IDatabaseDotnetDataMapperGenerator mapper, GeneratorOptions opt, ISqlParamNamingStrategy naming) {
            
        _adoDbConnectionFullClassName = adoDbConnectionFullClassName;
        _mapper = mapper;
        _opt = opt;
        _naming = naming;
    }

    private (IdentifierNameSyntax,List<Tuple<MemberAccessExpressionSyntax, InvocationExpressionSyntax>>)? 
            MaybeInsFollowedByInvocations(ExpressionSyntax expr) {
        
        var raw = new List<ExpressionSyntax>();
        
        while (true) {
            if (expr is InvocationExpressionSyntax x) {
                raw.Insert(0, expr);
                expr = x.Expression;
                continue;
            }

            if (expr is MemberAccessExpressionSyntax y) {
                raw.Insert(0, expr);
                expr = y.Expression;
                continue;
            }

            if (expr is IdentifierNameSyntax) {
                raw.Insert(0, expr);
            }
                
            if (raw.Count < 3) {
                return null;
            }

            break;
        }
        
        var ins = raw[0] as IdentifierNameSyntax;

        if (ins == null) {
            return null;
        }
        raw.RemoveAt(0);
        
        if (raw.Count % 2 != 0) {
            return null; //expected pairs
        }

        var invocations = new List<Tuple<MemberAccessExpressionSyntax, InvocationExpressionSyntax>>();
        
        while (raw.Any()) {
            if (raw[0] is MemberAccessExpressionSyntax { } maes && raw[1] is InvocationExpressionSyntax { } ies) {
                invocations.Add(Tuple.Create(maes,ies));
                raw.RemoveAt(0);
                raw.RemoveAt(0);
                continue;
            }

            return null; //expected different types
        }
        
        return (ins, invocations);
    }

    record MultiTableQueryAcc(
            IdentifierNameSyntax? RootType,
            List<JoinExpression> Joins,
            ParenthesizedLambdaExpressionSyntax? SelectBody,
            WhereExpression? WhereBody,
            List<OrderByExpression> OrderByExprs,
            string? PostgresForClause,
            int? TakeItemCount) {
        
        public static MultiTableQueryAcc CreateEmpty() => 
            new MultiTableQueryAcc(null, new List<JoinExpression>(), null, null, new List<OrderByExpression>(), null, null);

        public MultiTableQueryAcc WithJoin(JoinExpression join) => 
            this with {Joins = Joins.FluentAdd(join)};
        
        public MultiTableQueryAcc WithOrderBy(OrderByExpression orderBy) => 
            this with {OrderByExprs = OrderByExprs.FluentAdd(orderBy)};
    }

    private RequestedQuery? MaybeExtractMultiTableQuery(
            MultiTableQueryAcc acc,
            IEnumerable<Tuple<MemberAccessExpressionSyntax,InvocationExpressionSyntax>> invocations) {
        
        var (head, tail) = invocations.MaybeHeadAndTail();
        
        return head switch {
            //From
            {
                Item1.Name: GenericNameSyntax gns
            }
                when acc.RootType is null && gns.TypeArgumentList.Arguments.SingleOrDefault() is IdentifierNameSyntax ins 
            => MaybeExtractMultiTableQuery(acc with {RootType = ins}, tail),
            
            //Join
            {Item1.Name: {} maesName, Item2.ArgumentList: {} als }
                when maesName.GetNameAsText() is {} methodName && JoinTypeUtil.OfMethodName(methodName) is {} joinType
            => als.Arguments.Count switch {
                1 => als.Arguments.SingleOrDefault()?.Expression switch {
                    SimpleLambdaExpressionSyntax sles => MaybeExtractMultiTableQuery(
                        acc.WithJoin(new JoinExpression.SingleSles(sles, joinType)), tail),
                    ParenthesizedLambdaExpressionSyntax ples => MaybeExtractMultiTableQuery(
                        acc.WithJoin(new JoinExpression.SinglePles(ples, joinType)), tail),
                    _ => null
                },
                2 => als.Arguments.FirstAndSecondOrDefault() switch {
                    {
                        Item1.Expression: ParenthesizedLambdaExpressionSyntax {} f,
                        Item2.Expression: SimpleLambdaExpressionSyntax {} s
                    } => MaybeExtractMultiTableQuery(
                        acc.WithJoin(new JoinExpression.InverseJoinToSles(f, s, joinType)), tail),
                    {
                        Item1.Expression: ParenthesizedLambdaExpressionSyntax {} f,
                        Item2.Expression: ParenthesizedLambdaExpressionSyntax {} s
                    } => MaybeExtractMultiTableQuery(
                        acc.WithJoin(new JoinExpression.InverseJoinToPles(f, s, joinType)), tail),
                    _ => null
                },
                _ => null,
            },
            
            //Where
            {
                Item1.Name: {} maesName,
                Item2.ArgumentList: {} al
            } when maesName.GetNameAsText() == Consts.WherePocoQueryMethodName 
            => 
                acc.WhereBody is {}
                ? throw new Exception($"cannot request '{Consts.WherePocoQueryMethodName}' more than once in the same query")
                : al.Arguments.FirstOrDefault()?.Expression switch {
                        ParenthesizedLambdaExpressionSyntax ples => MaybeExtractMultiTableQuery(
                            acc with {WhereBody = new WhereExpression.Ples(ples)}, tail),
                        SimpleLambdaExpressionSyntax sles => MaybeExtractMultiTableQuery( 
                            acc with {WhereBody = new WhereExpression.Sles(sles)}, tail),
                        _ => null
                    },
            
            //Select
            {
                Item1.Name: {} maesName,
                Item2.ArgumentList: {} al
            } when 
                maesName.GetNameAsText() == Consts.SelectPocoQueryMethodName &&
                al.Arguments.FirstOrDefault()?.Expression is ParenthesizedLambdaExpressionSyntax ples
            => 
                acc.SelectBody is {}
                ? throw new Exception($"cannot request '{Consts.SelectPocoQueryMethodName}' more than once in the same query")
                : MaybeExtractMultiTableQuery(acc with {SelectBody = ples}, tail),
            
            //OrderByAsc, OrderByDesc
            {
                 Item1.Name: {} maesName,
                 Item2.ArgumentList.Arguments: {} args
            } when 
                maesName.GetNameAsText() is {} methodName &&
                AscOrDescUtil.OfMethodName(methodName) is {} ascOrDesc
                => args.FirstOrDefault()?.Expression switch {
                    SimpleLambdaExpressionSyntax sles => MaybeExtractMultiTableQuery(
                        acc.WithOrderBy(new OrderByExpression.Sles(sles, ascOrDesc)), tail),
                    ParenthesizedLambdaExpressionSyntax ples => MaybeExtractMultiTableQuery(
                        acc.WithOrderBy(new OrderByExpression.Ples(ples, ascOrDesc)), tail),
                    _ => null,
                },
            
            //Take
            {
                Item1.Name: {} maesName,
                Item2.ArgumentList.Arguments: {} args
            } when 
                maesName.GetNameAsText() is {} methodName &&
                methodName == Consts.TakePocoQueryMethodName &&
                args.SingleOrDefault()?.Expression is LiteralExpressionSyntax {Token.Value: int itemCount} 
                => 
                    acc.TakeItemCount is {}
                    ? throw new Exception($"cannot request '{Consts.TakePocoQueryMethodName}' more than once in the same query")
                    : MaybeExtractMultiTableQuery(
                        acc with {TakeItemCount = itemCount}, tail),
            
            //Postgresql 'select ... for' clause
            {
                Item1.Name: {} maesName, Item2.ArgumentList.Arguments: {} args
            } when
                maesName.GetNameAsText() is {} methodName &&
                PostgresForClause.OfMethodName(methodName, args.SingleOrDefault()) is {} forClauseStr
                => 
                    !_adoDbConnectionFullClassName.Contains(Consts.SubstringIndicatingAdoClassIsForPostgres)
                    ? throw new Exception($"not allowed to request postgres specific method {methodName} when ADO NET class is indicating nonpostgres database engine")
                    : (acc.PostgresForClause is {}
                    ? throw new Exception($"cannot request postgres 'for' clause more than once in the same query")
                    : MaybeExtractMultiTableQuery(
                        acc with {PostgresForClause = forClauseStr}, tail)),
            
            null => new RequestedQuery.MultiTableQuery(
                acc.RootType!, acc.Joins, acc.WhereBody, acc.SelectBody, acc.OrderByExprs, acc.TakeItemCount, 
                acc.PostgresForClause),
            
            _ => throw new Exception($"bug? there were {tail.Count()+1} expressions left after collection digestion")
        };
    }
    
    private RequestedQuery? MaybeExtractCondition(ExpressionSyntax expr) =>
        MaybeInsFollowedByInvocations(expr) switch {
            ({ } ins, { } invocations) when 
                new[]{Consts.GenerateQueryClassName1,Consts.GenerateQueryClassName2}.Contains(ins.GetNameAsText()) &&
                invocations.FirstOrDefault()?.Item1.Name.GetNameAsText() is {} methodName
            => methodName switch {
                    Consts.GenerateSimpleQueryMethod =>
                        invocations.First().Item2.ArgumentList.Arguments.SingleOrDefault()?.Expression switch {
                            ParenthesizedLambdaExpressionSyntax ples => new RequestedQuery.SingleTableQuery(ples),
                            _ => null
                        },
                    Consts.FromPocoQueryMethodName => MaybeExtractMultiTableQuery(MultiTableQueryAcc.CreateEmpty(),
                        invocations),
                    _ => null
                },
            _ => null
        };

    private QueryToGenerateFromSyntax? MaybeExtractQueryFromMethod(Compilation c, MethodDeclarationSyntax mds) {
        var syntaxNode = mds;
    
        var outerPrms = mds.ParameterList.Parameters.ToList();
            
        var queryName = mds.Identifier.Text;

        var aecs = syntaxNode.ChildNodes().WhereIsOfType<ArrowExpressionClauseSyntax>().ToList();
        var bses = syntaxNode.ChildNodes().WhereIsOfType<BlockSyntax>().ToList();

        var maybeIes = (aecs, bses) switch {
            ({Count: 1} aecColl, {Count: <= 0})
                when
                    aecColl.FirstOrDefault()?.Expression is InvocationExpressionSyntax ies
                => ies,
            ({Count: <= 0}, {Count: 1} bsColl)
                when
                    bsColl.FirstOrDefault()?.Statements.WhereIsOfType<ExpressionStatementSyntax>().ToList() is { } esses &&
                    esses.SingleOrDefault()?.Expression is InvocationExpressionSyntax ies
                => ies,
            _ => null
        };
            
        var maybeLes = maybeIes switch {
            {} x => MaybeExtractCondition(x),
            _ => null
        };
            
        return maybeLes switch {
            {} x => new QueryToGenerateFromSyntax(
                c, syntaxNode.SyntaxTree.GetCompilationUnitRoot(), syntaxNode.SyntaxTree, queryName, x, outerPrms),
            _ => null
        };
    }
    
    public void OnElement(Compilation c, SyntaxNode maybeClassSyntaxNode) {
        var result = maybeClassSyntaxNode switch {
            ClassDeclarationSyntax cds 
                when 
                    cds.AttributeLists
                    .SelectMany(als => als.Attributes.Where(
                        atrSntx => 
                            atrSntx.Name.GetNameAsText() is {} n && 
                            (Consts.GenerateQueryAttributeShortName == n || Consts.GenerateQueryAttributeFullName == n)))
                    .Any() 
                => cds.Members
                      .Select(x => x switch {
                              MethodDeclarationSyntax mds => MaybeExtractQueryFromMethod(c, mds),
                              _ => null
                          })
                      .WhereIsNotNull(),
            _ => Array.Empty<QueryToGenerateFromSyntax>()
        };
        
        _queries.AddRange(result);
    }

    private string GenerateCsForQuery(PocoSchema metaData, QueryToGenerateFromSyntax rawInp) {
        var tableAliasNameBuilder = _mapper.BuildTableAliasProvider();
        var inp = rawInp.ToTreated(_opt, metaData, tableAliasNameBuilder);
  
        return DatabaseClassGenerator.MaybeBuildSelectMethodAsCsCode(
            inp.Name, null, _naming, _mapper, _opt.DatabaseClassFullName, inp);
    }
        
    public ISet<SimpleNamedFile> GenerateFiles(PocoSchema metaData) {
        var queriesCs = _queries
            .Select(x => GenerateCsForQuery(metaData, x))
            .ConcatenateUsingNewLine();

        var createSqlParamMethods = DatabaseClassGenerator.CreateSqlParamMethods(_adoDbConnectionFullClassName); 
            
        return 
            new SimpleNamedFile(
                "queries.cs", 
                $@"
using System;
using System.Linq;

namespace {_opt.DatabaseClassNameSpace} {{
	public static class {_opt.DatabaseClassSimpleName}Extensions {{
        public static I Also<I>(this I self, Action<I> map) {{
            map(self);
            return self;
        }}

{queriesCs}

{createSqlParamMethods}
    }}
}}
").AsSingletonSet();
    }
}