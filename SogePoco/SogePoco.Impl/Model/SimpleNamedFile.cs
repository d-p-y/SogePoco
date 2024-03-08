using Microsoft.Extensions.Logging;
using SogePoco.Impl.Extensions;

namespace SogePoco.Impl.Model; 

public record SimpleNamedFile(string FileName, string Content) {
    public void SaveToDisk(string directory) =>
        System.IO.File.WriteAllText(
            Path.Combine(directory, FileName),
            Content);
}

public static class SetOfSimpleNamedFileExtensions {
    public static ILogger? Logger;
        
    public static void AssureExistsAndIsWithoutCsFilesThenSave(
        this ISet<SimpleNamedFile> self, string directoryPath) {

        Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} analysing dir={directoryPath}");
            
        if (!Directory.Exists(directoryPath)) {
            Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} nonexisting dir");
            Directory.CreateDirectory(directoryPath);
            Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} dir created");
        } else {
            Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} already exists dir");
            Directory.EnumerateFiles(directoryPath, "*.cs").ForEach(x => {
                Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} removing old cs file={x}");
                File.Delete(x);
            });
        }
            
        Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} about to save files");
            
        self.ForEach(x => {
            var path = Path.Combine(directoryPath, x.FileName);
            Logger?.LogDebug($"{nameof(AssureExistsAndIsWithoutCsFilesThenSave)} saving {path}");
            File.WriteAllText(path, x.Content);
        });
    }
}