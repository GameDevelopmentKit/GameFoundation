using Models;
using UnityEditor;
using UnityEngine.UIElements;

public class GeneralConfigEditor : VisualElement, IGameConfigEditor
{
    private GDKConfig gdkConfig;

    public void InitConfig(GDKConfig gdkConfigParam)
    {
        this.gdkConfig = gdkConfigParam;
    }

    public VisualElement LoadView()
    {
        var template = EditorGUIUtility.Load("Packages/com.gdk.core/Editor/GDKManager/GeneralConfigEditor.uxml") as VisualTreeAsset;
        if (template != null)
        {
            this.Add(template.CloneTree());
            this.Q<VisualElement>("GeneralConfigPanel").Add(this.gdkConfig.CreateUIElementInspector("gameConfigs"));
        }

        return this;
    }
}