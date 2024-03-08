namespace SogePoco.Impl.CodeGen; 

public record PocoSchema(
    string AdoDbConnectionFullClassName, string AdoDbCommandFullClassName, 
    IReadOnlyCollection<SqlTableForCodGen> Tables) {
        
    public SqlTableForCodGen? MaybeGetTable(ParamInfoHolder fstPocoParam) =>
        Tables.FirstOrDefault(x => x.FullClassName == fstPocoParam.TypeName);
}