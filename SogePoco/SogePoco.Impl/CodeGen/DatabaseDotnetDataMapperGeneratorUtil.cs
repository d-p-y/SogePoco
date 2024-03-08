using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.CodeGen; 

public static class DatabaseDotnetDataMapperGeneratorUtil {
    public static string GenerateInsertFromParametersThenSelect(
        string sqlPhraseToGetLastInsertedId,
        string tableName,
        string insertColumnNamesVar, string insertColumnParamsVar,
        string selectableColumnsNames,
        IReadOnlyCollection<string> pkColsWithAutoIncrement,
        IReadOnlyCollection<(string ColumnName, string ParamName)> pkColWithoutAutoIncrement) {
                    
        var colEqVal =
            pkColsWithAutoIncrement
                .Select(x => $"{x} = {sqlPhraseToGetLastInsertedId}")
                .Concat(
                    pkColWithoutAutoIncrement
                        .Select(x => $"{x.ColumnName} = {x.ParamName}"))
                .ConcatenateUsingSep(" AND ");
                    
        return $@"
    INSERT INTO {tableName} ({{{insertColumnNamesVar}}})
    VALUES ({{{insertColumnParamsVar}}}); 
    SELECT {selectableColumnsNames} FROM {tableName} WHERE {colEqVal};";
    }
}