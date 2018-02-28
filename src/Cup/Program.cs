using System;
using Autofac;
using Cup.Commands.Update;
using Cup.Commands.Update.Processing;
using Cup.Infrastructure;
using Cup.Infrastructure.Diagnostics;
using Spectre.CommandLine;
using Spectre.System;
using Spectre.System.IO;

namespace Cup
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var app = new CommandApp(CreateTypeResolver());
                app.Configure(config =>
                {
                    config.SetApplicationName("cup");
                    config.AddCommand<UpdateCommand>("update");
                });

                return app.Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An error occured: {0}", ex.Message);
                return 1;
            }
        }

        private static ITypeRegistrar CreateTypeResolver()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<FileSystem>().As<IFileSystem>().SingleInstance();
            builder.RegisterType<Spectre.System.Environment>().As<IEnvironment>().SingleInstance();
            builder.RegisterType<Platform>().As<IPlatform>().SingleInstance();
            builder.RegisterType<ConsoleLog>().As<IConsoleLog>().SingleInstance();
            builder.RegisterType<RepositoryProcessor>().SingleInstance();

            return new AutofacTypeRegistrar(builder);
        }
    }
}
