using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Tests.Utils;
using Xunit;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public enum UnderlyingIssue {
    RowRemoved,
    ConcurrencyTokenChanged
}

public record DbRecordIdent(object InsertedId, object? CurrentTokenVal);
    
public static class UnderlyingIssueExtensions {
    public static void AssertDbStateIsSane(
        this UnderlyingIssue issue,
        ILogger logger,
        (string PropName, object? PropValue)[] expectedChangesInDb,
        (IDictionary<string,object?> expectedDbStateIfPresent, 
            List<IDictionary<string,object?>> actualDbState,
            IDictionary<string,object?> expectedPoco, 
            IDictionary<string,object?> actualPoco) x) {
            
        //assure that poco was not affected by failed update
        AssertUtil.AssertSameEntities(
            logger, 
            "Id", 
            x.expectedPoco, 
            x.actualPoco);

        //assure that db was not affected by failed update
        switch (issue) {
            case UnderlyingIssue.RowRemoved:
                Assert.Empty(x.actualDbState);
                break;
                
            case UnderlyingIssue.ConcurrencyTokenChanged:
                Assert.Single(x.actualDbState);
                    
                (expectedChangesInDb ?? new (string PropName, object? PropValue)[0])
                    .ForEach(y => x.expectedDbStateIfPresent[y.PropName] = y.PropValue);
                    
                AssertUtil.AssertSameEntities(logger, "Id", x.expectedDbStateIfPresent, x.actualDbState[0]);
                break;
                
            default: throw new ArgumentException($"unsupported {nameof(UnderlyingIssue)}");
        }
    }
        
    public static async Task<object?> ApplyReturningConcurrencyTokenValue(
        this UnderlyingIssue issue, DbRecordIdent ident, ILogger logger, SystemUnderTest sut, object? idColumnValue) {
            
        // DbToTest.Sqlite => _ => (default, default,default, default),
        // DbToTest.Postgresql => ident => ("xmin::text", Convert.ToInt64(ident.InsertedId).ToString(), "a_bool", "TRUE"),
        // DbToTest.SqlServer => ident => ("EntityVersion", Convert.ToInt64(ident).ToString(), "aBool", "1"),

        switch(issue) {
            case UnderlyingIssue.RowRemoved: {
                var cmd = sut.DbConn.DbConn.BuildCmd(
                    sut.Naming,
                    @$"delete from {sut.TestingSchema.FooTableName} where {sut.TestingSchema.FooTable_IdColumnName} = {sut.Naming.NameForParameterUsage(0)}",
                    idColumnValue);
                    
                var affectedRows = Convert.ToInt64(await cmd.ExecuteNonQueryAsync());
                    
                logger.Log(LogLevel.Debug, $"ApplyUnderlyingIssue({issue}) Parameters={cmd.Parameters.ToPrettyString("\n")} CommandText={cmd.CommandText} affectedRows={affectedRows}");
                Assert.Equal(1L, affectedRows);
                return null;}

            case UnderlyingIssue.ConcurrencyTokenChanged: {
                var cmd = sut.DbConn.DbConn.BuildCmd(
                    sut.Naming,
                    @$"
update {sut.TestingSchema.FooTableName}
set {sut.TestingSchema.FooTable_AboolColumnName} = {sut.Naming.NameForParameterUsage(1)}
where {sut.TestingSchema.FooTable_IdColumnName} = {sut.Naming.NameForParameterUsage(0)}",
                    idColumnValue, sut.TestingSchema.FooTable_AboolTrueValue);
                    
                var affectedRows = Convert.ToInt64(await cmd.ExecuteNonQueryAsync());
            
                logger.Log(LogLevel.Debug, $"ApplyUnderlyingIssue({issue}) Parameters={cmd.Parameters.ToPrettyString("\n")} CommandText={cmd.CommandText} affectedRows={affectedRows}");

                Assert.Equal(1L, affectedRows);
                    
                return 
                    await sut.DbConn.DbConn.BuildCmd(
                            sut.Naming,
                            @$"
select {sut.TestingSchema.FooTable_ConcurrencyTokenPropertyName} 
from {sut.TestingSchema.FooTableName} 
where {sut.TestingSchema.FooTable_IdColumnName} = {sut.Naming.NameForParameterUsage(0)}",
                            idColumnValue)
                        .ExecuteScalarAsync();
            }
             
            default: throw new ArgumentOutOfRangeException(nameof(issue), issue, null);
        }
    }
}