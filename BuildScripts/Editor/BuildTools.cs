using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class BuildTools
{
    [MenuItem("Build/Tools/List Player Assemblies in Console")]
    public static void PrintAssemblyNames()
    {
        Debug.Log("== Player Assemblies ==");
        Assembly[] playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

        foreach (var assembly in playerAssemblies)
        {
            Debug.Log(assembly.name);
        }

        Debug.Log("== End ==");
    }

    [MenuItem("Build/Tools/List Assembly Definition Platform Names in Console")]
    public static void PrintAssemblyDefinitionPlatformNames()
    {
        Debug.Log("== Assembly Definition Platforms ==");
        var platforms = CompilationPipeline.GetAssemblyDefinitionPlatforms();

        foreach (var platform in platforms)
        {
            Debug.Log(
                $"DisplayName: {platform.DisplayName}, Name: {platform.Name}, BuildTarget: {platform.BuildTarget}"
            );
        }

        Debug.Log("== End ==");
    }

    [MenuItem("Build/Tools/Reset build settings")]
    public static void ResetBuildSettings()
    {
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildScriptsOnly = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.waitForManagedDebugger = false;
        EditorUserBuildSettings.waitForPlayerConnection = false;
    }

    [MenuItem("Build/Tools/Get editor user build settings")]
    public static void GetEditorUserBuildSettings()
    {
        var CopyPDBFiles = EditorUserBuildSettings.GetPlatformSettings("Standalone", "CopyPDBFiles");
        var CreateSolution = EditorUserBuildSettings.GetPlatformSettings("Standalone", "CreateSolution");

        var sb = new StringBuilder();
        sb.AppendLine("GetEditorUserBuildSettings");
        sb.AppendLine($"CopyPDBFiles = {CopyPDBFiles}");
        sb.AppendLine($"CreateSolution = {CreateSolution}");

        Debug.Log(sb.ToString());
    }
}