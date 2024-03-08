using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.Tests.Model; 

public class Sln {
    public static ILogger? Logger;
		
    private readonly string _dir;
    private readonly string _name;
    public string SlnFullPath { get; }

    public Sln(string baseDir, string name) 
        : this(baseDir, name, Array.Empty<Csproj>() ) { }

    public Sln(string baseDir, string name, IReadOnlyCollection<Csproj> projs) {
        Logger?.LogDebug($"{nameof(Sln)} ctor: baseDir={baseDir} name={name}");
			
        _dir = Path.Combine(baseDir, name);
        _name = name;

        //as tests may be invoked several times
        if (Directory.Exists(_dir)) {
            Directory.Delete(_dir, true);
        }
        Directory.CreateDirectory(_dir);
			
        var slnFileName = $"{name}.sln";
        SlnFullPath = Path.Combine(_dir, slnFileName);
			
        File.WriteAllText(
            Path.Combine(_dir, slnFileName),
            @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30114.105
MinimumVisualStudioVersion = 10.0.40219.1
Global
        GlobalSection(SolutionConfigurationPlatforms) = preSolution
                Debug|Any CPU = Debug|Any CPU
                Release|Any CPU = Release|Any CPU
        EndGlobalSection
        GlobalSection(SolutionProperties) = preSolution
                HideSolutionNode = FALSE
        EndGlobalSection
EndGlobal
");
			
        projs.ForEach(Add);
    }

    public void Add(Csproj prj) {
        var projDir = Path.Combine(_dir, prj.Name);
			
        Logger?.LogDebug($"{nameof(Sln)}->{nameof(Add)} prj={prj} into projDir={projDir}");
			
        Directory.CreateDirectory(projDir);

        prj.Files.ForEach(x => {
            var path = Path.Combine(projDir, x.FileName);
            File.WriteAllText(path, x.Content);
        });

        var fullPathToDotnetCmd = ProcessExec.Which("dotnet");
        ProcessExec.InDirExecCmdOrFail(
            _dir,
            fullPathToDotnetCmd,
            $"sln add {prj.Name}{Path.DirectorySeparatorChar}{prj.Name}.csproj");
    }
		
		
    public void RemoveFromDisk() {
        Logger?.LogDebug($"{nameof(Sln)}->{nameof(RemoveFromDisk)} baseDir={_dir}");
		    
        Directory.Delete(_dir, recursive:true);
    }

}