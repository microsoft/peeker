using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.ILSpyX.PdbProvider;

using ILSpySequencePoint = ICSharpCode.Decompiler.DebugInfo.SequencePoint;

namespace Peeker
{
    public partial class Decompilation
    {
        public ICSharpDecompiler Decompiler { get; set; }
        public PEFile Module { get; set; }
        public DecompilerSettings DecompilerSettings { get; set; }
        public UniversalAssemblyResolver AssemblyResolver { get; set; }

        internal SyntaxTree[] ILSpySyntaxTrees;

        internal Dictionary<IILFunction, List<ILSpySequencePoint>>[] SequencePointsByILSpyTrees;
        internal HashSet<EntityHandle> SanitizedFunctions = new HashSet<EntityHandle>();
        internal Dictionary<EntityHandle, IList<ILSpySequencePoint>> SequencePointsByPDBFunction;

        internal virtual void InitializeILSpy()
        {
            Module = new PEFile(FileName);
            AssemblyResolver = new UniversalAssemblyResolver(FileName, false, Module.DetectTargetFrameworkId());

            DecompilerSettings = new DecompilerSettings(LanguageVersion.CSharp8_0)
            {
                ThrowOnAssemblyResolveErrors = true,
                LiftNullables = false,
                NullPropagation = false,
                UseRefLocalsForAccurateOrderOfEvaluation = true,
                FileScopedNamespaces = false,
                NullableReferenceTypes = true,

                // for global:: fully qualified types:
                AlwaysQualifyMemberReferences = true,
                UsingDeclarations = false,
                StringInterpolation = false,
                QueryExpressions = true,
                // AlwaysUseGlobal = true // will be available once ILSpy PR #2762 is merged and released
            };

            Decompiler = new CSharpDecompilerExtended(FileName, AssemblyResolver, DecompilerSettings)
            {
                DebugInfoProvider = DebugInfoUtils.LoadSymbols(Module)
            };
        }

        internal virtual Dictionary<IILFunction, List<ILSpySequencePoint>>? GetSequencePointsForILSpyTree(SyntaxTree syntaxTree) =>
            GetSequencePointsForILSpyTree(Array.IndexOf(ILSpySyntaxTrees, syntaxTree));

        internal virtual Dictionary<IILFunction, List<ILSpySequencePoint>>? GetSequencePointsForILSpyTree(int syntaxTreeIndex)
        {
            if (SequencePointsByILSpyTrees == default)
            {
                SequencePointsByILSpyTrees = new Dictionary<IILFunction, List<ILSpySequencePoint>>[ILSpySyntaxTrees.Length];
            }

            if (syntaxTreeIndex < 0 || syntaxTreeIndex >= ILSpySyntaxTrees.Length)
            {
                return null;
            }

            if (SequencePointsByILSpyTrees[syntaxTreeIndex] == null)
            {
                return SequencePointsByILSpyTrees[syntaxTreeIndex] = Decompiler.CreateSequencePoints(ILSpySyntaxTrees[syntaxTreeIndex]);
            }

            return SequencePointsByILSpyTrees[syntaxTreeIndex];
        }

        internal virtual IList<ILSpySequencePoint>? GetSequencePointsForILSpyTreeAndFunction(int syntaxTreeIndex, IILFunction function)
        {
            var sequencePoints = GetSequencePointsForILSpyTree(syntaxTreeIndex);
            if (sequencePoints == null || !sequencePoints.ContainsKey(function))
            {
                return null;
            }

            var handle = function.Method.MetadataToken;
            if (!SanitizedFunctions.Contains(handle))
            {
                sequencePoints[function] = sequencePoints[function].Where(point => !point.IsHidden).OrderBy(point => point.StartLine).ThenBy(point => point.StartColumn).ToList();
                SanitizedFunctions.Add(handle);
            }

            return sequencePoints[function];
        }

        internal virtual IList<ILSpySequencePoint>? GetSequencePointsForFunctionInPDB(IILFunction function)
        {
            if (Decompiler.DebugInfoProvider == null)
            {
                return null;
            }

            var handle = function.Method.MetadataToken;

            if (SequencePointsByPDBFunction == null)
            {
                SequencePointsByPDBFunction = new();
            }

            if (!SequencePointsByPDBFunction.ContainsKey(handle))
            {
                var sequencePoints = Decompiler.DebugInfoProvider.GetSequencePoints((MethodDefinitionHandle)handle);
                if (sequencePoints == null)
                {
                    SequencePointsByPDBFunction[handle] = null;
                    return null;
                }

                for (int i = 0; i < sequencePoints.Count; i++)
                {
                    if (sequencePoints[i].IsHidden)
                    {
                        sequencePoints.RemoveAt(i);
                        i--;
                    }
                }
                SequencePointsByPDBFunction[handle] = sequencePoints;
            }

            return SequencePointsByPDBFunction[handle];
        }

        public virtual Microsoft.CodeAnalysis.FileLinePositionSpan? TryResolveOriginalLocation(Microsoft.CodeAnalysis.Diagnostic diagnostic)
        {
            if (diagnostic.Location.IsInSource)
            {
                var originalSyntaxTree = diagnostic.Location.SourceTree;
                var astIndex = Array.IndexOf(RoslynSyntaxTrees, originalSyntaxTree);
                var originalIlspyTree = ILSpySyntaxTrees[astIndex];
                var lineSpan = diagnostic.Location.GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line;
                var endLine = lineSpan.EndLinePosition.Line;

                if (Decompiler.DebugInfoProvider != null)
                {
                    var originalLocation = originalIlspyTree.GetNodeContaining(
                        new TextLocation(startLine + 1, lineSpan.StartLinePosition.Character),
                        new TextLocation(endLine + 1, lineSpan.EndLinePosition.Character)
                    );

                    var function = ILMapper.FindParentFunction(new AstNodeExtended(originalLocation));

                    if (function?.Method != null)
                    {
                        var decompiledSequencePoints = GetSequencePointsForILSpyTreeAndFunction(astIndex, function);
                        var originalSequencePoints = GetSequencePointsForFunctionInPDB(function);

                        if (originalSequencePoints.Any())
                        {
                            var matchingDecompilationSequencePoints = ILMapper.FindCoveringSequencePoints(decompiledSequencePoints, startLine + 1, endLine + 1, lineSpan.StartLinePosition.Character, lineSpan.EndLinePosition.Character).ToList();

                            if (matchingDecompilationSequencePoints.Any())
                            {
                                ILMapper.MaybeNudgeNopOutOfRange(matchingDecompilationSequencePoints, originalSequencePoints, function);
                                var mappedSequencePoints = ILMapper.MapCoveringSequencePointsByILOffset(matchingDecompilationSequencePoints, originalSequencePoints);

                                if (!mappedSequencePoints.Any())
                                {
                                    Logger.LogWarning("No covering sequence points found despite having IL range from decompilation.");
                                }
                                else
                                {
                                    var firstSequencePoint = mappedSequencePoints.First();
                                    var lastSequencePoint = mappedSequencePoints.Last();

                                    if (lastSequencePoint.EndLine < firstSequencePoint.StartLine ||
                                        (lastSequencePoint.EndLine == firstSequencePoint.StartLine && lastSequencePoint.EndColumn < firstSequencePoint.StartColumn))
                                    {
                                        Logger.LogWarning("Covering sequence point set is unordered, cannot create source mapping.");
                                    }
                                    else
                                    {
                                        return new Microsoft.CodeAnalysis.FileLinePositionSpan(
                                            path: firstSequencePoint.DocumentUrl,
                                            start: new Microsoft.CodeAnalysis.Text.LinePosition(firstSequencePoint.StartLine, firstSequencePoint.StartColumn),
                                            end: new Microsoft.CodeAnalysis.Text.LinePosition(lastSequencePoint.EndLine, lastSequencePoint.EndColumn)
                                        );
                                    }
                                }
                            }
                        }
                    }
                }

                // If we are dumping source and could not find the original location, might as well point to the dumped source.
                if (!string.IsNullOrEmpty(Config.DumpSourcePath))
                {
                    var dumpPath = GetOutputPathForSyntaxTree(originalIlspyTree);
                    return new Microsoft.CodeAnalysis.FileLinePositionSpan(
                        path: dumpPath,
                        start: lineSpan.StartLinePosition,
                        end: lineSpan.EndLinePosition);
                }
            }

            return null;
        }

        internal virtual void PrintDiagnostic(Microsoft.CodeAnalysis.Diagnostic diagnostic)
        {
            var diagnosticText = new StringBuilder();
            diagnosticText.AppendLine($"[{diagnostic.Id}:{diagnostic.Severity}] {diagnostic.GetMessage()}");
            if (diagnostic.IsSuppressed)
            {
                diagnosticText.AppendLine("^ diagnostic was suppressed");
            }

            if (diagnostic.Location.IsInSource)
            {
                var originalSyntaxTree = diagnostic.Location.SourceTree;
                var astIndex = Array.IndexOf(RoslynSyntaxTrees, originalSyntaxTree);
                var sourceAstAsString = SyntaxTreesText[astIndex];

                var lineSpan = diagnostic.Location.GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line;
                var currentLine = startLine;

                int startPreamble = diagnostic.Location.SourceSpan.Start;
                int endPreamble = diagnostic.Location.SourceSpan.End;

                while (startPreamble > 0 && sourceAstAsString[startPreamble] != '\n')
                {
                    startPreamble--;
                }

                if (sourceAstAsString[startPreamble] == '\n')
                {
                    startPreamble++;
                }

                while (endPreamble < sourceAstAsString.Length && sourceAstAsString[endPreamble] != '\n')
                {
                    endPreamble++;
                }

                if (sourceAstAsString[endPreamble] == '\n')
                {
                    endPreamble--;
                }

                var sourceSnippet = sourceAstAsString[startPreamble..endPreamble];
                var sourceLines = sourceSnippet.Split('\n');

                foreach (var sourceLine in sourceLines)
                {
                    diagnosticText.AppendLine($"  {currentLine++} | {sourceLine}");
                }

                var originalLocation = TryResolveOriginalLocation(diagnostic);

                if (originalLocation.HasValue)
                {
                    diagnosticText.AppendLine($"  ^ Has source mapping: {originalLocation.Value.Path} " +
                        $"({originalLocation.Value.StartLinePosition.Line}:{originalLocation.Value.StartLinePosition.Character}-" +
                        $"{originalLocation.Value.EndLinePosition.Line}:{originalLocation.Value.EndLinePosition.Character})");
                }
            }

            Logger.LogInformation("{diagnosticDetails}", diagnosticText.ToString());
        }
    }
}
