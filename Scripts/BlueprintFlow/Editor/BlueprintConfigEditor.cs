namespace BlueprintFlow.Editor
{
    using BlueprintFlow.BlueprintControlFlow;
    using UnityEditor;
    using UnityEngine.UIElements;

    public class BlueprintConfigEditor : BaseGameConfigEditor<BlueprintConfig>
    {
        protected override string ConfigName { get; } = "BlueprintConfig";
        protected override string ConfigPath { get; } = "GameConfigs";

        public override VisualElement LoadView()
        {
            var blueprintConfigTemplate = EditorGUIUtility.Load("Packages/com.gdk.core/Scripts/BlueprintFlow/Editor/BlueprintConfigEditor.uxml") as VisualTreeAsset;

            if (blueprintConfigTemplate == null) return this;
            var blueprintConfigVisual = blueprintConfigTemplate.CloneTree();
            blueprintConfigVisual.Add(this.Config.CreateUIElementInspector());
            this.Add(blueprintConfigVisual);

            return this;
        }
    }
}