using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.SchemaExtraction;
using Xunit;

namespace SogePoco.Impl.Tests; 

public static class SchemaUtil {
    public static void AssertEquals(IEnumerable<SqlTable>? expected, IEnumerable<SqlTable>? actual, ILogger logger) {
        var expTbls = (expected?.ToList() ?? new List<SqlTable>()).OrderBy(x => x.Schema).ThenBy(x => x.Name).ToList();
        var actTbls = (actual?.ToList() ?? new List<SqlTable>()).OrderBy(x => x.Schema).ThenBy(x => x.Name).ToList();
            
        Assert.Equal(
            expTbls.Select(x => (x.Schema, x.Name, x.Columns.Count, x.ForeignKeys.Count)), 
            actTbls.Select(x => (x.Schema, x.Name, x.Columns.Count, x.ForeignKeys.Count)));

        expTbls.ForEachI((iTbl,expTbl) => {
            logger.Log(LogLevel.Debug, $"Comparing table idx={iTbl} name={expTbls[iTbl].Name}");
            var expCols = expTbl.Columns.OrderBy(x => x.Name).ToList();
            var actCols = actTbls[iTbl].Columns.OrderBy(x => x.Name).ToList();
                
            expCols.ForEachI((iCol, expCol) => Assert.Equal(expCol, actCols[iCol]));
                
            var expFks = expTbl.ForeignKeys
                .OrderBy(x => (x.PrimaryKeySchema, x.PrimaryKeyTableName))
                .ThenBy(x => x.ForeignToPrimary.OrderBy(y => y.foreignColumnName).First())
                .ToList();
            var actFks = actTbls[iTbl].ForeignKeys
                .OrderBy(x => (x.PrimaryKeySchema, x.PrimaryKeyTableName))
                .ThenBy(x => x.ForeignToPrimary.OrderBy(y => y.foreignColumnName).First())
                .ToList();
                
            expFks.ForEachI((iFk, expFk) => {
                logger.Log(LogLevel.Debug, $"Comparing foreign keys in (foreign) table={expTbl.Name} referencing primaryTable={expFk.PrimaryKeyTableName}");
                    
                Assert.Equal(
                    (expFk.PrimaryKeySchema, expFk.PrimaryKeyTableName), 
                    (actFks[iFk].PrimaryKeySchema, actFks[iFk].PrimaryKeyTableName));
                Assert.Equal(expFk.ForeignToPrimary, actFks[iFk].ForeignToPrimary);
            });
        });
    }
}