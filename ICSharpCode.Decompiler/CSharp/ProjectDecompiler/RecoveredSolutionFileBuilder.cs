using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp.ProjectDecompiler;

public class RecoveredSolutionFileBuilder(
	string outputDir,
	string solutionName,
	IReadOnlyList<string> projectPaths)
{
	public string Build()
	{
		string fullOutputDir = Path.GetFullPath(outputDir);
		Directory.CreateDirectory(fullOutputDir);

		string solutionPath = ResolveSolutionPath();
		string solutionDir = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
		Directory.CreateDirectory(solutionDir);

		using (var writer = new StreamWriter(solutionPath))
		using (var xml = new XmlTextWriter(writer))
		{
			xml.Formatting = Formatting.Indented;
			xml.WriteStartElement("Solution");

			foreach (string projectPath in projectPaths.OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase))
			{
				xml.WriteStartElement("Project");
				xml.WriteAttributeString("Path", FileUtility.GetRelativePath(solutionDir, Path.GetFullPath(projectPath)).Replace('\\', '/'));
				xml.WriteEndElement();
			}

			xml.WriteEndElement();
		}

		return solutionPath;
	}

	private string ResolveSolutionPath()
	{
		if (string.IsNullOrWhiteSpace(solutionName))
		{
			string fullOutputDir = Path.GetFullPath(outputDir);
			string outputName = new DirectoryInfo(fullOutputDir).Name;
			return Path.Combine(fullOutputDir, $"{outputName}.slnx");
		}

		string solutionPath = Path.HasExtension(solutionName)
			? solutionName
			: $"{solutionName}.slnx";

		return Path.GetFullPath(Path.IsPathRooted(solutionPath)
			? solutionPath
			: Path.Combine(Path.GetFullPath(outputDir), solutionPath));
	}
}
