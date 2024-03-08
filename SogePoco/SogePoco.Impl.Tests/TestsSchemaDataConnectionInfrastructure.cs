using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.Tests.PocoGeneration;
using Xunit;
using Xunit.Abstractions;

namespace SogePoco.Impl.Tests; 

public class TestsSchemaDataConnectionInfrastructure : BaseTest {
    public TestsSchemaDataConnectionInfrastructure(ITestOutputHelper outputHelper) : base(outputHelper) { }

    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
    public async Task CreateAndValidateTestingSchemaAndData(DbToTest x) {
        using var sut = await SystemUnderTestFactory.Create(x);
            
        await sut.TestingSchema.CreateSchema(sut.DbConn.DbConn);
        await sut.TestingSchema.CreateData(sut.DbConn.DbConn);
    }
}