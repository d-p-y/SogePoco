using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Extensions; 

public static class DataParameterCollectionExtensions {
    public static IEnumerable<IDbDataParameter?> AsIEnumerable(this IDataParameterCollection self) {
        for (var i = 0; i < self.Count; i++) {
            yield return self[i] as IDbDataParameter;
        }
    }

    public static string ToPrettyString(this IDataParameterCollection self, string separator) =>
        string.Join(
            separator, 
            self.AsIEnumerable().Select(x => x == null ? "null" : $"ParameterName={x.ParameterName} Value={x.Value}"));
}

public static class DbConnectionExtensions {
    public static ILogger? Logger;
        
    /// <summary>returns rows affected count</summary>
    public static Task<int> ExecuteNonQueryAsync(
        this DbConnection self, ISqlParamNamingStrategy naming, string sql, params object?[] prms) {
            
        using var cmd = self.BuildCmd(naming, sql, prms);
        return cmd.ExecuteNonQueryAsync();
    }

    public static async IAsyncEnumerable<T> ExecuteMappingMapAsObjectArray<T>(
        this DbConnection self, ISqlParamNamingStrategy naming, Func<object[],T> map, string sql, params object?[] prms) {
            
        using var cmd = self.BuildCmd(naming, sql, prms); //'await using' is only available since netstandard2.1
        using var rdr = await cmd.ExecuteReaderAsync(); //'await using' is only available since netstandard2.1
  
        if (!await rdr.ReadAsync()) {
            yield break;
        }
            
        var result = new object[rdr.FieldCount];

        do {
            rdr.GetValues(result);
            yield return map(result);
        } while (await rdr.ReadAsync());
    }

    public static async IAsyncEnumerable<T> ExecuteMappingMapAsNamesAndObjectArray<T>(
        this DbConnection self, ISqlParamNamingStrategy naming, Func<(List<string> names, object[] values),T> map, 
        string sql, params object?[] prms) {
            
        using var cmd = self.BuildCmd(naming, sql, prms); //'await using' is only available since netstandard2.1
        using var rdr = await cmd.ExecuteReaderAsync(); //'await using' is only available since netstandard2.1

        if (!await rdr.ReadAsync()) {
            yield break;
        }

        var result = new object[rdr.FieldCount];
        var names = new List<string>(result.Length);
            
        for (var i = 0; i < result.Length; i++) {
            names.Add(rdr.GetName(i));
        }
            
        do {
            rdr.GetValues(result);
            yield return map((names, result));
        } while (await rdr.ReadAsync());
    }
        
    public static async Task<object?> ExecuteScalarAsync(
        this DbConnection self, ISqlParamNamingStrategy naming, string sql, params object?[] prms) {
            
        using var cmd = self.BuildCmd(naming, sql, prms); //'await using' is only available since netstandard2.1
        return await cmd.ExecuteScalarAsync();
    }
        
    public static async Task<object?> ExecuteScalarAsync(
        this DbConnection self, DbTransaction? tran, ISqlParamNamingStrategy naming, string sql, params object?[] prms) {
            
        using var cmd = self.BuildCmd(naming, sql, prms); //'await using' is only available since netstandard2.1
        cmd.Transaction = tran;
        return await cmd.ExecuteScalarAsync();
    }

    public static DbCommand BuildCmd(
        this DbConnection self, ISqlParamNamingStrategy naming, string sql, params object?[] prms) {
            
        var cmd = self.CreateCommand();
        cmd.CommandText = sql;

        prms.ForEachI((i,x) => {
            var prm = cmd.CreateParameter();
            prm.ParameterName = naming.NameForParameter(i);
            prm.Value = x;
            cmd.Parameters.Add(prm);
        });
            
        Logger?.Log(LogLevel.Debug, $"BuildCmd Parameters={cmd.Parameters.ToPrettyString("\n")} CommandText={cmd.CommandText}");
            
        return cmd;
    }
}