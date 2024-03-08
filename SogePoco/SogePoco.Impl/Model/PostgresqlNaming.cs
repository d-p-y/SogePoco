using System.Globalization;

namespace SogePoco.Impl.Model; 

//https://www.npgsql.org/doc/basic-usage.html
public class PostgresqlNaming : ISqlParamNamingStrategy {
    public string NameForParameter(int i) => "p" + i.ToString(CultureInfo.InvariantCulture);
    public string NameForParameterUsage(int i) => ":p" + i.ToString(CultureInfo.InvariantCulture);
}