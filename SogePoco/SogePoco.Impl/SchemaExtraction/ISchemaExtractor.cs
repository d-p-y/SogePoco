using System.Data.Common;

namespace SogePoco.Impl.SchemaExtraction; 

public interface ISchemaExtractor {
    IAsyncEnumerable<SqlTable> ExtractTables(DbConnection dbConn);
}