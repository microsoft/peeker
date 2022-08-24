using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.IL;
using Microsoft.Extensions.Logging;

namespace Peeker
{
    public class ILMapper : IILMapper
    {
        private readonly ILogger<ILMapper> Logger;

        public ILMapper(ILogger<ILMapper> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IILFunction? FindParentFunction(IAstNode node)
        {
            int visited = 0;
            int maxVisit = 1000;

            var nextNode = node;
            IILFunction? ret = null;

            while (visited++ < maxVisit && nextNode != null)
            {
                if ((ret = nextNode.FunctionAnnotation()) != null)
                {
                    return ret;
                }

                nextNode = nextNode.Parent;
            }

            return null;
        }

        public IEnumerable<SequencePoint> FindCoveringSequencePoints(IList<SequencePoint> points, int startLine, int endLine, int startColumn, int endColumn)
        {
            var pointsOrdered = points;
            int firstPointIndex = FindFirstSequencePointBySourceLocation(pointsOrdered, startLine, startColumn);

            if (firstPointIndex == -1)
            {
                return Enumerable.Empty<SequencePoint>();
            }

            int lastPointIndex = firstPointIndex;

            while (lastPointIndex < pointsOrdered.Count)
            {
                if (pointsOrdered[lastPointIndex].IsAhead(endLine, endColumn))
                {
                    break;
                }

                lastPointIndex++;
            }

            return pointsOrdered.Skip(firstPointIndex).Take((lastPointIndex - firstPointIndex) + 1);
        }

        public IEnumerable<SequencePoint> MapCoveringSequencePointsByILOffset(IList<SequencePoint> needlePoints, IList<SequencePoint> haystackPoints)
        {
            if (!needlePoints.Any() || !haystackPoints.Any())
            {
                return Enumerable.Empty<SequencePoint>();
            }

            var firstOffset = needlePoints[0].Offset;
            var lastOffset = needlePoints.Last().EndOffset;

            var firstMatchingPoint = FindFirstSequencePointByILOffset(haystackPoints, firstOffset);
            var lastMatchingPoint = FindLastSequencePointByILOffset(haystackPoints, lastOffset);

            if (lastMatchingPoint < firstMatchingPoint || firstMatchingPoint < 0 || lastMatchingPoint < 0)
            {
                return Enumerable.Empty<SequencePoint>();
            }

            return haystackPoints.Skip(firstMatchingPoint).Take((lastMatchingPoint - firstMatchingPoint) + 1);
        }


        #region Search functions
        public int FindLastSequencePointByILOffset(IList<SequencePoint> points, int endOffset)
        {
            if (points.Count == 0)
            {
                return -1;
            }

            if (points.Count == 1)
            {
                return 0;
            }

            int left = 0;
            int right = points.Count;
            int middle = (left + right) / 2;
            SequencePoint currentPoint = points[middle];

            while (left < right)
            {
                currentPoint = points[middle];

                if (currentPoint.Offset < endOffset)
                {
                    left = middle + 1;
                }
                else if (currentPoint.Offset > endOffset)
                {
                    right = middle - 1;
                }
                else
                {
                    break;
                }

                middle = (left + right) / 2;
            }

            bool isBehind = currentPoint.Offset < endOffset;

            if (isBehind)
            {
                while (currentPoint.Offset <= endOffset)
                {
                    middle++;

                    if (middle >= points.Count)
                        break;

                    currentPoint = points[middle];
                }

                middle--;
            }
            else
            {
                while (currentPoint.Offset >= endOffset)
                {
                    middle--;

                    if (middle < 0)
                        break;

                    currentPoint = points[middle];
                }

                middle++;
            }

            if (middle >= points.Count)
            {
                middle = points.Count - 1;
            }

            if (points[middle].Offset >= endOffset && middle > 0)
            {
                middle--;
            }

            return middle;
        }

        public int FindFirstSequencePointByILOffset(IList<SequencePoint> points, int startOffset)
        {
            if (points.Count == 0)
            {
                return -1;
            }

            if (points.Count == 1)
            {
                return 0;
            }

            int left = 0;
            int right = points.Count;
            int middle = (left + right) / 2;
            SequencePoint currentPoint = points[middle];

            while (left < right)
            {
                currentPoint = points[middle];

                if (currentPoint.Offset < startOffset)
                {
                    left = middle + 1;
                }
                else if (currentPoint.Offset > startOffset)
                {
                    right = middle - 1;
                }
                else
                {
                    break;
                }

                middle = (left + right) / 2;
            }

            bool isBehind = currentPoint.Offset < startOffset;

            if (isBehind)
            {
                while (currentPoint.Offset <= startOffset)
                {
                    middle++;

                    if (middle >= points.Count)
                        break;

                    currentPoint = points[middle];
                }
                middle--;
            }
            else
            {
                while (currentPoint.Offset >= startOffset)
                {
                    middle--;

                    if (middle < 0)
                        break;

                    currentPoint = points[middle];
                }

                middle++;
            }

            if (middle >= points.Count)
            {
                return -1;
            }

            return middle;
        }

        public int FindFirstSequencePointBySourceLocation(IList<SequencePoint> points, int startLine, int startColumn)
        {
            if (points.Count == 0)
            {
                return -1;
            }

            if (points.Count == 1)
            {
                return 0;
            }

            int left = 0;
            int right = points.Count;
            int middle = (left + right) / 2;
            SequencePoint currentPoint = points[middle];

            while (left < right)
            {
                currentPoint = points[middle];

                if (currentPoint.StartLine < startLine)
                {
                    left = middle + 1;
                }
                else if (currentPoint.StartLine >= startLine)
                {
                    right = middle - 1;
                }
                else
                {
                    break;
                }

                middle = (left + right) / 2;
            }

            bool isBehind = currentPoint.IsBehind(startLine, startColumn);

            if (isBehind)
            {
                while (currentPoint.IsBehind(startLine, startColumn))
                {
                    middle++;

                    if (middle >= points.Count)
                        break;

                    currentPoint = points[middle];
                }
                middle--;
            }
            else
            {
                while (!currentPoint.IsBehind(startLine, startColumn))
                {
                    middle--;

                    if (middle < 0)
                        break;

                    currentPoint = points[middle];
                }

                middle++;
            }

            if (middle >= points.Count)
            {
                return -1;
            }

            return middle;
        }

        public IEnumerable<SequencePoint> MaybeNudgeNopOutOfRange(IList<SequencePoint> selectedRange, IList<SequencePoint> originalPoints, IILFunction function)
        {
            // Maybe patch first sequence point if it aligns to a nop, avoiding overhighlighting of original source
            SequencePoint firstDecompilationSequencePoint = selectedRange.First();
            SequencePoint lastDecompilationSequencePoint = selectedRange.Last();
            int ilStart = firstDecompilationSequencePoint.Offset;
            int ilEnd = lastDecompilationSequencePoint.EndOffset;

            if (ilEnd - ilStart > 2)
            {
                // Only nudge forwards if there is no sequence point aligned
                bool pointAligned = false;
                for (int i = 0; i < originalPoints.Count; i++)
                {
                    var originalSequencePoint = originalPoints[i];

                    if (originalSequencePoint.Offset == ilStart)
                    {
                        pointAligned = true;
                        break;
                    }

                    if (originalSequencePoint.Offset > ilStart)
                    {
                        break;
                    }
                }

                if (!pointAligned)
                {
                    bool statementAligned = false;
                    bool statementOffByOne = false;

                    foreach (ILInstruction descendant in function.Body.Descendants)
                    {
                        if (descendant.StartILOffset == ilStart)
                        {
                            statementAligned = true;
                            break;
                        }

                        if (descendant.StartILOffset == ilStart + 1)
                        {
                            statementOffByOne = true;
                            break;
                        }

                        if (descendant.StartILOffset > ilStart)
                        {
                            break;
                        }
                    }

                    if (statementOffByOne)
                    {
                        Logger.LogTrace("Nudging IL range forward by 1");
                        var modifiedFirstSequencePoint = new SequencePoint()
                        {
                            Offset = ilStart + 1,
                            EndOffset = firstDecompilationSequencePoint.EndOffset,
                            StartLine = firstDecompilationSequencePoint.StartLine,
                            StartColumn = firstDecompilationSequencePoint.StartColumn,
                            EndLine = firstDecompilationSequencePoint.EndLine,
                            EndColumn = firstDecompilationSequencePoint.EndColumn,
                            DocumentUrl = firstDecompilationSequencePoint.DocumentUrl
                        };

                        selectedRange[0] = modifiedFirstSequencePoint;
                    }
                    else if (!statementAligned)
                    {
                        Logger.LogWarning("IL range neither aligned nor off by one.");
                    }
                }
            }

            return selectedRange;
        }
        #endregion
    }
}
