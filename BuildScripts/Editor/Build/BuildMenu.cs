    using System.Diagnostics;
    using System.IO;
    using UnityEditor;
    using UnityEditorInternal;

    public static class BuildMenu
    {
        [MenuItem("Build/Build  Windows 32bit IL2CPP (Slow)")]
        private static void Build_Win32()
        {
            BuildScript.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { BuildScript.PlatformWin32 });

            OpenLog("Build-Client-Report.win-x86.log");
        }

        [MenuItem("Build/Build  Windows 64bit IL2CPP (Slow)")]
        private static void Build_Win64()
        {
            BuildScript.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { BuildScript.PlatformWin64 });

            OpenLog("Build-Client-Report.win-x64.log");
        }

        [MenuItem("Build/Build  Windows 32bit Mono")]
        private static void Build_Win32_Mono()
        {
            BuildScript.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { BuildScript.PlatformWin32 });

            OpenLog("Build-Client-Report.win-x86.log");
        }

        [MenuItem("Build/Build  Windows 64bit Mono")]
        private static void Build_Win64_Mono()
        {
            BuildScript.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { BuildScript.PlatformWin64 });

            OpenLog("Build-Client-Report.win-x64.log");
        }
        
        [MenuItem("Build/Build Android")]
        private static void Build_Android_Mono()
        {
            BuildScript.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { BuildScript.PlatformAndroid });

            OpenLog("Build-Client-Report.android.log");
        }
        
        [MenuItem("Build/Build Android IL2CPP (slow)")]
        private static void Build_Android_IL2CPP()
        {
            BuildScript.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.None, new string[] { BuildScript.PlatformAndroid });

            OpenLog("Build-Client-Report.android.log");
        }

        [MenuItem("Build/Build  Mac Mono")]
        private static void Build_Mac()
        {
            BuildScript.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { BuildScript.PlatformOsx });

            OpenLog("Build-Client-Report.osx-x64.log");
        }

        [MenuItem("Build/Build  All")]
        private static void Build_All()
        {
            BuildScript.BuildInternal(ScriptingImplementation.Mono2x, BuildOptions.None, new string[] { BuildScript.PlatformWin32, BuildScript.PlatformWin64, BuildScript.PlatformOsx });
        }

        [MenuItem("Build/Build Debug  Windows 64bit IL2CPP (Slow)", priority = 1100)]
        private static void Build_DebugWin64()
        {
            BuildScript.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging, new string[] { BuildScript.PlatformWin64 });

            OpenLog("Build-Client-Report.win-x64.log");
        }

        [MenuItem("Build/Build Debug  (Scripts only) Windows 64bit IL2CPP (Slow)", priority = 1100)]
        static void Build_DebugScriptsOnlyWin64()
        {
            BuildScript.BuildInternal(ScriptingImplementation.IL2CPP, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.BuildScriptsOnly, new string[] { BuildScript.PlatformWin64 });

            OpenLog("Build-Client-Report.win-x64.log");
        }
        
        [MenuItem("Build/Set scripting define symbols", priority = 100)]
        static void Build_SetScriptingDefineSymbols()
        {
            BuildScript.SetScriptingDefineSymbolInternal(BuildTargetGroup.Standalone, "GPU_INSTANCER;ODIN_INSPECTOR;ODIN_INSPECTOR_3;PHOTON_UNITY_NETWORKING;PUN_2_0_OR_NEWER;PUN_2_OR_NEWER;PUN_2_19_OR_NEWER;CT_BWF;DDNA_IOS_PUSH_NOTIFICATIONS_REMOVED");
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
