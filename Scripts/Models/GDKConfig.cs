namespace Models
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
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
        [SerializeField] private string apiKey = "";
        [SerializeField] private string gameName;
        [SerializeField] private string gameId = "";

        [SerializeField] private List<IGameConfig> gameConfigs;

        private Dictionary<Type, IGameConfig> typeToGameConfig;

        #endregion

        private void OnEnable()
        {
            this.RefreshData();
        }

        private void RefreshData()
        {
            if (this.gameConfigs == null || this.gameConfigs.Count == 0) return;
            this.typeToGameConfig = new();
            foreach (var gameConfig in this.gameConfigs) this.typeToGameConfig.Add(gameConfig.GetType(), gameConfig);
        }

        public T GetGameConfig<T>() where T : IGameConfig
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

        public bool HasGameConfig<T>() where T : IGameConfig
        {
            return this.HasGameConfig(typeof(T));
        }

        public bool HasGameConfig(Type type)
        {
            return this.typeToGameConfig != null && this.typeToGameConfig.ContainsKey(type);
        }

        #if UNITY_EDITOR
        public void AddGameConfig(IGameConfig gameConfig)
        {
            if (this.gameConfigs == null) this.gameConfigs = new();
            this.gameConfigs.Add(gameConfig);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            this.RefreshData();
        }

        public void RemoveGameConfig(IGameConfig gameConfig)
        {
            this.gameConfigs?.Remove(gameConfig);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            this.RefreshData();
        }
        #endif
    }
}