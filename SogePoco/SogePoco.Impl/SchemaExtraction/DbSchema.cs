using System.Data.Common;
using System.Reflection;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.SchemaExtraction; 

public record DbSchema(
    string AdoDbConnectionFullClassName, string AdoDbCommandFullClassName, IReadOnlyCollection<SqlTable> Tables) {
        
    public static async Task<DbSchema> CreateOf(DbConnection dbConn, ISchemaExtractor schemaExtractor) => new(
        dbConn.GetType().FullName!,
        dbConn.GetType().GetMethod("CreateCommand", BindingFlags.Instance|BindingFlags.Public)!.ReturnType.FullName!,
        await schemaExtractor.ExtractTables(dbConn).ToListAsync());

    ///returns full path where schema was saved to disk 
    public static async Task<string> ExtractAndSerialize(IConfiguration cfg) {
        using var dbConn = cfg.ConnectionFactory(cfg.DeveloperConnectionString);
        await dbConn.OpenAsync();
        var dbSchema = await CreateOf(dbConn, cfg.Extractor);
        var serializedDbSchema = CodeGen.PocoClassesGenerator.SerializeDbSchema(dbSchema);
        serializedDbSchema.SaveToDisk(cfg.SchemaDirPath);
        return Path.Combine(cfg.SchemaDirPath,serializedDbSchema.FileName);
    }
        
    public static DbSchema CreateOf(DbConnection dbConn, IEnumerable<SqlTable> tables) => new(
        dbConn.GetType().FullName!,
        dbConn.GetType().GetMethod("CreateCommand", BindingFlags.Instance | BindingFlags.Public)!.ReturnType.FullName!,
        tables.ToList());
}