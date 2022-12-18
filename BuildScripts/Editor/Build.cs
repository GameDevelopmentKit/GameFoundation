using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFoundation.BuildScripts.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;

// ------------------------------------------------------------------------
// https://docs.unity3d.com/Manual/CommandLineArguments.html
// ------------------------------------------------------------------------
public static class Build
{
    public const string PlatformOsx     = "osx-x64";
    public const string PlatformWin64   = "win-x64";
    public const string PlatformWin32   = "win-x86";
    public const string PlatformAndroid = "android";
    public const string PlatformIOS     = "ios";
    public const string PlatformWebGL   = "webgl";

    private class BuildTargetInfo
    {
        public string           Platform; // eg "win-x64"
        public BuildTarget      BuildTarget;
        public BuildTargetGroup BuildTargetGroup;
    }

    private static readonly List<BuildTargetInfo> Targets = new()
                                                            {
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform = PlatformWin32, BuildTarget = BuildTarget.StandaloneWindows, BuildTargetGroup = BuildTargetGroup.Standalone
                                                                },
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform = PlatformWin64, BuildTarget = BuildTarget.StandaloneWindows64, BuildTargetGroup = BuildTargetGroup.Standalone
                                                                },
                                                                new BuildTargetInfo { Platform = PlatformOsx, BuildTarget = BuildTarget.StandaloneOSX, BuildTargetGroup = BuildTargetGroup.Standalone },
                                                                new BuildTargetInfo { Platform = PlatformAndroid, BuildTarget = BuildTarget.Android, BuildTargetGroup = BuildTargetGroup.Android },
                                                                new BuildTargetInfo { Platform = PlatformIOS, BuildTarget = BuildTarget.iOS, BuildTargetGroup = BuildTargetGroup.iOS },
                                                                new BuildTargetInfo { Platform = PlatformWebGL, BuildTarget = BuildTarget.WebGL, BuildTargetGroup = BuildTargetGroup.WebGL }
                                                            };

    static string[] SCENES = FindEnabledEditorScenes();

    public static void BuildFromCommandLine()
    {
        // Grab the CSV platforms string
        var platforms              = string.Join(";", Targets.Select(t => t.Platform));
        var scriptingBackend       = ScriptingImplementation.Mono2x;
        var args                   = Environment.GetCommandLineArgs();
        var buildOptions           = BuildOptions.None;
        var scriptingDefineSymbols = string.Empty;
        var outputPath             = "template.exe";
        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "-platforms":
                    platforms = args[++i];
                    break;
                case "-scriptingBackend":
                    scriptingBackend = args[++i].ToLowerInvariant() switch
                                       {
                                           "il2cpp" => ScriptingImplementation.IL2CPP,
                                           "mono"   => ScriptingImplementation.Mono2x,
                                           _        => throw new Exception("Unknown scripting backend")
                                       };

                    break;
                case "-development":
                    buildOptions |= BuildOptions.Development;
                    break;
                case "-scriptingDefineSymbols":
                    scriptingDefineSymbols = args[++i];
                    break;
                case "-outputPath":
                    outputPath = args[++i];
                    break;
            }
        }

        // Get a list of targets to build
        var platformTargets = platforms.Split(';');
        BuildInternal(scriptingBackend, buildOptions, platformTargets, outputPath, scriptingDefineSymbols);
    }

    public static void BuildInternal(ScriptingImplementation scriptingBackend, BuildOptions options, IEnumerable<string> platformTargets, string outputPath, string scriptingDefineSymbols = "")
    {
        BuildTools.ResetBuildSettings();
        var platforms = platformTargets.Select(platformText => Targets.Single(t => t.Platform == platformText)).ToArray();
        Console.WriteLine("Building Targets: " + string.Join(", ", platforms.Select(target => target.Platform).ToArray())); // Log which targets we're gonna build

        var errors = false;
        foreach (var platform in platforms)
        {
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");
            Console.WriteLine($"Building: {platform.Platform}");
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");

            PlayerSettings.SetScriptingBackend(platform.BuildTargetGroup, scriptingBackend);
            SetApplicationVersion();
            BuildAddressable(platform);

            // If we're not in batch mode, we can do this
            if (!InternalEditorUtility.inBatchMode)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(platform.BuildTargetGroup, platform.BuildTarget);
            }

            // Set up the build options
            if (platform.Platform.Equals(PlatformWebGL)) options &= ~BuildOptions.Development; // can't build development for webgl, it make the build larger and cant gzip
            var buildPlayerOptions = new BuildPlayerOptions { scenes = SCENES, locationPathName = Path.GetFullPath($"../Build/Client/{platform.Platform}/{outputPath}"), target = platform.BuildTarget, options = options };

            if (!string.IsNullOrEmpty(scriptingDefineSymbols))
                SetScriptingDefineSymbolInternal(platform.BuildTargetGroup, scriptingDefineSymbols);

            // Perform the build
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            WriteReport(buildResult);
            errors = errors || buildResult.summary.result != BuildResult.Succeeded;
        }

        Console.WriteLine(errors ? "*** Some targets failed ***" : "All targets built successfully!");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
    }

    /// <summary>
    /// Clean Addressable before build and init FMOD
    /// </summary>
    /// <param name="buildTargetInfo"></param>
    private static void BuildAddressable(BuildTargetInfo buildTargetInfo)
    {
        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();
    }

    /// <summary>
    /// Find all active sences in build setting.
    /// </summary>
    /// <returns></returns>
    private static string[] FindEnabledEditorScenes() => (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();

    private static void WriteReport(BuildReport report)
    {
        Directory.CreateDirectory("../Build/Logs");
        var platform = Targets.SingleOrDefault(t => t.BuildTarget == report.summary.platform)?.Platform ?? "unknown";
        var filePath = $"../Build/Logs/Build-Client-Report.{platform}.log";
        var summary  = report.summary;
        using (var file = new StreamWriter(filePath))
        {
            file.WriteLine($"Build {summary.guid} for {summary.platform}.");
            file.WriteLine($"Build began at {summary.buildStartedAt} and ended at {summary.buildEndedAt}. Total {summary.totalTime}.");
            file.WriteLine($"Build options: {summary.options}");
            file.WriteLine($"Build output to: {summary.outputPath}");
            file.WriteLine($"Build result: {summary.result} ({summary.totalWarnings} warnings, {summary.totalErrors} errors).");
            file.WriteLine($"Build size: {summary.totalSize}");

            file.WriteLine();

            foreach (var step in report.steps)
            {
                WriteStep(file, step);
            }

            file.WriteLine();

#if UNITY_2022_1_OR_NEWER
            foreach (var buildFile in report.GetFiles())
#else
            foreach (var buildFile in report.files)
#endif
            {
                file.WriteLine($"Role: {buildFile.role}, Size: {buildFile.size} bytes, Path: {buildFile.path}");
            }

            file.WriteLine();
        }

        // output structured editor build log for later analysis
        // var jsonFilePath = $"../Build/Logs/Build-Client.{platform}.json";
        // using (var file = new StreamWriter(jsonFilePath))
        // {
        //     var serializer = new JsonSerializer();

        //     using (JsonWriter writer = new JsonTextWriter(file))
        //     {
        //         serializer.Serialize(writer, report);
        //     }
        // }
    }

    private static void WriteStep(StreamWriter file, BuildStep step)
    {
        file.WriteLine($"Step {step.name}  Depth: {step.depth} Time: {step.duration}");
        foreach (var message in step.messages)
        {
            file.WriteLine($"{Prefix(message.type)}: {message.content}");
        }

        file.WriteLine();
    }

    private static string Prefix(LogType type) =>
        type switch
        {
            LogType.Assert    => "A",
            LogType.Error     => "E",
            LogType.Exception => "X",
            LogType.Log       => "L",
            LogType.Warning   => "W",
            _                 => "????"
        };

    /// <summary>
    ///  Sync build version with blockchain server, photon server by version file which was generated by jenkins
    /// </summary>
    private static void SetApplicationVersion()
    {
        // Bundle version will be use for some third party like Backtrace, DeltaDNA,...
        PlayerSettings.bundleVersion = GameVersion.Version;
    }

    public static void SetScriptingDefineSymbolInternal(BuildTargetGroup buildTargetGroup, string scriptingDefineSymbols) =>
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, scriptingDefineSymbols);
}