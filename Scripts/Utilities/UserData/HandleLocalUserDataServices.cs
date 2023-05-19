namespace GameFoundation.Scripts.Utilities.UserData
{
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using Sirenix.Utilities;
    using UnityEngine;

    public class HandleLocalUserDataServices : BaseHandleUserDataServices
    {
        public HandleLocalUserDataServices(ILogService logService) : base(logService)
        {
        }

        protected override UniTask SaveJsons(params (string key, string json)[] values)
        {
            values.ForEach(value => PlayerPrefs.SetString(value.key, value.json));
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        protected override UniTask<string[]> LoadJsons(params string[] keys)
        {
            return UniTask.FromResult(keys.Select(PlayerPrefs.GetString).ToArray());
        }
    }
}