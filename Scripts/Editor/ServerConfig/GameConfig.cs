namespace GameFoundation.Editor.ServerConfig
{
    using GameFoundation.Scripts.Editor.ServerConfig;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Mech/Game config")]
    public class GameConfig : ScriptableObject
    {
        [InlineEditor] public ServerConfig ServerConfig;
    }
}