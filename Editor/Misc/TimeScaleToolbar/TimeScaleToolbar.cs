namespace GDK.Editor.Misc
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;

    /// <summary>
    /// Adds a TimeScale slider to the Unity Editor main toolbar.
    /// Settings are stored as a ScriptableObject at Assets/Settings/Editor/TimeScaleToolbarSettings.asset.
    /// </summary>
    public static class TimeScaleToolbar
    {
        public const  string ToolElementPath  = "GDK/TimeScale";
        const         string SettingsAssetPath = "Assets/Settings/Editor/TimeScaleToolbarSettings.asset";

        // ── Per-user runtime state (EditorPrefs) ──────────────────────
        const string Key_LastTimeScale = "GDK_TimeScaleToolbar_LastTimeScale";

        internal static float LastTimeScale
        {
            get => EditorPrefs.GetFloat(Key_LastTimeScale, GetSettings().defaultScale);
            set => EditorPrefs.SetFloat(Key_LastTimeScale, value);
        }

        // ── Settings Asset ────────────────────────────────────────────
        static TimeScaleToolbarSettings cachedSettings;

        internal static TimeScaleToolbarSettings GetSettings()
        {
            if (cachedSettings != null) return cachedSettings;

            cachedSettings = AssetDatabase.LoadAssetAtPath<TimeScaleToolbarSettings>(SettingsAssetPath);

            if (cachedSettings == null)
            {
                cachedSettings = ScriptableObject.CreateInstance<TimeScaleToolbarSettings>();
                var dir = Path.GetDirectoryName(SettingsAssetPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(cachedSettings, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[TimeScaleToolbar] Created settings at {SettingsAssetPath}");
            }

            return cachedSettings;
        }

        internal static void SaveSettings()
        {
            EditorUtility.SetDirty(cachedSettings);
            AssetDatabase.SaveAssets();
        }

        // ── Toolbar Elements (grouped as one) ─────────────────────────
        [MainToolbarElement(ToolElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static System.Collections.Generic.IEnumerable<MainToolbarElement> CreateTimeScaleElements()
        {
            var s = GetSettings();

            // Slider
            var sliderIcon    = EditorGUIUtility.IconContent("SpeedScale").image as Texture2D;
            var sliderContent = new MainToolbarContent("TimeScale", sliderIcon, "Adjust Time.timeScale (speed up / slow down)");
            float initial     = s.enabled ? LastTimeScale : s.defaultScale;
            var slider        = new MainToolbarSlider(sliderContent, initial, s.minTimeScale, s.maxTimeScale, OnSliderValueChanged);
            slider.displayed  = s.enabled;
            yield return slider;

            // Reset button
            var resetIcon    = EditorGUIUtility.IconContent("d_RotateTool").image as Texture2D;
            var resetContent = new MainToolbarContent("", resetIcon, "Reset TimeScale to default");
            var resetButton  = new MainToolbarButton(resetContent, OnResetClicked);
            resetButton.displayed = s.enabled;
            yield return resetButton;
        }

        static void OnSliderValueChanged(float newValue)
        {
            LastTimeScale  = newValue;
            Time.timeScale = newValue;
        }

        static void OnResetClicked()
        {
            var s = GetSettings();
            Time.timeScale = s.defaultScale;
            LastTimeScale  = s.defaultScale;
            RefreshToolbar();
        }

        // ── Play Mode Hook ────────────────────────────────────────────
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var s = GetSettings();
            if (!s.enabled) return;

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (s.resetOnPlay)
                    {
                        Time.timeScale = s.defaultScale;
                        LastTimeScale  = s.defaultScale;
                        RefreshToolbar();
                    }
                    else
                    {
                        Time.timeScale = LastTimeScale;
                    }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    Time.timeScale = 1f;
                    break;
            }
        }

        internal static void RefreshToolbar() => MainToolbar.Refresh(ToolElementPath);
    }

    /// <summary>
    /// ScriptableObject holding TimeScale toolbar configuration.
    /// Stored at Assets/Settings/Editor/TimeScaleToolbarSettings.asset.
    /// </summary>
    public class TimeScaleToolbarSettings : ScriptableObject
    {
        public bool  enabled        = true;
        public float minTimeScale   = 0f;
        public float maxTimeScale   = 5f;
        public float defaultScale   = 1f;
        public bool  resetOnPlay    = true;
        public bool  forcedOverride = false;
    }
}
