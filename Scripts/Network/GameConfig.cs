namespace GameFoundation.Editor.ServerConfig
{
    using GameFoundation.Scripts.Network;
    using UnityEngine;

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Mech/Game config")]
    public class GameConfig : ScriptableObject
    {
        public ServerConfig ServerConfig;
    }
}