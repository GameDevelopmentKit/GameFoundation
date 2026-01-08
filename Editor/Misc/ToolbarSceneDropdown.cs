namespace Editor.Misc
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
        private const string ToolElementPath = "GDK/Scene Selector";

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

            var icon    = EditorGUIUtility.IconContent("UnityLogo").image as Texture2D;
            var content = new MainToolbarContent(activeSceneName, icon, "Select active scene");
            return new MainToolbarDropdown(content, ShowDropdownMenu);
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

                string folderName   = Application.dataPath + "/Scenes";
                var    dirInfo      = new DirectoryInfo(folderName);
                var    allFileInfos = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

                foreach (var fileInfo in allFileInfos)
                {
                    var fullPath  = fileInfo.FullName.Replace(@"\", "/");
                    var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");

                    listScenePaths.Add(scenePath);
                }

                //Add more SceneExtend Folder
                SceneToolBarExtend.Instance.AddMoreSceneExtend(listScenePaths);

                scenePaths  = listScenePaths.ToArray();
            }
            
        }

        static void SceneSwitched(Scene oldScene, Scene newScene)
        {
            MainToolbar.Refresh(ToolElementPath);
        }

        static ToolbarSceneDropdown()
        {
            RefreshSceneList();
            EditorApplication.projectChanged                += RefreshSceneList;
            SceneManager.activeSceneChanged                 += SceneSwitched;
            EditorSceneManager.activeSceneChangedInEditMode += SceneSwitched;
        }
    }
}
