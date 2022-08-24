using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
    public interface IILFunction
    {
        IMethod? Method { get; }
        ILInstruction Body { get; }
    }
}
