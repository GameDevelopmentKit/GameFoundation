namespace GameFoundation.Scripts.Auth.Editor
{
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>
    /// GDK Config Editor panel for <see cref="AuthConfig"/>.
    /// Automatically discovered by <see cref="GDKManagerEditor"/> via reflection
    /// (implements <see cref="IGameConfigEditor"/>).
    ///
    /// When the GDK Manager window opens, this editor:
    ///   1. Creates an AuthConfig ScriptableObject in Resources/GameConfigs/ if it doesn't exist
    ///   2. Adds it to GDKConfig's game configs list
    ///   3. Renders the AuthConfig inspector fields in the GDK Manager panel
    /// </summary>
    public class AuthConfigEditor : BaseGameConfigEditor<AuthConfig>
    {
        protected override string ConfigName { get; } = "AuthConfig";
        protected override string ConfigPath { get; } = "GameConfigs";

        public override VisualElement LoadView()
        {
            var template = EditorGUIUtility.Load(
                "Packages/com.gdk.core/Scripts/Auth/Editor/AuthConfigEditor.uxml") as VisualTreeAsset;

            if (template == null) return this;

            var visual = template.CloneTree();
            visual.Add(this.Config.CreateUIElementInspector());
            this.Add(visual);

            return this;
        }
    }
}
