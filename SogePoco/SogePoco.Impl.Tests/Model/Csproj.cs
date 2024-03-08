using System;
using System.Collections.Generic;
using System.Linq;
using SogePoco.Impl.Extensions;
using SogePoco.Impl.Model;

namespace SogePoco.Impl.Tests.Model; 

public record NugetPackage(string Name, string Version);
	
public static class NugetPackageCollection {
	public static IReadOnlyCollection<NugetPackage> Empty = Array.Empty<NugetPackage>();
		
	public static IReadOnlyCollection<NugetPackage> ForGeneratedPocos = new[] {
		new NugetPackage("Microsoft.Extensions.Logging.Abstractions", "6.0.1"),
		new NugetPackage("System.Linq.Async", "6.0.1"),
		new NugetPackage("Microsoft.Data.Sqlite", "5.0.5"),
		new NugetPackage("Npgsql", "5.0.4"),
		new NugetPackage("System.Data.SqlClient", "4.5.1") };
		
	public static IReadOnlyCollection<NugetPackage> ForGeneratedQueries = new[] {
		new NugetPackage("Microsoft.Extensions.Logging.Abstractions", "6.0.1"),
		new NugetPackage("System.Linq.Async", "6.0.1"),
		new NugetPackage("Microsoft.Data.Sqlite", "5.0.5"),
		new NugetPackage("Npgsql", "5.0.4"),
		new NugetPackage("System.Data.SqlClient", "4.5.1") };
}

public static class EmbeddedResourceCollection {
	public static IReadOnlyCollection<SimpleNamedFile> Empty = Array.Empty<SimpleNamedFile>();
}
	
public static class CsprojCollection {
	public static IReadOnlyCollection<Csproj> Empty = Array.Empty<Csproj>();
}
	
public class Csproj {
	public string Name { get; }
	public string CsProjFileName {get;}
	private readonly List<SimpleNamedFile> _files = new();
	public IEnumerable<SimpleNamedFile> Files => _files;

	public Csproj(
		string name, 
		IReadOnlyCollection<SimpleNamedFile> sources,
		IReadOnlyCollection<SimpleNamedFile> embeddedResources,
		IReadOnlyCollection<NugetPackage> nugetPackages, 
		IReadOnlyCollection<Csproj> referencesProjs) {
			
		Name = name;
		_files.AddRange(sources);
		_files.AddRange(embeddedResources);
			
		CsProjFileName = $"{name}.csproj";

		var nugetPackagesCsprojSnippet = nugetPackages
			.Select(x => $@"    <PackageReference Include=""{x.Name}"" Version=""{x.Version}"" />")
			.ConcatenateUsingNewLine();
			
		var referencesProjsCsprojSnippet = referencesProjs
			.Select(x => $@"    <ProjectReference Include=""..\{x.Name}\{x.Name}.csproj"" />")
			.ConcatenateUsingNewLine();;
			
		var embeddedResourcesCsprojSnippet = embeddedResources
			.Select(x => $@"    <EmbeddedResource Include=""{x.FileName}"" />")
			.ConcatenateUsingNewLine();
				
		_files.Add(new SimpleNamedFile(
			CsProjFileName, 
			$@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>9.0</LangVersion>
  </PropertyGroup>
  <ItemGroup>
{nugetPackagesCsprojSnippet}
  </ItemGroup>
  <ItemGroup>
{embeddedResourcesCsprojSnippet}
  </ItemGroup>
  <ItemGroup>
{referencesProjsCsprojSnippet}
  </ItemGroup>
</Project>
"));
			
	}
}