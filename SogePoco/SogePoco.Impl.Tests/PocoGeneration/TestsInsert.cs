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

namespace SogePoco.Impl.Tests.PocoGeneration; 

public class TestsInsert : BaseTest {
    public TestsInsert(ITestOutputHelper outputHelper) : base(outputHelper) {}
        
        
    public static IEnumerable<object[]> AllValuesFor_InsertContainingDefaultsWorks => 
        DbToTestUtil.GetAllToBeTested().SelectMany(x => new []{
            new object[] {x, DefaultsStrategy.NeverInsert, 12, 13, 5, 6},
            new object[] {x, DefaultsStrategy.AlwaysInsert, 0, 0, 0, 0},
            new object[] {x, DefaultsStrategy.AlwaysInsert, 1, 2, 1, 2},
            new object[] {x, DefaultsStrategy.InsertOnlyWhenSeemsNotDotnetDefault, 0, 0, 5, 6},
            new object[] {x, DefaultsStrategy.InsertOnlyWhenSeemsNotDotnetDefault, 10, 12, 10, 12} });

    [Theory]
    [MemberData(nameof(AllValuesFor_InsertContainingDefaultsWorks))]
    public async Task InsertContainingDefaultsWorks(
        DbToTest dbToTest,
        DefaultsStrategy defaults,
        int requestedNotNullableIntWithSimpleDefault, int requestedNotNullableIntWithComplexDefault,
        int expectedNotNullableIntWithSimpleDefault, int expectedNotNullableIntWithComplexDefault) {
            
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        await ImplInsertUsingSingleColAutoincrementPk(
            nameof(InsertContainingDefaultsWorks),
            sut.ConvertIntToDbNativeIntType,
            sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, sut.MapperGenerator,
            WithOrWithoutTransaction.WithoutTransaction,
            defaults.CreateDefaultableColumnShouldInsert(),
            requestedNotNullableIntWithSimpleDefault, requestedNotNullableIntWithComplexDefault,
            expectedNotNullableIntWithSimpleDefault, expectedNotNullableIntWithComplexDefault);
    }

        
    public static IEnumerable<object[]> AllValuesFor_InsertUsingAutoIncrementWorks => 
        DbToTestUtil.GetAllToBeTested()
            .SelectMany(x => Enum.GetValues<WithOrWithoutTransaction>().Select(y => new object[] {x, y}) );
        
    [Theory]
    [MemberData(nameof(AllValuesFor_InsertUsingAutoIncrementWorks))]
    public async Task InsertUsingAutoIncrementWorks(DbToTest dbToTest, WithOrWithoutTransaction tranStrategy) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        await ImplInsertUsingSingleColAutoincrementPk(
            nameof(InsertUsingAutoIncrementWorks),
            sut.ConvertIntToDbNativeIntType,
            sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, 
            sut.MapperGenerator, tranStrategy,
            DefaultsStrategy.NeverInsert.CreateDefaultableColumnShouldInsert(), 
            12, 13, 5, 6);
    }

    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest))]
    public async Task InsertUsingManualPrimaryKeyWorks(DbToTest dbToTest) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        await ImplInsertUsingCompositeManualPk(
            nameof(InsertUsingManualPrimaryKeyWorks),
            sut.ConvertIntToDbNativeIntType,
            sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, 
            sut.MapperGenerator, 332211, 2032);
    }
        
    private async Task ImplInsertUsingSingleColAutoincrementPk(
        string testName,
        Func<int,object> convertIntToDbNativeIntType,
        SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
        ISqlParamNamingStrategy naming,
        ICodeConvention convention,
        IDatabaseDotnetDataMapperGenerator mapper,
        WithOrWithoutTransaction tranStrategy,
        DefaultableColumnShouldInsert defaultsStrategy,
        int requestedNotNullableIntWithSimpleDefault,
        int requestedNotNullableIntWithComplexDefault,
        int expectedNotNullableIntWithSimpleDefault,
        int expectedNotNullableIntWithComplexDefault) {

        using var cleanup = new OnFinallyAction();
           
        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt, naming, convention, mapper, SchemaDataOp.CreateSchemaOnly, 
            defaultsStrategy,
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        await tranStrategy.ApplyStrategy(tran => rawDbConn.DbConn.IsInTransaction(tran), x, async _ => {
            var pocoT = x.FooT;
                
            var pocoTFullName = 
                pocoT.FullName 
                ?? throw new Exception($"bug, could not get {nameof(pocoT)} name");
                
            dynamic poco = 
                x.Asm.CreateInstance(pocoTFullName)
                ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
                
            var nullableTextValue = "ente";
            poco!.NullableText = nullableTextValue;

            var notNullableTextValue = "n1n23t4";
            poco.NotNullableText = notNullableTextValue;

            var notNullTextWithMaxLengthValue = "ąę€OÓ";
            poco.NotNullTextWithMaxLength = notNullTextWithMaxLengthValue;

            ((object) poco).SetPropertyValue("NotNullableIntWithSimpleDefault",
                convertIntToDbNativeIntType(requestedNotNullableIntWithSimpleDefault));
                
            ((object)poco).SetPropertyValue("NotNullableIntWithComplexDefault",
                convertIntToDbNativeIntType(requestedNotNullableIntWithComplexDefault));

            await ((dynamic)x.DbConn).Insert(poco);
                
            var expected = new Dictionary<string, object?> {
                    {"Id", poco.Id},
                    {"NullableText", nullableTextValue},
                    {"NotNullableText", notNullableTextValue},
                    {"NotNullableIntWithSimpleDefault", convertIntToDbNativeIntType(expectedNotNullableIntWithSimpleDefault)},
                    {"NotNullableIntWithComplexDefault", convertIntToDbNativeIntType(expectedNotNullableIntWithComplexDefault)},
                    {"NotNullTextWithMaxLength", notNullTextWithMaxLengthValue},
                    {"NullableInt", null},
                    {"NotNullableInt", convertIntToDbNativeIntType(0)},
                    {"NullableBool", null},
                    {"NotNullableBool", default(bool)},
                    {"NullableDecimal", null},
                    {"NotNullableDecimal", default(decimal)},
                    {"NullableDateTime", null},
                    {"NotNullableDateTime", default(DateTime)},
                    {"NullableBinaryData", null},
                    {"NotNullableBinaryData", new byte[0]},
                    {"FirstComputed", nullableTextValue+"1"},
                    {"SecondComputed", convertIntToDbNativeIntType(expectedNotNullableIntWithSimpleDefault+1)} }
                .IfTrueThenAlso(
                    () => testingSchema.FooTable_ConcurrencyTokenPropertyName != null, 
                    y => y.Add(
                        testingSchema.FooTable_ConcurrencyTokenPropertyName!, 
                        ((object)poco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName!) ))
                .DowncastCollectionToInterface()
                .AsSingletonCollection()
                .ToList();
                
            object rawPocos = ((dynamic)x.DbConn).FetchFoo();

            var pocos = (await rawPocos
                    .AsIAsyncEnumerableOfObject(pocoT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();
                
            AssertUtil.AssertSameEntitiesColl(Logger, "Id", expected, pocos);
        });
            
        cleanup.EnableInvokeActionInFinally();
    }
        
    private async Task ImplInsertUsingCompositeManualPk(
        string testName,
        Func<int,object> convertIntToDbNativeIntType,
        SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
        ISqlParamNamingStrategy naming,
        ICodeConvention convention,
        IDatabaseDotnetDataMapperGenerator mapper,
        int requestedPkId,
        int requestedPkYear) {

        using var cleanup = new OnFinallyAction();
        
        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt, naming, convention, mapper, SchemaDataOp.CreateSchemaOnly, 
            DefaultsStrategy.AlwaysInsert.CreateDefaultableColumnShouldInsert(),
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        var pocoT = x.TableWithCompositePkT;
        var pocoTFullName = pocoT.FullName ?? throw new Exception($"bug, could not get {nameof(pocoT)} name");
        var poco = 
            x.Asm.CreateInstance(pocoTFullName)
            ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
            
        poco.SetPropertyValue("Id", convertIntToDbNativeIntType(requestedPkId));
        poco.SetPropertyValue("Year", convertIntToDbNativeIntType(requestedPkYear));
        // poco!.Id = convertIntToDbNativeIntType(requestedPkId);
        // poco!.Year = convertIntToDbNativeIntType(requestedPkYear);

        await ((dynamic)x.DbConn).Insert((dynamic)poco);
            
        var expected = new Dictionary<string, object?> {
                {"Id", convertIntToDbNativeIntType(requestedPkId)},
                {"Year", convertIntToDbNativeIntType(requestedPkYear)},
                {"Value", null} }
            .IfTrueThenAlso(
                () => testingSchema.TableWithCompositePk_ConcurrencyTokenPropertyName != null,
                y => y.Add(
                    testingSchema.TableWithCompositePk_ConcurrencyTokenPropertyName!, 
                    poco.GetPropertyValue(testingSchema.TableWithCompositePk_ConcurrencyTokenPropertyName !) ))
            .DowncastCollectionToInterface()
            .AsSingletonCollection()
            .ToList();
            
        object rawPocos = ((dynamic)x.DbConn).FetchTableWithCompositePk();

        var pocos = (await rawPocos
                .AsIAsyncEnumerableOfObject(pocoT)
                .ToListAsync())
            .ToPropertyNameAndValueDict();
            
        AssertUtil.AssertSameEntitiesColl(Logger, "Id", expected, pocos);
            
        cleanup.EnableInvokeActionInFinally();
    }
}