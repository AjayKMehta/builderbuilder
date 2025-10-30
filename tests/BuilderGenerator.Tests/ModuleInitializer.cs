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

        DiffTools.UseOrder(DiffTool.WinMerge);
        ResolvedTool? _ = DiffTools.AddToolBasedOn(
            DiffTool.WinMerge,
            name: "MyDiffTool",
            launchArguments: new(
                Left: TargetLeftArguments,
                Right: TargetRightArguments)
            );

        VerifySourceGenerators.Initialize();
    }
    private static string TargetLeftArguments(string temp, string target)
    {
        var tempTitle = Path.GetFileName(temp);
        var targetTitle = Path.GetFileName(target);
        return $"/u /ignoreeol /wl /e \"{target}\" \"{temp}\" /dl \"{targetTitle}\" /dr \"{tempTitle}\" /cfg Backup/EnableFile=0";
    }

    private static string TargetRightArguments(string temp, string target)
    {
        var tempTitle = Path.GetFileName(temp);
        var targetTitle = Path.GetFileName(target);
        return $"/u /ignoreeol /wl /e \"{temp}\" \"{target}\" /dl \"{tempTitle}\" /dr \"{targetTitle}\" /cfg Backup/EnableFile=0";
    }
}
