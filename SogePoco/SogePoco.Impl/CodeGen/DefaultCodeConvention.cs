using SogePoco.Impl.Extensions;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public class DefaultCodeConvention : ICodeConvention {
    public IReadOnlyCollection<SqlColumn> SortColumns(IReadOnlyCollection<SqlColumn> cols) =>
        cols.OrderBy(y => y.PrimaryKeyIdx ?? 1000)
            .ThenBy(y => y.Name)
            .ToList();

    public IReadOnlyCollection<SqlForeignKey> SortForeignKeys(IReadOnlyCollection<SqlForeignKey> fks) =>
        fks.OrderBy(y => y.SomeStableSortingKey).ToList();

    public string BuildDotnetClassNameFromSqlTableName(int classIndex, string inp) => 
        inp.ToUpperCamelCaseOrNull() ?? $"Class{classIndex}";

    public string BuildDotnetConstructorParamPropertyNameFromSqlColumnName(
        int classIndex, string sqlTableName, int propertyIndex, string sqlColumnName) =>
        sqlColumnName.ToLowerCamelCaseOrNull() ?? $"property{propertyIndex}";
            
    public string BuildDotnetPropertyNameFromSqlColumnName(
        int classIndex, string sqlTableName, int propertyIndex, string sqlColumnName) =>
        sqlColumnName.ToUpperCamelCaseOrNull() ?? $"Property{propertyIndex}";
}