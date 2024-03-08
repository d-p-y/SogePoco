using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.Tests.Schemas; 

public interface ITestingSchema {
    public Task CreateSchema(DbConnection dbConn);
    public Task<TestData> CreateData(DbConnection dbConn);
    public IEnumerable<SqlTable> GetAsSyntheticModel();
        
    string? FooTable_ConcurrencyTokenPropertyName { get; }
    string? TableWithCompositePk_ConcurrencyTokenPropertyName { get; }
    string FooTable_AboolPropertyName { get; }
    object FooTable_AboolTrueValue { get; }
        
    string FooTableName { get; }
    string FooTable_IdColumnName { get; }
    string FooTable_AboolColumnName { get; }
        
    string TableWithCompositePkName { get; }
    string TableWithCompositePk_IdColumnName { get; }
    string TableWithCompositePk_YearColumnName { get; }
}