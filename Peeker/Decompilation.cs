using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace Peeker
{
    public partial class Decompilation : IDecompilation
    {
        public string SarifPath { get; internal set; }
        public bool ProcessCompilerDiagnostics { get; internal set; }
        public string FileName { get; internal set; }

        internal readonly IILMapper ILMapper;
        internal readonly ILogger<Decompilation> Logger;
        internal AnalyzerConfig Config;

        internal string[] SyntaxTreesText;

        internal List<Diagnostic> FilteredDiagnostics;

        internal ImmutableArray<DiagnosticAnalyzer> Analyzers;
        internal MergedRuleSet RuleSet;

        public Decompilation(IILMapper ilMapper, ILogger<Decompilation> logger)
        {
            ILMapper = ilMapper ?? throw new ArgumentNullException(nameof(ilMapper));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(AnalyzerConfig config, ImmutableArray<DiagnosticAnalyzer> analyzers, MergedRuleSet ruleSet, string file)
        {
            if (Config != null)
            {
                throw new InvalidOperationException("Cannot initialize Decompilation more than once.");
            }

            Config = config ?? throw new ArgumentNullException(nameof(config));

            RuleSet = ruleSet;
            FileName = file;
            Analyzers = analyzers;
            SarifPath = Config.Output;
            ProcessCompilerDiagnostics = Config.IncludeCompilerDiagnostics;

            InitializeILSpy();
            CreateSyntaxTrees();

            if (!string.IsNullOrWhiteSpace(Config.DumpSourcePath))
            {
                DumpTreesToDisk();
            }

            var compilation = CreateCompilation();
            GetAndProcessDiagnostics(compilation);
        }

        public virtual IEnumerable<Diagnostic> GetDiagnostics() => FilteredDiagnostics;

        internal virtual MetadataReference[] ResolveReferences()
        {
            var assembliesResolved = new HashSet<string>();
            var assemblyPathsFound = new HashSet<string>();
            var referencesToResolve = new List<object>();
            var referencesResolved = new List<MetadataReference>();

            referencesToResolve.AddRange(Decompiler.TypeSystem.ReferencedModules);

            while (referencesToResolve.Any())
            {
                var maybeModule = referencesToResolve[0];
                referencesToResolve.RemoveAt(0);

                string moduleName = "";
                string assemblyName = "";
                string location = "";

                if (maybeModule is MinimalCorlib)
                {
                    continue;
                }
                if (maybeModule is IModule referencedModule)
                {
                    location = referencedModule.PEFile?.FileName;
                    assemblyName = referencedModule.FullAssemblyName;
                    moduleName = referencedModule.Name;
                }
                else if (maybeModule is IAssemblyReference referencedAssembly)
                {
                    moduleName = referencedAssembly.Name;
                    assemblyName = referencedAssembly.FullName;
                    location = AssemblyResolver.FindAssemblyFile(referencedAssembly);
                }

                if (assembliesResolved.Contains(assemblyName) || assemblyPathsFound.Contains(location))
                {
                    continue;
                }

                assemblyPathsFound.Add(location);

                if (!string.IsNullOrWhiteSpace(location))
                {
                    Logger.LogDebug("Resolved reference {moduleName} to file {location}", moduleName, location);
                    var resolvedReference = MetadataReference.CreateFromFile(location);
                    referencesResolved.Add(resolvedReference);
                    assembliesResolved.Add(assemblyName);

                    if (maybeModule is IModule referencedModule2)
                    {
                        foreach (var childReference in referencedModule2.PEFile.AssemblyReferences)
                        {
                            referencesToResolve.Add(childReference);
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("Could not resolve reference {moduleName} ({assemblyName}, {moduleReferenceType})", moduleName, assemblyName, maybeModule.GetType());
                }
            }

            return referencesResolved.ToArray();
        }

        internal virtual void CreateSyntaxTrees()
        {
            ILSpySyntaxTrees = Decompiler.DecompileTrees(Module, DecompilerSettings, AssemblyResolver, Config.OneFilePerType).ToArray();
            Logger.LogDebug("Decompilation complete for {file}.", FileName);
            SyntaxTreesText = ILSpySyntaxTrees.Select(tree => Decompiler.SyntaxTreeToString(tree, DecompilerSettings)).ToArray();
            GenerateRoslynTrees();
            Logger.LogDebug("Generated {count} syntax tree(s) for {file}.", SyntaxTreesText.Length, FileName);
        }

        internal virtual string GetOutputPathForSyntaxTree(ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree tree)
        {
            var filename = tree.FileName;

            filename = filename.Replace('<', '(');
            filename = filename.Replace('>', ')');

            foreach (var forbidden in Path.GetInvalidPathChars())
                filename = filename.Replace(forbidden, '_');

            return Path.GetFullPath(Path.Combine(Config.DumpSourcePath, FileName, filename));
        }

        internal virtual void DumpTreesToDisk()
        {
            if (File.Exists(Config.DumpSourcePath))
            {
                Logger.LogError("Decompiled source will not be dumped to {targetPath} because a file exists with that name.", Config.DumpSourcePath);
                // Set DumpSourcePath to null so checks in TryResolveOriginalLocation will skip.
                Config.DumpSourcePath = null;
                return;
            }

            Directory.CreateDirectory(Path.Combine(Config.DumpSourcePath, FileName));

            for (int i = 0; i < SyntaxTreesText.Length; i++)
            {
                var ilspyAst = ILSpySyntaxTrees[i];
                File.WriteAllText(GetOutputPathForSyntaxTree(ilspyAst), SyntaxTreesText[i]);
            }
        }

        internal virtual void GetAndProcessDiagnostics(Microsoft.CodeAnalysis.Diagnostics.CompilationWithAnalyzers compilation)
        {
            var analyzerDiagnostics = compilation.GetAnalyzerDiagnosticsAsync().Result;
            var compilerDiagnostics = ImmutableArray.Create<Diagnostic>();

            Logger.LogInformation("{numDiagnostics} analyzer diagnostics found for {file}", analyzerDiagnostics.Length, FileName);

            if (ProcessCompilerDiagnostics)
            {
                compilerDiagnostics = compilation.Compilation.GetDiagnostics();
                Logger.LogInformation("{numDiagnostics} compiler diagnostics found for {file}", compilerDiagnostics.Length, FileName);
            }

            if (analyzerDiagnostics.Any())
            {
                Logger.LogInformation("--- Analyzer diagnostics:");

                foreach (var diagnostic in analyzerDiagnostics)
                {
                    PrintDiagnostic(diagnostic);
                }
            }

            if (ProcessCompilerDiagnostics && compilerDiagnostics.Any())
            {
                Logger.LogInformation("--- Compiler diagnostics:");

                foreach (var diagnostic in compilerDiagnostics)
                {
                    PrintDiagnostic(diagnostic);
                }
            }

            var diagnosticsToLog = ProcessCompilerDiagnostics ? analyzerDiagnostics.Concat(compilerDiagnostics) : analyzerDiagnostics;
            FilteredDiagnostics = diagnosticsToLog.ToList();
        }
    }
}
