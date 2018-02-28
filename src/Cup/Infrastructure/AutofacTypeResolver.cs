using System;
using Autofac;
using Spectre.CommandLine;

namespace Cup.Infrastructure
{
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