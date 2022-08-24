using CommandLine;

namespace Peeker
{
    public class AnalyzerConfig
    {
        [Option("target", Required = true, HelpText = ".NET binaries to run analyzers against. Can be specified multiple times.")]
        public IEnumerable<string> Target { get; set; }
        [Option("ruleset", HelpText = "Ruleset files used to filter diagnostic results. Can be specified multiple times.")]
        public IEnumerable<string> Rulesets { get; set; }
        [Option("analyzers", HelpText = ".dll files to load Roslyn analyzers from. Can be specified multiple times.")]
        public IEnumerable<string> Analyzers { get; set; }
        [Option("analyzers-from-dir", HelpText = "Directories to load Roslyn analyzers from. Can be specified multiple times.")]
        public IEnumerable<string> AnalyzersFromDirectories { get; set; }
        [Option("output", Default = "PeekerResults", HelpText = "Directory to write analysis results to in the SARIF format. Output files will be named e.g. Peeker.dll.sarif if the input was Peeker.dll.")]
        public string Output { get; set; } = "PeekerResults";
        [Option("include-compiler-diagnostics", Default = false, HelpText = "Process compiler-generated diagnostic results.")]
        public bool IncludeCompilerDiagnostics { get; set; } = false;
        [Option("log-level", Default = "information", HelpText = "Logging verbosity.")]
        public string LogLevel { get; set; } = "information";
        [Option("pretty-print-sarif", Default = false, HelpText = "Indent and format outputted SARIF.")]
        public bool PrettyPrintSarif { get; set; } = false;
        [Option("dump-source", HelpText = "Target directory to dump decompiled source code into. Disabled by default, useful for debugging. If this option is enabled, source resolutions will fall back to pointing to the dumped source code.")]
        public string DumpSourcePath { get; set; } = string.Empty;
        [Option("file-per-type", Default = false, HelpText = "Create one syntax tree/source file per decompiled type. One syntax tree is generated for the entire assembly by default. Useful for debugging.")]
        public bool OneFilePerType { get; set; } = false;
    }
}
