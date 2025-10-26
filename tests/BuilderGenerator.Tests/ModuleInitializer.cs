using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using DiffEngine;

using VerifyTests;

namespace BuilderGenerator.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // https://stackoverflow.com/a/60545278/781045
        AssemblyConfigurationAttribute? assemblyConfigurationAttribute = typeof(ModuleInitializer).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        if (assemblyConfigurationAttribute is not null)
            Console.WriteLine($"Build Configuration is {assemblyConfigurationAttribute.Configuration}.");

        VerifySourceGenerators.Initialize();
    }
}
