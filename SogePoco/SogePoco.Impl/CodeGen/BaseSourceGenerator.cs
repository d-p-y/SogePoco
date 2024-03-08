using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;

namespace SogePoco.Impl.CodeGen; 

public abstract class BaseSourceGenerator : IIncrementalGenerator {
    private readonly string _dbSchemaFullPath;
    private readonly DbSchema _dbSchema;
    private readonly PocoSchema _pocosMetadata;
    protected IConfiguration Config { get; }
    private readonly IQueryGenerator _generator;
    
    protected BaseSourceGenerator(IConfiguration config) {
        Config = config;

        try {
            if (!Directory.Exists(config.SchemaDirPath)) {
                Config.Logger?.LogDebug($"SchemaDirPath points to nonexisting dir {config.SchemaDirPath}. Creating it");
                Directory.CreateDirectory(config.SchemaDirPath);
            }
            
            _dbSchemaFullPath = Path.Combine(config.SchemaDirPath, PocoClassesGenerator.SerializedDbSchemaDefaultFileName);

            if (!File.Exists(_dbSchemaFullPath)) {
                var msg = $"dbschema file doesn't exist {_dbSchemaFullPath}. Fetch schema first";
                Config.Logger?.LogDebug(msg);
                throw new Exception(msg);
            }
            
            _dbSchema = PocoClassesGenerator.DeserializeDbSchemaOrFail(
                new SimpleNamedFile(_dbSchemaFullPath, File.ReadAllText(_dbSchemaFullPath)));
            _pocosMetadata = PocoClassesGenerator.BuildRichModel(_dbSchema, config.Options, config.Convention, config.Mapper);
            
            _generator = new DefaultQueryGenerator(
                _dbSchema.AdoDbConnectionFullClassName, Config.Mapper, Config.Options, Config.Naming);            
        } catch (Exception ex) {
            Config.Logger?.LogDebug($"exception in ctor {ex}");
            throw;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        Config.Logger?.LogDebug($"{nameof(BaseSourceGenerator)} version=20230926-112500");
        
        //TODO follow guides
        //https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
        //https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/

        var ivp = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (s, _) => s.IsRequestingGenerateDatabaseClassAndPocos(Config) || s.IsRequestingGenerateQueries(Config),
            transform: static (gsc, _) => gsc);
            
        
        var compAndClasses = context.CompilationProvider.Combine(ivp.Collect());
        
        context.RegisterSourceOutput(compAndClasses, (spc,src) => GenerateSources(spc, src.Left, src.Right));
    }

    private void GenerateSources(SourceProductionContext spc, Compilation compilation, IEnumerable<GeneratorSyntaxContext> ctx) {
        try {
            GenerateSourcesUnsafe(spc, compilation, ctx);
        } catch (Exception ex) {
            Config.Logger?.LogDebug($"exception in GenerateSources {ex}");
            throw;
        }
    }

    private void GenerateSourcesUnsafe(SourceProductionContext spc, Compilation compilation, IEnumerable<GeneratorSyntaxContext> ctxs) {
        Config.Logger?.LogDebug($"entering GenerateSourcesUnsafe for compilation assembly {compilation.AssemblyName}");
        
        foreach (var ctx in ctxs) {
            if (ctx.Node.IsRequestingGenerateDatabaseClassAndPocos(Config)) {
                var pocosAndDbClass = ClassesGenerator.GeneratePocosAndDatabaseClasses(
                    _pocosMetadata, Config.Naming, Config.Options, Config.Mapper);
                
                pocosAndDbClass.ForEach(x => {
                    Config.Logger?.LogDebug($"file={x.FileName} content={x.Content}");
                    spc.AddSource(x.FileName, x.Content);
                });
        
                return;
            }

            if (ctx.Node.IsRequestingGenerateQueries(Config)) {
                _generator.OnElement(compilation, ctx.Node);
                
                var queries = _generator.GenerateFiles(_pocosMetadata);
                
                Config.Logger?.LogDebug($"generated query files count={queries.Count}");

                queries.ForEach(x => {
                    Config.Logger?.LogDebug($"file={x.FileName} content={x.Content}\n");
                    spc.AddSource(x.FileName, x.Content);
                });
                
                return;
            }
        }
    }
}
