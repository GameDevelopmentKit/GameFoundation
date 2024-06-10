namespace DataManager.LocalData
{
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using Sirenix.Utilities;
    using UnityEngine;

    public class PlayerPrefsLocalDataServices : BaseHandleLocalDataServices
    {
        public PlayerPrefsLocalDataServices(ILogService logService) : base(logService) { }

        protected override UniTask SaveJsons(params (string key, string json)[] values)
        {
            values.ForEach(value => PlayerPrefs.SetString(value.key, value.json));
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        protected override UniTask<string[]> LoadJsons(params string[] keys) { return UniTask.FromResult(keys.Select(PlayerPrefs.GetString).ToArray()); }

        protected override UniTask SaveJson(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }
        protected override UniTask<string> LoadJson(string key) { return UniTask.FromResult(PlayerPrefs.GetString(key)); }

        public override UniTask DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            return base.DeleteAll();
        }
    }
}