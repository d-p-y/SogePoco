using System.Text.RegularExpressions;

namespace SogePoco.Common;

public static class Consts {
    public const string GeneratedDatabaseClassDbConnFieldName = "_dbConn";
    public const string SubstringIndicatingAdoClassIsForPostgres = "Npgsql";
    public static readonly string[] ReservedLiteralsInQueryGenerator = {"iCol", "self", "cmd", "rdr", "x"};
    public static readonly Regex[] ReservedNamesInQueryGenerator = {new ("^itm[0-9]+$")}; 
    
    public const string ErrorCallingAttemptToActuallyCallDslMethod = 
        "This code should not be called in runtime, it only servers as DSL during query generation";
    public const string ForeignKeysPropertyName = "ForeignKeys";
    public const string ForeignKeysPropertyBody = 
        $"throw new System.Exception(\"{ErrorCallingAttemptToActuallyCallDslMethod}\")";
    
    public const string ForeignKeyPropertyBody = 
        $"throw new System.Exception(\"{ErrorCallingAttemptToActuallyCallDslMethod}\")";
    
    public const string GenerateQueryAttributeShortName = SogePoco.Common.GenerateQueriesAttribute.ShortName; 
    public const string GenerateQueryAttributeFullName = SogePoco.Common.GenerateQueriesAttribute.FullName;
        
    public const string GenerateQueryClassName1 = nameof(SogePoco.Common.Query);
    public const string GenerateQueryClassName2 = nameof(SogePoco.Common.PostgresQuery);
    
    public const string GenerateSimpleQueryMethod = nameof(SogePoco.Common.Query.Register);
    public const string FromPocoQueryMethodName = nameof(SogePoco.Common.Query.From);
    public const string InnerJoinPocoQueryMethodName = nameof(SogePoco.Common.Query1<IUsesGenericSql,int>.Join);
    public const string LeftJoinPocoQueryMethodName = nameof(SogePoco.Common.Query1<IUsesGenericSql,int>.LeftJoin);
    public const string WherePocoQueryMethodName = nameof(SogePoco.Common.Query2<IUsesGenericSql,int,int>.Where);
    public const string SelectPocoQueryMethodName = nameof(SogePoco.Common.Query2<IUsesGenericSql,int,int>.Select);
    public const string OrderByAscPocoQueryMethodName = nameof(SogePoco.Common.Query1<IUsesGenericSql,int>.OrderByAsc);
    public const string OrderByDescPocoQueryMethodName = nameof(SogePoco.Common.Query1<IUsesGenericSql,int>.OrderByDesc);
    public const string TakePocoQueryMethodName = nameof(SogePoco.Common.Query1<IUsesGenericSql,int>.Take);
    
    public const string PostgresForUpdateQueryMethodName = nameof(SogePoco.Common.Query1Extensions.ForUpdate);
    public const string PostgresForShareQueryMethodName = nameof(SogePoco.Common.Query1Extensions.ForShare);
    public const string PostgresForNoKeyUpdateQueryMethodName = nameof(SogePoco.Common.Query1Extensions.ForNoKeyUpdate);
    public const string PostgresWithForClauseQueryMethodName = nameof(SogePoco.Common.Query1Extensions.WithForClause);
    
} 

//marker interfaces to expose database specific Query building methods (alternative: make poco classes implement similar marker interface but they are breaking poco definition promise)
public interface IUsesGenericSql {}
public interface IUsesPostgres {}
public interface IUsesSqlite {}
public interface IUsesSqlServer {}

public static class Query {
    public static Query1<IUsesGenericSql,PocoT> From<PocoT>() => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    public static void Register<PocoT>(Func<PocoT, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    public static void Register<PocoT, Param1T>(Func<PocoT, Param1T, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class PostgresQuery {
    public static Query1<IUsesPostgres, PocoT> From<PocoT>() => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public interface IJoinInfoTo<T> {}
public class JoinInfo<FromT,ToT> : IJoinInfoTo<ToT> {}

public class Query1<DbEngineT,PocoT1> {
    /// <summary>inner join initiated from known poco</summary>
    public Query2<DbEngineT, PocoT1, PocoT2> Join<PocoT2>(Func<PocoT1, JoinInfo<PocoT1,PocoT2>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query2<DbEngineT, PocoT1, PocoT2?> LeftJoin<PocoT2>(Func<PocoT1, JoinInfo<PocoT1,PocoT2>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query2<DbEngineT, PocoT1, PocoT2> Join<PocoT2>(Func<PocoT2,JoinInfo<PocoT2,PocoT1>> of, Func<PocoT1,PocoT1> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query2<DbEngineT, PocoT1, PocoT2> LeftJoin<PocoT2>(Func<PocoT2,JoinInfo<PocoT2,PocoT1>> of, Func<PocoT1,PocoT1> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query1<DbEngineT,PocoT1> Where(Func<PocoT1, bool> query) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    public Query1<DbEngineT,PocoT1> Select(Func<PocoT1, object?> query) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query1<DbEngineT, PocoT1> OrderByAsc(Func<PocoT1, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query1<DbEngineT, PocoT1> OrderByDesc(Func<PocoT1, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query1<DbEngineT, PocoT1> Take(int count) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query1Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query1<IUsesPostgres,PocoT1> WithForClause<PocoT1>(
            this Query1<IUsesPostgres,PocoT1> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query1<IUsesPostgres,PocoT1> ForUpdate<PocoT1>(this Query1<IUsesPostgres,PocoT1> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query1<IUsesPostgres,PocoT1> ForShare<PocoT1>(this Query1<IUsesPostgres,PocoT1> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query1<IUsesPostgres,PocoT1> ForNoKeyUpdate<PocoT1>(this Query1<IUsesPostgres,PocoT1> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query2<DbEngineT, PocoT1, PocoT2> {
    /// <summary>inner join initiated from known poco</summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> Join<PocoT3>(Func<PocoT1, PocoT2, IJoinInfoTo<PocoT3>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3?> LeftJoin<PocoT3>(Func<PocoT1, PocoT2, IJoinInfoTo<PocoT3>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> Join<PocoT3,SrcT>(Func<PocoT3,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> LeftJoin<PocoT3,SrcT>(Func<PocoT3,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query2<DbEngineT, PocoT1, PocoT2> Where(Func<PocoT1,PocoT2, bool> query) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    public Query2<DbEngineT, PocoT1, PocoT2> Select(Func<PocoT1,PocoT2, object?> query) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query2<DbEngineT, PocoT1, PocoT2> OrderByAsc(Func<PocoT1,PocoT2, object?> colOrTuple) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query2<DbEngineT, PocoT1, PocoT2> OrderByDesc(Func<PocoT1,PocoT2, object?> colOrTuple) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query2<DbEngineT, PocoT1, PocoT2> Take(int count) => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query2Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query2<IUsesPostgres, PocoT1, PocoT2> WithForClause<PocoT1, PocoT2>(
            this Query2<IUsesPostgres,PocoT1, PocoT2> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query2<IUsesPostgres, PocoT1, PocoT2> ForUpdate<PocoT1, PocoT2>(
            this Query2<IUsesPostgres,PocoT1, PocoT2> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query2<IUsesPostgres, PocoT1, PocoT2> ForShare<PocoT1, PocoT2>(
            this Query2<IUsesPostgres, PocoT1, PocoT2> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query2<IUsesPostgres, PocoT1, PocoT2> ForNoKeyUpdate<PocoT1, PocoT2>(
            this Query2<IUsesPostgres, PocoT1, PocoT2> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query3<DbEngineT, PocoT1, PocoT2, PocoT3> {
    /// <summary>inner join initiated from known poco</summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> Join<PocoT4>(Func<PocoT1, PocoT2, PocoT3, IJoinInfoTo<PocoT4>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4?> LeftJoin<PocoT4>(Func<PocoT1, PocoT2, PocoT3, IJoinInfoTo<PocoT4>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> Join<PocoT4,SrcT>(Func<PocoT4,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, PocoT3, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> LeftJoin<PocoT4,SrcT>(Func<PocoT4,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, PocoT3, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> Where(Func<PocoT1,PocoT2, PocoT3, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> Select(Func<PocoT1,PocoT2, PocoT3, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> OrderByAsc(Func<PocoT1,PocoT2, PocoT3, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> OrderByDesc(Func<PocoT1,PocoT2, PocoT3, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query3<DbEngineT, PocoT1, PocoT2, PocoT3> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query3Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> WithForClause<PocoT1, PocoT2, PocoT3>(
            this Query3<IUsesPostgres,PocoT1, PocoT2, PocoT3> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> ForUpdate<PocoT1, PocoT2, PocoT3>(
            this Query3<IUsesPostgres,PocoT1, PocoT2, PocoT3> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> ForShare<PocoT1, PocoT2, PocoT3>(
            this Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> ForNoKeyUpdate<PocoT1, PocoT2, PocoT3>(
            this Query3<IUsesPostgres, PocoT1, PocoT2, PocoT3> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> {
    /// <summary>inner join initiated from known poco</summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> Join<PocoT5>(Func<PocoT1, PocoT2, PocoT3, PocoT4, IJoinInfoTo<PocoT5>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5?> LeftJoin<PocoT5>(Func<PocoT1, PocoT2, PocoT3, PocoT4, IJoinInfoTo<PocoT5>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> Join<PocoT5,SrcT>(Func<PocoT5,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, PocoT3, PocoT4, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> LeftJoin<PocoT5,SrcT>(Func<PocoT5,IJoinInfoTo<SrcT>> of, Func<PocoT1, PocoT2, PocoT3, PocoT4, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> Where(Func<PocoT1,PocoT2, PocoT3, PocoT4, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> Select(Func<PocoT1,PocoT2, PocoT3, PocoT4, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> OrderByAsc(Func<PocoT1,PocoT2, PocoT3, PocoT4, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> OrderByDesc(Func<PocoT1,PocoT2, PocoT3, PocoT4, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query4<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query4Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> WithForClause<PocoT1, PocoT2, PocoT3, PocoT4>(
            this Query4<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4>(
            this Query4<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> ForShare<PocoT1, PocoT2, PocoT3, PocoT4>(
            this Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4>(
            this Query4<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> {
    /// <summary>inner join initiated from known poco</summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> Join<PocoT6>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, IJoinInfoTo<PocoT6>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6?> LeftJoin<PocoT6>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, IJoinInfoTo<PocoT6>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> Join<PocoT6,SrcT>(
            Func<PocoT6,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> LeftJoin<PocoT6,SrcT>(
            Func<PocoT6,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query5<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query5Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5>(
            this Query5<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5>(
            this Query5<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5>(
            this Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5>(
            this Query5<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> {
    /// <summary>inner join initiated from known poco</summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> Join<PocoT7>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, IJoinInfoTo<PocoT7>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7?> LeftJoin<PocoT7>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, IJoinInfoTo<PocoT7>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> Join<PocoT7,SrcT>(
            Func<PocoT7,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> LeftJoin<PocoT7,SrcT>(
            Func<PocoT7,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query6<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query6Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6>(
            this Query6<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6>(
            this Query6<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6>(
            this Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6>(
            this Query6<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> {
    /// <summary>inner join initiated from known poco</summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> Join<PocoT8>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, IJoinInfoTo<PocoT8>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8?> LeftJoin<PocoT8>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, IJoinInfoTo<PocoT8>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> Join<PocoT8,SrcT>(
            Func<PocoT8,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> LeftJoin<PocoT8,SrcT>(
            Func<PocoT8,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query7<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);    
}

public static class Query7Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7>(
            this Query7<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7>(
            this Query7<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7>(
            this Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7>(
            this Query7<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> {
    /// <summary>inner join initiated from known poco</summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> Join<PocoT9>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, IJoinInfoTo<PocoT9>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9?> LeftJoin<PocoT9>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, IJoinInfoTo<PocoT9>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> Join<PocoT9,SrcT>(
            Func<PocoT9,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> LeftJoin<PocoT9,SrcT>(
            Func<PocoT9,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query8<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);        
}

public static class Query8Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8>(
            this Query8<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8>(
            this Query8<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8>(
            this Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8>(
            this Query8<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> {
    /// <summary>inner join initiated from known poco</summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> Join<PocoT10>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, IJoinInfoTo<PocoT10>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join initiated from known poco</summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10?> LeftJoin<PocoT10>(
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, IJoinInfoTo<PocoT10>> how) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>inner join initiated from target table</summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> Join<PocoT10,SrcT>(
            Func<PocoT10,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>left join but initiated from target table</summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> LeftJoin<PocoT10,SrcT>(
            Func<PocoT10,IJoinInfoTo<SrcT>> of, 
            Func<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, SrcT> to) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query9<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query9Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9>(
            this Query9<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> 
                self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9>(
            this Query9<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9>(
            this Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9>(
            this Query9<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public class Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> {
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> Where(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10, bool> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> Select(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10, object?> query) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in ascending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> OrderByAsc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// order by poco's field in descending order. if you need to order by multiple criteria, just add another following OrderByAsc/OrderByDesc invocation
    /// </summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> OrderByDesc(
            Func<PocoT1,PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10, object?> colOrTuple) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>return [count] first elements</summary>
    public Query10<DbEngineT, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> Take(int count) => 
        throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}

public static class Query10Extensions {
    /// <summary>
    /// adds 'FOR' clause to query with its inner content specified in forClauseInnerContent. WithForClause("foo") yields '... FOR foo;'
    /// More info: syntax: https://www.postgresql.org/docs/current/sql-select.html 
    ///            lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> 
        WithForClause<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10>(
            this Query10<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> 
                self, string forClauseInnerContent)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);

    /// <summary>
    /// adds 'FOR UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> 
        ForUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10>(
            this Query10<IUsesPostgres,PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR SHARE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> 
        ForShare<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10>(
            this Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
    
    /// <summary>
    /// adds 'FOR NO KEY UPDATE' clause to query'
    /// More info: lock strength: https://www.postgresql.org/docs/current/explicit-locking.html#LOCKING-ROWS
    /// </summary>
    public static Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> 
        ForNoKeyUpdate<PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10>(
            this Query10<IUsesPostgres, PocoT1, PocoT2, PocoT3, PocoT4, PocoT5, PocoT6, PocoT7, PocoT8, PocoT9, PocoT10> self)
        => throw new Exception(Consts.ErrorCallingAttemptToActuallyCallDslMethod);
}
