using System;
using System.Collections.Generic;
using GameFoundation.Scripts.Utilities.Extension;
using Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GDKManagerEditor : EditorWindow
{
    private const string SDKToolsFolderPath = "Packages/com.gdk.core/Editor/GDKManager/";

    private VisualElement initPanel;
    private VisualElement configPanel;

    private List<IGameConfigEditor> listGameConfigEditors = new();

    [MenuItem("GDK/GDKManager")]
    public static void ShowExample()
    {
        var wnd = GetWindow<GDKManagerEditor>();
        wnd.titleContent = new("GDKManager");
    }

    public void CreateGUI()
    {
        // Import UXML
        this.rootVisualElement.Add(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SDKToolsFolderPath + "GDKManager.uxml").Instantiate());

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

        foreach (var configEditor in this.listGameConfigEditors) this.configPanel.Add(configEditor.LoadView());
    }
}