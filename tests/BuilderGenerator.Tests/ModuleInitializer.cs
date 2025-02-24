using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using DiffEngine;

using VerifyTests;

namespace BuilderGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // https://stackoverflow.com/a/60545278/781045
        AssemblyConfigurationAttribute? assemblyConfigurationAttribute = typeof(ModuleInitializer).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        if (assemblyConfigurationAttribute is not null)
            Console.WriteLine($"Build Configuration is {assemblyConfigurationAttribute.Configuration}.");

        VerifySourceGenerators.Initialize();

        SetupDiffTool();
    }

    private static readonly string[] s_binaryExtensions =
            [
                ".bmp",
                ".cut",
                ".dds",
                ".exr",
                ".g3",
                ".gif",
                ".hdr",
                ".ico",
                ".iff",
                ".lbm",
                ".j2k",
                ".j2c",
                ".jng",
                ".jp2",
                ".jpg",
                ".jif",
                ".jpeg",
                ".jpe",
                ".jxr",
                ".wdp",
                ".hdp",
                ".koa",
                ".mng",
                ".pcd",
                ".pcx",
                ".pfm",
                ".pct",
                ".pict",
                ".pic",
                ".png",
                ".pbm",
                ".pgm",
                ".ppm",
                ".psd",
                ".ras",
                ".sgi",
                ".rgb",
                ".rgba",
                ".bw",
                ".tga",
                ".targa",
                ".tif",
                ".tiff",
                ".wap",
                ".wbm",
                ".webp",
                ".xbm",
                ".xpm"
            ];

    private static void SetupDiffTool()
    {
        static string TargetLeftArguments(string temp, string target)
        {
            var tempTitle = Path.GetFileName(temp);
            var targetTitle = Path.GetFileName(target);
            return $"/u /ignoreeol /wl /e \"{target}\" \"{temp}\" /dl \"{targetTitle}\" /dr \"{tempTitle}\"";
        }

        static string TargetRightArguments(string temp, string target)
        {
            var tempTitle = Path.GetFileName(temp);
            var targetTitle = Path.GetFileName(target);
            return $"/u /ignoreeol /wl /e \"{temp}\" \"{target}\" /dl \"{tempTitle}\" /dr \"{targetTitle}\"";
        }

        var launchArguments = new LaunchArguments(
            Left: TargetLeftArguments,
            Right: TargetRightArguments);

        _ = DiffTools.AddTool(
            name: "MyTool",
            autoRefresh: true,
            isMdi: false,
            supportsText: true,
            requiresTarget: true,
            binaryExtensions: s_binaryExtensions,
            osSupport: new OsSupport(
                Windows: new OsSettings(
                    @"D:\Apps\WinMerge\WinMergeU.exe",
                    launchArguments)
        ));
    }
}
