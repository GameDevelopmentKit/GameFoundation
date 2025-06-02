namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;
    using Newtonsoft.Json;
    using TheOne.Extensions;
    using TheOne.Logging;
    using UnityEngine;
    using ILogger = TheOne.Logging.ILogger;

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

        private readonly ILogger                        logger;
        private readonly Dictionary<string, ILocalData> userDataCache = new();

        protected BaseHandleUserDataServices(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
        }

        public async UniTask Save<T>(T data, bool force = false) where T : class, ILocalData
        {
            var key = KeyOf(typeof(T));

            this.userDataCache.TryAdd(key, data);

            if (!force) return;

            await this.SaveJsons((key, JsonConvert.SerializeObject(data, JsonSetting)));
            this.logger.Info($"Saved {key}".WithColor(Color.green));
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
                                this.logger.Error($"Failed to load data {key}");
                                return null;
                            }

                            if (string.IsNullOrEmpty(json)) data.Init();

                            data.OnDataLoaded();
                            this.logger.Info($"Level Data Loaded: {json}".WithColor(Color.green));
                            this.logger.Info($"Loaded {key}".WithColor(Color.green));
                            return data;
                        });
                }).ToArray();
        }

        public async UniTask SaveAll()
        {
            await this.SaveJsons(this.userDataCache.Select(value =>
            {
                this.logger.Info($"Saved {value.Key}".WithColor(Color.green));
                return (value.Key, JsonConvert.SerializeObject(value.Value, JsonSetting));
            }).ToArray());
            this.logger.Info("Saved all data".WithColor(Color.green));
        }

        protected abstract UniTask SaveJsons(params (string key, string json)[] values);

        protected abstract UniTask<string[]> LoadJsons(params string[] keys);
    }
}