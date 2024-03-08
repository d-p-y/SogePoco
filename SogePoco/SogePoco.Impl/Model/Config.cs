using SogePoco.Impl.CodeGen;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.Model; 

public interface IConfiguration {
    public Microsoft.Extensions.Logging.ILogger? Logger {get;}
    public string DeveloperConnectionString {get;}
    public string SchemaDirPath {get;}
    public GeneratorOptions Options { get; }
    public ICodeConvention Convention { get; }
    public IDatabaseDotnetDataMapperGenerator Mapper { get; }
    public ISqlParamNamingStrategy Naming { get; }
    public ISchemaExtractor Extractor { get; }
    public Func<string,System.Data.Common.DbConnection> ConnectionFactory {get;}
}
