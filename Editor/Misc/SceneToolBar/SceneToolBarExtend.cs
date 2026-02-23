namespace GDK.Editor.Misc
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject holding Scene Toolbar configuration.
    /// Stored at Assets/Settings/Editor/SceneToolbarSettings.asset.
    /// </summary>
    public class SceneToolbarSettings : ScriptableObject
    {
        public bool         enabled      = true;
        public List<string> sceneFolders = new();
    }

    /// <summary>
    /// Loader / accessor for the SceneToolbarSettings asset.
    /// </summary>
    public static class SceneToolbarSettingsManager
    {
        const string SettingsAssetPath = "Assets/Settings/Editor/SceneToolbarSettings.asset";

        static SceneToolbarSettings cachedSettings;

        internal static SceneToolbarSettings GetSettings()
        {
            if (cachedSettings != null) return cachedSettings;

            cachedSettings = AssetDatabase.LoadAssetAtPath<SceneToolbarSettings>(SettingsAssetPath);

            if (cachedSettings == null)
            {
                cachedSettings = ScriptableObject.CreateInstance<SceneToolbarSettings>();
                var dir = Path.GetDirectoryName(SettingsAssetPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(cachedSettings, SettingsAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[SceneToolbar] Created settings at {SettingsAssetPath}");
            }

            return cachedSettings;
        }

        internal static void SaveSettings()
        {
            EditorUtility.SetDirty(cachedSettings);
            AssetDatabase.SaveAssets();
        }
    }
}