namespace GDK.Editor.Misc
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Project Settings page for the Scene Toolbar.
    /// Accessible via Edit > Project Settings > GDK > Scene Toolbar.
    /// </summary>
    public static class SceneToolbarSettingsProvider
    {
        static Vector2 scrollPos;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/GDK/Scene Toolbar", SettingsScope.Project)
            {
                label      = "Scene Toolbar",
                guiHandler = DrawSettingsGUI,
                keywords   = new[] { "scene", "toolbar", "selector", "dropdown" }
            };
        }

        static void DrawSettingsGUI(string searchContext)
        {
            var settings = SceneToolbarSettingsManager.GetSettings();

            EditorGUIUtility.labelWidth = 200f;
            EditorGUILayout.Space(8);

            // ── General ───────────────────────────────────────────
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            settings.enabled = EditorGUILayout.Toggle(
                new GUIContent("Enabled", "Show / hide the Scene Selector dropdown in the main toolbar."),
                settings.enabled);

            if (EditorGUI.EndChangeCheck())
            {
                SceneToolbarSettingsManager.SaveSettings();
                ToolbarSceneDropdown.RefreshToolbar();
            }

            EditorGUILayout.Space(12);

            // ── Extra Scene Folders ───────────────────────────────
            EditorGUILayout.LabelField("Extra Scene Folders", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scenes inside Assets/Scenes are always included.\nAdd extra folders below to include more scenes in the dropdown.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            bool changed = false;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(300));

            for (int i = 0; i < settings.sceneFolders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string newValue = EditorGUILayout.TextField(settings.sceneFolders[i]);
                if (newValue != settings.sceneFolders[i])
                {
                    settings.sceneFolders[i] = newValue;
                    changed = true;
                }

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string selected = EditorUtility.OpenFolderPanel("Select Scene Folder", settings.sceneFolders[i], "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        settings.sceneFolders[i] = selected;
                        changed = true;
                    }
                }

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    settings.sceneFolders.RemoveAt(i);
                    changed = true;
                    SceneToolbarSettingsManager.SaveSettings();
                    ToolbarSceneDropdown.InvalidateSceneList();
                    ToolbarSceneDropdown.RefreshToolbar();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            // ── Add Folder Button ─────────────────────────────────
            if (GUILayout.Button("+ Add Folder", GUILayout.Height(24)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Scene Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    settings.sceneFolders.Add(selected);
                    changed = true;
                }
            }

            if (changed)
            {
                SceneToolbarSettingsManager.SaveSettings();
                ToolbarSceneDropdown.InvalidateSceneList();
                ToolbarSceneDropdown.RefreshToolbar();
            }

            EditorGUILayout.Space(16);

            // ── Reset ─────────────────────────────────────────────
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(28)))
            {
                settings.enabled      = true;
                settings.sceneFolders = new List<string>();
                SceneToolbarSettingsManager.SaveSettings();
                ToolbarSceneDropdown.InvalidateSceneList();
                ToolbarSceneDropdown.RefreshToolbar();
            }

            EditorGUILayout.Space(8);

            var style = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic };
            EditorGUILayout.LabelField("Settings stored at Assets/Settings/Editor/SceneToolbarSettings.asset", style);
        }
    }
}
