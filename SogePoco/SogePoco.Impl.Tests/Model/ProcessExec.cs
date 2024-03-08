using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.Tests.Model; 

public static class ProcessExec {
    private static List<string> LookupDirs { get; }
    public static string HandyTempDirLocation { get; }
    public static ILogger? Logger;

    static ProcessExec() {
        var pth = Path.GetDirectoryName(typeof(ProcessExec).Assembly.Location);

        if (pth != null) {
            var pthComp = pth.Split(Path.DirectorySeparatorChar);
				
            if (pthComp.Length >= 3 && 
                pthComp[pthComp.Length-3].ToLower() != "bin" && 
                pthComp[pthComp.Length-2].ToLower() != "debug") {
						
                pth = null; //strange, likely unwanted location
            }
        }
			
        pth = System.Environment.GetEnvironmentVariable("TMPDIR") ?? pth;

        if (pth == null && Directory.Exists("/tmp")) {
            pth = "/tmp";
        }

        HandyTempDirLocation = pth ?? throw new Exception("could not compute safe temp dir");
			 	
        LookupDirs = 
            (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .ToList()
            .IfTrueThenAlso(
                () => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                x => x.Add(Environment.CurrentDirectory));
    }

    private static string Which(string cmd, bool calledRecursively) {
        Logger?.LogDebug($"{nameof(ProcessExec)}->{nameof(Which)} cmd={cmd}");
        var result = LookupDirs
            .Select(x => {
                var fullPth = Path.Combine(x, cmd);
                return File.Exists(fullPth) ? fullPth : null; })
            .WhereIsNotNull()
            .FirstOrDefault();

        var alsoLookForExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			
        if (result == null && alsoLookForExe && !calledRecursively) {
            result = Which(cmd+".exe", true);
        }
			
        Logger?.LogDebug($"{nameof(ProcessExec)}->{nameof(Which)} result={result}");
			
        return result ?? throw new Exception($"couldn't find executable {cmd} in any PATH environment variable element");
    }

    public static string Which(string cmd) => Which(cmd, false);

    public static (int exitCode, string stdOut) InDirExecCmd(string workDir, string fullPathCmd, string args="") {
        Logger?.LogDebug($"{nameof(ProcessExec)}->{nameof(InDirExecCmd)} workDir={workDir} fullPathCmd={fullPathCmd} args={args}");

        var pi = new ProcessStartInfo() {
            CreateNoWindow = true,
            WorkingDirectory = workDir,
            FileName = fullPathCmd,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false};

        var p = new Process() {StartInfo = pi};

        var sb = new StringBuilder();

        p.OutputDataReceived += (_, ea) => sb.AppendLine(ea.Data);
			
        p.Start();
        p.WaitForExit();

        return (p.ExitCode, sb.ToString());
    }	
		
    public static void InDirExecCmdOrFail(string workDir, string fullPathCmd, string args="") {
        var res = InDirExecCmd(workDir, fullPathCmd, args);

        if (res.exitCode != 0) {
            throw new Exception("executing command returned nonzero exit code");
        }
    }
}