namespace GameFoundation.Scripts
{
    using GameFoundation.Scripts.Network;
    using UnityEngine;

    public static class ScriptableObjectUtils
    {
        public static ServerConfig ResourceInstance(this ServerConfig scriptableObject, string path) { return Resources.Load<ServerConfig>(path); }
    }
}