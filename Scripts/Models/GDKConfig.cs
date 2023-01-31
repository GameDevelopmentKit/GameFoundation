namespace Models
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class GDKConfig : SerializedScriptableObject
    {
        #region Public feilds

        /// <summary>
        /// 
        /// </summary>
        public string ConfigVersion => this.configVersion;

        /// <summary>
        /// 
        /// </summary>
        public string APIKey => this.apiKey;

        /// <summary>
        /// 
        /// </summary>
        public string GameName => this.gameName;

        /// <summary>
        /// 
        /// </summary>
        public string GameId => this.gameId;

        #endregion


        #region private feild

        [SerializeField] private string configVersion;
        [SerializeField] private string apiKey = "HGDHSGDHSSFDDS";
        [SerializeField] private string gameName;
        [SerializeField] private string gameId = "8a5ddf62-4c95-44b0-9db0-be5f8f9ef819";

        [SerializeField] private List<ScriptableObject> gameConfigs;

        private Dictionary<Type, ScriptableObject> typeToGameConfig;

        #endregion

        private void OnEnable() { this.RefreshData(); }

        private void RefreshData()
        {
            if (this.gameConfigs == null || this.gameConfigs.Count == 0) return;
            this.typeToGameConfig = new Dictionary<Type, ScriptableObject>();
            foreach (var gameConfig in this.gameConfigs)
            {
                this.typeToGameConfig.Add(gameConfig.GetType(), gameConfig);
            }
        }

        public T GetGameConfig<T>() where T : ScriptableObject
        {
            if (this.typeToGameConfig.TryGetValue(typeof(T), out var result))
            {
                return (T)result;
            }
            else
            {
                Debug.Log($"Don't find any game config with type = {typeof(T).Name}");
                return default;
            }
        }

#if UNITY_EDITOR
        public void AddGameConfig(ScriptableObject gameConfig)
        {
            if (this.gameConfigs == null) this.gameConfigs = new List<ScriptableObject>();
            this.gameConfigs.Add(gameConfig);
            this.RefreshData();
        }

        public void RemoveGameConfig(ScriptableObject gameConfig)
        {
            this.gameConfigs?.Remove(gameConfig);
            this.RefreshData();
        }
#endif
    }
}