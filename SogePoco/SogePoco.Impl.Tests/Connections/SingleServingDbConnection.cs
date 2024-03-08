using System;
using System.Data.Common;
using System.Reflection;

namespace SogePoco.Impl.Tests.Connections; 

public interface SingleServingDbConnection : IDisposable {
    public DbConnection DbConn { get; }
    
    public string InitialConnectionString { get; }
    public string TargetDatabaseName { get; }
    
    DbConnection CreateAndOpenAnotherConnection();
    
    public string AdoDbConnectionFullClassName => DbConn.GetType().FullName!; 
    public string AdoDbCommandFullClassName => DbConn.GetType().GetMethod("CreateCommand", BindingFlags.Instance|BindingFlags.Public)!.ReturnType.FullName!;
}