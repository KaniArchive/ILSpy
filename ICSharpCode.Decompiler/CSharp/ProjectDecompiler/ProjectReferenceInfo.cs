using System.Collections.Generic;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler;

public class ProjectReferenceInfo
{
	public ProjectReferenceInfo(string assemblyName, string relativeCsprojPath)
	{
		AssemblyName = assemblyName;
		RelativeCsprojPath = relativeCsprojPath;
	}

	public string AssemblyName { get; }

	public string RelativeCsprojPath { get; }
}

public interface IProjectReferenceInfoProvider
{
	IReadOnlyList<ProjectReferenceInfo> ProjectReferences { get; }
}

public interface IProjectDependencyHintProvider
{
	bool TryGetDependencyHintPath(string assemblyName, out string hintPath);
}
