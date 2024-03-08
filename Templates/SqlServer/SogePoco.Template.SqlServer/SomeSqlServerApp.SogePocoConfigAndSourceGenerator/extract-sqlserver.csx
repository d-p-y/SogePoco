#! "netstandard2.0"

// requires https://github.com/dotnet-script/dotnet-script
// install by running: 
//     dotnet tool install -g dotnet-script

#nullable enable

//dependencies of SogePoco
#r "nuget: System.Linq.Async, 6.0.1"
#r "nuget: Microsoft.Bcl.AsyncInterfaces, 7.0.0.0"

//SogePoco nuget
//#r "nuget: SogePoco.Impl, 0.0.2"
#r "../../../../SogePoco/SogePoco.Impl/bin/Debug/netstandard2.0/SogePoco.Impl.dll"

//db driver
#r "nuget: System.Data.SqlClient, 4.8.6"


//configuration for source generation
#load "SogePocoSqlServerConfig.cs"

var cfg = new SogePocoSqlServerConfig();
var resultPath = await SogePoco.Impl.SchemaExtraction.DbSchema.ExtractAndSerialize(cfg);
Console.WriteLine($"OK extracted and saved dbschema to {resultPath}\n");
