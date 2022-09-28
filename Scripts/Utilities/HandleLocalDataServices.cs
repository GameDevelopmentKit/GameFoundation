namespace GameFoundation.Scripts.Utilities
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>
    /// Manager save Load Local data
    /// </summary>
    public class HandleLocalDataServices
    {
        private const string LocalDataPrefix = "LD-";

        private readonly Dictionary<string, object> localDataCaches = new();

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
        public T Load<T>() where T : class, new()
        {
            var key = LocalDataPrefix + typeof(T).Name;
            if (this.localDataCaches.TryGetValue(key, out var cache))
            {
                return (T)cache;
            }

            var json   = PlayerPrefs.GetString(key);
            var result = string.IsNullOrEmpty(json) ? new T() : JsonConvert.DeserializeObject<T>(json);
            this.localDataCaches.Add(key, result);
            return result;
        }

        public void StoreAllToLocal()
        {
            foreach (var localData in this.localDataCaches)
            {
                PlayerPrefs.SetString(localData.Key, JsonConvert.SerializeObject(localData.Value));
            }

            PlayerPrefs.Save();
            Debug.Log("Save Data To File");
        }
    }
}