using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace Peeker
{
    public class DecompilationController : IDecompilationController
    {
        private readonly Func<IDecompilation> DecompilationFactory;
        private readonly IResultWriter ResultWriter;
        private readonly ILogger<DecompilationController> Logger;

        internal DiagnosticAnalyzer[] Analyzers;

        public DecompilationController(IResultWriter resultWriter, Func<IDecompilation> decompilationFactory, ILogger<DecompilationController> logger)
        {
            ResultWriter = resultWriter ?? throw new ArgumentNullException(nameof(resultWriter));
            DecompilationFactory = decompilationFactory ?? throw new ArgumentNullException(nameof(decompilationFactory));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Process(AnalyzerConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var analyzerPaths = new List<string>();

            if (config.Analyzers != null)
            {
                analyzerPaths.AddRange(config.Analyzers);
            }

            if (config.AnalyzersFromDirectories != null)
            {
                foreach (var analyzerDirectory in config.AnalyzersFromDirectories)
                {
                    analyzerPaths.AddRange(SearchForAnalyzersInDirectory(analyzerDirectory));
                }
            }

            if (!analyzerPaths.Any())
            {
                Logger.LogError("No code analyzers provided.");
                throw new ArgumentException("No code analyzers provided.");
            }
            else if (analyzerPaths.Any(path => !File.Exists(path)))
            {
                var notFound = analyzerPaths.Where(path => !File.Exists(path));
                var notFoundString = string.Join(", ", notFound);
                Logger.LogError("The following analyzer files could not be found: {analyzerFiles}", notFoundString);
                throw new FileNotFoundException($"Analyzer files not found: {notFoundString}");
            }

            Directory.CreateDirectory(config.Output);

            var sw = Stopwatch.StartNew();
            var analyzers = LoadAnalyzers(analyzerPaths).ToImmutableArray();
            var ruleSet = new MergedRuleSet(config.Rulesets?.Select(RuleSet.LoadEffectiveRuleSetFromFile) ?? Enumerable.Empty<RuleSet>());

            var filesSkipped = new List<string>();

            foreach (var file in config.Target)
            {
                try
                {
                    var decompilation = DecompilationFactory();
                    decompilation.Process(config, analyzers, ruleSet, file);

                    var outputPath = Path.Combine(config.Output, $"{Path.GetFileName(decompilation.FileName)}.sarif");

                    var log = ResultWriter.ProcessResults(decompilation);
                    ResultWriter.WriteResults(log, path: outputPath, debug: config.PrettyPrintSarif);

                    Logger.LogInformation("Results from {file} written to {resultPath}.", decompilation.FileName, outputPath);
                }
                catch (PEFileNotSupportedException)
                {
                    Logger.LogInformation("Binary {file} is not a .NET assembly.", file);
                }
                catch (Exception ex)
                {
                    // TODO: Report these, possibly in SARIF?
                    Logger.LogError(ex, "Failed to process {file}.", file);
                    filesSkipped.Add(file);
                }
            }

            Logger.LogInformation("Processing took {timeSeconds}s.", sw.Elapsed.TotalSeconds.ToString("0.00"));

            if (filesSkipped.Any())
            {
                Logger.LogError("Could not process {count} files: ", filesSkipped.Count);
                Logger.LogError("{files}", string.Join("\n", filesSkipped.Select(file => $" - {file}")));
            }
        }

        internal virtual IEnumerable<string> SearchForAnalyzersInDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return Enumerable.Empty<string>();
            }

            // Only supports the folder structure "analyzers/dotnet/cs/*.dll".
            var searchPath = Path.Combine(directory, "analyzers/dotnet/cs"); 
            if (!Directory.Exists(searchPath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(searchPath, "*.dll", SearchOption.TopDirectoryOnly);
        }

        internal virtual IEnumerable<DiagnosticAnalyzer> LoadAnalyzers(IEnumerable<string> analyzerPaths)
        {
            List<DiagnosticAnalyzer> analyzers = new();

            foreach (var analyzerFileName in analyzerPaths)
            {
                var assembly = Assembly.LoadFrom(analyzerFileName);
                var analyzersFromFile = assembly.GetTypes()
                                        .Where(t => t.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
                                        .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)).ToList();

                Logger.LogDebug("Loaded {analyzerCount} analyzers from {analyzerFileName}", analyzersFromFile.Count, analyzerFileName);
                analyzers.AddRange(analyzersFromFile);
            }

            Logger.LogInformation("{analyzerCount} total analyzers loaded.", analyzers.Count);
            return analyzers;
        }
    }
}
