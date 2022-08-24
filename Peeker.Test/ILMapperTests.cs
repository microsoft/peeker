using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Logging;
using Moq;

namespace Peeker.Test
{
    public class ILMapperTests
    {
        private ILMapper GetMapper() => new ILMapper(new Mock<ILogger<ILMapper>>().Object);

        private static SequencePoint[] PointsA = new[]
        {
            CreatePoint(0x0, 0x1, 10, 0, 10, 20),
            CreatePoint(0x1, 0x7, 11, 0, 12, 20),
            CreatePoint(0x7, 0xc, 12, 20, 12, 40),
            CreatePoint(0xc, 0x10, 13, 0, 14, 40),
            CreatePoint(0x10, 0x12, 15, 0, 15, 1),
        };
        private static SequencePoint[] PointsB = new[]
        {
            CreatePoint(0x0, 0x0, 20, 0, 20, 20),
            CreatePoint(0x1, 0x0, 21, 0, 22, 20),
            CreatePoint(0x7, 0x0, 22, 20, 22, 40),
            CreatePoint(0xd, 0x0, 23, 0, 24, 40),
            CreatePoint(0x10, 0x0, 25, 0, 25, 1),
        };

        #region FindParentFunction
        [Fact]
        public void FindParentFunction_Expected()
        {
            var fakeMethod = new Mock<IMethod>();
            var fakeFunction = new Mock<IILFunction>();
            var fakeNode = new Mock<IAstNode>();
            var fakeParent = new Mock<IAstNode>();
            fakeNode.SetupGet(mock => mock.Parent).Returns(fakeParent.Object);
            fakeParent.Setup(mock => mock.FunctionAnnotation()).Returns(fakeFunction.Object);

            var mapper = GetMapper();
            var actual = mapper.FindParentFunction(fakeNode.Object);

            Assert.Equal(fakeFunction.Object, actual);

            fakeNode.VerifyGet(mock => mock.Parent, Times.Once());
        }

        [Fact]
        public void FindParentFunction_NoFunction()
        {
            var fakeMethod = new Mock<IMethod>();
            var fakeFunction = new Mock<IILFunction>();
            var fakeNode = new Mock<IAstNode>();
            var fakeParent = new Mock<IAstNode>();
            fakeNode.SetupGet(mock => mock.Parent).Returns(fakeParent.Object);
            fakeParent.SetupGet(mock => mock.Parent).Returns((IAstNode?)null);

            var mapper = GetMapper();
            var actual = mapper.FindParentFunction(fakeNode.Object);

            Assert.Null(actual);

            fakeNode.VerifyGet(mock => mock.Parent, Times.Once());
        }

        [Fact]
        public void FindParentFunction_SelfReference()
        {
            var fakeMethod = new Mock<IMethod>();
            var fakeFunction = new Mock<IILFunction>();
            var fakeNode = new Mock<IAstNode>();
            var fakeParent = new Mock<IAstNode>();
            fakeNode.SetupGet(mock => mock.Parent).Returns(fakeNode.Object);

            var mapper = GetMapper();
            var actual = mapper.FindParentFunction(fakeNode.Object);

            Assert.Null(actual);
        }
        #endregion

        #region FindFirstSequencePointBySourceLocation
        [Fact]
        public void FindFirstSequencePointBySourceLocation_Empty()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(Array.Empty<SequencePoint>(), 0, 0);
            Assert.Equal(-1, actual);
        }

        [Fact]
        public void FindFirstSequencePointBySourceLocation_Aligned()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 11, 0);
            Assert.Equal(1, actual);
        }
        [Fact]
        public void FindFirstSequencePointBySourceLocation_Misaligned()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 11, 1);
            Assert.Equal(1, actual);
        }
        [Fact]
        public void FindFirstSequencePointBySourceLocation_Misaligned2()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 12, 18);
            Assert.Equal(1, actual);
        }
        [Fact]
        public void FindFirstSequencePointBySourceLocation_Misaligned3()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 12, 0);
            Assert.Equal(1, actual);
        }
        [Fact]
        public void FindFirstSequencePointBySourceLocation_Misaligned4()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 11, 999);
            Assert.Equal(1, actual);
        }
        [Fact]
        public void FindFirstSequencePointBySourceLocation_OutOfRange()
        {
            var actual = GetMapper().FindFirstSequencePointBySourceLocation(PointsA, 2222, 0);
            Assert.Equal(-1, actual);
        }
#endregion

        #region FindFirstSequencePointByILOffset
        [Fact]
        public void FindFirstSequencePointByILOffset_Empty()
        {
            var actual = GetMapper().FindFirstSequencePointByILOffset(Array.Empty<SequencePoint>(), 0x1);
            Assert.Equal(-1, actual);
        }

        [Fact]
        public void FindFirstSequencePointByILOffset_Aligned()
        {
            var actual = GetMapper().FindFirstSequencePointByILOffset(PointsA, 0x1);
            Assert.Equal(1, actual);
        }

        [Fact]
        public void FindFirstSequencePointByILOffset_Misaligned()
        {
            var actual = GetMapper().FindFirstSequencePointByILOffset(PointsA, 0x3);
            Assert.Equal(1, actual);
        }

        [Fact]
        public void FindFirstSequencePointByILOffset_OutOfRange()
        {
            var actual = GetMapper().FindFirstSequencePointByILOffset(PointsA, 0x2222);
            Assert.Equal(-1, actual);
        }
#endregion

        #region FindLastSequencePointByILOffset
        [Fact]
        public void FindLastSequencePointByILOffset_Empty()
        {
            var actual = GetMapper().FindLastSequencePointByILOffset(Array.Empty<SequencePoint>(), 0x1);
            Assert.Equal(-1, actual);
        }

        [Fact]
        public void FindLastSequencePointByILOffset_Aligned()
        {
            var actual = GetMapper().FindLastSequencePointByILOffset(PointsA, 0x1);
            Assert.Equal(0, actual);
        }

        [Fact]
        public void FindLastSequencePointByILOffset_Misaligned()
        {
            var actual = GetMapper().FindLastSequencePointByILOffset(PointsA, 0x3);
            Assert.Equal(1, actual);
        }

        [Fact]
        public void FindLastSequencePointByILOffset_OutOfRange()
        {
            var actual = GetMapper().FindLastSequencePointByILOffset(PointsA, 0x2222);
            Assert.Equal(PointsA.Length - 1, actual);
        }
#endregion

        #region FindCoveringSequencePoints
        [Fact]
        public void FindCoveringSequencePoints_Empty()
        {
            var actual = (GetMapper().FindCoveringSequencePoints(Array.Empty<SequencePoint>(), 0, 10, 0, 0) ?? Array.Empty<SequencePoint>()).ToArray();
            Assert.Empty(actual);
        }

        [Fact]
        public void FindCoveringSequencePoints_Aligned()
        {
            var actual = (GetMapper().FindCoveringSequencePoints(PointsA, 12, 14, 20, 40) ?? Array.Empty<SequencePoint>()).ToArray();

            Assert.Equal(2, actual.Length);

            var offset = 2;
            for (int i = 0; i < actual.Length; i++)
            {
                Assert.Equal(PointsA[i + offset], actual[i]);
            }
        }

        [Fact]
        public void FindCoveringSequencePoints_Misaligned()
        {
            var actual = (GetMapper().FindCoveringSequencePoints(PointsA, 12, 14, 18, 40) ?? Array.Empty<SequencePoint>()).ToArray();
            Assert.Equal(PointsA.AsSpan(1, 3).ToArray(), actual);
        }

        [Fact]
        public void FindCoveringSequencePoints_Misaligned2()
        {
            var actual = (GetMapper().FindCoveringSequencePoints(PointsA, 12, 14, 18, 41) ?? Array.Empty<SequencePoint>()).ToArray();
            Assert.Equal(PointsA.AsSpan(1, 4).ToArray(), actual);
        }
        [Fact]
        public void FindCoveringSequencePoints_OutOfRange()
        {
            var actual = (GetMapper().FindCoveringSequencePoints(PointsA, 2222, 3000, 0, 0) ?? Array.Empty<SequencePoint>()).ToArray();
            Assert.Empty(actual);
        }
        #endregion

        #region MapCoveringSequencePointsByILOffset
        [Fact]
        public void MapCoveringSequencePointsByILOffset_Empty()
        {
            var actual = GetMapper().MapCoveringSequencePointsByILOffset(Array.Empty<SequencePoint>(), Array.Empty<SequencePoint>());
            Assert.Empty(actual);
        }

        [Fact]
        public void MapCoveringSequencePointsByILOffset_FullyAligned()
        {
            var actual = GetMapper().MapCoveringSequencePointsByILOffset(PointsA, PointsB);
            Assert.Equal(PointsB, actual);
        }

        [Fact]
        public void MapCoveringSequencePointsByILOffset_SubsetAligned()
        {
            var actual = GetMapper().MapCoveringSequencePointsByILOffset(PointsA.AsSpan(1, 2).ToArray(), PointsB);
            Assert.Equal(PointsB.AsSpan(1, 2).ToArray(), actual);
        }

        [Fact]
        public void MapCoveringSequencePointsByILOffset_SubsetAligned2()
        {
            var actual = GetMapper().MapCoveringSequencePointsByILOffset(PointsA.AsSpan(1, 3).ToArray(), PointsB);
            Assert.Equal(PointsB.AsSpan(1, 3).ToArray(), actual);
        }
        [Fact]
        public void MapCoveringSequencePointsByILOffset_SubsetAligned3()
        {
            var actual = GetMapper().MapCoveringSequencePointsByILOffset(PointsA.AsSpan(1, 4).ToArray(), PointsB);
            Assert.Equal(PointsB.AsSpan(1, 4).ToArray(), actual);
        }
        [Fact]
        public void MapCoveringSequencePointsByILOffset_Misaligned()
        {
            var searchPoints = new[]
            {
                CreatePoint(0x2, 0x3)
            };

            var actual = GetMapper().MapCoveringSequencePointsByILOffset(searchPoints, PointsB);
            Assert.Equal(PointsB.AsSpan(1, 1).ToArray(), actual);
        }
        [Fact]
        public void MapCoveringSequencePointsByILOffset_Misaligned2()
        {
            var searchPoints = new[]
            {
                CreatePoint(0x2, 0x3),
                CreatePoint(0x3, 0xd)
            };

            var actual = GetMapper().MapCoveringSequencePointsByILOffset(searchPoints, PointsB);
            Assert.Equal(PointsB.AsSpan(1, 2).ToArray(), actual);
        }
        [Fact]
        public void MapCoveringSequencePointsByILOffset_Misaligned3()
        {
            var searchPoints = new[]
            {
                CreatePoint(0x2, 0x3),
                CreatePoint(0x3, 0xe)
            };

            var actual = GetMapper().MapCoveringSequencePointsByILOffset(searchPoints, PointsB);
            Assert.Equal(PointsB.AsSpan(1, 3).ToArray(), actual);
        }
        #endregion

        private static SequencePoint CreatePoint(int ilStart, int ilEnd = 0, int lineStart = 0, int columnStart = 0, int lineEnd = 0, int columnEnd = 0)
        {
            return new SequencePoint()
            {
                Offset = ilStart,
                EndOffset = ilEnd,
                StartLine = lineStart,
                EndLine = lineEnd,
                StartColumn = columnStart,
                EndColumn = columnEnd
            };
        }
    }
}