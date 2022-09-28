namespace GameFoundation.Scripts.Network.NetworkConfig
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Mech/Game config")]
    public class GameConfig : ScriptableObject
    {
        public ServerConfig ServerConfig;
    }
}