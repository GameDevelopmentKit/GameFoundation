namespace GameFoundation.Scripts.Backup.Editor
{
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>
    /// GDK Config Editor panel for <see cref="BackupConfig"/>.
    /// Automatically discovered by <see cref="GDKManagerEditor"/> via reflection
    /// (implements <see cref="IGameConfigEditor"/>).
    ///
    /// Replaces the old <c>CloudSyncConfigEditor</c>.
    /// </summary>
    public class BackupConfigEditor : BaseGameConfigEditor<BackupConfig>
    {
        protected override string ConfigName { get; } = "BackupConfig";
        protected override string ConfigPath { get; } = "GameConfigs";
        public    override string TabName    => "Backup";

        public override VisualElement LoadView()
        {
            var template = EditorGUIUtility.Load(
                "Packages/com.gdk.core/Scripts/Backup/Editor/BackupConfigEditor.uxml") as VisualTreeAsset;

            if (template == null) return this;

            var visual = template.CloneTree();
            visual.Add(this.Config.CreateUIElementInspector());
            this.Add(visual);

            return this;
        }
    }
}
