namespace BlueprintFlow.Editor
{
    using BlueprintFlow.BlueprintControlFlow;
    using Models;
    using UnityEditor;
    using UnityEngine.UIElements;

    public class BlueprintConfigEditor : BaseGameConfigEditor
    {
        public BlueprintConfigEditor(GDKConfig gdkConfig) : base(gdkConfig) { }
        public override void PreSetup() { this.GdkConfig.AddGameConfig(this.CreateInstanceInResource<BlueprintConfig>($"BlueprintConfig", "GameConfigs")); }

        public override VisualElement LoadView()
        {
            var blueprintConfigTemplate = EditorGUIUtility.Load("Packages/com.gdk.core/Scripts/BlueprintFlow/Editor/BlueprintConfigEditor.uxml") as VisualTreeAsset;

            if (blueprintConfigTemplate == null) return this;
            var blueprintConfigVisual = blueprintConfigTemplate.CloneTree();
            blueprintConfigVisual.Add(this.GdkConfig.GetGameConfig<BlueprintConfig>().CreateUIElementInspector());
            this.Add(blueprintConfigVisual);

            return this;
        }
    }
}