using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public interface ICodeConvention {
    IReadOnlyCollection<SqlColumn> SortColumns(IReadOnlyCollection<SqlColumn> cols);
    IReadOnlyCollection<SqlForeignKey> SortForeignKeys(IReadOnlyCollection<SqlForeignKey> fks);
        
    string BuildDotnetClassNameFromSqlTableName(int classIndex, string sqlTableName);
        
    string BuildDotnetConstructorParamPropertyNameFromSqlColumnName(
        int classIndex, string sqlTableName, int propertyIndex, string sqlColumnName);
        
    string BuildDotnetPropertyNameFromSqlColumnName(
        int classIndex, string sqlTableName, int propertyIndex, string sqlColumnName);
}