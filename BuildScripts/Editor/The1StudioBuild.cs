#if UNITY_WEBGL
using UnityEditor.WebGL;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Reporting;
using UnityEngine;

// ReSharper disable once CheckNamespace
public static class The1StudioBuild
{
    public const string PlatformOsx     = "osx-x64";
    public const string PlatformWin64   = "win-x64";
    public const string PlatformWin32   = "win-x86";
    public const string PlatformAndroid = "android";
    public const string PlatformIOS     = "ios";
    public const string PlatformWebGL   = "webgl";

    private const string PlatformDelimiter = ";";

    private static readonly Dictionary<string, BuildTargetInfo> Targets = new()
    {
        [The1StudioBuild.PlatformWin32] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformWin32,
            BuildTarget      = BuildTarget.StandaloneWindows,
            BuildTargetGroup = BuildTargetGroup.Standalone,
        },
        [The1StudioBuild.PlatformWin64] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformWin64,
            BuildTarget      = BuildTarget.StandaloneWindows64,
            BuildTargetGroup = BuildTargetGroup.Standalone,
        },
        [The1StudioBuild.PlatformOsx] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformOsx,
            BuildTarget      = BuildTarget.StandaloneOSX,
            BuildTargetGroup = BuildTargetGroup.Standalone,
        },
        [The1StudioBuild.PlatformAndroid] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformAndroid,
            BuildTarget      = BuildTarget.Android,
            BuildTargetGroup = BuildTargetGroup.Android,
        },
        [The1StudioBuild.PlatformIOS] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformIOS,
            BuildTarget      = BuildTarget.iOS,
            BuildTargetGroup = BuildTargetGroup.iOS,
        },
        [The1StudioBuild.PlatformWebGL] = new BuildTargetInfo
        {
            Platform         = The1StudioBuild.PlatformWebGL,
            BuildTarget      = BuildTarget.WebGL,
            BuildTargetGroup = BuildTargetGroup.WebGL,
        },
    };


    private static BuildInfo CurrentBuildInfo { get; } = new();
    private static string[]  Scenes           { get; } = The1StudioBuild.FindEnabledEditorScenes();

    private static BuildTargetInfo[] AllPlatforms => The1StudioBuild.Targets.Select(t => t.Value).ToArray();

    private static BuildTargetInfo[] ToBuildTargetInfos(this IEnumerable<string> platforms)
    {
        return platforms
               .Select(platformText => The1StudioBuild.Targets[platformText.ToLowerInvariant()])
               .ToArray();
    }

    private static BuildTargetInfo[] ToBuildTargetInfos(this string platforms)
    {
        return platforms
               .Split(The1StudioBuild.PlatformDelimiter)
               .ToBuildTargetInfos();
    }

    public static void BuildFromCommandLine()
    {
        var args = Environment.GetCommandLineArgs();

        PlayerSettings.Android.useCustomKeystore = false;
        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "-platforms":
                    The1StudioBuild.CurrentBuildInfo.BuildTargetInfos = args[++i].ToBuildTargetInfos();
                    break;
                case "-scriptingBackend":
                    The1StudioBuild.CurrentBuildInfo.ScriptingBackend = args[++i].ToLowerInvariant() switch
                    {
                        "il2cpp" => ScriptingImplementation.IL2CPP,
                        "mono"   => ScriptingImplementation.Mono2x,
                        _        => throw new Exception("Unknown scripting backend"),
                    };
                    break;
                case "-development":
                    The1StudioBuild.CurrentBuildInfo.Options |= BuildOptions.Development;
                    break;
                case "-outputPath":
                    The1StudioBuild.CurrentBuildInfo.OutputPath = args[++i];
                    break;
                case "-buildAppBundle":
                    The1StudioBuild.CurrentBuildInfo.BuildAppBundle = true;
                    break;
                case "-optimizeSize":
                    The1StudioBuild.CurrentBuildInfo.OptimizeBuildSize = true;
                    break;
                case "-packageName":
                    The1StudioBuild.CurrentBuildInfo.PackageName = args[++i];
                    break;
                case "-keyStoreFileName":
                    The1StudioBuild.CurrentBuildInfo.KeyStoreFileName = args[++i];
                    break;
                case "-keyStorePassword":
                    The1StudioBuild.CurrentBuildInfo.KeyStorePassword = args[++i];
                    break;
                case "-keyStoreAliasName":
                    The1StudioBuild.CurrentBuildInfo.KeyAliasName = args[++i];
                    break;
                case "-keyStoreAliasPassword":
                    The1StudioBuild.CurrentBuildInfo.KeyAliasPass = args[++i];
                    break;
                case "-iosTargetOSVersion":
                    The1StudioBuild.CurrentBuildInfo.IosTargetOSVersion = args[++i];
                    break;
                case "-iosSigningTeamId":
                    The1StudioBuild.CurrentBuildInfo.IosSigningTeamId = args[++i];
                    break;
            }
        }


#if PRODUCTION
        PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
#endif

        The1StudioBuild.SetupAndroid(The1StudioBuild.CurrentBuildInfo);
        The1StudioBuild.SetupIOS(The1StudioBuild.CurrentBuildInfo);

        PlayerSettings.SplashScreen.showUnityLogo = false;
        // Get a list of targets to build
        The1StudioBuild.BuildInternal(The1StudioBuild.CurrentBuildInfo);
    }

    private static void SetupAndroid(BuildInfo buildInfo)
    {
        EditorUserBuildSettings.buildAppBundle = buildInfo.BuildAppBundle;

        if (buildInfo.BuildAppBundle)
        {
            The1StudioBuild.WriteLog(LogLevel.Info, "----- Setup android keystore -----");
            The1StudioBuild.WriteLog(LogLevel.Info, $"keystore file name: {buildInfo.KeyStoreFileName}");
            The1StudioBuild.WriteLog(LogLevel.Info, $"keystore file pass: {buildInfo.KeyStorePassword}");
            The1StudioBuild.WriteLog(LogLevel.Info, $"keystore alias name: {buildInfo.KeyAliasName}");
            The1StudioBuild.WriteLog(LogLevel.Info, $"keystore alias pass: {buildInfo.KeyAliasPass}");

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName      = buildInfo.KeyStoreFileName;
            PlayerSettings.Android.keystorePass      = buildInfo.KeyStorePassword;
            PlayerSettings.Android.keyaliasName      = buildInfo.KeyAliasName;
            PlayerSettings.Android.keyaliasPass      = buildInfo.KeyAliasPass;
            The1StudioBuild.WriteLog(LogLevel.Info, "----- Setup android keystore finished -----");

            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
        }
        else
        {
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
        }
    }

    private static void SetupIOS(BuildInfo buildInfo)
    {
        PlayerSettings.iOS.appleDeveloperTeamID  = buildInfo.IosSigningTeamId;
        PlayerSettings.iOS.targetOSVersionString = buildInfo.IosTargetOSVersion;
    }

    public static void BuildInternal(BuildInfo buildInfo)
    {
        BuildTools.ResetBuildSettings();

        The1StudioBuild.WriteLog(LogLevel.Info, "Building Targets: " + string.Join(", ", buildInfo.BuildTargetInfos.Select(e => e.Platform)));

        var errors = false;
        foreach (var platform in buildInfo.BuildTargetInfos)
        {
            The1StudioBuild.WriteLog(LogLevel.Info, "----------------------------------------");
            The1StudioBuild.WriteLog(LogLevel.Info, $"Building Platform: {platform.Platform}");
            The1StudioBuild.WriteLog(LogLevel.Info, "----------------------------------------");

            // General settings
            PlayerSettings.SetScriptingBackend(platform.BuildTargetGroup, buildInfo.ScriptingBackend);

            if (!string.IsNullOrEmpty(buildInfo.PackageName))
            {
                PlayerSettings.SetApplicationIdentifier(platform.BuildTargetGroup, buildInfo.PackageName);
            }

            The1StudioBuild.SpecificActionForEachPlatform(platform);
            The1StudioBuild.SetApplicationVersion(The1StudioBuild.CurrentBuildInfo);

            EditorUserBuildSettings.SwitchActiveBuildTarget(platform.BuildTargetGroup, platform.BuildTarget);
            The1StudioBuild.BuildAddressable();

            // Set up the build options
            if (platform.Platform.Equals(Build.PlatformWebGL))
            {
                // can't build development for webgl, it make the build larger and cant gzip
                buildInfo.Options &= ~BuildOptions
                    .Development;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes           = The1StudioBuild.Scenes,
                locationPathName = Path.GetFullPath($"../Build/Client/{platform.Platform}/{buildInfo.OutputPath}"),
                target           = platform.BuildTarget,
                options          = buildInfo.Options,
            };

            // Perform the build
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            The1StudioBuild.WriteReport(buildResult);
            errors = errors || buildResult.summary.result != BuildResult.Succeeded;
        }

        The1StudioBuild.WriteLog(LogLevel.Info, errors ? "*** Some targets failed ***" : "All targets built successfully!");

        The1StudioBuild.WriteLog(LogLevel.Info, new string('=', 80));
    }

    private static void SpecificActionForEachPlatform(BuildTargetInfo platform)
    {
        var il2CppCodeGeneration = The1StudioBuild.CurrentBuildInfo.OptimizeBuildSize ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
#if !UNITY_2022_1_OR_NEWER
        EditorUserBuildSettings.il2CppCodeGeneration = il2CppCodeGeneration;
#endif
        switch (platform.BuildTarget)
        {
            case BuildTarget.iOS:
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                break;
            case BuildTarget.Android:
                //Change build architecture to ARMv7 and ARM6
#if !UNITY_2022_1_OR_NEWER
                PlayerSettings.Android.minifyWithR8 = true;
#endif
                PlayerSettings.Android.minifyRelease = true;
                PlayerSettings.Android.minifyDebug   = true;
                PlayerSettings.SetManagedStrippingLevel(platform.BuildTargetGroup, ManagedStrippingLevel.High);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
#if UNITY_2022_1_OR_NEWER
                PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, il2CppCodeGeneration);
#endif
                break;
#if UNITY_WEBGL
            case BuildTarget.WebGL:
                PlayerSettings.SetManagedStrippingLevel(platform.BuildTargetGroup, ManagedStrippingLevel.High);
                PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Disabled; // Disable compression for FBInstant game
                PlayerSettings.WebGL.decompressionFallback = false; // Disable compression for FBInstant game
                PlayerSettings.runInBackground             = false;
                PlayerSettings.WebGL.powerPreference       = WebGLPowerPreference.Default;
                PlayerSettings.WebGL.dataCaching           = true;
                PlayerSettings.WebGL.exceptionSupport      = WebGLExceptionSupport.None;
#if UNITY_2022_1_OR_NEWER
                PlayerSettings.WebGL.initialMemorySize = 64;
                UserBuildSettings.codeOptimization     = WasmCodeOptimization.DiskSize;
                PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
                PlayerSettings.WebGL.showDiagnostics = false;
#if FB_INSTANT
                PlayerSettings.WebGL.showDiagnostics = false;
#else
                PlayerSettings.WebGL.showDiagnostics = true;
#endif // FB_INSTANT

#endif // UNITY_2022_1_OR_NEWER
                break;
#endif //UNITY_WEBGL
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void BuildAddressable()
    {
        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
        The1StudioBuild.WriteLog(LogLevel.Info, "Clean addressable");
        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
        AddressableAssetSettings.CleanPlayerContent();
        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
        The1StudioBuild.WriteLog(LogLevel.Info, "Build addressable");
        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
        AddressableAssetSettings.BuildPlayerContent(out var result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            var errorMessage = "Addressable build error encountered: " + result.Error;
            Debug.LogError(errorMessage);
            throw new Exception(errorMessage);
        }

        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
        The1StudioBuild.WriteLog(LogLevel.Info, "Finish building addressable");
        The1StudioBuild.WriteLog(LogLevel.Info, "--------------------");
    }

    private static string[] FindEnabledEditorScenes()
    {
        return EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
    }

    private static void WriteReport(BuildReport report)
    {
        Directory.CreateDirectory("../Build/Logs");
        var platform = The1StudioBuild.Targets.SingleOrDefault(t => t.Value.BuildTarget == report.summary.platform).Value.Platform ?? "unknown";
        var summary  = report.summary;

        using var file = new StreamWriter($"../Build/Logs/Build-Client-Report.{platform}.log");

        file.WriteLine($"Build {summary.guid} for {summary.platform}.");
        file.WriteLine($"Build began at {summary.buildStartedAt} and ended at {summary.buildEndedAt}. Total {summary.totalTime}.");
        file.WriteLine($"Build options: {summary.options}");
        file.WriteLine($"Build output to: {summary.outputPath}");
        file.WriteLine($"Build result: {summary.result} ({summary.totalWarnings} warnings, {summary.totalErrors} errors).");
        file.WriteLine($"Build size: {summary.totalSize}");

        file.WriteLine();

        foreach (var step in report.steps)
        {
            The1StudioBuild.WriteStep(file, step);
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
            file.WriteLine($"{The1StudioBuild.Prefix(message.type)}: {message.content}");
        }

        file.WriteLine();
    }

    private static string Prefix(LogType type)
    {
        return type switch
        {
            LogType.Assert    => "[A]",
            LogType.Error     => "[E]",
            LogType.Exception => "[X]",
            LogType.Log       => "[L]",
            LogType.Warning   => "[W]",
            _                 => "[?]",
        };
    }

    private static void SetApplicationVersion(BuildInfo buildInfo)
    {
        PlayerSettings.bundleVersion             = buildInfo.Version;
        PlayerSettings.Android.bundleVersionCode = buildInfo.VersionCode;
    }

    private static void WriteLog(LogLevel logLevel, string content)
    {
        var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{currentTime}] [{logLevel}] {nameof(The1StudioBuild)}: {content}");
    }

    public class BuildInfo
    {
        public BuildTargetInfo[]       BuildTargetInfos { get; set; } = The1StudioBuild.AllPlatforms;
        public ScriptingImplementation ScriptingBackend { get; set; } = ScriptingImplementation.Mono2x;
        public BuildOptions            Options          { get; set; } = BuildOptions.None;

        // General
        public string OutputPath        { get; set; } = "template.exe";
        public string PackageName       { get; set; } = "";
        public bool   OptimizeBuildSize { get; set; }
        public string Version           { get; set; }
        public int    VersionCode       { get; set; }

        // Android
        public bool   BuildAppBundle   { get; set; }
        public string KeyStoreFileName { get; set; } = "the1_googleplay.keystore";
        public string KeyStorePassword { get; set; } = "tothemoon";
        public string KeyAliasName     { get; set; } = "theonestudio";
        public string KeyAliasPass     { get; set; } = "tothemoon";

        // iOS
        public string IosTargetOSVersion { get; set; } = "12.0";
        public string IosSigningTeamId   { get; set; } = "";
    }

    public class BuildTargetInfo
    {
        public BuildTarget      BuildTarget;
        public BuildTargetGroup BuildTargetGroup;
        public string           Platform;
    }
}