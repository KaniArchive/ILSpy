using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler;

public class RecoveredProjectFileBuilder(
	string dllPath,
	string outputDir,
	string projectName,
	List<ProjectReferenceInfo> knownProjectRefs,
	string overrideCsVersion = null,
	string overrideTargetFramework = null,
	IReadOnlyList<string> dependencyDirs = null)
{
	public string Build()
	{
		var file = new PEFile(dllPath);
		var resolver = new UniversalAssemblyResolver(dllPath, false, file.DetectTargetFrameworkId(), file.DetectRuntimePack());
		AddSearchDirectories(resolver);

		Directory.CreateDirectory(outputDir);
		var outputPath = Path.Combine(outputDir, $"{projectName}.csproj");

		using (var writer = new StreamWriter(outputPath))
		{
			ProjectFileWriterSdkStyle.Create().Write(
				writer,
				new RecoveredProjectInfo(
					outputDir,
					resolver,
					knownProjectRefs,
					ResolveDependencyHints(),
					ResolveLanguageVersion(),
					NormalizeTargetFrameworkOverride()),
				Enumerable.Empty<ProjectItemInfo>(),
				file);
		}

		return outputPath;
	}

	private void AddSearchDirectories(UniversalAssemblyResolver resolver)
	{
		if (dependencyDirs == null)
			return;

		foreach (var dependencyDir in dependencyDirs.Where(Directory.Exists))
			resolver.AddSearchDirectory(dependencyDir);
	}

	private IReadOnlyDictionary<string, string> ResolveDependencyHints()
	{
		var hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dependencyDirs == null)
			return hints;

		foreach (var dependencyDir in dependencyDirs.Where(Directory.Exists))
		{
			foreach (var path in Directory.GetFiles(dependencyDir, "*.dll"))
			{
				var assemblyName = Path.GetFileNameWithoutExtension(path);
				if (!hints.ContainsKey(assemblyName))
					hints.Add(assemblyName, path);
			}
		}

		return hints;
	}

	private LanguageVersion ResolveLanguageVersion()
	{
		switch (overrideCsVersion)
		{
			case "15":
				return LanguageVersion.CSharp15_0;
			case "14":
				return LanguageVersion.CSharp14_0;
			case "13":
				return LanguageVersion.CSharp13_0;
			case "12":
				return LanguageVersion.CSharp12_0;
			case "11":
				return LanguageVersion.CSharp11_0;
			case "10":
				return LanguageVersion.CSharp10_0;
			case "9":
				return LanguageVersion.CSharp9_0;
			case "8":
				return LanguageVersion.CSharp8_0;
			case "latest":
			case null:
				return LanguageVersion.Latest;
			default:
				return LanguageVersion.Latest;
		}
	}

	private string NormalizeTargetFrameworkOverride()
	{
		if (string.IsNullOrWhiteSpace(overrideTargetFramework))
			return null;

		return overrideTargetFramework.Trim();
	}

	private sealed class RecoveredProjectInfo : IProjectInfoProvider, IProjectReferenceInfoProvider, IProjectDependencyHintProvider, IProjectTargetFrameworkOverrideProvider
	{
		public RecoveredProjectInfo(
			string targetDirectory,
			UniversalAssemblyResolver resolver,
			IReadOnlyList<ProjectReferenceInfo> projectReferences,
			IReadOnlyDictionary<string, string> dependencyHints,
			LanguageVersion languageVersion,
			string targetFrameworkOverride)
		{
			TargetDirectory = targetDirectory;
			AssemblyResolver = resolver;
			AssemblyReferenceClassifier = resolver;
			ProjectReferences = projectReferences;
			DependencyHints = dependencyHints;
			LanguageVersion = languageVersion;
			TargetFrameworkOverride = targetFrameworkOverride;
		}

		public IAssemblyResolver AssemblyResolver { get; }

		public AssemblyReferenceClassifier AssemblyReferenceClassifier { get; }

		public LanguageVersion LanguageVersion { get; }

		public bool CheckForOverflowUnderflow => false;

		public Guid ProjectGuid { get; } = Guid.NewGuid();

		public string TargetDirectory { get; }

		public string StrongNameKeyFile => null;

		public IReadOnlyList<ProjectReferenceInfo> ProjectReferences { get; }

		public string TargetFrameworkOverride { get; }

		private IReadOnlyDictionary<string, string> DependencyHints { get; }

		public bool TryGetDependencyHintPath(string assemblyName, out string hintPath) =>
			DependencyHints.TryGetValue(assemblyName, out hintPath);
	}
}
