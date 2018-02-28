﻿using System;
using Autofac;
using Spectre.CommandLine;

namespace Cup.Infrastructure
{
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
}