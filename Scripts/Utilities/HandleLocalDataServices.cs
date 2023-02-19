namespace GameFoundation.Scripts.Utilities
{
    using System.Collections.Generic;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Manager save Load Local data
    /// </summary>
    public class HandleLocalDataServices
    {
        private const    string      LocalDataPrefix = "LD-";

        #region inject

        private readonly DiContainer diContainer;
        private readonly ILogService logService;

        #endregion

        private readonly Dictionary<string, object> localDataCaches = new();

        public HandleLocalDataServices(DiContainer diContainer, ILogService logService)
        {
            this.diContainer = diContainer;
            this.logService  = logService;
        }

        /// <summary>
        /// Save a class data to local
        /// </summary>
        /// <param name="data">class data</param>
        /// <param name="force"> if true, save data immediately to local</param>
        /// <typeparam name="T"> type of class</typeparam>
        public void Save<T>(T data, bool force = false) where T : class
        {
            var key = LocalDataPrefix + typeof(T).Name;

            if (!this.localDataCaches.ContainsKey(key))
            {
                this.localDataCaches.Add(key, data);
            }

            if (!force) return;
            var json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(key, json);
            Debug.Log("Save " + key + ": " + json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load data from local
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>() where T : class, ILocalData
        {
            var key = LocalDataPrefix + typeof(T).Name;

            if (this.localDataCaches.TryGetValue(key, out var cache))
            {
                return (T)cache;
            }

            var json = PlayerPrefs.GetString(key);

            var isHadLocalDataAlready = !string.IsNullOrEmpty(json);
            var result = this.diContainer.Instantiate<T>();

            if (isHadLocalDataAlready)
            {
                JsonConvert.PopulateObject(json, result);
            }
            else
            {
                result.Init();
            }

            this.localDataCaches.Add(key, result);

            return result;
        }

        public void StoreAllToLocal()
        {
            foreach (var localData in this.localDataCaches)
            {
                PlayerPrefs.SetString(localData.Key, JsonConvert.SerializeObject(localData.Value));
                this.logService.LogWithColor($"Saved {localData.Key}", Color.green);
            }

            PlayerPrefs.Save();
        }
    }
}