using ICSharpCode.Decompiler.DebugInfo;

namespace Peeker.Test
{
    public class ILSpyUtilTests
    {
        [Fact]
        public void IsBehind_Tests()
        {
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(1, 1));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(1, 10));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(1, 11));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(2, 0));

            Assert.False(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(1, 0));
            Assert.False(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsBehind(0, 11));
        }

        [Fact]
        public void IsAhead_Tests()
        {
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(1, 0));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(1, 1));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(1, 10));
            Assert.True(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(0, 11));

            Assert.False(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(1, 11));
            Assert.False(CreatePoint(lineStart: 1, columnStart: 1, lineEnd: 1, columnEnd: 10).IsAhead(2, 0));

        }

        private static SequencePoint CreatePoint(int lineStart = 0, int columnStart = 0, int lineEnd = 0, int columnEnd = 0)
        {
            return new SequencePoint()
            {
                StartLine = lineStart,
                EndLine = lineEnd,
                StartColumn = columnStart,
                EndColumn = columnEnd
            };
        }
    }
}
