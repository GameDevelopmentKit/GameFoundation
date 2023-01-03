namespace BlueprintFlow.Editor
{
    using BlueprintFlow.BlueprintControlFlow;
    using global::Editor.GDKManager;
    using Models;
    using UnityEngine.UIElements;

    public class BlueprintConfigEditor : BaseGameConfigEditor
    {
        public BlueprintConfigEditor(GDKConfig gdkConfig) : base(gdkConfig)
        {
        }
        public override void PreSetup()
        {
            this.GdkConfig.AddGameConfig(this.CreateInstanceInResource<BlueprintConfig>($"BlueprintConfig", "GameConfigs"));
        }
        
        public override VisualElement LoadView()
        {
            // display network config
            var networkConfigElement = this.Q<VisualElement>("BlueprintConfig");
            networkConfigElement.Add(this.GdkConfig.GetGameConfig<BlueprintConfig>().CreateUIElementInspector());
            this.Add(networkConfigElement);
            return this;
        }
    }
}