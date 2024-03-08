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

public class TestsDelete : BaseTest {
    public TestsDelete(ITestOutputHelper outputHelper) : base(outputHelper){ }

    public static IEnumerable<object[]> AllValuesOf_DbToTest_WithOrWithoutTransaction => 
        DbToTestUtil.GetAllToBeTested().SelectMany(dtt => 
            Enum.GetValues<WithOrWithoutTransaction>().Select(wowt => new object[] {dtt, wowt}));

    public static IEnumerable<object[]> UpdateFailureIsReportedParams =>
        DbToTestUtil.GetAllToBeTested()
            .SelectMany(p => 
                Enum.GetValues<ZeroOrException>()
                    .SelectMany(zoe => Enum.GetValues<UnderlyingIssue>()
                        .Where(ui => p != DbToTest.Sqlite || ui != UnderlyingIssue.ConcurrencyTokenChanged)
                        .Select(ui => new object[] {p, zoe, ui} )));

    [Theory]
    [MemberData(nameof(AllValuesOf_DbToTest_WithOrWithoutTransaction))]
    public async Task DeleteWorks(DbToTest dbToTest, WithOrWithoutTransaction tranStrategy) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        await ImplDeleteOnSingleColPkWorks(
            nameof(DeleteWorks), sut.DbConn, sut.TestingSchema, sut.Naming, sut.CodeConvention, 
            sut.MapperGenerator, tranStrategy);
    }
        
    [Theory]
    [MemberData(nameof(UpdateFailureIsReportedParams))]
    public async Task DeleteFailureIsReported(DbToTest dbToTest, ZeroOrException strategy, UnderlyingIssue issue) {
        using var sut = await SystemUnderTestFactory.Create(dbToTest);
            
        object? postConflictConcurTokenVal = null;
            
        await ImplDeleteFailureIsReported(
            nameof(DeleteFailureIsReported),
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
                expectedChangesInDb: sut.TestingSchema.FooTable_ConcurrencyTokenPropertyName != null 
                    ? new [] {
                        (sut.TestingSchema.FooTable_ConcurrencyTokenPropertyName!, postConflictConcurTokenVal), 
                        (sut.TestingSchema.FooTable_AboolPropertyName, true)
                    }
                    : Array.Empty<(string,object?)>(),
                x));
    }

    private async Task ImplDeleteFailureIsReported(
        string testName,
        Func<int,object> convertIntToDbNativeIntType,
        Action<GeneratedCodeResult> afterCodeGen,
        Func<DbRecordIdent, Task> beforeDelete,
        Action<(object? Value, Exception? Ex)> onDeleteReturned,
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

        var pocoTFullName = pocoT.FullName ?? throw new Exception($"bug, could not get fullname of {nameof(pocoT)}");
        dynamic poco = 
            x.Asm.CreateInstance(pocoTFullName) 
            ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
            
        await ((dynamic)x.DbConn).Insert(poco);

        object? postInsertConcurrencyTokenValue = 
            testingSchema.FooTable_ConcurrencyTokenPropertyName == null ? null : ((object)poco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName);
            
        var expectedPoco = new Dictionary<string, object?> {
                {"Id", poco.Id},
                {"NullableText", null},
                {"NotNullableText", ""},
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
                
        await beforeDelete(new(poco.Id, postInsertConcurrencyTokenValue));

        Action? callOnDeleteReturned = null;
        try {
            object? rowsAffected = await ((dynamic)x.DbConn).Delete(poco);
            callOnDeleteReturned = () => onDeleteReturned((Value: rowsAffected, Ex:null));
        } catch (Exception ex) {
            callOnDeleteReturned = () => onDeleteReturned((Value:null, Ex:ex));
        }
            
        callOnDeleteReturned?.Invoke();

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

    private async Task ImplDeleteOnSingleColPkWorks(
        string testName,
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
                
            var pocoTFullName = pocoT.FullName ?? throw new Exception($"bug, could not get fullname of {nameof(pocoT)}");
                
            dynamic fstPoco = 
                x.Asm.CreateInstance(pocoTFullName)
                ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
                
            await ((dynamic)x.DbConn).Insert(fstPoco);

                
            dynamic sndPoco = 
                x.Asm.CreateInstance(pocoTFullName)
                ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");
                
            await ((dynamic)x.DbConn).Insert(sndPoco);
                
                
            object rawPocosBeforeRemoval = ((dynamic)x.DbConn).FetchFoo();

            var pocosBeforeRemoval = (await rawPocosBeforeRemoval
                    .AsIAsyncEnumerableOfObject(pocoT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();
                
                
            dynamic fstPocoEquivalent = 
                x.Asm.CreateInstance(pocoTFullName)
                ?? throw new Exception($"bug, could not create instance of {nameof(pocoT)} by name");

            fstPocoEquivalent.Id = fstPoco.Id;
                
            if (testingSchema.FooTable_ConcurrencyTokenPropertyName != null) {
                ((object)fstPocoEquivalent).SetPropertyValue(
                    testingSchema.FooTable_ConcurrencyTokenPropertyName,
                    ((object) fstPoco).GetPropertyValue(testingSchema.FooTable_ConcurrencyTokenPropertyName));
            }

            var rowsAffected = await ((dynamic)x.DbConn).Delete(fstPocoEquivalent);
                
            object rawPocosAfterRemoval = ((dynamic)x.DbConn).FetchFoo();

            var pocosAfterRemoval = (await rawPocosAfterRemoval
                    .AsIAsyncEnumerableOfObject(pocoT)
                    .ToListAsync())
                .ToPropertyNameAndValueDict();

            Assert.Equal(2, pocosBeforeRemoval.Count);
            Assert.Equal(1, rowsAffected);
            Assert.Single(pocosAfterRemoval);
                
            AssertUtil.AssertSameEntitiesColl(Logger, "Id", 
                ((object)sndPoco).AsSingletonCollection().ToPropertyNameAndValueDict(), 
                pocosAfterRemoval);
        });
        
        cleanup.EnableInvokeActionInFinally();
    }
}