using System.Data.Common;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.SchemaExtraction; 

public record PostgresqlColumn(
    string TableSchema, string TableName, string ColumnName, bool IsNullable,
    string? PopulatedFromSequence, string? ColumnDefault, string DataType, long? CharMaxLen, bool IsGenerated);

public class PostgresqlSchemaExtractor : ISchemaExtractor {
    private static ISqlParamNamingStrategy naming = new PostgresqlNaming();

    private static IAsyncEnumerable<PostgresqlColumn> ExtractColumns(DbConnection dbConn) =>
        dbConn.ExecuteMappingMapAsNamesAndObjectArray(
            naming,
            y => new PostgresqlColumn(
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
                PopulatedFromSequence: y.values[y.names.IndexOf("populated_from_sequence")] switch {
                    DBNull => null,
                    string s => s,
                    _ => throw new Exception("populated_from_sequence is of unknown type")},
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
                IsGenerated:y.values[y.names.IndexOf("is_generated")] switch {
                    "ALWAYS" => true,
                    "NEVER" => false,
                    _ => throw new Exception("is_generated is of unknown type")}),
            @"select
                    pg_get_serial_sequence(table_schema||'.'||table_name, column_name) as populated_from_sequence, 
                    table_schema,table_name,column_name,is_nullable,column_default,data_type,character_maximum_length, 
                    is_generated 
                from information_schema.columns 
                where table_schema not in ('pg_catalog','information_schema');");

    public IAsyncEnumerable<SqlTable> ExtractTables(DbConnection dbConn) => PostgresqlSchemaExtractor.ExtractTables(dbConn);

    public static async IAsyncEnumerable<SqlTable> ExtractTables(DbConnection dbConn, bool addXminSystemColumn = true) {
        var rawTables = await dbConn
            .ExecuteMappingMapAsObjectArray(
                naming,
                x => new {
                    schemaname = x[0] switch {
                        string s => s,
                        _ => throw new Exception("schemaname is of unknown type") },
                    tablename = x[1] switch {
                        string s => s,
                        _ => throw new Exception("tablename is of unknown type") }},
                @"select schemaname, tablename from pg_tables as o where schemaname not in ('pg_catalog','information_schema');")
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
                        DefaultValue:x.column.PopulatedFromSequence == null ? x.column.ColumnDefault : null,
                        PrimaryKeyIdx:x.primaryKey.Where(y => y.ColumnName == x.column.ColumnName).Select(y => (int?)y.OrdinalPosition).FirstOrDefault(),
                        IsComputedColumn:x.column.IsGenerated,
                        UniqueIdentityGeneratedByDb:x.column.PopulatedFromSequence != null,
                        IsConcurrencyToken:false))
                    .ToSet(),
                ForeignKeys = tblToFrgnKeys
                    .GetValueOrInvoke((tbl.Schema, tbl.Name), () => new List<InfoSchemaForeignKey>())
                    .GroupBy(x => x.ConstraintName)
                    .Select(x => new SqlForeignKey(
                        PrimaryKeySchema:x.First().PrimarySchema,
                        PrimaryKeyTableName:x.First().PrimaryTable,
                        ForeignToPrimary:x.Select(y => (y.ForeignColumn, y.PrimaryColumn)).ToSet() ))
                    .ToSet()};

            if (addXminSystemColumn) {
                res.Columns.Add(new SqlColumn(
                    Name: "xmin",
                    Type:"XID",
                    Nullable:false,
                    DefaultValue:null,
                    PrimaryKeyIdx:null,
                    IsComputedColumn:true,
                    UniqueIdentityGeneratedByDb:false,
                    IsConcurrencyToken:true));
            }

            yield return res;
        }
    }
}