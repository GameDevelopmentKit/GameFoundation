namespace DataManager.LocalData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.Utils;
    using Newtonsoft.Json;
    using UnityEngine;

    public abstract class BaseHandleLocalDataServices : IHandleLocalDataServices
    {
        public const string UserDataPrefix = "LD-";

        public static string KeyOf(Type type) => UserDataPrefix + type.Name;

        private static readonly JsonSerializerSettings JsonSetting = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        private readonly ILogService                    logService;
        private readonly Dictionary<string, ILocalData> localDataCache = new();

        protected BaseHandleLocalDataServices(ILogService logService) { this.logService = logService; }

        public async UniTask Save<T>(T data, bool force = false) where T : class, ILocalData
        {
            var key = KeyOf(typeof(T));

            this.localDataCache.TryAdd(key, data);

            if (!force) return;

            await this.SaveJson(key, JsonConvert.SerializeObject(data, JsonSetting));
            this.logService.LogWithColor($"Saved {key}", Color.green);
        }

        public async UniTask<T> Load<T>() where T : class, ILocalData { return (T)await this.Load(typeof(T)); }

        public async UniTask<ILocalData> Load(Type type) { return this.InternalLoad(KeyOf(type), await this.LoadJson(KeyOf(type)), type); }

        public async UniTask<ILocalData[]> Load(params Type[] types)
        {
            var keys = types.Select(KeyOf).ToArray();

            return IterTools.Zip(types, keys, await this.LoadJsons(keys), (type, key, json) => this.InternalLoad(key, json, type)).ToArray();
        }

        private ILocalData InternalLoad(string key, string json, Type type)
        {
            return this.localDataCache.GetOrAdd(key, () =>
            {
                var result = string.IsNullOrEmpty(json) ? Activator.CreateInstance(type) : JsonConvert.DeserializeObject(json, type, JsonSetting);

                if (result is not ILocalData data)
                {
                    this.logService.Error($"Failed to load data {key}");
                    return null;
                }

                if (string.IsNullOrEmpty(json))
                {
                    data.Init();
                }

                this.logService.LogWithColor($"Loaded {key}", Color.green);
                return data;
            });
        }

        public async UniTask SaveAll()
        {
            await this.SaveJsons(this.localDataCache.Select(value =>
            {
                this.logService.LogWithColor($"Saved {value.Key}", Color.green);
                return (value.Key, JsonConvert.SerializeObject(value.Value, JsonSetting));
            }).ToArray());
            this.logService.LogWithColor("Saved all data", Color.green);
        }

        public virtual UniTask DeleteAll()
        {
            this.localDataCache.Clear();
            return this.SaveAll();
        }

        protected abstract UniTask SaveJsons(params (string key, string json)[] values);

        protected abstract UniTask<string[]> LoadJsons(params string[] keys);

        protected abstract UniTask SaveJson(string key, string json);

        protected abstract UniTask<string> LoadJson(string key);
    }
}