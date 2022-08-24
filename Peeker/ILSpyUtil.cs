namespace Peeker
{
    public static class ILSpyUtil
    {
        public static bool IsBehind(this ICSharpCode.Decompiler.DebugInfo.SequencePoint point, int startLine, int startColumn)
        {
            return point.StartLine < startLine || (point.StartLine == startLine && point.StartColumn <= startColumn);
        }

        public static bool IsAhead(this ICSharpCode.Decompiler.DebugInfo.SequencePoint point, int endLine, int endColumn)
        {
            return point.EndLine > endLine || (point.EndLine == endLine && point.EndColumn >= endColumn);
        }
    }
}
