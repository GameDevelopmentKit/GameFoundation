namespace GameFoundation.Editor
{
    using GameFoundation.Editor.ServerConfig;
    using Sirenix.OdinInspector;
    using Sirenix.OdinInspector.Editor;
    using UnityEditor;

    public class ServerConfigurationEditorWindow : OdinEditorWindow
    {
        [MenuItem("Custom Config/Server")]
        private static void OpenWindow() { GetWindow<ServerConfigurationEditorWindow>().Show(); }

        [InlineEditor] public Scripts.Editor.ServerConfig.ServerConfig LocalServer;

        [InlineEditor] public Scripts.Editor.ServerConfig.ServerConfig DevelopServer;

        [InlineEditor] public Scripts.Editor.ServerConfig.ServerConfig StagingServer;

        [InlineEditor] public Scripts.Editor.ServerConfig.ServerConfig ReleaseServer;

        [InlineEditor] public GameConfig GameConfig;
    }
}