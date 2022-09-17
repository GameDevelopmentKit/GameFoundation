using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;

// ------------------------------------------------------------------------
// https://docs.unity3d.com/Manual/CommandLineArguments.html
// ------------------------------------------------------------------------
public static class BuildScript
{
    public const string AppName = "PackingJam";

    public const string PlatformOsx     = "osx-x64";
    public const string PlatformWin64   = "win-x64";
    public const string PlatformWin32   = "win-x86";
    public const string PlatformAndroid = "android";

    public class BuildTargetInfo
    {
        public string           Platform; // eg "win-x64"
        public BuildTarget      BuildTarget;
        public BuildTargetGroup BuildTargetGroup;
        public string           OutputPath; // eg "PC/{AppName}.exe"
    }

    private static readonly List<BuildTargetInfo> Targets = new List<BuildTargetInfo>
                                                            {
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform         = PlatformWin32,
                                                                    BuildTarget      = BuildTarget.StandaloneWindows,
                                                                    BuildTargetGroup = BuildTargetGroup.Standalone,
                                                                    OutputPath       = $"{PlatformWin32}/{AppName}.exe"
                                                                },
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform         = PlatformWin64,
                                                                    BuildTarget      = BuildTarget.StandaloneWindows64,
                                                                    BuildTargetGroup = BuildTargetGroup.Standalone,
                                                                    OutputPath       = $"{PlatformWin64}/{AppName}.exe"
                                                                },
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform         = PlatformOsx,
                                                                    BuildTarget      = BuildTarget.StandaloneOSX,
                                                                    BuildTargetGroup = BuildTargetGroup.Standalone,
                                                                    OutputPath       = $"{PlatformOsx}/{AppName}.app"
                                                                },
                                                                new BuildTargetInfo
                                                                {
                                                                    Platform         = PlatformAndroid,
                                                                    BuildTarget      = BuildTarget.Android,
                                                                    BuildTargetGroup = BuildTargetGroup.Android,
                                                                    OutputPath       = $"{PlatformAndroid}/{AppName}.apk"
                                                                }
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
        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "-platforms":
                    platforms = args[++i];
                    break;
                case "-scriptingBackend":
                    switch (args[++i].ToLowerInvariant())
                    {
                        case "il2cpp":
                            scriptingBackend = ScriptingImplementation.IL2CPP;
                            break;
                        case "mono":
                            scriptingBackend = ScriptingImplementation.Mono2x;
                            break;
                        default: throw new Exception("Unknown scripting backend");
                    }

                    break;
                case "-development":
                    buildOptions |= BuildOptions.Development;
                    break;
                case "-scriptingDefineSymbols":
                    scriptingDefineSymbols = args[++i];
                    break;
            }
        }

        //TODO: remove it when fix il2cpp build bug
        Debug.Log($"--------- scripting backend: {scriptingBackend} -------------");
        scriptingBackend = ScriptingImplementation.Mono2x;

        // Get a list of targets to build
        var plaftformTargets = platforms.Split(';');
        BuildInternal(scriptingBackend, buildOptions, plaftformTargets, scriptingDefineSymbols);
    }

    public static void BuildInternal(ScriptingImplementation scriptingBackend, BuildOptions options, IEnumerable<string> platformTargets, string scriptingDefineSymbols = "")
    {
        BuildTools.ResetBuildSettings();
        SetApplicationVersion();

        var platforms = platformTargets.Select(platformText => Targets.Single(t => t.Platform == platformText)).ToArray();

        // Log which targets we're gonna build
        Console.WriteLine("Building Targets: " + string.Join(", ", platforms.Select(target => target.Platform).ToArray()));

        var errors = false;
        BuildAddressable();
        foreach (var platform in platforms)
        {
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");
            Console.WriteLine($"Building: {platform.Platform}");
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");

            PlayerSettings.SetScriptingBackend(platform.BuildTargetGroup, scriptingBackend);

            // If we're not in batch mode, we can do this
            if (!InternalEditorUtility.inBatchMode)
                EditorUserBuildSettings.SwitchActiveBuildTarget(platform.BuildTargetGroup, platform.BuildTarget);

            // Set up the build options
            var buildPlayerOptions = new BuildPlayerOptions
                                     {
                                         scenes = SCENES, locationPathName = Path.GetFullPath("../Build/Client/" + platform.OutputPath), target = platform.BuildTarget, options = options
                                     };
            SetScriptingDefineSymbolInternal(platform.BuildTargetGroup, scriptingDefineSymbols);

            // Perform the build
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            WriteReport(buildResult);
            errors = errors || buildResult.summary.result != BuildResult.Succeeded;
        }

        if (errors)
        {
            Console.WriteLine("*** Some targets failed ***");
        }
        else
        {
            Console.WriteLine("All targets built successfully!");
        }
    }

    /// <summary>
    /// Clean Addressable before build and init FMOD
    /// </summary>
    private static void BuildAddressable()
    {
        AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
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

            foreach (var buildFile in report.files)
            {
                file.WriteLine($"Role: {buildFile.role}, Size: {buildFile.size} bytes, Path: {buildFile.path}");
            }

            file.WriteLine();
        }

        // output structured editor build log for later analysis
        var jsonFilePath = $"../Build/Logs/Build-Client.{platform}.json";
        using (var file = new StreamWriter(jsonFilePath))
        {
            var serializer = new JsonSerializer();

            using (JsonWriter writer = new JsonTextWriter(file))
            {
                serializer.Serialize(writer, report);
            }
        }
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

    private static string Prefix(LogType type)
    {
        switch (type)
        {
            case LogType.Assert:    return "A";
            case LogType.Error:     return "E";
            case LogType.Exception: return "X";
            case LogType.Log:       return "L";
            case LogType.Warning:   return "W";
        }

        return "????";
    }

    /// <summary>
    ///  Sync build version with blockchain server, photon server by version file which was generated by jenkins
    /// </summary>
    private static void SetApplicationVersion()
    {
        // Bundle version will be use for some third party like Backtrace, DeltaDNA,...
        PlayerSettings.bundleVersion = GameVersion.Version;
    }

    public static void SetScriptingDefineSymbolInternal(BuildTargetGroup buildTargetGroup, string scriptingDefineSymbols)
    {
        if (string.IsNullOrEmpty(scriptingDefineSymbols)) return;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, scriptingDefineSymbols);
    }
}