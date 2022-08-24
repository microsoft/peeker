using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Peeker
{
    public class ResultWriter : IResultWriter
    {
        private readonly ILogger<ResultWriter> Logger;

        public ResultWriter(ILogger<ResultWriter> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual SarifLog ProcessResults(IDecompilation decompilation)
        {
            if (decompilation == null)
            {
                throw new ArgumentNullException(nameof(decompilation));
            }

            var log = new SarifLog();
            var diagnostics = decompilation.GetDiagnostics() ?? Array.Empty<Diagnostic>();

            Tool tool = new Tool()
            {
                Driver = new ToolComponent()
                {
                    Name = "roslyn-precompiled",
                    Version = "1.0.0"
                }
            };

            var ruleBag = new RuleBag();
            var run = new Run
            {
                Tool = tool,
                Results = new List<Result>(),
                Artifacts = new List<Artifact>(),
                LogicalLocations = new List<LogicalLocation>()
            };

            foreach (var diagnostic in diagnostics)
            {
                ProcessDiagnosticForSarif(decompilation, diagnostic, ruleBag, run);
            }

            run.Tool.Driver.Rules = ruleBag.Rules.ToList();

            log.Runs = new List<Run>() { run };
            return log;
        }

        public virtual void WriteResults(SarifLog log, string path, bool debug)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (debug)
            {
                using (var ms = new MemoryStream())
                using (var jsonWriter = new Utf8JsonWriter(File.Open(path, FileMode.Create), new JsonWriterOptions() { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                {
                    log.Save(ms);
                    var parsed = JsonDocument.Parse(ms.ToArray());
                    parsed.WriteTo(jsonWriter);
                }
            }
            else
            {
                log.Save(path);
            }
        }

        internal void ProcessDiagnosticForSarif(IDecompilation decompilation, Diagnostic diagnostic, RuleBag ruleBag, Run run)
        {
            (int ruleIndex, _) = ruleBag.RegisterRule(diagnostic.Descriptor);

            if (ruleIndex < 0)
            {
                Logger.LogError("Failed to register rule {ruleId} with RuleBag. Some results are omitted.", diagnostic.Id);
                return;
            }    

            var result = new Result()
            {
                RuleId = diagnostic.Descriptor.Id,
                RuleIndex = ruleIndex,
                Level = RuleBag.GetLevel(diagnostic.Severity)
            };

            string? message = diagnostic.GetMessage();
            if (!string.IsNullOrEmpty(message))
            {
                result.Message = new Message()
                {
                    Text = message
                };
            }

            var maybeOriginalLocation = decompilation.TryResolveOriginalLocation(diagnostic);

            result.Locations = new List<Microsoft.CodeAnalysis.Sarif.Location>();

            if (maybeOriginalLocation is FileLinePositionSpan originalLocation)
            {
                result.Locations.Add(new Microsoft.CodeAnalysis.Sarif.Location()
                {
                    PhysicalLocation = new PhysicalLocation()
                    {
                        ArtifactLocation = new ArtifactLocation()
                        {
                            Uri = new Uri(originalLocation.Path, UriKind.RelativeOrAbsolute)
                        },
                        Region = new Region()
                        {
                            StartLine = originalLocation.StartLinePosition.Line,
                            EndLine = originalLocation.EndLinePosition.Line,
                            StartColumn = originalLocation.StartLinePosition.Character,
                            EndColumn = originalLocation.EndLinePosition.Character,
                        }
                    }
                });
            }
            else
            {
                result.Locations.Add(new Microsoft.CodeAnalysis.Sarif.Location()
                {
                    PhysicalLocation = new PhysicalLocation()
                    {
                        ArtifactLocation = new ArtifactLocation()
                        {
                            Uri = new Uri(decompilation.FileName, UriKind.RelativeOrAbsolute)
                        }
                    },
                    LogicalLocations = new List<LogicalLocation>()
                    {
                        new LogicalLocation()
                        {
                            DecoratedName = diagnostic.Location.ToString(),
                            Kind = LogicalLocationKind.Module
                        }
                    }
                });
            }

            if (diagnostic.WarningLevel > 0)
            {
                result.SetProperty("warningLevel", diagnostic.WarningLevel);
            }

            if (diagnostic.Properties.Count > 0)
            {
                result.SetProperty("customProperties", diagnostic.Properties);
            }

            run.Results.Add(result);
        }
    }
}
