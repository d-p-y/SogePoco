using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.SchemaExtraction;
using SogePoco.Impl.Tests.Connections;
using SogePoco.Impl.Tests.PocoGeneration;
using SogePoco.Impl.Tests.Schemas;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests; 

public class TestsSchemaExtraction : BaseTest {
    public TestsSchemaExtraction(ITestOutputHelper outputHelper) : base(outputHelper) { }

    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
    public async void ExtractsSchema(DbToTest x) {
        using var sut = await SystemUnderTestFactory.Create(x);
        await ExtractsSchemaImpl(sut.DbConn, sut.TestingSchema, sut.SchemaExtractor);
    }
        
    private async Task ExtractsSchemaImpl(SingleServingDbConnection dbConn, ITestingSchema testingSchema, ISchemaExtractor extractor) {
        await testingSchema.CreateSchema(dbConn.DbConn);
        await testingSchema.CreateData(dbConn.DbConn);
        var expected = testingSchema.GetAsSyntheticModel();
            
        var actual = (await DbSchema.CreateOf(dbConn.DbConn, extractor)).Tables;
        SchemaUtil.AssertEquals(expected, actual, Logger);
            
        // redundant so may be incomplete
        Assert.Equal(
            new []{testingSchema.FooTable_IdColumnName},
            actual.Where(x => x.Name.ToLower() == testingSchema.FooTableName.ToLower()).SelectMany(x => x.GetPrimaryKey().Select(y => y.Name)).ToArray() );
            
        Assert.Equal(
            new []{testingSchema.TableWithCompositePk_IdColumnName, testingSchema.TableWithCompositePk_YearColumnName},
            actual.Where(x => x.Name.ToLower() == testingSchema.TableWithCompositePkName.ToLower()).SelectMany(x => x.GetPrimaryKey().Select(y => y.Name)).ToArray() );
    }

    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
    public async void SerializedSchemaWorks(DbToTest x) {
        using var sut = await SystemUnderTestFactory.Create(x);
            
        await SerializedSchemaWorksImpl(sut.DbConn, sut.TestingSchema, sut.SchemaExtractor);
    }
        
    private async Task SerializedSchemaWorksImpl(
        SingleServingDbConnection dbConn, ITestingSchema testingSchema, ISchemaExtractor extractor) {
            
        await testingSchema.CreateSchema(dbConn.DbConn);
        await testingSchema.CreateData(dbConn.DbConn);
        var expected = testingSchema.GetAsSyntheticModel();
            
        var actual = await DbSchema.CreateOf(dbConn.DbConn, extractor);

        var embRes = PocoClassesGenerator.SerializeDbSchema(actual);
        actual = PocoClassesGenerator.DeserializeDbSchemaOrFail(embRes);
            
        SchemaUtil.AssertEquals(expected, actual.Tables, Logger);
    }
}