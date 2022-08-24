using Autofac;
using CommandLine;
using Microsoft.Extensions.Logging;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Peeker.Test")]

namespace Peeker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = new Parser(conf =>
            {
                conf.GetoptMode = true;
                conf.HelpWriter = Console.Error;
            });
            var parserResult = parser.ParseArguments<AnalyzerConfig>(args);
            parserResult
                .WithParsed(config =>
                {
                    if (config.Target.Any() != true)
                    {
                        Console.WriteLine($"Target file not found or provided (use --target).");
                        Environment.Exit(1);
                    }

                    if (!Enum.TryParse<LogLevel>(config.LogLevel, true, out _))
                    {
                        Console.WriteLine($"Log level {config.LogLevel} not recognized, valid options are: {string.Join(',', Enum.GetNames<LogLevel>())}");
                        Environment.Exit(1);
                    }

                    Run(config);
                })
                .WithNotParsed(errors =>
                {
                    var errorsFiltered = errors.Where(error => error is not HelpRequestedError && error is not VersionRequestedError);

                    if (errorsFiltered.Any())
                    {
                        Console.WriteLine($"Failed to parse command line.");
                        foreach (var error in errorsFiltered)
                        {
                            Console.WriteLine(error.ToString());
                        }
                        Environment.Exit(1);
                    }
                });
        }

        static void Run(AnalyzerConfig config)
        {
            var container = Bootstrapper.Bootstrap(config);

            using (var rootScope = container.BeginLifetimeScope())
            {
                var controller = rootScope.Resolve<IDecompilationController>();
                controller.Process(config);
            }
        }
    }
}