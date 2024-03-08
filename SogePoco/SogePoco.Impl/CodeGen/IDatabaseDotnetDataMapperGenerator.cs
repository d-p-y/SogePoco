using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public record SqlParamInfo(SqlParamNamingResult Name, DotnetTypeDescr SourceDotnetTypeName, string SourceCsValue);
    
public record QueryChunk(
    string LiteralSql, 
    bool IsNull, 
    IReadOnlyCollection<SqlParamInfo>? SqlParams) {
        
    public QueryBuildingResult ToQueryBuildingResult(QueryToGenerateTreated q, Func<string,bool> paramNameIsUnavailable) {
        var oldToNew = new Dictionary<string, string>();
        var sanitizingPrefix = "p_";
        
        var sanitizedPrmsInfos = q.GetOuterPrmsInfos().Select(x => {
            if (!paramNameIsUnavailable(x.VariableName)) {
                return x;
            }

            var newName = sanitizingPrefix + x.VariableName;
            
            oldToNew.Add(x.VariableName, newName);
            return x with {VariableName = newName};
        }).ToList();

        var sanitizedSqlParams = 
            (SqlParams ?? Array.Empty<SqlParamInfo>())
            .Select(x => oldToNew.TryGetValue(x.SourceCsValue, out var newName) ? x with {SourceCsValue = newName} : x)
            .ToList(); 
        
        return new QueryBuildingResult(
            LiteralSql,
            sanitizedPrmsInfos,
            sanitizedSqlParams);
    }
}

public record SqlServerUserDataTypeInfo(string userDataTypeName, string valueColumnName);
    
/// <summary>
/// db vendor specific mapping
/// </summary>
public interface IDatabaseDotnetDataMapperGenerator {
    string SqlLiteralForTrue { get; }
    string SqlLiteralForFalse { get; }

    Func<string> BuildTableAliasProvider();
        
    string QuoteSqlIdentifier(string identifier);
    DotnetTypeDescr DbTypeNameToDotnetTypeName(SqlTable t, SqlColumn c);
    string CsCodeToMapDatabaseRawObjectToPoco(SqlTable t, SqlColumn c, string objectProducerCodeToWrap);
        
    string GenerateInsertFromParametersThenSelect(
        string tableNameWithSchema,
        string insertColumnNamesVar,
        string insertColumnParamsVar,
        string selectableColumnsNames,
        IReadOnlyCollection<string> pkColsWithAutoIncrement,
        IReadOnlyCollection<(string ColumnName, string ParamName)> pkColWithoutAutoIncrement);

    string GenerateUpdateFromParametersThenSelect(string tableNameWithSchema,
        string updatableColNameToParamName,
        IReadOnlyCollection<(SqlColumnForCodGen col, string colToParamName)> identifierCols,
        string selectableColumnsNames);

    string GenerateDeleteFromParameters(
        string tableNameWithSchema, 
        string identifierColNameToParamName);
        
    string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(SqlTable t, SqlColumn c);
    string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(DotnetTypeDescr t);
        
    bool ConcurrencyTokenIsStableInTransaction { get; }
    QueryChunk GenerateExpressionValueIsContainedInCollection(QueryChunk lookedVal, QueryChunk collToLookInto);
    string? CustomCsCodeToMapDotnetValueAsSqlParameter(SqlParamNamingResult name, DotnetTypeDescr t, string csValueHolder);
        
    string? GenerateLimitItemsCountClause(int count);
    string? GenerateTopItemsCountClause(int count);
}