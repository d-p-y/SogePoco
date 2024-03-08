using Microsoft.Data.Sqlite;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

#nullable enable

public class SogePocoSqliteConfig : IConfiguration {
    public Microsoft.Extensions.Logging.ILogger? Logger { get; }
        //example:
        //= new LogToFile("/tmp/sourcegeneratorlog.txt");

    public string DeveloperConnectionString => throw new Exception("missing: connection string to your developer sqlite database");
        //example: 
        //=> "Data Source=/home/dominik/Projects/soge_poco/Templates/SogePoco.Template.Sqlite/App.SogePocoConfigAndSourceGenerator/sogepoco_tester_db.db;Cache=Shared";
    
    public string SchemaDirPath => throw new Exception("missing: directory that contains your dbschema.json file");
        //example:
        //=> "/home/dominik/Projects/soge_poco/Templates/SogePoco.Template.Sqlite/App.SogePocoConfigAndSourceGenerator";

    public GeneratorOptions Options { get; } = new GeneratorOptions() {
        DatabaseClassFullName = "SomeSqliteApp.Database",
        PocoClassesNameSpace = "SomeSqliteApp.Pocos"
    };
    public ICodeConvention Convention { get; } = new DefaultCodeConvention();
    public IDatabaseDotnetDataMapperGenerator Mapper { get; } = new SqliteMapper();
    public ISqlParamNamingStrategy Naming { get; } = new SqliteNaming();
    public ISchemaExtractor Extractor { get; } = new SqliteSchemaExtractor();
    public Func<string,System.Data.Common.DbConnection> ConnectionFactory {get;} = x => new SqliteConnection(x);
}
