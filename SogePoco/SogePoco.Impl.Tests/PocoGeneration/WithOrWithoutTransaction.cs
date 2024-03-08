using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace SogePoco.Impl.Tests.PocoGeneration; 

public enum WithOrWithoutTransaction {
    WithTransactionCommitingIfSuccess,
    WithTransactionRollbackingAlways,
    WithoutTransaction
}

public static class WithOrWithoutTransactionExtensions {
        
    public static async Task ApplyStrategy(
        this WithOrWithoutTransaction self, Func<DbTransaction?,Task<bool>> isInTransaction, GeneratedCodeResult on, 
        Func<DbTransaction?,Task> continuation) {
            
        switch (self) {
            case WithOrWithoutTransaction.WithoutTransaction:
                Assert.False(await isInTransaction(null));
                await continuation(null);
                break;
                
            case WithOrWithoutTransaction.WithTransactionCommitingIfSuccess: {
                DbTransaction tran = await ((dynamic)on.DbConn).BeginTransactionAsync();
                var commit = false;
                    
                Assert.True(await isInTransaction(tran));
                    
                try {
                    await continuation(tran);
                    commit = true;
                } finally {
                    if (commit) {
                        await ((dynamic)on.DbConn).CommitAsync();
                    } else {
                        await ((dynamic)on.DbConn).RollbackAsync();
                    }
                    Assert.False(await isInTransaction(tran));
                }
                break; }
                
            case WithOrWithoutTransaction.WithTransactionRollbackingAlways: {
                DbTransaction tran = await ((dynamic)on.DbConn).BeginTransactionAsync();
                Assert.True(await isInTransaction(tran));
                    
                try {
                    await continuation(tran);
                } finally {
                    await ((dynamic)on.DbConn).RollbackAsync();
                    Assert.False(await isInTransaction(tran));
                }
                break; }

            default: throw new Exception($"unsupported {nameof(WithOrWithoutTransaction)}");
        }
    }
}