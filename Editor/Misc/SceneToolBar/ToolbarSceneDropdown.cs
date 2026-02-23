namespace GDK.Editor.Misc
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class ToolbarSceneDropdown
    {
        public const string ToolElementPath = "GDK/Scene Selector";

        static string[] scenePaths;

        [MainToolbarElement(ToolElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateSceneSelectorDropdown()
        {
            string activeSceneName;
            if (Application.isPlaying)
                activeSceneName = SceneManager.GetActiveScene().name;
            else
                activeSceneName = EditorSceneManager.GetActiveScene().name;
            if (activeSceneName.Length == 0)
                activeSceneName = "Untitled";

            var icon     = EditorGUIUtility.IconContent("UnityLogo").image as Texture2D;
            var content  = new MainToolbarContent(activeSceneName, icon, "Select active scene");
            var dropdown = new MainToolbarDropdown(content, ShowDropdownMenu);
            dropdown.displayed = SceneToolbarSettingsManager.GetSettings().enabled;
            return dropdown;
        }

        static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();
            if (scenePaths.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Scenes in Project"));
            }
            foreach (string scenePath in scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                menu.AddItem(new GUIContent(sceneName), false, () =>
                {
                    SwitchScene(scenePath);
                });
            }
            menu.DropDown(dropDownRect);
        }

        static void SwitchScene(string scenePath)
        {
            if (Application.isPlaying)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.Log($"Switching to scene: {sceneName}");
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    Debug.LogError($"Scene '{sceneName}' is not in the Build Settings.");
                }
            }
            else
            {
                if (File.Exists(scenePath))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.Log($"Switching to scene: {scenePath}");
                        EditorSceneManager.OpenScene(scenePath);
                    }
                }
                else
                {
                    Debug.LogError($"Scene at path '{scenePath}' does not exist.");
                }
            }
        }

        static void RefreshSceneList()
        {
            if (scenePaths == null)
            {
                List<string> listScenePaths = new List<string>();

                // Default: Assets/Scenes
                string defaultFolder = Application.dataPath + "/Scenes";
                if (Directory.Exists(defaultFolder))
                {
                    var dirInfo      = new DirectoryInfo(defaultFolder);
                    var allFileInfos = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

                    foreach (var fileInfo in allFileInfos)
                    {
                        var fullPath  = fileInfo.FullName.Replace(@"\", "/");
                        var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");
                        listScenePaths.Add(scenePath);
                    }
                }

                // Extra folders from ScriptableObject settings
                var settings = SceneToolbarSettingsManager.GetSettings();
                foreach (var folderName in settings.sceneFolders)
                {
                    if (!Directory.Exists(folderName)) continue;

                    var dirInfo      = new DirectoryInfo(folderName);
                    var allFileInfos = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

                    foreach (var fileInfo in allFileInfos)
                    {
                        var fullPath  = fileInfo.FullName.Replace(@"\", "/");
                        var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");
                        listScenePaths.Add(scenePath);
                    }
                }

                scenePaths = listScenePaths.ToArray();
            }
        }

        internal static void InvalidateSceneList()
        {
            scenePaths = null;
            RefreshSceneList();
        }

        internal static void RefreshToolbar()
        {
            MainToolbar.Refresh(ToolElementPath);
        }

        static void SceneSwitched(Scene oldScene, Scene newScene)
        {
            MainToolbar.Refresh(ToolElementPath);
        }

        static ToolbarSceneDropdown()
        {
            RefreshSceneList();
            EditorApplication.projectChanged                += () => { scenePaths = null; RefreshSceneList(); };
            SceneManager.activeSceneChanged                 += SceneSwitched;
            EditorSceneManager.activeSceneChangedInEditMode += SceneSwitched;
        }
    }
}
