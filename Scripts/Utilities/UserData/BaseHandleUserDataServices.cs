namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;

    public abstract class BaseHandleUserDataServices : IHandleUserDataServices
    {
        public const string UserDataPrefix = "LD-";

        private readonly ILogService                    logService;
        private readonly Dictionary<string, ILocalData> userDataCache = new();

        protected BaseHandleUserDataServices(ILogService logService)
        {
            this.logService = logService;
        }

        public async UniTask Save<T>(T data, bool force = false) where T : class, ILocalData
        {
            var key = UserDataPrefix + typeof(T).Name;

            if (!this.userDataCache.ContainsKey(key))
            {
                this.userDataCache.Add(key, data);
            }

            if (!force) return;

            await this.SaveJson(key, JsonConvert.SerializeObject(data));
            this.logService.LogWithColor($"Saved {key}", Color.green);
        }

        public async UniTask<T> Load<T>() where T : class, ILocalData, new()
        {
            var key = UserDataPrefix + typeof(T).Name;

            return (T)await this.userDataCache.GetOrAdd(key, async () =>
            {
                var json   = await this.LoadJson(key);
                var result = string.IsNullOrEmpty(json) ? new() : JsonConvert.DeserializeObject<T>(json);

                if (result is not ILocalData localData)
                {
                    this.logService.Error($"Failed to load local data {key}");
                    return null;
                }

                if (string.IsNullOrEmpty(json))
                {
                    localData.Init();
                }

                return localData;
            });
        }

        public async UniTask<ILocalData> Load(Type type)
        {
            var key = UserDataPrefix + type.Name;

            return await this.userDataCache.GetOrAdd(key, async () =>
            {
                var json   = await this.LoadJson(key);
                var result = string.IsNullOrEmpty(json) ? Activator.CreateInstance(type) : JsonConvert.DeserializeObject(json, type);

                if (result is not ILocalData localData)
                {
                    this.logService.Error($"Failed to load local data {key}");
                    return null;
                }

                if (string.IsNullOrEmpty(json))
                {
                    localData.Init();
                }

                return localData;
            });
        }

        public async UniTask SaveAll()
        {
            foreach (var (key, value) in this.userDataCache)
            {
                await this.SaveJson(key, JsonConvert.SerializeObject(value));
                this.logService.LogWithColor($"Saved {key}", Color.green);
            }

            Debug.Log("User data saved");
        }

        protected abstract UniTask         SaveJson(string key, string json);
        protected abstract UniTask<string> LoadJson(string key);
    }
}