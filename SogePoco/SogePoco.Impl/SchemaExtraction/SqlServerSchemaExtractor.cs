using System.Data.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.SchemaExtraction; 

public record SqlServerColumn(
    string TableSchema, string TableName, string ColumnName, bool IsNullable,
    bool IsIdentity, string? ColumnDefault, string DataType, long? CharMaxLen, bool IsComputed);

    
public class SqlServerSchemaExtractor : ISchemaExtractor {
    private static ISqlParamNamingStrategy naming = new SqlServerNaming();
    private static readonly string[] ConcurrencyTokenDataTypes = { "rowversion", "timestamp"};

    private static IAsyncEnumerable<SqlServerColumn> ExtractColumns(DbConnection dbConn) =>
        dbConn.ExecuteMappingMapAsNamesAndObjectArray(
            naming,
            y => new SqlServerColumn(
                TableSchema: y.values[y.names.IndexOf("table_schema")] switch {
                    string s => s,
                    _ => throw new Exception("table_schema is of unknown type")},
                TableName: y.values[y.names.IndexOf("table_name")] switch {
                    string s => s,
                    _ => throw new Exception("table_name is of unknown type")},
                ColumnName: y.values[y.names.IndexOf("column_name")] switch {
                    string s => s,
                    _ => throw new Exception("column_name is of unknown type")},
                IsNullable: y.values[y.names.IndexOf("is_nullable")] switch {
                    "YES" => true,
                    "NO" => false,
                    _ => throw new Exception("is_nullable is of unknown type") },
                IsIdentity: y.values[y.names.IndexOf("is_identity")] switch {
                    int x when  x != 0 => true,
                    int _ => false,
                    _ => throw new Exception("is_identity is of unknown type")},
                ColumnDefault: y.values[y.names.IndexOf("column_default")] switch {
                    DBNull => null,
                    string s => s,
                    _ => throw new Exception("column_default is of unknown type")},
                DataType:y.values[y.names.IndexOf("data_type")] switch {
                    string s => s.ToUpper(),
                    _ => throw new Exception("data_type is of unknown type")},
                CharMaxLen:y.values[y.names.IndexOf("character_maximum_length")] switch {
                    DBNull => null,
                    int i => i,
                    _ => throw new Exception("character_maximum_length is of unknown type")},
                IsComputed:y.values[y.names.IndexOf("is_computed")] switch {
                    int x when  x != 0 => true,
                    int _ => false,
                    _ => throw new Exception("is_generated is of unknown type")}),
            @"select
                    COLUMNPROPERTY(OBJECT_ID(table_schema + '.' + table_name), column_name,'IsIdentity') as is_identity,
                    table_schema,table_name,column_name,is_nullable,column_default,data_type,character_maximum_length,
                   COLUMNPROPERTY(OBJECT_ID(table_schema + '.' + table_name), column_name,'IsComputed') as is_computed
                from information_schema.columns;");

    public async IAsyncEnumerable<SqlTable> ExtractTables(DbConnection dbConn) {
        var query = @"select schema_name(t.schema_id), t.name from sys.tables t where t.type = 'U';";

        var rawTables = await dbConn
            .ExecuteMappingMapAsObjectArray(
                naming,
                x => new {
                    schemaname = x[0] switch {
                        string s => s,
                        _ => throw new Exception("schema_name is of unknown type") },
                    tablename = x[1] switch {
                        string s => s,
                        _ => throw new Exception("name is of unknown type") }},
                query)
            .ToListAsync();
            
        var sqlTables = rawTables
            .Select(x => new SqlTable(
                Schema:x.schemaname,
                Name: x.tablename,
                Columns: new HashSet<SqlColumn>(),
                ForeignKeys: new HashSet<SqlForeignKey>()) )
            .ToList();

        var tblToPrimKey = (await InfoSchema.ExtractPrimaryKeys(naming, dbConn).ToListAsync())
            .GroupBy(x => (x.TableSchema, x.TableName))
            .ToDictionary(x => x.Key, x => x.ToList());

        var tblToCols = 
            (await ExtractColumns(dbConn).ToListAsync())
            .GroupBy(x => (x.TableSchema, x.TableName))
            .ToDictionary(x => x.Key, x => x.ToList());

        var tblToFrgnKeys = 
            (await InfoSchema.ExtractForeignKeys(naming, dbConn).ToListAsync())
            .GroupBy(x => (x.ForeignSchema, x.ForeignTable))
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var tbl in sqlTables) {
            var res = tbl with {
                Columns = tblToCols
                    .GetValueOrInvoke((tbl.Schema, tbl.Name), () => throw new Exception($"bug, table ({tbl.Schema},{tbl.Name}) is without columns"))
                    .Select(column => 
                        (column, primaryKey:tblToPrimKey.GetValueOrInvoke((tbl.Schema, tbl.Name), () => new List<InfoSchemaPrimaryKey>()) ))
                    .Select(x => new SqlColumn(
                        Name:x.column.ColumnName,
                        Type:!x.column.CharMaxLen.HasValue ? x.column.DataType : $"{x.column.DataType}({x.column.CharMaxLen.Value})",
                        Nullable:x.column.IsNullable,
                        DefaultValue:x.column.IsIdentity ? null : x.column.ColumnDefault,
                        PrimaryKeyIdx:x.primaryKey.Where(y => y.ColumnName == x.column.ColumnName).Select(y => (int?)y.OrdinalPosition).FirstOrDefault(),
                        IsComputedColumn:x.column.IsComputed || ConcurrencyTokenDataTypes.Contains(x.column.DataType.ToLower()),
                        UniqueIdentityGeneratedByDb:x.column.IsIdentity,
                        IsConcurrencyToken:ConcurrencyTokenDataTypes.Contains(x.column.DataType.ToLower()) ))
                    .ToSet(),
                ForeignKeys = tblToFrgnKeys
                    .GetValueOrInvoke((tbl.Schema, tbl.Name), () => new List<InfoSchemaForeignKey>())
                    .GroupBy(x => x.ConstraintName)
                    .Select(x => new SqlForeignKey(
                        PrimaryKeySchema:x.First().PrimarySchema,
                        PrimaryKeyTableName:x.First().PrimaryTable,
                        ForeignToPrimary:x.Select(y => (y.ForeignColumn, y.PrimaryColumn)).ToSet() ))
                    .ToSet()};

            yield return res;
        }
    }
}