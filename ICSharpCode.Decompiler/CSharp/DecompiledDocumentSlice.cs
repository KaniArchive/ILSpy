using System.Collections.Generic;

using ICSharpCode.Decompiler.CSharp.Syntax;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.CSharp
{
	public sealed class DecompiledDocumentSlice
	{
		internal DecompiledDocumentSlice(string documentUrl, SyntaxTree syntaxTree, IReadOnlyList<SRM.EntityHandle> memberHandles)
		{
			this.DocumentUrl = documentUrl;
			this.SyntaxTree = syntaxTree;
			this.MemberHandles = memberHandles;
		}

		public string DocumentUrl { get; }

		public SyntaxTree SyntaxTree { get; }

		public IReadOnlyList<SRM.EntityHandle> MemberHandles { get; }
	}
}
