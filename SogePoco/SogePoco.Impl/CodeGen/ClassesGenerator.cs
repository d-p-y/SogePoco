using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.CodeGen; 

public static class ClassesGenerator {
    public static ISet<SimpleNamedFile> GeneratePocosAndDatabaseClasses(
        PocoSchema richModel,
        ISqlParamNamingStrategy naming,
        GeneratorOptions options,
        IDatabaseDotnetDataMapperGenerator mapper) => 
        PocoClassesGenerator
            .GeneratePocos(richModel.Tables, options)
            .Append(DatabaseClassGenerator.GenerateDatabaseClass(
                richModel.AdoDbConnectionFullClassName,
                richModel.AdoDbCommandFullClassName,
                richModel.Tables,
                naming,
                options,
                mapper))
            .ToSet();
}