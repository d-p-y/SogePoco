#! "netstandard2.0"

// requires https://github.com/dotnet-script/dotnet-script
// install by running: 
//     dotnet tool install -g dotnet-script

#nullable enable

//dependencies of SogePoco

#r "nuget: Microsoft.Bcl.AsyncInterfaces, 7.0.0.0"

//SogePoco nuget
//#r "nuget: SogePoco.Impl, 0.0.1"
#r "../../../../SogePoco/SogePoco.Impl/bin/Debug/netstandard2.0/SogePoco.Impl.dll"

//db driver
#r "nuget: SQLitePCLRaw.core, 2.1.4"
#r "nuget: Microsoft.Data.Sqlite, 7.0.11"

//configuration for source generation
#load "SogePocoSqliteConfig.cs"

var cfg = new SogePocoSqliteConfig();
var resultPath = await SogePoco.Impl.SchemaExtraction.DbSchema.ExtractAndSerialize(cfg);
Console.WriteLine($"OK extracted and saved dbschema to {resultPath}\n");


/* if you are getting "e_sqlite3" message like one below (when script succeeds but has problem cleaning up during exit), it seems to be related to dotnet script bug. 
On my PC I've fixed it by doing:
$ ln -s ~/.nuget/packages/sqlitepclraw.lib.e_sqlite3/2.1.4/runtimes/linux-x64/native/libe_sqlite3.so ~/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/libe_sqlite3.so



Unhandled exception. System.DllNotFoundException: Unable to load shared library 'e_sqlite3' or one of its dependencies. In order to help diagnose loading problems, consider using a tool like strace. If you're using glibc, consider setting the LD_DEBUG environment variable: 
/usr/share/dotnet/shared/Microsoft.NETCore.App/7.0.11/e_sqlite3.so: cannot open shared object file: No such file or directory
/home/dominik/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/e_sqlite3.so: cannot open shared object file: No such file or directory
/usr/share/dotnet/shared/Microsoft.NETCore.App/7.0.11/libe_sqlite3.so: cannot open shared object file: No such file or directory
/home/dominik/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/libe_sqlite3.so: cannot open shared object file: No such file or directory
/usr/share/dotnet/shared/Microsoft.NETCore.App/7.0.11/e_sqlite3: cannot open shared object file: No such file or directory
/home/dominik/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/e_sqlite3: cannot open shared object file: No such file or directory
/usr/share/dotnet/shared/Microsoft.NETCore.App/7.0.11/libe_sqlite3: cannot open shared object file: No such file or directory
/home/dominik/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.4/lib/net6.0/libe_sqlite3: cannot open shared object file: No such file or directory

   at SQLitePCL.SQLite3Provider_e_sqlite3.NativeMethods.sqlite3_close_v2(IntPtr db)
   at SQLitePCL.SQLite3Provider_e_sqlite3.SQLitePCL.ISQLite3Provider.sqlite3_close_v2(IntPtr db)
   at SQLitePCL.raw.internal_sqlite3_close_v2(IntPtr p)
   at SQLitePCL.sqlite3.ReleaseHandle()
   at System.Runtime.InteropServices.SafeHandle.InternalRelease(Boolean disposeOrFinalizeOperation)
   at System.Runtime.InteropServices.SafeHandle.Dispose()
   at Microsoft.Data.Sqlite.SqliteConnectionInternal.Dispose()
   at Microsoft.Data.Sqlite.SqliteConnectionPool.DisposeConnection(SqliteConnectionInternal connection)
   at Microsoft.Data.Sqlite.SqliteConnectionPool.Clear()
   at Microsoft.Data.Sqlite.SqliteConnectionFactory.ReleasePool(SqliteConnectionPool pool, Boolean clearing)
   at Microsoft.Data.Sqlite.SqliteConnectionPoolGroup.Clear()
   at Microsoft.Data.Sqlite.SqliteConnectionFactory.ClearPools()
   at Microsoft.Data.Sqlite.SqliteConnectionFactory.<.ctor>b__7_1(Object _, EventArgs _)

*/