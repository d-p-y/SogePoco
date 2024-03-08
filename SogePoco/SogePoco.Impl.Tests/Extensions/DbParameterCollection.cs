using System.Data.Common;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.Tests.Extensions; 

public static class DbParameterCollectionExtensions {
    public static void AddMany(this DbParameterCollection self, params DbParameter[] prms) 
        => prms.ForEach(x => self.Add(x));
}