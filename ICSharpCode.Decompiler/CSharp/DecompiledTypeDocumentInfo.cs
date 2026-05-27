using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;

using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp
{
	public sealed class DecompiledTypeDocumentInfo
	{
		readonly IReadOnlyDictionary<string, IReadOnlyList<EntityHandle>> membersByDocument;
		readonly IReadOnlyList<EntityHandle> unmappedMembers;

		internal DecompiledTypeDocumentInfo(
			FullTypeName typeName,
			DecompiledTypeCacheEntry cacheEntry,
			Dictionary<string, List<EntityHandle>> membersByDocument,
			List<EntityHandle> unmappedMembers)
		{
			this.TypeName = typeName;
			this.CacheEntry = cacheEntry;
			this.membersByDocument = membersByDocument.ToDictionary(
				pair => pair.Key,
				pair => (IReadOnlyList<EntityHandle>)new ReadOnlyCollection<EntityHandle>(pair.Value)
			);
			this.unmappedMembers = new ReadOnlyCollection<EntityHandle>(unmappedMembers);
		}

		public FullTypeName TypeName { get; }

		public DecompiledTypeCacheEntry CacheEntry { get; }

		public IReadOnlyDictionary<string, IReadOnlyList<EntityHandle>> MembersByDocument => membersByDocument;

		public IReadOnlyList<EntityHandle> UnmappedMembers => unmappedMembers;
	}
}
