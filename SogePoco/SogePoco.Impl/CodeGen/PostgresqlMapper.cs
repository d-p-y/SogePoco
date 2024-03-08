using System.Globalization;
using System.Text.RegularExpressions;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public class PostgresqlMapper : IDatabaseDotnetDataMapperGenerator {
    public string SqlLiteralForTrue => "TRUE";
    public string SqlLiteralForFalse => "FALSE";
        
    public Func<string> BuildTableAliasProvider() => new SingleUseBuildTableAliasProvider().Build;
        
    public string QuoteSqlIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
        
    //https://www.npgsql.org/doc/types/basic.html
    //https://www.postgresql.org/docs/current/datatype-character.html
    private static string[] varCharTypes = {"character varying", "varchar", "character", "char", "text"};
    private static readonly Regex reLimitedTextType = new("^([a-z ]+)((?:\\([0-9]+\\)$)|$)");
        
    public DotnetTypeDescr DbTypeNameToDotnetTypeName(SqlTable t, SqlColumn c) {
        var typeName = c.Type.ToLower(CultureInfo.InvariantCulture);
        var varchar = reLimitedTextType.Match(typeName);
            
        var fullClassName = typeName switch {
            _ when varchar.Success && varCharTypes.Contains(varchar.Groups[1].Value) => "string",
            "integer" => "int",
            "serial" => "int",
            "smallint" => "short",
            "smallserial" => "short",
            "bigint" => "long",
            "bigserial" => "long",
            "xid" => "uint",
            "boolean" => "bool",
            "date" => "System.DateTime",
            "timestamp without time zone" => "System.DateTime",
            "timestamp with time zone" => "System.DateTime",
            "numeric" => "decimal",
            "decimal" => "decimal",
            "bytea" => "byte[]",
            _ => throw new Exception(
                nameof(PostgresqlMapper) + " " + nameof(DbTypeNameToDotnetTypeName) +
                " Don't know how to map type: "+c.Type)
        };

        return DotnetTypeDescr.CreateOf(fullClassName, c.Nullable);
    }

    public string CsCodeToMapDatabaseRawObjectToPoco(SqlTable t, SqlColumn c, string objectProducerCodeToWrap) {
        var res = DbTypeNameToDotnetTypeName(t, c);
            
        return !res.IsNullable 
            ? $"({res.NamespaceAndClassNameAndMaybeArray}){objectProducerCodeToWrap}" 
            : $"{objectProducerCodeToWrap} switch {{ System.DBNull => null, var x => ({res.NamespaceAndGenericClassName})x}}";
    }

    //returning returns actual data inserted even if there were triggers https://www.postgresql.org/docs/current/dml-returning.html
    public string GenerateInsertFromParametersThenSelect(
        string tableName,
        string insertColumnNamesVar,
        string insertColumnParamsVar,
        string selectableColumnsNames,
        IReadOnlyCollection<string> pkColsWithAutoIncrement,
        IReadOnlyCollection<(string ColumnName, string ParamName)> pkColWithoutAutoIncrement) => $@"
INSERT INTO {tableName} ({{{insertColumnNamesVar}}})
VALUES ({{{insertColumnParamsVar}}}) 
RETURNING {selectableColumnsNames};";
        
    //xmin updates only when last updater of given row is different than current transaction (even implicit one)
    public bool ConcurrencyTokenIsStableInTransaction => true;
        
    //returning returns actual data inserted even if there were triggers https://www.postgresql.org/docs/current/dml-returning.html
    //xmin updates only when last updater of given row is different than current transaction (even implicit one) 
    public string GenerateUpdateFromParametersThenSelect(string tableNameWithSchema,
        string updatableColNameToParamName,
        IReadOnlyCollection<(SqlColumnForCodGen col, string colToParamName)> identifierCols,
        string selectableColumnsNames) => $@"
UPDATE {tableNameWithSchema} 
SET {updatableColNameToParamName}
WHERE {identifierCols.Select(x => x.colToParamName).ConcatenateUsingSep(" AND ")} 
RETURNING -1, {selectableColumnsNames};";

    public string GenerateDeleteFromParameters(
        string tableNameWithSchema, string identifierColNameToParamName) => $@"
DELETE FROM {tableNameWithSchema} 
WHERE {identifierColNameToParamName}";
   
    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(SqlTable t, SqlColumn c) =>
        c.Type.ToLower(CultureInfo.InvariantCulture) switch {
            "xid" => "NpgsqlTypes.NpgsqlDbType.Xid",
            _ => null
        };

    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(DotnetTypeDescr t) =>
        t.NamespaceAndClassNameAndMaybeArray switch {
            "string" => "NpgsqlTypes.NpgsqlDbType.Text",
            "bool" => "NpgsqlTypes.NpgsqlDbType.Boolean",
            "int" => "NpgsqlTypes.NpgsqlDbType.Integer",
            "decimal" => "NpgsqlTypes.NpgsqlDbType.Numeric",
            "System.DateTime" => "NpgsqlTypes.NpgsqlDbType.Timestamp",
            _ => null
        };
        
    public QueryChunk GenerateExpressionValueIsContainedInCollection(QueryChunk lookedVal, QueryChunk collToLookInto) => 
        new(
            $"{lookedVal.LiteralSql} = ANY({collToLookInto.LiteralSql})",
            IsNull: false,
            (lookedVal.SqlParams ?? new SqlParamInfo[0])
            .Concat(collToLookInto.SqlParams ?? new SqlParamInfo[0])
            .ToList());

    public string? CustomCsCodeToMapDotnetValueAsSqlParameter(
        SqlParamNamingResult name, DotnetTypeDescr t, string csValueHolder) => null;

    public string GenerateLimitItemsCountClause(int count) => $"LIMIT {count}";
    public string? GenerateTopItemsCountClause(int count) => null; //sql server specific
}