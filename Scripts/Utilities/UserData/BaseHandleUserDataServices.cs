namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;

    public abstract class BaseHandleUserDataServices : IHandleUserDataServices
    {
        public const string UserDataPrefix = "LD-";

        public static string KeyOf(Type type)
        {
            return UserDataPrefix + type.Name;
        }

        public static readonly JsonSerializerSettings JsonSetting = new()
        {
            TypeNameHandling      = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        private readonly ILogService                    logService;
        private readonly Dictionary<string, ILocalData> userDataCache = new();

        protected BaseHandleUserDataServices(ILogService logService)
        {
            this.logService = logService;
        }

        public async UniTask Save<T>(T data, bool force = false) where T : class, ILocalData
        {
            var key = KeyOf(typeof(T));

            if (!this.userDataCache.ContainsKey(key)) this.userDataCache.Add(key, data);

            if (!force) return;

            await this.SaveJsons((key, JsonConvert.SerializeObject(data, JsonSetting)));
            this.logService.LogWithColor($"Saved {key}", Color.green);
        }

        public async UniTask<T> Load<T>() where T : class, ILocalData
        {
            return (T)(await this.Load(typeof(T)))[0];
        }

        public async UniTask<ILocalData[]> Load(params Type[] types)
        {
            var keys = types.Select(KeyOf).ToArray();

            return IterTools.Zip(types,
                keys,
                await this.LoadJsons(keys),
                (type, key, json) =>
                {
                    return this.userDataCache.GetOrAdd(key,
                        () =>
                        {
                            var result = string.IsNullOrEmpty(json) ? Activator.CreateInstance(type) : JsonConvert.DeserializeObject(json, type, JsonSetting);

                            if (result is not ILocalData data)
                            {
                                this.logService.Error($"Failed to load data {key}");
                                return null;
                            }

                            if (string.IsNullOrEmpty(json)) data.Init();

                            this.logService.LogWithColor($"Loaded {key}", Color.green);
                            return data;
                        });
                }).ToArray();
        }

        public async UniTask SaveAll()
        {
            await this.SaveJsons(this.userDataCache.Select(value =>
            {
                this.logService.LogWithColor($"Saved {value.Key}", Color.green);
                return (value.Key, JsonConvert.SerializeObject(value.Value, JsonSetting));
            }).ToArray());
            this.logService.LogWithColor("Saved all data", Color.green);
        }

        protected abstract UniTask SaveJsons(params (string key, string json)[] values);

        protected abstract UniTask<string[]> LoadJsons(params string[] keys);
    }
}