using SogePoco.Impl.CodeGen;

namespace SogePoco.Impl.UsingMsBuild;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SogePoco.Impl.Model;

public class QueryGenerationClasses {
    private readonly IEnumerable<(Compilation compilation, IEnumerable<(CompilationUnitSyntax cus,SyntaxTree st)> cusAndSts)> _items;
    private readonly PocoSchema _metaData;
    
    private QueryGenerationClasses(
            PocoSchema metaData,
            IEnumerable<(Compilation compilation, IEnumerable<(CompilationUnitSyntax,SyntaxTree)> cus)> items) {

        _metaData = metaData;
        _items = items;
    }

    public static ILogger? Logger;

    private static bool _msBuildLocatorRegisterDefaultsCalled = false;
    

    public static async Task<QueryGenerationClasses> CreateFromMsbuild(
            string slnPath, PocoSchema metaData) {
        
        //TODO add abstraction to provide separately CS AST and embedded resources 
        //TODO dispose
        
        /*
         * Following line is needed due to:
         * 
         * Unable to load one or more of the requested types.
         * Could not load file or assembly 'Microsoft.Build.Framework,
         * [...]
         */
        if (!_msBuildLocatorRegisterDefaultsCalled) {
            MSBuildLocator.RegisterDefaults();
            _msBuildLocatorRegisterDefaultsCalled = true;    
        }
        
        var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
        
        workspace.WorkspaceFailed += (_, args) => Logger?.Log(LogLevel.Debug, args.Diagnostic.Message); 
            
        var sln = await workspace.OpenSolutionAsync(slnPath);
        Logger?.LogDebug($"sln={slnPath} projectsCount={sln.Projects.Count()}");
        
        var projects = sln.Projects.ToList();

        var items = new List<(Compilation cu, IEnumerable<(CompilationUnitSyntax,SyntaxTree)> cus)>();
        
        foreach (var project in projects) {
            Logger?.LogDebug($"project={project.FilePath}");
            
            var compilation = await project.GetCompilationAsync();
            
            if (compilation == null) {
                throw new Exception($"{nameof(compilation)} is null");
            }
            
            var csDocs = project.Documents
                .Select(doc => (doc.FilePath?.ToLower().EndsWith(".cs"), doc))
                .Where(x => x.Item1 == true)
                .Select(x => x.doc)
                .ToList();

            var csAndSts = new List<(CompilationUnitSyntax,SyntaxTree)>();
            
            foreach (var csDoc in csDocs) {
                Logger?.LogDebug($"csDoc={csDoc.FilePath}");
                
                var csSyntaxTree = await csDoc.GetSyntaxTreeAsync();
                    
                if (csSyntaxTree == null) {
                    throw new Exception($"{nameof(csSyntaxTree)} is null");
                }
                
                var csCompilationUnit = csSyntaxTree.GetCompilationUnitRoot();
                csAndSts.Add((csCompilationUnit, csSyntaxTree));
            }
            
            items.Add((compilation, csAndSts));
        }
        
        return new QueryGenerationClasses(metaData, items);
    }

    public ISet<SimpleNamedFile> Process(
            Action<Compilation, SyntaxNode> visitor,
            Func<PocoSchema,ISet<SimpleNamedFile>> generator) 
        => Process(new QueryGeneratorOfDelegates(visitor, generator));

    public ISet<SimpleNamedFile> Process(IQueryGenerator generator) {
        foreach (var item in _items) {
            foreach (var cusAndSt in item.cusAndSts) {
                foreach (var syntaxNode in cusAndSt.cus.DescendantNodes()) {
                    generator.OnElement(item.compilation, syntaxNode);
                }
            }
        }
        return generator.GenerateFiles(_metaData);
    }
}
