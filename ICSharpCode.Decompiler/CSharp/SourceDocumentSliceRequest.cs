using System.Collections.Generic;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp
{
	public sealed class SourceDocumentSliceRequest
	{
		public SourceDocumentSliceRequest(string documentPath, IReadOnlyList<EntityHandle> memberHandles, bool isGenerated = false)
			: this(documentPath, memberHandles, EmptyList<EntityHandle>.Instance, isGenerated)
		{
		}

		public SourceDocumentSliceRequest(string documentPath, IReadOnlyList<EntityHandle> memberHandles, IReadOnlyList<EntityHandle> typeDeclarationHandles, bool isGenerated = false)
		{
			this.DocumentPath = documentPath;
			this.MemberHandles = memberHandles;
			this.TypeDeclarationHandles = typeDeclarationHandles;
			this.IsGenerated = isGenerated;
		}

		public string DocumentPath { get; }

		public IReadOnlyList<EntityHandle> MemberHandles { get; }

		public IReadOnlyList<EntityHandle> TypeDeclarationHandles { get; }

		public bool IsGenerated { get; }
	}
}
