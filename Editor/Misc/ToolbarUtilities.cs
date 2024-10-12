namespace GDK.Editor.Misc
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    [Serializable]
    public enum ToolbarZone
    {
        ToolbarZoneRightAlign,
        ToolbarZoneLeftAlign,
    }

    [InitializeOnLoad]
    public static class ToolbarUtilities
    {
        private static ScriptableObject _toolbar;
        private static string[]         _scenePaths;
        private static string[]         _sceneNames;

        static ToolbarUtilities()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
            };
        }

        private static void Update()
        {
            if (_toolbar == null)
            {
                var editorAssembly = typeof(Editor).Assembly;

                var toolbars = Resources.FindObjectsOfTypeAll(editorAssembly.GetType("UnityEditor.Toolbar"));
                _toolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (_toolbar != null)
                {
                    var root    = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(_toolbar);
                    var mRoot   = rawRoot as VisualElement;
                    RegisterCallback(ToolbarZone.ToolbarZoneRightAlign.ToString(), OnGUI);

                    void RegisterCallback(string root, Action cb)
                    {
                        var toolbarZone = mRoot.Q(root);

                        if (toolbarZone != null)
                        {
                            var parent = new VisualElement()
                            {
                                style =
                                {
                                    flexGrow      = 1,
                                    flexDirection = FlexDirection.Row,
                                },
                            };

                            var container = new IMGUIContainer();
                            container.onGUIHandler += () =>
                            {
                                cb?.Invoke();
                            };
                            parent.Add(container);
                            toolbarZone.Add(parent);
                        }
                    }
                }
            }

            if (_scenePaths == null)
            {
                var scenePaths = new List<string>();
                var sceneNames = new List<string>();

                var folderName   = Application.dataPath + "/Scenes";
                var dirInfo      = new DirectoryInfo(folderName);
                var allFileInfos = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

                foreach (var fileInfo in allFileInfos)
                {
                    var fullPath  = fileInfo.FullName.Replace(@"\", "/");
                    var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");

                    scenePaths.Add(scenePath);
                    sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
                }

                //Add more SceneExtend Folder
                SceneToolBarExtend.Instance.AddMoreSceneExtend(scenePaths, sceneNames);

                _scenePaths = scenePaths.ToArray();
                _sceneNames = sceneNames.ToArray();
            }
        }

        private static void OnGUI()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var sceneName  = SceneManager.GetActiveScene().name;
                var sceneIndex = -1;

                for (var i = 0; i < _sceneNames.Length; ++i)
                {
                    if (sceneName == _sceneNames[i])
                    {
                        sceneIndex = i;

                        break;
                    }
                }

                var newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200.0f));

                if (newSceneIndex != sceneIndex)
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(_scenePaths[newSceneIndex], OpenSceneMode.Single);
            }
        }
    }
}