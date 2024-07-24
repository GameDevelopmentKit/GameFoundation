using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuildScripts.Editor.Addressable;
using GameFoundation.BuildScripts.Runtime;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
#if UNITY_WEBGL
using UnityEditor.WebGL;
#endif
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
            Platform         = PlatformWin32, BuildTarget = BuildTarget.StandaloneWindows,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform         = PlatformWin64, BuildTarget = BuildTarget.StandaloneWindows64,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform         = PlatformOsx, BuildTarget = BuildTarget.StandaloneOSX,
            BuildTargetGroup = BuildTargetGroup.Standalone
        },
        new BuildTargetInfo
        {
            Platform = PlatformAndroid, BuildTarget = BuildTarget.Android, BuildTargetGroup = BuildTargetGroup.Android
        },
        new BuildTargetInfo
            { Platform = PlatformIOS, BuildTarget = BuildTarget.iOS, BuildTargetGroup = BuildTargetGroup.iOS },
        new BuildTargetInfo
            { Platform = PlatformWebGL, BuildTarget = BuildTarget.WebGL, BuildTargetGroup = BuildTargetGroup.WebGL }
    };

    private static string[] SCENES           = FindEnabledEditorScenes();
    private static bool     OptimizeBuildSie = false;


    private static BuildTargetInfo[] GetBuildTargetInfoFromString(string platforms)
    {
        return platforms.Split(';').Select(platformText => Targets.Single(t => t.Platform == platformText))
                        .ToArray();
    }

    private static BuildTargetInfo[] GetBuildTargetInfoFromString(IEnumerable<string> platforms)
    {
        return platforms.Select(platformText => Targets.Single(t => t.Platform == platformText))
                        .ToArray();
    }

    public static void SetScriptingDefineSymbols()
    {
        var args                   = Environment.GetCommandLineArgs();
        var platforms              = string.Join(";", Targets.Select(t => t.Platform));
        var scriptingDefineSymbols = string.Empty;

        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "-platforms":
                    platforms = args[++i];
                    break;
                case "-scriptingDefineSymbols":
                    scriptingDefineSymbols = args[++i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(scriptingDefineSymbols)) return;

        foreach (var buildTargetInfo in GetBuildTargetInfoFromString(platforms))
        {
            SetScriptingDefineSymbolInternal(buildTargetInfo.BuildTargetGroup, scriptingDefineSymbols);
            CompilationPipeline.RequestScriptCompilation();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }
    }

    public static void BuildFromCommandLine()
    {
        AndroidExternalToolsSettings.gradlePath = null;
        // Grab the CSV platforms string
        var platforms             = string.Join(";", Targets.Select(t => t.Platform));
        var scriptingBackend      = ScriptingImplementation.Mono2x;
        var args                  = Environment.GetCommandLineArgs();
        var buildOptions          = BuildOptions.CompressWithLz4HC;
        var outputPath            = "template.exe";
        var buildAppBundle        = false;
        var packageName           = "";
        var keyStoreFileName      = "the1_googleplay.keystore";
        var keyStoreAliasName     = "theonestudio";
        var keyStorePassword      = "tothemoon";
        var keyStoreAliasPassword = "tothemoon";
        var iosTargetOSVersion    = "13.0";
        var iosSigningTeamId      = "";
        var remoteAddressableBuildPath = "";
        var remoteAddressableLoadPath = "";

        PlayerSettings.Android.useCustomKeystore = false;
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
                case "-outputPath":
                    outputPath = args[++i];
                    break;
                case "-buildAppBundle":
                    buildAppBundle = true;
                    break;
                case "-optimizeSize":
                    OptimizeBuildSie = true;
                    break;
                case "-packageName":
                    packageName = args[++i];
                    break;
                case "-keyStoreFileName":
                    keyStoreFileName = args[++i];
                    break;
                case "-keyStorePassword":
                    keyStorePassword = args[++i];
                    break;
                case "-keyStoreAliasName":
                    keyStoreAliasName = args[++i];
                    break;
                case "-keyStoreAliasPassword":
                    keyStoreAliasPassword = args[++i];
                    break;
                case "-iosTargetOSVersion":
                    iosTargetOSVersion = args[++i];
                    break;
                case "-iosSigningTeamId":
                    iosSigningTeamId = args[++i];
                    break;
                case "-remoteAddressableBuildPath":
                    remoteAddressableBuildPath = args[++i];
                    break;
                case "-remoteAddressableLoadPath":
                    remoteAddressableLoadPath = args[++i];
                    break;
            }
        }


#if PRODUCTION
            PlayerSettings.SetStackTraceLogType(LogType.Assert,  StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            PlayerSettings.SetStackTraceLogType(LogType.Log,     StackTraceLogType.None);
#endif
        if (!string.IsNullOrEmpty(remoteAddressableBuildPath) && !string.IsNullOrEmpty(remoteAddressableLoadPath))
        {
            AddressableBuildTool.CreateOrUpdateTheOneCDNProfile(remoteAddressableBuildPath, remoteAddressableLoadPath);
        }
        
        if (buildAppBundle)
        {
            SetUpAndroidKeyStore(keyStoreFileName, keyStorePassword, keyStoreAliasName, keyStoreAliasPassword);
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
        }
        else
        {
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
        }

        SetupIos(iosSigningTeamId, iosTargetOSVersion);

        PlayerSettings.SplashScreen.showUnityLogo = false;
        // Get a list of targets to build
        BuildInternal(scriptingBackend, buildOptions, platforms.Split(";"), outputPath,
            buildAppBundle, packageName);
    }

    private static void SetupIos(string teamId, string targetOSVersion)
    {
        PlayerSettings.iOS.appleDeveloperTeamID  = teamId;
        PlayerSettings.iOS.targetOSVersionString = targetOSVersion;
    }

    public static void SetUpAndroidKeyStore(string keyStoreFileName = "the1_googleplay.keystore",
        string keyStorePass = "tothemoon", string keyaliasName = "theonestudio",
        string keyaliasPass = "tothemoon")
    {
        Console.WriteLine("-----Setup android keystore-----");
        Console.WriteLine($"keystore file name: {keyStoreFileName}");
        Console.WriteLine($"keystore file pass: {keyStorePass}");
        Console.WriteLine($"keystore alias name: {keyaliasName}");
        Console.WriteLine($"keystore alias pass: {keyaliasPass}");

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName      = keyStoreFileName;
        PlayerSettings.Android.keystorePass      = keyStorePass;
        PlayerSettings.Android.keyaliasName      = keyaliasName;
        PlayerSettings.Android.keyaliasPass      = keyaliasPass;
        Console.WriteLine("-----Setup android keystore finished-----");
    }

    public static void BuildInternal(ScriptingImplementation scriptingBackend, BuildOptions options,
        IEnumerable<string> platforms, string outputPath,
        bool buildAppBundle = false, string packageName = "")
    {
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        
        BuildTools.ResetBuildSettings();
        EditorUserBuildSettings.buildAppBundle = buildAppBundle;

        var buildTargetInfos = GetBuildTargetInfoFromString(platforms);
        Console.WriteLine("Building Targets: " +
                          string.Join(", ",
                              buildTargetInfos.Select(target => target.Platform)
                                              .ToArray())); // Log which targets we're gonna build

        var errors = false;
        foreach (var platform in buildTargetInfos)
        {
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");
            Console.WriteLine($"Building: {platform.Platform}");
            Console.WriteLine($"----------{new string('-', platform.Platform.Length)}");

            PlayerSettings.SetScriptingBackend(platform.BuildTargetGroup, scriptingBackend);
            if (!string.IsNullOrEmpty(packageName))
            {
                PlayerSettings.SetApplicationIdentifier(platform.BuildTargetGroup, packageName);
            }

            SpecificActionForEachPlatform(platform);
            SetApplicationVersion();

            EditorUserBuildSettings.SwitchActiveBuildTarget(platform.BuildTargetGroup, platform.BuildTarget);
            AddressableBuildTool.BuildAddressable();

            // Set up the build options
            if (platform.Platform.Equals(PlatformWebGL))
                options &= ~BuildOptions
                    .Development; // can't build development for webgl, it make the build larger and cant gzip
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes           = SCENES,
                locationPathName = Path.GetFullPath($"../Build/Client/{platform.Platform}/{outputPath}"),
                target           = platform.BuildTarget, options = options
            };

            // Perform the build
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            WriteReport(buildResult);
            errors = errors || buildResult.summary.result != BuildResult.Succeeded;
        }

        Console.WriteLine(errors ? "*** Some targets failed ***" : "All targets built successfully!");

        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
    }

    private static void SpecificActionForEachPlatform(BuildTargetInfo platform)
    {
        var il2CppCodeGeneration = OptimizeBuildSie ? Il2CppCodeGeneration.OptimizeSize : Il2CppCodeGeneration.OptimizeSpeed;
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
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled; // Disable compression for FBInstant game
                PlayerSettings.WebGL.decompressionFallback = false; // Disable compression for FBInstant game
                PlayerSettings.runInBackground = false;
                PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
#if UNITY_2022_1_OR_NEWER
                PlayerSettings.WebGL.initialMemorySize = 64;
                UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSize;
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

    /// <summary>
    /// Find all active sences in build setting.
    /// </summary>
    /// <returns></returns>
    private static string[] FindEnabledEditorScenes() =>
        (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();

    private static void WriteReport(BuildReport report)
    {
        Directory.CreateDirectory("../Build/Logs");
        var platform = Targets.SingleOrDefault(t => t.BuildTarget == report.summary.platform)?.Platform ?? "unknown";
        var filePath = $"../Build/Logs/Build-Client-Report.{platform}.log";
        var summary  = report.summary;
        using (var file = new StreamWriter(filePath))
        {
            file.WriteLine($"Build {summary.guid} for {summary.platform}.");
            file.WriteLine(
                $"Build began at {summary.buildStartedAt} and ended at {summary.buildEndedAt}. Total {summary.totalTime}.");
            file.WriteLine($"Build options: {summary.options}");
            file.WriteLine($"Build output to: {summary.outputPath}");
            file.WriteLine(
                $"Build result: {summary.result} ({summary.totalWarnings} warnings, {summary.totalErrors} errors).");
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
        PlayerSettings.bundleVersion             = GameVersion.Version;
#if UNITY_ANDROID
        PlayerSettings.Android.bundleVersionCode = GameVersion.BuildNumber;
#elif UNITY_IOS
        PlayerSettings.iOS.buildNumber           = GameVersion.BuildNumber.ToString();
#endif
    }

    public static void SetScriptingDefineSymbolInternal(BuildTargetGroup buildTargetGroup,
        string scriptingDefineSymbols) =>
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, scriptingDefineSymbols);
}