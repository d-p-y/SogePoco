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

namespace SogePoco.Impl.Tests.PocoGeneration; 

public class TestsUpdate : BaseTest {
    public TestsUpdate(ITestOutputHelper outputHelper) : base(outputHelper) { }
        
    public static IEnumerable<object[]> AllValuesOf_DbToTest_WithOrWithoutTransaction => 
        DbToTestUtil.GetAllToBeTested().SelectMany(y=> Enum.GetValues<WithOrWithoutTransaction>().Select(x => new object[] {y, x})); 

    public static IEnumerable<object[]> UpdateFailureIsReportedParams =>
        DbToTestUtil.GetAllToBeTested()
            .SelectMany(p => 
                Enum.GetValues<ZeroOrException>()
                    .SelectMany(zoe => Enum.GetValues<UnderlyingIssue>()
                        .Where(ui => p != DbToTest.Sqlite || ui != UnderlyingIssue.ConcurrencyTokenChanged)
                        .Select(ui => new object[] {p, zoe, ui} )));
                
    [Theory]
    [MemberData(nameof(UpdateFailureIsReportedParams))]
    public async Task UpdateFailureIsReported(DbToTest dbToTest, ZeroOrException strategy, UnderlyingIssue issue) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        object? postConflictConcurTokenVal = null;
            
        await ImplUpdateFailureIsReported(
            nameof(UpdateFailureIsReported),
            sut.ConvertIntToDbNativeIntType,
            x => x.ApplyZeroOrException(strategy),
            async x => {
                var result = await issue.ApplyReturningConcurrencyTokenValue(x, Logger, sut, x.InsertedId);
                postConflictConcurTokenVal = result;
            },
            x => strategy.ReactOnActualUpdateBehavior(Logger, x),
            sut.DbConn,
            sut.TestingSchema,
            sut.Naming,
            sut.CodeConvention,
            sut.MapperGenerator,
            assertStateIsSane: x => issue.AssertDbStateIsSane(
                Logger, 
                expectedChangesInDb:new []{
                    (sut.TestingSchema.FooTable_ConcurrencyTokenPropertyName!, postConflictConcurTokenVal), 
                    (sut.TestingSchema.FooTable_AboolPropertyName, true)}, 
                x));
    }
        
    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest_WithOrWithoutTransaction))]
    public async Task UpdateWorksUsingSingleColPk(DbToTest dbToTest, WithOrWithoutTransaction tranStrategy) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        await ImplUpdateOnSingleColPkWorks(
            nameof(UpdateWorksUsingSingleColPk),
            sut.ConvertIntToDbNativeIntType,
            sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, 
            sut.MapperGenerator, tranStrategy);
    }
        
    private async Task ImplUpdateOnSingleColPkWorks(
        string testName,
        Func<int,object> convertIntToDbNativeIntType,
        SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
        ISqlParamNamingStrategy naming,
        ICodeConvention convention,
        IDatabaseDotnetDataMapperGenerator mapper,
        WithOrWithoutTransaction tranStrategy) {

        using var cleanup = new OnFinallyAction();
            
        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt, naming, convention, mapper, SchemaDataOp.CreateSchemaOnly, 
            DefaultsStrategy.AlwaysInsert.CreateDefaultableColumnShouldInsert(),
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        await tranStrategy.ApplyStrategy(tran => rawDbConn.DbConn.IsInTransaction(tran), x, async _ => {
            var pocoT = x.FooT;
            var pocoTFullName = pocoT.FullName ?? throw new Exception($"bug, could not get {nameof(pocoT)} name");
            dynamic poco = 
                x.Asm.CreateInstance(pocoTFullName)
                ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");;
                
            await ((dynamic)x.DbConn).Insert(poco);

            var nullableTextValue = "ente";
            poco!.NullableText = nullableTextValue;

            var notNullableTextValue = "n1n23t4";
            poco.NotNullableText = notNullableTextValue;

            var notNullTextWithMaxLengthValue = "ąę€OÓ";
            poco.NotNullTextWithMaxLength = notNullTextWithMaxLengthValue;

            var simDef = 33;
            poco.NotNullableIntWithSimpleDefault = simDef; //implicitly converted to int or long
            poco.NotNullableIntWithComplexDefault = 44; //same as above

            object? postInsertConcurrencyTokenValue = 
                testingSchema.FooTable_ConcurrencyTokenPropertyName == null 
                    ? null 
                    : ((object)poco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName);
                
            var expected = new Dictionary<string, object?> {
                    {"Id", poco.Id},
                    {"NullableText", nullableTextValue},
                    {"NotNullableText", notNullableTextValue},
                    {"NotNullableIntWithSimpleDefault", poco.NotNullableIntWithSimpleDefault},
                    {"NotNullableIntWithComplexDefault", poco.NotNullableIntWithComplexDefault},
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
                    {"SecondComputed", convertIntToDbNativeIntType(simDef+1)} }
                .DowncastCollectionToInterface()
                .AsSingletonCollection()
                .ToList();
                
            var rowsAffected = await ((dynamic)x.DbConn).Update(poco);
                
            Assert.Equal(1L, rowsAffected);
                
            object? postUpdateConcurrencyTokenValue = 
                testingSchema.FooTable_ConcurrencyTokenPropertyName == null ? null : ((object)poco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName);

            object rawPocos = ((dynamic)x.DbConn).FetchFoo();

            var pocos = (await rawPocos
                    .AsIAsyncEnumerableOfObject(pocoT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();
                
            if (testingSchema.FooTable_ConcurrencyTokenPropertyName != null) {
                if (tranStrategy == WithOrWithoutTransaction.WithoutTransaction) {
                    //assure that before update and post update concurrency token was actually changed by db
                    Assert.NotEqual(postInsertConcurrencyTokenValue, postUpdateConcurrencyTokenValue);
                } else if (mapper.ConcurrencyTokenIsStableInTransaction) {
                    //in transaction so equal despite update
                    Assert.Equal(postInsertConcurrencyTokenValue, postUpdateConcurrencyTokenValue);
                }

                Assert.Equal(postUpdateConcurrencyTokenValue, pocos[0][testingSchema.FooTable_ConcurrencyTokenPropertyName]);

                //prepare for check that concurrency token in poco is the same in fetched dict as in poco (last updated during insert)
                expected[0].Add(testingSchema.FooTable_ConcurrencyTokenPropertyName, postUpdateConcurrencyTokenValue);
            }
                
            AssertUtil.AssertSameEntitiesColl(Logger, "Id", expected, pocos);
        });
                    
        cleanup.EnableInvokeActionInFinally();
    }

    private async Task ImplUpdateFailureIsReported(
            string testName,
            Func<int,object> convertIntToDbNativeIntType,
            Action<GeneratedCodeResult> afterCodeGen,
            Func<DbRecordIdent, Task> beforeUpdate,
            Action<(object? Value, Exception? Ex)> onUpdateReturned,
            SingleServingDbConnection rawDbConn, ITestingSchema testingSchema,
            ISqlParamNamingStrategy naming,
            ICodeConvention convention,
            IDatabaseDotnetDataMapperGenerator mapper,
            Action<(
                IDictionary<string,object?> expectedDbStateIfPresent, 
                List<IDictionary<string,object?>> actualDbState,
                IDictionary<string,object?> expectedPoco, 
                IDictionary<string,object?>  actualPoco)> assertStateIsSane) {
            
        using var cleanup = new OnFinallyAction();
            
        var opt = new GeneratorOptions() {ShouldGenerateMethod = _ => true};
        var x = await GeneratedCodeUtil.BuildGeneratedCode(
            rawDbConn, testingSchema, opt, naming, convention, mapper, SchemaDataOp.CreateSchemaOnly, 
            DefaultsStrategy.AlwaysInsert.CreateDefaultableColumnShouldInsert(),
            x => GeneratedCodeUtil.DumpToDisk(testName, x).Also(sln => cleanup.Add(sln.RemoveFromDisk)));
            
        cleanup.Add(() => x.ForwardLastSqlToLogger(Logger));

        afterCodeGen(x);
            
        var pocoT = x.FooT;
        var pocoTFullName = pocoT.FullName ?? throw new Exception($"bug, could not get {nameof(pocoT)} name");
            
        dynamic poco = 
            x.Asm.CreateInstance(pocoTFullName)
            ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
            
        await ((dynamic)x.DbConn).Insert(poco);

        var nullableTextValue = "ente";
        poco!.NullableText = nullableTextValue; //diff!

        object? postInsertConcurrencyTokenValue = 
            testingSchema.FooTable_ConcurrencyTokenPropertyName == null ? null : ((object)poco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName);
            
        var expectedPoco = new Dictionary<string, object?> {
                {"Id", poco.Id},
                {"NullableText", nullableTextValue},
                {"NotNullableText", ""}, //diff!
                {"NotNullableIntWithSimpleDefault", convertIntToDbNativeIntType(0)},
                {"NotNullableIntWithComplexDefault", convertIntToDbNativeIntType(0)},
                {"NotNullTextWithMaxLength", ""},
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
                {"FirstComputed", "1"},
                {"SecondComputed", convertIntToDbNativeIntType(0+1)} }
            .IfTrueThenAlso(
                () => testingSchema.FooTable_ConcurrencyTokenPropertyName != null,
                x => x.Add(testingSchema.FooTable_ConcurrencyTokenPropertyName!, postInsertConcurrencyTokenValue))
            .DowncastCollectionToInterface();
            
        var expectedDbStateIfPresent = new Dictionary<string, object?> {
                {"Id", poco.Id},
                {"NullableText", null},
                {"NotNullableText", ""}, //diff!
                {"NotNullableIntWithSimpleDefault", convertIntToDbNativeIntType(0)},
                {"NotNullableIntWithComplexDefault", convertIntToDbNativeIntType(0)},
                {"NotNullTextWithMaxLength", ""},
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
                {"FirstComputed", "1"},
                {"SecondComputed", convertIntToDbNativeIntType(0+1)} }
            .IfTrueThenAlso(
                () => testingSchema.FooTable_ConcurrencyTokenPropertyName != null,
                x => x.Add(testingSchema.FooTable_ConcurrencyTokenPropertyName!, postInsertConcurrencyTokenValue))
            .DowncastCollectionToInterface();
                
        await beforeUpdate(new (poco.Id, postInsertConcurrencyTokenValue));

        Action? callOnUpdateReturned = null;
        try {
            object? rowsAffected = await ((dynamic)x.DbConn).Update(poco);
            callOnUpdateReturned = () => onUpdateReturned((Value: rowsAffected, Ex:null));
        } catch (Exception ex) {
            callOnUpdateReturned = () => onUpdateReturned((Value:null, Ex:ex));
        }
            
        callOnUpdateReturned?.Invoke();

        var actualPoco = ((object) poco).AsSingletonCollection().ToPropertyNameAndValueDict()[0];

        object rawActual = ((dynamic)x.DbConn).FetchFoo();

        var actualDbState = (await rawActual
                .AsIAsyncEnumerableOfObject(pocoT)
                .ToListAsync())
            .ToPropertyNameAndValueDict()
            .Select(x => x.DowncastCollectionToInterface())
            .ToList();
            
        assertStateIsSane((expectedDbStateIfPresent, actualDbState, expectedPoco, actualPoco));
            
        cleanup.EnableInvokeActionInFinally();
    }
}