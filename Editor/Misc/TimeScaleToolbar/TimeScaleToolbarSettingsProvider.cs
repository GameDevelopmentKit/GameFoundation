namespace GDK.Editor.Misc
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Project Settings page for the TimeScale Toolbar.
    /// Accessible via Edit > Project Settings > GDK > TimeScale Toolbar.
    /// Draws the inspector for the ScriptableObject settings asset.
    /// </summary>
    public static class TimeScaleToolbarSettingsProvider
    {
        static UnityEditor.Editor cachedEditor;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/GDK/TimeScale Toolbar", SettingsScope.Project)
            {
                label      = "TimeScale Toolbar",
                guiHandler = DrawSettingsGUI,
                keywords   = new[] { "time", "scale", "toolbar", "speed", "timescale" }
            };
        }

        static void DrawSettingsGUI(string searchContext)
        {
            var settings = TimeScaleToolbar.GetSettings();

            if (cachedEditor == null || cachedEditor.target != settings)
                UnityEditor.Editor.CreateCachedEditor(settings, null, ref cachedEditor);

            EditorGUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            cachedEditor.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                TimeScaleToolbar.SaveSettings();
                TimeScaleToolbar.RefreshToolbar();

                if (!settings.enabled)
                    Time.timeScale = 1f;
            }

            EditorGUILayout.Space(16);

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(28)))
            {
                settings.enabled        = true;
                settings.minTimeScale   = 0f;
                settings.maxTimeScale   = 5f;
                settings.defaultScale   = 1f;
                settings.resetOnPlay    = true;
                settings.forcedOverride = false;
                TimeScaleToolbar.LastTimeScale = 1f;
                Time.timeScale = 1f;
                TimeScaleToolbar.SaveSettings();
                TimeScaleToolbar.RefreshToolbar();
            }

            EditorGUILayout.Space(8);

            var style = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic };
            EditorGUILayout.LabelField("Settings stored at Assets/Settings/Editor/TimeScaleToolbarSettings.asset", style);
        }
    }
}
