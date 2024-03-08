using System.Data.Common;
using System.Text.RegularExpressions;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.SchemaExtraction; 

public record SqliteColumn(string Name, string Type, long NotNull, string? DefaultValue, int? PrimaryKeyIdx, long Hidden) {
    private static Regex ReGeneratedAlways = new Regex(@"(\s+GENERATED\s+ALWAYS)$", RegexOptions.IgnoreCase);
        
    public bool TypeIsGeneratedAlways => ReGeneratedAlways.Match(Type).Success;
    public string TypeWithoutGeneratedAlways => ReGeneratedAlways.Replace(Type, "");
}

public record SqliteSqlTable(string Schema, string Name, string Sql) {
    private static Regex ReSeemsToContainAutoIncrement =
        new Regex(@"\s+autoincrement[),\s]+", RegexOptions.IgnoreCase);

    public bool SeemsToContainAutoIncrement => ReSeemsToContainAutoIncrement.Match(Sql).Success;
}

public class SqliteSchemaExtractor : ISchemaExtractor {
    private static ISqlParamNamingStrategy naming = new SqliteNaming();
        
    public static IAsyncEnumerable<SqliteColumn> ExtractColumnsForTable(DbConnection dbConn, SqlTable x) =>
        dbConn.ExecuteMappingMapAsNamesAndObjectArray(
            naming,
            y => new SqliteColumn(
                Name: y.values[y.names.IndexOf("name")] switch {
                    string s => s,
                    _ => throw new Exception("name is of unknown type")},
                Type: y.values[y.names.IndexOf("type")] switch {
                    string s => s.ToUpper(),
                    _ => throw new Exception("type is of unknown type")},
                NotNull: y.values[y.names.IndexOf("notnull")] switch {
                    long i => i,
                    _ => throw new Exception("notnull is of unknown type") },
                DefaultValue: y.values[y.names.IndexOf("dflt_value")] switch {
                    DBNull => null,
                    string s => s,
                    _ => throw new Exception("dflt_value is of unknown type")},
                PrimaryKeyIdx:y.values[y.names.IndexOf("pk")] switch {
                    long i when i<1 => null,
                    long i => (int)i-1,
                    _ => throw new Exception("pk is of unknown type") },
                Hidden:y.values[y.names.IndexOf("hidden")] switch {
                    long i => i,
                    _ => throw new Exception("hidden is of unknown type") }),
            $"PRAGMA {x.Schema}.table_xinfo({x.Name});");

    public static async IAsyncEnumerable<SqlForeignKey> ExtractForeignKeysForTable(DbConnection dbConn, SqlTable x) {
        var items = await dbConn
            .ExecuteMappingMapAsNamesAndObjectArray(
                naming,
                y => new {
                    Id = y.values[y.names.IndexOf("id")] switch {
                        long l => l,
                        _ => throw new Exception("id is of unknown type")},
                    Seq = y.values[y.names.IndexOf("seq")] switch {
                        long l => l,
                        _ => throw new Exception("seq is of unknown type")},
                    TableName = y.values[y.names.IndexOf("table")] switch {
                        string s => s,
                        _ => throw new Exception("table is of unknown type")},
                    From = y.values[y.names.IndexOf("from")] switch {
                        string s => s,
                        _ => throw new Exception("from is of unknown type")},
                    To = y.values[y.names.IndexOf("to")] switch {
                        string s => s,
                        _ => throw new Exception("to is of unknown type")}},
                $"PRAGMA {x.Schema}.foreign_key_list({x.Name});")
            .ToListAsync(); //force complete enumeration to avoid multiple commands (that may be unsupported by ADO driver)

        foreach (var y in items.GroupBy(y => y.Id)) {
            yield return new SqlForeignKey(
                PrimaryKeySchema:x.Schema,
                PrimaryKeyTableName: y.First().TableName,
                ForeignToPrimary: y
                    .Select(z => (foreignColumnName: z.From, primaryColumnName: z.To))
                    .ToSet());
        }
    }

    public async IAsyncEnumerable<SqlTable> ExtractTables(DbConnection dbConn) {
        var tskSqlTables =
            new[] {"main", "temp"} //https://www.sqlite.com/schematab.html
                .Select(x => (
                    schemaName:x,
                    query:$"select tbl_name,sql from {x}.sqlite_master where type='table' and tbl_name <> 'sqlite_sequence' order by tbl_name;"))
                .Select(x => 
                    dbConn.ExecuteMappingMapAsObjectArray(
                        naming,
                        r => new SqliteSqlTable(
                            Schema:x.schemaName,
                            Name: r[0] switch {
                                string s => s,
                                _ => throw new Exception("tbl_name is of unknown type") },
                            Sql:r[1] switch {
                                string s => s,
                                _ => throw new Exception("sql is of unknown type") 
                            }),
                        x.query));
            
        foreach (var tsk in tskSqlTables) {
            var sqlTables = await tsk.ToListAsync();

            foreach (var rawTbl in sqlTables) {
                var tbl = new SqlTable(
                    Schema: rawTbl.Schema,
                    Name: rawTbl.Name,
                    Columns: new HashSet<SqlColumn>(),
                    ForeignKeys: new HashSet<SqlForeignKey>());

                tbl = tbl with {
                    Columns = 
                    (await ExtractColumnsForTable(dbConn, tbl).ToHashSetAsync())
                    .Select(x => new SqlColumn(
                        Name:x.Name,
                        Type:x.TypeWithoutGeneratedAlways,
                        Nullable:x.NotNull == 0,
                        DefaultValue:x.DefaultValue,
                        PrimaryKeyIdx:x.PrimaryKeyIdx,
                        IsComputedColumn:x.Hidden switch {
                            var i when i == 2 || i == 3 => true,
                            var i when i != 2 && i != 3 && x.TypeIsGeneratedAlways => throw new Exception(
                                $"processing column {x.Name} - values of hidden and type in table_xinfo are inconsistent hidden={x.Hidden} type={x.Type}"),
                            _ => false},
                        UniqueIdentityGeneratedByDb:x.PrimaryKeyIdx.HasValue && rawTbl.SeemsToContainAutoIncrement,
                        IsConcurrencyToken:false))
                    .ToSet(),
                    ForeignKeys = await ExtractForeignKeysForTable(dbConn, tbl).ToHashSetAsync() };
                    
                yield return tbl;
            }
        }
    }
}