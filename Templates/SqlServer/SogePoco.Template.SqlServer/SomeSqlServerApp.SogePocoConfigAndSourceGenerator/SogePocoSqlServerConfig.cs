using System.Data.SqlClient;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

#nullable enable

public class SogePocoSqlServerConfig : IConfiguration {
    public Microsoft.Extensions.Logging.ILogger? Logger { get; } 
        //example:
        //= new LogToFile("/tmp/sourcegeneratorlog.txt");

    public string DeveloperConnectionString => throw new Exception("missing: connection string to your developer sqlserver database");
        //example:
        //=> "Data Source=127.0.0.1,14332;Initial Catalog=master;Trusted_Connection=False;User id=sogepoco_tester_user;Password=sogepoco_tester_passwd;Connection Timeout=2;TrustServerCertificate=True";

    public string SchemaDirPath => throw new Exception("missing: directory that contains your dbschema.json file");
        //example:
        //=> "/home/dominik/Projects/soge_poco/Templates/SogePoco.Template.SqlServer/SomeSqlServerApp.SogePocoConfigAndSourceGenerator";

    public GeneratorOptions Options { get; } = new GeneratorOptions() {
        DatabaseClassFullName = "SomeSqlServerApp.Database",
        PocoClassesNameSpace = "SomeSqlServerApp.Pocos"
    };
    public ICodeConvention Convention { get; } = new DefaultCodeConvention();
    public IDatabaseDotnetDataMapperGenerator Mapper { get; } = new SqlServerMapper();
    public ISqlParamNamingStrategy Naming { get; } = new SqlServerNaming();
    public ISchemaExtractor Extractor { get; } = new SqlServerSchemaExtractor();
    public Func<string,System.Data.Common.DbConnection> ConnectionFactory {get;} = x => new SqlConnection(x);
}
