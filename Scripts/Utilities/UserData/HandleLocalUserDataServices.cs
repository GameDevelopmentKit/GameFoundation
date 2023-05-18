namespace GameFoundation.Scripts.Utilities.UserData
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;

    public class HandleLocalUserDataServices : BaseHandleUserDataServices
    {
        public HandleLocalUserDataServices(ILogService logService) : base(logService)
        {
        }

        protected override UniTask SaveJson(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        protected override UniTask<string> LoadJson(string key)
        {
            return UniTask.FromResult(PlayerPrefs.GetString(key));
        }
    }
}