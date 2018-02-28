using System;
using Autofac;
using Cup.Commands;
using Cup.Diagnostics;
using Cup.Processing;
using Cup.Utils;
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

    internal sealed class AutofacTypeRegistrar : ITypeRegistrar
    {
        private readonly ContainerBuilder _builder;

        public AutofacTypeRegistrar(ContainerBuilder builder)
        {
            _builder = builder;
        }

        public void Register(Type service, Type implementation)
        {
            _builder.RegisterType(implementation).As(service);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _builder.RegisterInstance(implementation).As(service);
        }

        public ITypeResolver Build()
        {
            return new AutofacTypeResolver(_builder.Build());
        }
    }

    internal sealed class AutofacTypeResolver : ITypeResolver, IDisposable
    {
        private readonly IContainer _container;

        public AutofacTypeResolver(IContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            _container?.Dispose();
        }

        public object Resolve(Type type)
        {
            if (!_container.TryResolve(type, out var result))
            {
                // Type was not registered. Try creating it anyway.
                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    result = Activator.CreateInstance(type);
                }
            }
            return result;
        }
    }
}
