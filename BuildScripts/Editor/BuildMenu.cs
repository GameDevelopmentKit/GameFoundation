using System.Diagnostics;
using System.IO;
using BuildScripts.Editor.Addressable;
using UnityEditor;
using UnityEditorInternal;

public static class BuildMenu
{
    #region Standalone

    [MenuItem("Build/Standalone/build Windows 32bit IL2CPP (Slow)")]
    private static void Build_Win32()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformWin32 }, "default.exe");

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Standalone/build Windows 64bit IL2CPP (Slow)")]
    private static void Build_Win64()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Standalone/build Windows 32bit Mono")]
    private static void Build_Win32_Mono()
    {
        Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformWin32 }, "default.exe");

        OpenLog("Build-Client-Report.win-x86.log");
    }

    [MenuItem("Build/Standalone/build Windows 64bit Mono")]
    private static void Build_Win64_Mono()
    {
        Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Standalone/build Mac Mono")]
    private static void Build_Mac()
    {
        Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformOsx }, "default.app");

        OpenLog("Build-Client-Report.osx-x64.log");
    }

    [MenuItem("Build/Standalone/build All")]
    private static void Build_All() { Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformWin32, Build.PlatformWin64, Build.PlatformOsx }, "default.app"); }

    [MenuItem("Build/Standalone/build Debug MNA Windows 64bit IL2CPP (Slow)", priority = 1100)]
    private static void Build_DebugWin64()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging, new[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    [MenuItem("Build/Standalone/build Debug MNA (Scripts only) Windows 64bit IL2CPP (Slow)", priority = 1100)]
    static void Build_DebugScriptsOnlyWin64()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.BuildScriptsOnly, new[] { Build.PlatformWin64 }, "default.exe");

        OpenLog("Build-Client-Report.win-x64.log");
    }

    #endregion

    #region android
    
    [MenuItem("Build/Android/build android AAB (Slow)", priority = 1100)]
    private static void Build_Android_AAB()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformAndroid }, "default.aab", true);

        OpenLog("Build-Client-Report.android.log");
    }

    [MenuItem("Build/Android/build android IL2CPP (Slow)", priority = 1100)]
    private static void Build_Android_IL2CPP()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformAndroid }, "default.apk");

        OpenLog("Build-Client-Report.android.log");
    }

    [MenuItem("Build/Android/build android Mono", priority = 1100)]
    private static void Build_Android_Mono()
    {
        Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformAndroid }, "default.apk");

        OpenLog("Build-Client-Report.android.log");
    }

 
    [MenuItem("Build/Android/Setup keystore", priority = 1100)]
    private static void Build_Setup_KeyStore()
    {
        Build.SetUpAndroidKeyStore();
    }
    

    #endregion

    #region ios

    // private static void Build_IOS_IL2CPP()
    // {
    //     Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformIOS }, "default");
    //
    //     OpenLog("Build-Client-Report.android.log");
    // }
    //
    // [MenuItem("Build/Standalone/build android Mono")]
    // private static void Build_IOS_Mono()
    // {
    //     Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformIOS }, "default");
    //
    //     OpenLog("Build-Client-Report.android.log");
    // }
    //
    // [MenuItem("Build/Standalone/build android IL2CPP (Slow)")]
    // private static void Build_IOS_ABB()
    // {
    //     Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformIOS }, "default");
    //
    //     OpenLog("Build-Client-Report.android.log");
    // }

    #endregion
    
    #region webgl

    [MenuItem("Build/WebGL/build WebGL IL2CPP (Slow)", priority = 1100)]
    private static void Build_WebGL_IL2CPP()
    {
        Build.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new[] { Build.PlatformWebGL }, "default");

        OpenLog("Build-Client-Report.webgl.log");
    }

    [MenuItem("Build/WebGL/build WebGL Mono", priority = 1100)]
    private static void Build_WebGL_Mono()
    {
        Build.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new[] { Build.PlatformWebGL }, "default");

        OpenLog("Build-Client-Report.webgl.log");
    }

    #endregion
    
    [MenuItem("Build/Addressable/build_fresh", priority = 1100)]
    private static void Build_Addressable_fresh()
    {
        AddressableBuildTool.BuildAddressable();
    }



    [MenuItem("Build/Set scripting define symbols", priority = 100)]
    static void Build_SetScriptingDefineSymbols()
    {
        Build.SetScriptingDefineSymbolInternal(BuildTargetGroup.Android,
                                               "TextMeshPro;ODIN_INSPECTOR;ODIN_INSPECTOR_3;EASY_MOBILE;EASY_MOBILE_PRO;EM_ADMOB;EM_URP;ADDRESSABLES_ENABLED");
    }

    private static void OpenLog(string fileName)
    {
        if (!InternalEditorUtility.inBatchMode)
        {
            var d        = Directory.GetCurrentDirectory();
            var filePath = Path.GetFullPath($"../Build/Logs/{fileName}");
            if (File.Exists(filePath)) Process.Start(filePath);
        }
    }
}