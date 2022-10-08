using Models;
using UnityEditor;
using UnityEngine.UIElements;

namespace Editor.GDKManager
{
    public class GeneralConfigEditor : BaseGameConfigEditor
    {
        public GeneralConfigEditor(GDKConfig gdkConfig) : base(gdkConfig)
        {
        }
        
        public override void PreSetup()
        {
            
        }
        public override VisualElement LoadView()
        {
            var template = EditorGUIUtility.Load("Packages/com.gdk.core/Editor/GDKManager/GeneralConfigEditor.uxml") as VisualTreeAsset;
            if (template != null)
            {
                this.Add(template.CloneTree());
                this.Q<VisualElement>("GeneralConfigPanel").Add(this.GdkConfig.CreateUIElementInspector("gameConfigs"));
            }

            return this;
        }
    }
}
