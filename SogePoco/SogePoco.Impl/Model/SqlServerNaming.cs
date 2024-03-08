using System.Globalization;

namespace SogePoco.Impl.Model; 

public class SqlServerNaming : ISqlParamNamingStrategy {
    public string NameForParameter(int i) => '@' + i.ToString(CultureInfo.InvariantCulture);
    public string NameForParameterUsage(int i) => '@' + i.ToString(CultureInfo.InvariantCulture);
}