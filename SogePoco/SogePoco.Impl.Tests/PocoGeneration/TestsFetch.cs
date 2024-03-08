using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SogePoco.Impl.CodeGen;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;
using SogePoco.Impl.Tests.Connections;
using SogePoco.Impl.Tests.Extensions;
using SogePoco.Impl.Tests.Schemas;
using SogePoco.Impl.Tests.Utils;
using Xunit;
using Xunit.Abstractions;
using DefaultableColumnShouldInsert = System.Func<(System.Type poco, string pocoPropertyName, object pocoInstance, object? pocoPropertyValue),bool>;
using ObjectExtensions = SogePoco.Impl.Tests.Extensions.ObjectExtensions;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public class TestsFetch : BaseTest {
    public TestsFetch(ITestOutputHelper outputHelper) : base(outputHelper) { }
 
    //TODO parameterized 'not type safe' fetch getting "where something" and (sql?)parameters collection
        
    public static IEnumerable<object[]> AllValuesOf_DbToTest_WithOrWithoutTransaction => 
        DbToTestUtil.GetAllToBeTested().SelectMany(y=> Enum.GetValues<WithOrWithoutTransaction>().Select(x => new object[] {y, x})); 
        
    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest_WithOrWithoutTransaction))]
    public async Task FetchWorks(DbToTest dbToTest, WithOrWithoutTransaction tranStrategy) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await FetchWorksImpl(
            nameof(FetchWorks), sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, sut.MapperGenerator, 
            tranStrategy);
    }
    
    private async Task FetchWorksImpl(
            string testName,
            SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
            ISqlParamNamingStrategy naming,
            ICodeConvention convention,
            IDatabaseDotnetDataMapperGenerator mapper,
            WithOrWithoutTransaction tranStrategy) {
                
        using var cleanup = new OnFinallyAction();

        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt,naming, convention, mapper, SchemaDataOp.CreateSchemaAndPopulateData, 
            DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert(), 
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        await tranStrategy.ApplyStrategy(tran => rawDbConn.DbConn.IsInTransaction(tran), x, async tran => {
            object rawFoos = ((dynamic)x.DbConn).FetchFoo();
            var foos = (await rawFoos
                    .AsIAsyncEnumerableOfObject(x.FooT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();

            AssertUtil.AssertSameEntitiesColl(Logger, "Id", x.TestData!.Foos, foos);

            object rawChildOfFoos = ((dynamic)x.DbConn).FetchChildOfFoo();

            var childOfFoos = (await rawChildOfFoos
                    .AsIAsyncEnumerableOfObject(x.ChildOfFooT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();

            AssertUtil.AssertSameEntitiesColl(Logger, "Id", x.TestData!.ChildOfFoos, childOfFoos);
        });
            
        cleanup.EnableInvokeActionInFinally();
    }
    
    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest_WithOrWithoutTransaction))]
    public async Task PocosAreSerializableToJson(DbToTest dbToTest, WithOrWithoutTransaction tranStrategy) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
        await FetchSerializationWorksImpl(
            nameof(FetchWorks), sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, sut.MapperGenerator, 
            tranStrategy);
    }

    private async Task FetchSerializationWorksImpl(
            string testName,
            SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
            ISqlParamNamingStrategy naming,
            ICodeConvention convention,
            IDatabaseDotnetDataMapperGenerator mapper,
            WithOrWithoutTransaction tranStrategy) {
                
        using var cleanup = new OnFinallyAction();

        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt,naming, convention, mapper, SchemaDataOp.CreateSchemaAndPopulateData, 
            DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert(), 
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        await tranStrategy.ApplyStrategy(tran => rawDbConn.DbConn.IsInTransaction(tran), x, async tran => {
            object rawFoos = ((dynamic)x.DbConn).FetchFoo();
            var foos = (await rawFoos
                    .AsIAsyncEnumerableOfObject(x.FooT)
                    .ToListAsync());
            
            var fstFoo = foos.First();
            var fstFooAsJson = Newtonsoft.Json.JsonConvert.SerializeObject(fstFoo);
            var fstFooAsDict = ObjectExtensions.ItemToPropertyNameAndValueDict(fstFoo, autoSkipForeignKeys:true);

            var fstFoo2 = Newtonsoft.Json.JsonConvert.DeserializeObject(fstFooAsJson, x.FooT);
            var fstFoo2AsDict = ObjectExtensions.ItemToPropertyNameAndValueDict(fstFoo2, autoSkipForeignKeys:true);
            
            AssertUtil.AssertSameEntitiesColl(Logger, "Id", new[] {fstFooAsDict}, new[] {fstFoo2AsDict});
        });
            
        cleanup.EnableInvokeActionInFinally();
    }
}
