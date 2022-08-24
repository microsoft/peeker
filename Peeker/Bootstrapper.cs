using Autofac;
using Microsoft.Extensions.Logging;

namespace Peeker
{
    public static class Bootstrapper
    {
        public static IContainer Bootstrap(AnalyzerConfig config)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ILMapper>().As<IILMapper>();
            builder.RegisterType<Decompilation>().As<IDecompilation>();
            builder.RegisterType<DecompilationController>().As<IDecompilationController>();
            builder.RegisterType<ResultWriter>().As<IResultWriter>();
            builder.RegisterInstance(config).SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
            builder.Register(handler => LoggerFactory.Create(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(Enum.Parse<LogLevel>(config.LogLevel, true));
            })).As<ILoggerFactory>().SingleInstance();

            return builder.Build();
        }
    }
}
