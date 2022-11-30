using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

public static class MNABuildMenu
{
    [MenuItem("Build/Build MNA Windows 32bit IL2CPP (Slow)")]
    private static void BuildMNA_Win32()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { MNABuild.PlatformWin32 });

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Build MNA Windows 64bit IL2CPP (Slow)")]
    private static void BuildMNA_Win64()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { MNABuild.PlatformWin64 });

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build MNA Windows 32bit Mono")]
    private static void BuildMNA_Win32_Mono()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { MNABuild.PlatformWin32 });

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Build MNA Windows 64bit Mono")]
    private static void BuildMNA_Win64_Mono()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { MNABuild.PlatformWin64 });

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build MNA Mac Mono")]
    private static void BuildMNA_Mac()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { MNABuild.PlatformOsx });

        OpenLog("Build-Client-Report.osx-x64.log");
    }

    [MenuItem("Build/Build MNA All")]
    private static void BuildMNA_All()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { MNABuild.PlatformWin32, MNABuild.PlatformWin64, MNABuild.PlatformOsx });
    }

    [MenuItem("Build/Build Debug MNA Windows 64bit IL2CPP (Slow)", priority = 1100)]
    private static void BuildMNA_DebugWin64()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging, new string[] { MNABuild.PlatformWin64 });

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build Debug MNA (Scripts only) Windows 64bit IL2CPP (Slow)", priority = 1100)]
    static void BuildMNA_DebugScriptsOnlyWin64()
    {
        MNABuild.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.BuildScriptsOnly, new string[] { MNABuild.PlatformWin64 });

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Set scripting define symbols", priority = 100)]
    static void BuildMNA_SetScriptingDefineSymbols()
    {
        MNABuild.SetScriptingDefineSymbolInternal(BuildTargetGroup.Standalone, "GPU_INSTANCER;ODIN_INSPECTOR;ODIN_INSPECTOR_3;PHOTON_UNITY_NETWORKING;PUN_2_0_OR_NEWER;PUN_2_OR_NEWER;PUN_2_19_OR_NEWER;CT_BWF;DDNA_IOS_PUSH_NOTIFICATIONS_REMOVED");
    }

    private static void OpenLog(string fileName)
    {
        if (!InternalEditorUtility.inBatchMode)
        {
            var d = Directory.GetCurrentDirectory();
            var filePath = Path.GetFullPath($"../Build/Logs/{fileName}");
            if (File.Exists(filePath)) Process.Start(filePath);
        }
    }
}
