using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
    public class ILFunctionExtended : IILFunction
    {
        internal readonly ILFunction Function;
        public IMethod? Method => Function.Method;
        public ILInstruction Body => Function.Body;
        public ILFunctionExtended(ILFunction baseFunction)
        {
            Function = baseFunction ?? throw new ArgumentNullException(nameof(baseFunction));
        }

        public override bool Equals(object? obj)
        {
            return obj is ILFunctionExtended extended && Function.Equals(extended.Function);
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }

        public static bool operator ==(ILFunctionExtended? left, ILFunctionExtended? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(ILFunctionExtended? left, ILFunctionExtended? right)
        {
            return !(left == right);
        }
    }
}
