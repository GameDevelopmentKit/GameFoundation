namespace DataManager.LocalSave.Editor
{
    using UnityEditor;
    using UnityEngine.UIElements;

    public class DataManagerConfigEditor : BaseGameConfigEditor<LocalSaveConfig>
    {
        protected override string ConfigName { get; } = "DataManagerConfig";
        protected override string ConfigPath { get; } = "GameConfigs";
        public override VisualElement LoadView()
        {
            var dataManagerConfigTemplate = EditorGUIUtility.Load("Packages/com.gdk.core/Scripts/DataManager/LocalData/Editor/DataManagerConfigEditor.uxml") as VisualTreeAsset;

            if (dataManagerConfigTemplate == null) return this;
            var dataManagerConfigVisual = dataManagerConfigTemplate.CloneTree();
            dataManagerConfigVisual.Add(this.Config.CreateUIElementInspector());
            this.Add(dataManagerConfigVisual);

            return this;
        }
    }
}
