using System.Collections.Generic;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler;

public class ProjectReferenceInfo
{
	public ProjectReferenceInfo(string assemblyName, string relativeCsprojPath)
		: this(assemblyName, relativeCsprojPath, [])
	{
	}

	public ProjectReferenceInfo(string assemblyName, string relativeCsprojPath, IReadOnlyList<string> dependencyAssemblyNames)
	{
		AssemblyName = assemblyName;
		RelativeCsprojPath = relativeCsprojPath;
		DependencyAssemblyNames = dependencyAssemblyNames;
	}

	public string AssemblyName { get; }

	public string RelativeCsprojPath { get; }

	public IReadOnlyList<string> DependencyAssemblyNames { get; }
}

public interface IProjectReferenceInfoProvider
{
	IReadOnlyList<ProjectReferenceInfo> ProjectReferences { get; }
}

public interface IProjectDependencyHintProvider
{
	bool TryGetDependencyHintPath(string assemblyName, out string hintPath);
}

public interface IProjectTargetFrameworkOverrideProvider
{
	string TargetFrameworkOverride { get; }
}
