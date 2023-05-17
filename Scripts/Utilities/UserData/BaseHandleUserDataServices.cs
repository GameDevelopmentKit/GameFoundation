namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using System.Collections.Generic;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;

    public abstract class BaseHandleUserDataServices : IHandleUserDataServices
    {
        public const string UserDataPrefix = "UD-";

        private readonly ILogService                logService;
        private readonly Dictionary<string, object> userDataCache = new();

        protected BaseHandleUserDataServices(ILogService logService)
        {
            this.logService = logService;
        }

        public void Save<T>(T data, bool force = false) where T : class
        {
            var key = UserDataPrefix + typeof(T).Name;

            if (!this.userDataCache.ContainsKey(key))
            {
                this.userDataCache.Add(key, data);
            }

            if (!force) return;

            this.SaveJson(key, JsonConvert.SerializeObject(data));
            this.logService.LogWithColor($"Saved {key}", Color.green);
        }

        public T Load<T>() where T : class, ILocalData, new()
        {
            var key = UserDataPrefix + typeof(T).Name;

            return (T)this.userDataCache.GetOrAdd(key, () =>
            {
                var json   = this.LoadJson(key);
                var result = string.IsNullOrEmpty(json) ? new T() : JsonConvert.DeserializeObject<T>(json);

                if (string.IsNullOrEmpty(json))
                {
                    result?.Init();
                }

                return result;
            });
        }
        
        public object Load(Type type)
        {
            var key = UserDataPrefix + type.Name;

            return this.userDataCache.GetOrAdd(key, () =>
            {
                var json   = this.LoadJson(key);
                var result = string.IsNullOrEmpty(json) ? Activator.CreateInstance(type) : JsonConvert.DeserializeObject(json, type);

                if (string.IsNullOrEmpty(json))
                {
                    result?.GetType().GetMethod("Init")?.Invoke(result, null);
                }

                return result;
            });
        }

        public void SaveAll()
        {
            foreach (var (key, value) in this.userDataCache)
            {
                this.SaveJson(key, JsonConvert.SerializeObject(value));
                this.logService.LogWithColor($"Saved {key}", Color.green);
            }

            Debug.Log("User data saved");
        }

        protected abstract void   SaveJson(string key, string json);
        protected abstract string LoadJson(string key);
    }
}