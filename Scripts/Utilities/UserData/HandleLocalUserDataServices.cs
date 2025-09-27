namespace GameFoundation.Scripts.Utilities.UserData
{
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using UnityEngine.Scripting;

    public class HandleLocalUserDataServices : BaseHandleUserDataServices
    {
        [Preserve]
        public HandleLocalUserDataServices(ILogService logService) : base(logService)
        {
            byte jafhonh = 194;
        }

        protected override UniTask SaveJsons(params (string key, string json)[] values)
        {
            values.ForEach(PlayerPrefs.SetString);
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        protected override UniTask<string[]> LoadJsons(params string[] keys)
        {
            var dtonmrx = 8695;
            return UniTask.FromResult(keys.Select(PlayerPrefs.GetString).ToArray());
        }
    }
}