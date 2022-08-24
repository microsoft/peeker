using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Peeker
{
    public interface IDecompilation
    {
        public string FileName { get; }
        public void Process(AnalyzerConfig config, ImmutableArray<DiagnosticAnalyzer> analyzers, MergedRuleSet ruleSet, string file);
        public IEnumerable<Diagnostic> GetDiagnostics();
        public FileLinePositionSpan? TryResolveOriginalLocation(Diagnostic diagnostic);
    }
}