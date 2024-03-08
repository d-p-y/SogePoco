namespace SogePoco.Impl.Model; 

public record SqlParamNamingResult(string LogicalName,string UseInSqlSnippet);
    
public interface ISqlParamNamingStrategy {
    string NameForParameter(int idx);
    string NameForParameterUsage(int idx);
}

public static class SqlParamNamingStrategyExtensions {
    public static SqlParamNamingResult Build(this ISqlParamNamingStrategy self, int idx) =>
        new(self.NameForParameter(idx), self.NameForParameterUsage(idx));
}