using System.Globalization;
using System.Text.RegularExpressions;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public class SqlServerMapper : IDatabaseDotnetDataMapperGenerator {
    private readonly Func<DotnetTypeDescr, SqlServerUserDataTypeInfo?> _userDataTypeInfoForArrayParameter;
    public string SqlLiteralForTrue => "cast(1 as bit)";
    public string SqlLiteralForFalse => "cast(0 as bit)";

    public Func<string> BuildTableAliasProvider() => new SingleUseBuildTableAliasProvider().Build;
        
    public string QuoteSqlIdentifier(string identifier) => "[" + identifier.Replace("[", "[[").Replace("]", "]") + "]";
        
    private static string[] varCharTypes = { "text", "char", "character", "nchar", "varchar", "nvarchar", "charvarying", "charactervarying" };
    private static string[] varBinTypes = { "varbinary" };
        
    private static readonly Regex reNameThenOptionalLengthInBrackets = new("^([a-z ]+)((?:\\(\\-?[0-9]+\\)$)|$)");

    public SqlServerMapper(Func<DotnetTypeDescr,SqlServerUserDataTypeInfo?>? userDataTypeInfoForArrayParameter = null) {
        _userDataTypeInfoForArrayParameter = userDataTypeInfoForArrayParameter ?? (_ => null);
    }

    public DotnetTypeDescr DbTypeNameToDotnetTypeName(SqlTable t, SqlColumn c) {
        var typeName = c.Type.ToLower(CultureInfo.InvariantCulture);
        var nameLengthMatch = reNameThenOptionalLengthInBrackets.Match(typeName);

        var fullClassName = typeName switch {
            _ when nameLengthMatch.Success && varCharTypes.Contains(nameLengthMatch.Groups[1].Value) => "string",
            _ when nameLengthMatch.Success && varBinTypes.Contains(nameLengthMatch.Groups[1].Value) => "byte[]",
            _ when nameLengthMatch.Success && "ntext" == nameLengthMatch.Groups[1].Value => "string",
            "int" => "int",
            "tinyint" => "byte",
            "smallint" => "short",
            "bigint" => "long",
            "float" => "double",
            "timestamp" => "byte[]",
            "bit" => "bool",
            "date" => "System.DateTime",
            "smalldatetime" => "System.DateTime",
            "datetime" => "System.DateTime",
            "datetime2" => "System.DateTime",
            "time" => "System.DateTime",
            "numeric" => "decimal",
            "decimal" => "decimal",
            "uniqueidentifier" => "string",
            _ => throw new Exception(
                nameof(SqlServerMapper) + " " + nameof(DbTypeNameToDotnetTypeName) +
                " Don't know how to map type: "+c.Type)
        };

        return DotnetTypeDescr.CreateOf(fullClassName, c.Nullable);
    }
        
    public string CsCodeToMapDatabaseRawObjectToPoco(SqlTable t, SqlColumn c, string objectProducerCodeToWrap) {
        var res = DbTypeNameToDotnetTypeName(t, c);
            
        return
            !res.IsNullable 
                ? $"({res.NamespaceAndClassNameAndMaybeArray}){objectProducerCodeToWrap}" 
                : $"{objectProducerCodeToWrap} switch {{ System.DBNull => null, var x => ({res.NamespaceAndGenericClassName})x}}";
    }

    //in sqlserver OUTPUT clause doesn't properly work with triggers
    //https://docs.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql?view=sql-server-2017
    public string GenerateInsertFromParametersThenSelect(
        string tableName,
        string insertColumnNamesVar, string insertColumnParamsVar,
        string selectableColumnsNames,
        IReadOnlyCollection<string> pkColsWithAutoIncrement,
        IReadOnlyCollection<(string ColumnName, string ParamName)> pkColWithoutAutoIncrement) =>
        DatabaseDotnetDataMapperGeneratorUtil.GenerateInsertFromParametersThenSelect(
            sqlPhraseToGetLastInsertedId: "scope_identity()",
            tableName,
            insertColumnNamesVar, insertColumnParamsVar,
            selectableColumnsNames,
            pkColsWithAutoIncrement,
            pkColWithoutAutoIncrement);

    //rowversion updates everytime row is updated, even in the same running transaction
        
    public bool ConcurrencyTokenIsStableInTransaction => false;
        
    //in sqlserver OUTPUT clause doesn't properly work with triggers
    //https://docs.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql?view=sql-server-2017
    //rowversion updates everytime row is updated, even in the same running transaction
    public string GenerateUpdateFromParametersThenSelect(string tableNameWithSchema,
        string updatableColNameToParamName,
        IReadOnlyCollection<(SqlColumnForCodGen col, string colToParamName)> identifierCols,
        string selectableColumnsNames) =>
        $@"
UPDATE {tableNameWithSchema} 
SET {updatableColNameToParamName}
WHERE {identifierCols.Select(x => x.colToParamName).ConcatenateUsingSep(" AND ")};

SELECT @@ROWCOUNT, {selectableColumnsNames}
FROM {tableNameWithSchema}
WHERE {identifierCols.Where(x=> !x.col.Col.IsConcurrencyToken).Select(x => x.colToParamName).ConcatenateUsingSep(" AND ")};";

    public string GenerateDeleteFromParameters(
        string tableNameWithSchema, string identifierColNameToParamName) => $@"
DELETE FROM {tableNameWithSchema} 
WHERE {identifierColNameToParamName}";
        
    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(SqlTable t, SqlColumn c) =>
        DbTypeNameToDotnetTypeName(t, c).NamespaceAndClassNameAndMaybeArray switch {
            "byte[]" => "System.Data.SqlDbType.VarBinary",
            "System.DateTime" => "System.Data.SqlDbType.DateTime2",
            _ => null };
        
        
    public string? CsCodeForDbSpecificOptionalParamOfCreateSqlParameterOrNull(DotnetTypeDescr t) =>
        t.NamespaceAndClassNameAndMaybeArray switch {
            "byte[]" => "System.Data.SqlDbType.VarBinary",
            "System.DateTime" => "System.Data.SqlDbType.DateTime2",
            _ => null };
        
    public QueryChunk GenerateExpressionValueIsContainedInCollection(QueryChunk lookedVal, QueryChunk collToLookInto) =>
        new(
            $"{lookedVal.LiteralSql} in (select V from {collToLookInto.LiteralSql})",
            IsNull: false,
            (lookedVal.SqlParams ?? new SqlParamInfo[0])
            .Concat(collToLookInto.SqlParams ?? new SqlParamInfo[0])
            .ToList());

    //leverages "user data type" https://docs.microsoft.com/en-us/sql/t-sql/statements/create-type-transact-sql?view=sql-server-ver15
    public string? CustomCsCodeToMapDotnetValueAsSqlParameter(SqlParamNamingResult name, DotnetTypeDescr t, string csValueHolder) {
        if (t.NamespaceAndClassNameAndMaybeArray.EndsWith("[]")) {
            var info = _userDataTypeInfoForArrayParameter(t);

            if (info == null) {
                throw new Exception(
                    $@"SQL Server doesn't have built in support for requested array based parameter {t.NamespaceAndGenericClassName}. 
Support for array is provided via tabular user defined type and SqlDbType.Structured. 
It requires you to provide name of tabular user data type and its sole column. 
Please provide it via constructor of {typeof(SqlServerMapper).FullName} ");
            }

            //hatch to avoid: DataSet does not support System.Nullable<>
            var itemType = t.ArrayItemType.EndsWith("?") ? t.ArrayItemType.Substring(0, t.ArrayItemType.Length-1) : t.ArrayItemType;
                
            return $@"cmd.Parameters.Add(cmd.CreateParameter().Also(x => {{
    x.SqlDbType = System.Data.SqlDbType.Structured;
    x.ParameterName = {name.LogicalName.StringAsCsCodeStringValue()};
    x.TypeName = ""{info.userDataTypeName}"";
    x.Value = new System.Data.DataTable()
        .Also(dt => dt.Columns.Add(new System.Data.DataColumn(""{info.valueColumnName}"", typeof({itemType}))))
        .Also(dt => {{ foreach (var itm in p) {{ dt.Rows.Add(itm); }} }}); 
}}));";
        }

        return null; //no custom code needed
    }

    public string? GenerateLimitItemsCountClause(int count) {
        return null; //maybe revisit in future to implement as OFFSET. Not sure as ORDER BY uniqueness seems to be quirky
    }

    public string GenerateTopItemsCountClause(int count) => $"TOP {count}";
}