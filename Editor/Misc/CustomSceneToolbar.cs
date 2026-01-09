using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CustomSceneToolbar
{
    private static string[] scenePaths;
    private static string[] sceneNames;

    [MainToolbarElement(
        "Custom/Open Project Settings",
        defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ProjectSettingsButton()
    {
        var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;

        var content = new MainToolbarContent(icon)
        {
            tooltip = "Open Project Settings"
        };

        return new MainToolbarButton(
            content,
            () => SettingsService.OpenProjectSettings());
    }

    [MainToolbarElement(
        "Custom/Reload Domain",
        defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ReloadDomainButton()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                       "Packages/com.gdk.core/Editor/Icon/reload.png")
                   ?? EditorGUIUtility.IconContent("d_Refresh").image as Texture2D;

        var content = new MainToolbarContent(icon)
        {
            tooltip = "Force Domain Reload (Reset all static variables)"
        };

        return new MainToolbarButton(
            content,
            EditorUtility.RequestScriptReload);
    }

    // [MainToolbarElement(
    //     "Custom/Scene Selector",
    //     defaultDockPosition = MainToolbarDockPosition.Left)]

    //ToDo Re-enable Scene Selector after Unity fixing the issues with ToolbarExtender
    public static MainToolbarElement SceneSelector()
    {
        EnsureScenesLoaded();

        var content = new MainToolbarContent()
        {
            tooltip = "Quick Scene Switcher",
            text    = "Click To Switch Scene"
        };

        return new MainToolbarButton(content, ShowSceneMenu);
    }

    private static void ShowSceneMenu()
    {
        EnsureScenesLoaded();

        var menu         = new GenericMenu();
        var currentScene = SceneManager.GetActiveScene().path;

        for (var i = 0; i < sceneNames.Length; i++)
        {
            var path = scenePaths[i];

            menu.AddItem(
                new GUIContent(sceneNames[i]),
                path == currentScene,
                () => OpenScene(path));
        }

        menu.ShowAsContext();
    }

    private static void OpenScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

        if (sceneAsset != null)
            EditorGUIUtility.PingObject(sceneAsset);
    }

    private static void EnsureScenesLoaded()
    {
        if (scenePaths != null)
            return;

        var paths = new List<string>();
        var names = new List<string>();

        string folderName = Application.dataPath + "/Scenes";

        if (!Directory.Exists(folderName))
            return;

        var dirInfo = new DirectoryInfo(folderName);
        var files   = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var fullPath  = file.FullName.Replace("\\", "/");
            var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");

            paths.Add(scenePath);
            names.Add(Path.GetFileNameWithoutExtension(scenePath));
        }

        SceneToolBarExtend.Instance.AddMoreSceneExtend(paths, names);

        scenePaths = paths.ToArray();
        sceneNames = names.ToArray();
    }
}