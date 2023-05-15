namespace GameFoundation.Scripts.Utilities.UserData
{
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;

    public class LocalHandleUserDataServices : BaseHandleUserDataServices
    {
        public LocalHandleUserDataServices(ILogService logService) : base(logService)
        {
        }

        protected override void SaveJson(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        protected override string LoadJson(string key)
        {
            return PlayerPrefs.GetString(key);
        }
    }
}