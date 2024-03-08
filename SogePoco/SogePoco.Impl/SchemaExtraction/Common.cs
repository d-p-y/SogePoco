using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.SchemaExtraction; 

/// <summary>
/// PrimaryKeyIdx is zero based index
/// IsComputedColumn column is generated/computed so no insertable or updateable
/// UniqueIdentityGeneratedByDb column is AUTO_INCREMENT/IDENTITY(int,int)
/// IsConcurrencyToken Postgres'es xmin, SqlServer's rowversion
/// </summary>
public record SqlColumn(
    string Name, string Type, bool Nullable, string? DefaultValue, int? PrimaryKeyIdx, 
    bool IsComputedColumn, bool UniqueIdentityGeneratedByDb, bool IsConcurrencyToken) {
        
    //TODO consider splitting IsConcurrencyToken into:
    // UpdateAutoMutatesUnconditionally =no matter the conditions, every update automatically increments it (sql server rowversion)
    // UpdateAutoMutatesWhenTransactionChanges = is auto incremented only when current transaction is different than latest row updater (postgresql xmin)
}

public record SqlForeignKey(
    string PrimaryKeySchema, string PrimaryKeyTableName,
    ISet<(string foreignColumnName, string primaryColumnName)> ForeignToPrimary) {

    public string SomeStableSortingKey => ForeignToPrimary.Select(x => x.foreignColumnName).Concatenate();
}
    
public record SqlTable(
    string Schema, string Name, ISet<SqlColumn> Columns,ISet<SqlForeignKey> ForeignKeys) {
        
    public IEnumerable<SqlColumn> GetPrimaryKey() => Columns.Where(x => x.PrimaryKeyIdx.HasValue).OrderBy(x => x.PrimaryKeyIdx);
}