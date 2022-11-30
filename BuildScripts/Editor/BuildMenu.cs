using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

public static class MNABuildMenu
{
    [MenuItem("Build/Build MNA Windows 32bit IL2CPP (Slow)")]
    private static void BuildMNA_Win32()
    {
        Build.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { Build.PlatformWin32 }, "default.exe");

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Build MNA Windows 64bit IL2CPP (Slow)")]
    private static void BuildMNA_Win64()
    {
        Build.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build MNA Windows 32bit Mono")]
    private static void BuildMNA_Win32_Mono()
    {
        Build.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { Build.PlatformWin32 }, "default.exe");

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Build MNA Windows 64bit Mono")]
    private static void BuildMNA_Win64_Mono()
    {
        Build.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build MNA Mac Mono")]
    private static void BuildMNA_Mac()
    {
        Build.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { Build.PlatformOsx }, "default.app");

        OpenLog("Build-Client-Report.osx-x64.log");
    }

    [MenuItem("Build/Build MNA All")]
    private static void BuildMNA_All()
    {
        Build.BuildMNAInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { Build.PlatformWin32, Build.PlatformWin64, Build.PlatformOsx }, "default.app");
    }

    [MenuItem("Build/Build Debug MNA Windows 64bit IL2CPP (Slow)", priority = 1100)]
    private static void BuildMNA_DebugWin64()
    {
        Build.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging, new string[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Build Debug MNA (Scripts only) Windows 64bit IL2CPP (Slow)", priority = 1100)]
    static void BuildMNA_DebugScriptsOnlyWin64()
    {
        Build.BuildMNAInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.BuildScriptsOnly, new string[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Set scripting define symbols", priority = 100)]
    static void BuildMNA_SetScriptingDefineSymbols()
    {
        Build.SetScriptingDefineSymbolInternal(BuildTargetGroup.Standalone, "GPU_INSTANCER;ODIN_INSPECTOR;ODIN_INSPECTOR_3;PHOTON_UNITY_NETWORKING;PUN_2_0_OR_NEWER;PUN_2_OR_NEWER;PUN_2_19_OR_NEWER;CT_BWF;DDNA_IOS_PUSH_NOTIFICATIONS_REMOVED");
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
