using System;
using System.Collections.Generic;
using System.Linq;
using GameConfigs;
using GameFoundation.Scripts.Utilities.Extension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GDKManagerEditor : EditorWindow
{
    private const string SDKToolsFolderPath = "Packages/com.gdk.core/Editor/GDKManager/";

    private VisualElement initPanel;
    private VisualElement configPanel;

    private List<IGameConfigEditor> listGameConfigEditors = new();

    // Tab state
    private VisualElement        tabBar;
    private VisualElement        tabContent;
    private int                  selectedTabIndex = -1;
    private List<VisualElement>  cachedViews      = new();

    [MenuItem("GDK/GDKManager")]
    public static void ShowExample()
    {
        GDKManagerEditor wnd = GetWindow<GDKManagerEditor>();
        wnd.titleContent = new GUIContent("GDKManager");
        wnd.minSize      = new Vector2(400, 300);
    }

    public void CreateGUI()
    {
        // Load stylesheet
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(SDKToolsFolderPath + "GDKManager.uss");
        if (styleSheet != null)
            this.rootVisualElement.styleSheets.Add(styleSheet);

        this.rootVisualElement.style.flexGrow = 1;

        // --- Init Panel (hidden by default) ---
        this.initPanel = new VisualElement();
        this.initPanel.style.justifyContent = Justify.Center;
        this.initPanel.style.alignItems     = Align.Center;
        this.initPanel.style.flexGrow       = 1;
        this.initPanel.style.display        = DisplayStyle.None;
        this.initPanel.Add(new Label("Welcome to GDK") { name = "WelcomeLabel" });
        var btnInit = new Button(this.OnInitSdkSO) { text = "Init SDK Config" };
        btnInit.style.width  = 150;
        btnInit.style.height = 35;
        this.initPanel.Add(btnInit);
        this.rootVisualElement.Add(this.initPanel);

        // --- Config Panel (tabbed) ---
        this.configPanel = new VisualElement();
        this.configPanel.style.flexGrow = 1;
        this.configPanel.style.display  = DisplayStyle.None;

        // Tab bar (no header — just tabs)
        this.tabBar = new VisualElement();
        this.tabBar.AddToClassList("gdk-tab-bar");
        this.configPanel.Add(this.tabBar);

        // Tab content area with scroll
        this.tabContent = new ScrollView(ScrollViewMode.Vertical);
        this.tabContent.AddToClassList("gdk-tab-content");
        this.tabContent.style.flexGrow = 1;
        this.configPanel.Add(this.tabContent);

        this.rootVisualElement.Add(this.configPanel);

        // --- Load config ---
        var sdkConfig = Resources.Load<GDKConfig>("GameConfigs/GDKConfig");
        if (sdkConfig != null)
        {
            this.LoadSDKConfig(sdkConfig);
            this.BuildTabs();
        }
        else
        {
            this.initPanel.style.display   = DisplayStyle.Flex;
            this.configPanel.style.display = DisplayStyle.None;
        }
    }

    private void OnInitSdkSO()
    {
        var newSdkConfig = this.CreateInstanceInResource<GDKConfig>(nameof(GDKConfig), "GameConfigs");
        this.LoadSDKConfig(newSdkConfig);
        this.BuildTabs();
    }

    private void LoadSDKConfig(GDKConfig gdkConfig)
    {
        foreach (var gameConfigEditorType in ReflectionUtils.GetAllDerivedTypes<IGameConfigEditor>())
        {
            var gameConfigEditor = (IGameConfigEditor)Activator.CreateInstance(gameConfigEditorType);
            gameConfigEditor.InitConfig(gdkConfig);
            this.listGameConfigEditors.Add(gameConfigEditor);
        }

        // Sort: "General" first, then alphabetical by TabName
        this.listGameConfigEditors = this.listGameConfigEditors
            .OrderBy(e => e.TabName == "General" ? 0 : 1)
            .ThenBy(e => e.TabName)
            .ToList();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void BuildTabs()
    {
        this.configPanel.style.display = DisplayStyle.Flex;
        this.initPanel.style.display   = DisplayStyle.None;

        this.tabBar.Clear();
        this.cachedViews.Clear();

        // Pre-build all views once so LoadView() is only called once per editor
        for (int i = 0; i < this.listGameConfigEditors.Count; i++)
        {
            var editor  = this.listGameConfigEditors[i];
            var tabName = editor.TabName;
            if (string.IsNullOrEmpty(tabName))
                tabName = $"Config {i + 1}";

            // Cache the view — LoadView() appends children to itself, so only call once
            var view = editor.LoadView();
            this.cachedViews.Add(view);

            var tabButton = new Button { text = tabName };
            tabButton.AddToClassList("gdk-tab-button");
            int capturedIndex = i;
            tabButton.clicked += () => this.SelectTab(capturedIndex);
            this.tabBar.Add(tabButton);
        }

        // Select first tab by default
        if (this.listGameConfigEditors.Count > 0)
            this.SelectTab(0);
    }

    private void SelectTab(int index)
    {
        if (index == this.selectedTabIndex) return;
        this.selectedTabIndex = index;

        // Update tab button styles
        for (int i = 0; i < this.tabBar.childCount; i++)
        {
            var btn = this.tabBar[i];
            if (i == index)
                btn.AddToClassList("gdk-tab-button--active");
            else
                btn.RemoveFromClassList("gdk-tab-button--active");
        }

        // Swap content — use cached view, never call LoadView() again
        this.tabContent.Clear();
        this.tabContent.Add(this.cachedViews[index]);
    }
}