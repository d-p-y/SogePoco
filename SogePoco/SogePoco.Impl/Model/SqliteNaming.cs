using System.Globalization;

namespace SogePoco.Impl.Model; 

//https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters
public class SqliteNaming : ISqlParamNamingStrategy {
    public string NameForParameter(int i) => '$' + i.ToString(CultureInfo.InvariantCulture);
    public string NameForParameterUsage(int i) => '$' + i.ToString(CultureInfo.InvariantCulture);
}