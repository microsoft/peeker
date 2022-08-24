using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;

namespace Peeker
{
    public partial class Decompilation
    {
        private SyntaxTree[] RoslynSyntaxTrees;

        internal virtual void GenerateRoslynTrees()
        {
            RoslynSyntaxTrees = SyntaxTreesText.Select(str => CSharpSyntaxTree.ParseText(str)).ToArray();
        }

        public virtual CompilationWithAnalyzers CreateCompilation()
        {
            var outputKind = Module.Reader.PEHeaders.PEHeader.Subsystem switch
            {
                Subsystem.WindowsCui => OutputKind.ConsoleApplication,
                Subsystem.WindowsGui => OutputKind.WindowsApplication,
                Subsystem.Unknown => OutputKind.DynamicallyLinkedLibrary,
                _ => OutputKind.DynamicallyLinkedLibrary
            };

            if (Module.Reader.PEHeaders.IsDll)
            {
                outputKind = OutputKind.DynamicallyLinkedLibrary;
            }

            var compilationOptions = new CSharpCompilationOptions(
                outputKind, 
                nullableContextOptions: NullableContextOptions.Annotations, 
                allowUnsafe: true,
                delaySign: false,
                checkOverflow: false,
                reportSuppressedDiagnostics: true,
                generalDiagnosticOption: RuleSet.GeneralDiagnosticOption,
                specificDiagnosticOptions: RuleSet.SpecificDiagnosticOptions);

            var compilation = CSharpCompilation.Create(Module.Name, syntaxTrees: RoslynSyntaxTrees, ResolveReferences(), compilationOptions);
            return compilation.WithAnalyzers(Analyzers);
        }
    }
}