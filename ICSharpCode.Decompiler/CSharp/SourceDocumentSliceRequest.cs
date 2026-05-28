using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.CSharp
{
	public sealed class SourceDocumentSliceRequest
	{
		public SourceDocumentSliceRequest(string documentPath, IReadOnlyList<EntityHandle> memberHandles, bool isGenerated = false)
		{
			this.DocumentPath = documentPath;
			this.MemberHandles = memberHandles;
			this.IsGenerated = isGenerated;
		}

		public string DocumentPath { get; }

		public IReadOnlyList<EntityHandle> MemberHandles { get; }

		public bool IsGenerated { get; }
	}
}
