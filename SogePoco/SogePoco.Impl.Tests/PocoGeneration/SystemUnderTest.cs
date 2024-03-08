using System;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Model;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Connections;
using SogePoco.Impl.Tests.Schemas;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public class SystemUnderTest : IDisposable {
    public ICodeConvention CodeConvention { get; }
    public SingleServingDbConnection DbConn { get; }
    
    public ITestingSchema TestingSchema { get; }
    public IDatabaseDotnetDataMapperGenerator MapperGenerator { get; }
    public ISqlParamNamingStrategy Naming { get; }
        
    public ISchemaExtractor SchemaExtractor { get; }
    public Func<int,object> ConvertIntToDbNativeIntType { get; } 
        
    public SystemUnderTest(
        ICodeConvention convention, SingleServingDbConnection dbConn, ITestingSchema testingSchema,
        IDatabaseDotnetDataMapperGenerator mapperGenerator, ISqlParamNamingStrategy naming,
        ISchemaExtractor schemaExtractor, Func<int,object> convertIntToDbNativeIntType) {
            
        CodeConvention = convention;
        DbConn = dbConn;
        TestingSchema = testingSchema;
        MapperGenerator = mapperGenerator;
        Naming = naming;
        SchemaExtractor = schemaExtractor;
        ConvertIntToDbNativeIntType = convertIntToDbNativeIntType;
    }

    public void Dispose() => DbConn.Dispose();
}