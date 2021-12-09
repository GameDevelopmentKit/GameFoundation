namespace GameFoundation.Scripts
{
    using GameFoundation.Scripts.Editor.ServerConfig;
    using UnityEngine;

    public static class ScriptableObjectUtils
    {
        public static ServerConfig ResourceInstance(this ServerConfig scriptableObject, string path) { return Resources.Load<ServerConfig>(path); }
    }
}