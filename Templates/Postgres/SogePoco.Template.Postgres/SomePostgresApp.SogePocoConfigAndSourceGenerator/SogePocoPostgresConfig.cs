using Npgsql;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

#nullable enable

public class SogePocoPostgresConfig : IConfiguration {
    public Microsoft.Extensions.Logging.ILogger? Logger { get; } 
        //example:
        //= new LogToFile("/tmp/sourcegeneratorlog.txt");

    public string DeveloperConnectionString => throw new Exception("missing: connection string to your developer postgres database");
        //example:
        //=> "Host=127.0.0.1;Port=54330;Username=sogepoco_tester_user;Password=sogepoco_tester_passwd;Database=sogepoco_tester_db";

    public string SchemaDirPath => throw new Exception("missing: directory that contains your dbschema.json file");
        //example:
        //=> "/home/dominik/Projects/soge_poco/Templates/SogePoco.Template.Postgres/SomePostgresApp.SogePocoConfigAndSourceGenerator";

    public GeneratorOptions Options { get; } = new GeneratorOptions() {
        DatabaseClassFullName = "SomePostgresApp.Database",
        PocoClassesNameSpace = "SomePostgresApp.Pocos"
    };
    public ICodeConvention Convention { get; } = new DefaultCodeConvention();
    public IDatabaseDotnetDataMapperGenerator Mapper { get; } = new PostgresqlMapper();
    public ISqlParamNamingStrategy Naming { get; } = new PostgresqlNaming();
    public ISchemaExtractor Extractor { get; } = new PostgresqlSchemaExtractor();
    public Func<string,System.Data.Common.DbConnection> ConnectionFactory {get;} = x => new NpgsqlConnection(x);
}
