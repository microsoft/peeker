using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.IL;

namespace Peeker
{
    public interface IILMapper
    {
        public IILFunction? FindParentFunction(IAstNode node);
        public IEnumerable<SequencePoint> FindCoveringSequencePoints(IList<SequencePoint> points, int startLine, int endLine, int startColumn, int endColumn);
        public IEnumerable<SequencePoint> MaybeNudgeNopOutOfRange(IList<SequencePoint> selectedRange, IList<SequencePoint> originalPoints, IILFunction function);
        public IEnumerable<SequencePoint> MapCoveringSequencePointsByILOffset(IList<SequencePoint> needlePoints, IList<SequencePoint> haystackPoints);
        public int FindLastSequencePointByILOffset(IList<SequencePoint> points, int endOffset);
        public int FindFirstSequencePointByILOffset(IList<SequencePoint> points, int startOffset);
        public int FindFirstSequencePointBySourceLocation(IList<SequencePoint> points, int startLine, int startColumn);
    }
}
