using System;
using System.Collections.Generic;
using Editor.GDKManager;
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


    private List<BaseGameConfigEditor> listGameConfigEditors = new List<BaseGameConfigEditor>();

    [MenuItem("GDK/GDKManager")]
    public static void ShowExample()
    {
        GDKManagerEditor wnd = GetWindow<GDKManagerEditor>();
        wnd.titleContent = new GUIContent("GDKManager");
    }

    public void CreateGUI()
    {
        // Import UXML
        this.rootVisualElement.Add(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SDKToolsFolderPath + "GDKManager.uxml").Instantiate());

        this.initPanel   = this.rootVisualElement.Q<VisualElement>("InitPanel");
        this.configPanel = this.rootVisualElement.Q<VisualElement>("ConfigPanel");
        var sdkConfig = Resources.Load<GDKConfig>("GameConfigs/GDKConfig");
        if (sdkConfig == null)
        {
            this.configPanel.SetActive(false);
            this.initPanel.SetActive(true);
            this.initPanel.Q<Button>("btnInit").clicked += this.OnInitSdkSO;
        }
        else
        {
            foreach (var gameConfigEditorType in ReflectionUtils.GetAllDerivedTypes<BaseGameConfigEditor>())
            {
                var gameConfigEditor = (BaseGameConfigEditor)Activator.CreateInstance(gameConfigEditorType, new object[]{sdkConfig});
                this.listGameConfigEditors.Add(gameConfigEditor);
            }
            
            this.DisplaySDKConfig();
        }
    }
    
    /// <summary>
    /// Create new SDK Config scriptable object
    /// </summary>
    private void OnInitSdkSO()
    {
        var newSdkConfig = this.CreateInstanceInResource<GDKConfig>(nameof(GDKConfig), "GameConfigs");
        foreach (var gameConfigEditorType in ReflectionUtils.GetAllDerivedTypes<BaseGameConfigEditor>())
        {
            var gameConfigEditor = (BaseGameConfigEditor)Activator.CreateInstance(gameConfigEditorType, new object[]{newSdkConfig});
            gameConfigEditor.PreSetup();
            this.listGameConfigEditors.Add(gameConfigEditor);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        this.DisplaySDKConfig();
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