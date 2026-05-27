using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp
{
	public sealed class DecompiledTypeCacheEntry
	{
		readonly SyntaxTree syntaxTree;

		internal DecompiledTypeCacheEntry(FullTypeName typeName, SyntaxTree syntaxTree)
		{
			this.TypeName = typeName;
			this.syntaxTree = syntaxTree;
		}

		public FullTypeName TypeName { get; }

		public SyntaxTree CreateClone()
		{
			return (SyntaxTree)syntaxTree.Clone();
		}
	}
}
