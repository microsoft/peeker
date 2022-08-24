using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.CSharp
{
    public interface ICSharpDecompiler
    {
        IDecompilerTypeSystem TypeSystem { get; }
        IDebugInfoProvider DebugInfoProvider { get; }
        Dictionary<IILFunction, List<SequencePoint>> CreateSequencePoints(SyntaxTree syntaxTree);
        string SyntaxTreeToString(SyntaxTree syntaxTree, DecompilerSettings settings);
        IEnumerable<SyntaxTree> DecompileTrees(PEFile module, DecompilerSettings decompilerSettings, IAssemblyResolver assemblyResolver, bool oneTreePerType);
    }
}
