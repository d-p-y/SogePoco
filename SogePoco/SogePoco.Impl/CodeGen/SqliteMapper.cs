using System.Globalization;
using System.Text.RegularExpressions;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public class SqliteMapper : IDatabaseDotnetDataMapperGenerator {
        
    //safer to use 0 and 1 for backward compat https://www.sqlite.org/datatype3.html
    public string SqlLiteralForTrue => "1";
    public string SqlLiteralForFalse => "0";
        
    public Func<string> BuildTableAliasProvider() => new SingleUseBuildTableAliasProvider().Build;
        
    public string QuoteSqlIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
    public bool ConcurrencyTokenIsStableInTransaction => false;
        
    //long is the native type for storage https://www.sqlite.org/datatype3.html
    //'integer primary key' implicit means 'rowid' being long type https://sqlite.org/autoinc.html
        
    private static string[] varCharTypes = {
        "character", "varchar", "varying character", "native character", "nchar", "nvarchar", "clob"};
    private static readonly Regex reLimitedTextType = new("^([a-z ]+)((?:\\([0-9]+\\)$)|$)");
        
    public DotnetTypeDescr DbTypeNameToDotnetTypeName(SqlTable t, SqlColumn c) {
        var typeName = c.Type.ToLower(CultureInfo.InvariantCulture);
        var varchar = reLimitedTextType.Match(typeName);

        var fullClassName = typeName switch {
            _ when varchar.Success && varCharTypes.Contains(varchar.Groups[1].Value) => "string",
            "text" => "string",
            "blob" => "byte[]",
            "numeric" => "decimal",
            "date" => "System.DateTime",
            "boolean" => "bool",
            "int" => "long",
            "integer" => "long",
            "long" => "long",
            _ => throw new Exception(
                nameof(SqliteMapper) + " " + nameof(DbTypeNameToDotnetTypeName) +
                " Don't know how to map type: "+c.Type)
        };

        return DotnetTypeDescr.CreateOf(fullClassName, c.Nullable);
    }
        
    public string CsCodeToMapDatabaseRawObjectToPoco(SqlTable t, SqlColumn c, string objectProducerCodeToWrap) {
        var res = DbTypeNameToDotnetTypeName(t, c);

        //complicated because bool is actually int64 in sqlite
        return res.NamespaceAndClassNameAndMaybeArray switch {
            "bool" when res.IsNullable => 
                $"{objectProducerCodeToWrap} switch {{ System.DBNull => (bool?)null, long x when x!=0 => true, _ => false}}",
            "bool"  => 
                $"{objectProducerCodeToWrap} switch {{ long x when x!=0 => true, _ => false}}",
            "System.DateTime" when res.IsNullable => 
                $"{objectProducerCodeToWrap} switch {{ System.DBNull => null, string x => System.DateTime.Parse(x, System.Globalization.CultureInfo.InvariantCulture), var x => throw new System.Exception($\"datetime expected string input but got {{x?.GetType().FullName}}\")}}",
            "System.DateTime" => 
                $"{objectProducerCodeToWrap} switch {{ string x => System.DateTime.Parse(x, System.Globalization.CultureInfo.InvariantCulture), var x => throw new System.Exception($\"datetime expected string input but got {{x?.GetType().FullName}}\")}}", 
            "decimal" when res.IsNullable =>  
                $"{objectProducerCodeToWrap} switch {{ System.DBNull => null, double x => System.Convert.ToDecimal(x), long x => System.Convert.ToDecimal(x), var x => throw new System.Exception($\"decimal expected double input but got {{x?.GetType().FullName}}\")}}",
            "decimal" =>  
                $"{objectProducerCodeToWrap} switch {{ double x => System.Convert.ToDecimal(x), long x => System.Convert.ToDecimal(x), var x => throw new System.Exception($\"decimal expected double input but got {{x?.GetType().FullName}}\")}}",
            _ when res.IsNullable => 
                $"{objectProducerCodeToWrap} switch {{ System.DBNull => null, var x => ({res.NamespaceAndGenericClassName})x}}",
            _ => 
                $"({res.NamespaceAndClassNameAndMaybeArray}){objectProducerCodeToWrap}" 
        };
    }
//(x == null ? "null" : (x.GetType().FullName));
    //in sqlite returning doesn't report changes made by triggers https://www.sqlite.org/lang_returning.html
    public string GenerateInsertFromParametersThenSelect(
        string tableName,
        string insertColumnNamesVar, string insertColumnParamsVar,
        string selectableColumnsNames,
        IReadOnlyCollection<string> pkColsWithAutoIncrement,
        IReadOnlyCollection<(string ColumnName, string ParamName)> pkColWithoutAutoIncrement) =>
        DatabaseDotnetDataMapperGeneratorUtil.GenerateInsertFromParametersThenSelect(
            sqlPhraseToGetLastInsertedId: "last_insert_rowid()",
            tableName,
            insertColumnNamesVar, insertColumnParamsVar,
            selectableColumnsNames,
            pkColsWithAutoIncrement,
            pkColWithoutAutoIncrement);
        
    //in sqlite returning doesn't report changes made by triggers https://www.sqlite.org/lang_returning.html
    public string GenerateUpdateFromParametersThenSelect(string tableNameWithSchema,
        string updatableColNameToParamName,
        IReadOnlyCollection<(SqlColumnForCodGen col, string colToParamName)> identifierCols,
        string selectableColumnsNames) {

        var idents = identifierCols
            .Select(x => x.colToParamName)
            .ConcatenateUsingSep(" AND ");
            
        return $@"
UPDATE {tableNameWithSchema} 
SET {updatableColNameToParamName}
WHERE {idents};

SELECT changes(), {selectableColumnsNames}
FROM {tableNameWithSchema}
WHERE {idents};";
    }

    public string GenerateDeleteFromParameters(
        string tableNameWithSchema, string identifierColNameToParamName) => $@"
DELETE FROM {tableNameWithSchema} 
WHERE {identifierColNameToParamName}";
        
    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(SqlTable t, SqlColumn c) => null;
    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(DotnetTypeDescr t) => null;
        
    public string? CustomCsCodeToMapDotnetValueAsSqlParameter(SqlParamNamingResult name, DotnetTypeDescr t, string csValueHolder)
        => null;
        
    public QueryChunk GenerateExpressionValueIsContainedInCollection(QueryChunk lookedVal, QueryChunk collToLookInto) => 
        throw new NotImplementedException(
            $"value-is-in-collection syntax is not supported in {nameof(SqliteMapper)} because carray extension is not by default available in sqlite https://sqlite.org/carray.html");
        
    public string GenerateLimitItemsCountClause(int count) => $"LIMIT {count}";
    public string? GenerateTopItemsCountClause(int count) => null; //sql server specific
}