using ICSharpCode.Decompiler.IL;

namespace ICSharpCode.Decompiler.CSharp.Syntax
{
    public interface IAstNode
    {
        public IAstNode? Parent { get; }
        public IILFunction? FunctionAnnotation();
    }
}
