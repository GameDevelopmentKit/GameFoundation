namespace GameFoundation.Scripts.Editor.ServerConfig
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "ServerConfig", menuName = "Mech/Server Config data")]
    public class ServerConfig : ScriptableObject
    {
        public string AuthServer;
        public string GameServer;
    }
}