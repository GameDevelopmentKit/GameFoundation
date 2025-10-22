using System;
using System.Collections.Generic;
using System.IO;
using GameFoundation.Scripts.Utilities.Extension;
using Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GDKManagerEditor : EditorWindow
{
    private VisualElement initPanel;
    private VisualElement configPanel;

    private List<IGameConfigEditor> listGameConfigEditors = new();

    [MenuItem("GDK/GDKManager")]
    public static void ShowExample()
    {
        GDKManagerEditor wnd = GetWindow<GDKManagerEditor>();
        wnd.titleContent = new GUIContent("GDKManager");
    }

    private string FindAssetPath(string fileName)
    {
        var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileName(assetPath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }
        }

        var packagesDirs = Directory.GetDirectories("Packages", "*", SearchOption.AllDirectories);

        foreach (var dir in packagesDirs)
        {
            var files = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                var fullPath     = files[0].Replace("\\", "/");
                var projectPath  = Path.GetFullPath(".").Replace("\\", "/");
                var relativePath = fullPath.Replace(projectPath + "/", "");

                return relativePath;
            }
        }

        Debug.LogWarning($"❌ Không tìm thấy file: {fileName}");

        return null;
    }

    public void CreateGUI()
    {
        var asset = this.FindAssetPath("GDKManager.uxml");

        var instance = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(asset).Instantiate();
        // Import UXML
        this.rootVisualElement.Add(instance);

        this.initPanel   = this.rootVisualElement.Q<VisualElement>("InitPanel");
        this.configPanel = this.rootVisualElement.Q<VisualElement>("ConfigPanel");
        var sdkConfig = Resources.Load<GDKConfig>("GameConfigs/GDKConfig");

        if (sdkConfig != null)
        {
            this.LoadSDKConfig(sdkConfig);
            this.DisplaySDKConfig();
        }
        else
        {
            this.configPanel.SetActive(false);
            this.initPanel.SetActive(true);
            this.initPanel.Q<Button>("btnInit").clicked += this.OnInitSdkSO;
        }
    }

    /// <summary>
    /// Create new SDK Config scriptable object
    /// </summary>
    private void OnInitSdkSO()
    {
        var newSdkConfig = this.CreateInstanceInResource<GDKConfig>(nameof(GDKConfig), "GameConfigs");
        this.LoadSDKConfig(newSdkConfig);
        this.DisplaySDKConfig();
    }

    private void LoadSDKConfig(GDKConfig gdkConfig)
    {
        foreach (var gameConfigEditorType in ReflectionUtils.GetAllDerivedTypes<IGameConfigEditor>())
        {
            var gameConfigEditor = (IGameConfigEditor)Activator.CreateInstance(gameConfigEditorType);
            gameConfigEditor.InitConfig(gdkConfig);
            this.listGameConfigEditors.Add(gameConfigEditor);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void DisplaySDKConfig()
    {
        this.configPanel.SetActive(true);
        this.initPanel.SetActive(false);

        foreach (var configEditor in this.listGameConfigEditors)
        {
            this.configPanel.Add(configEditor.LoadView());
        }
    }
}