namespace Mech.GameManager
{
    using Mech.Services;
    using UnityEngine;
    using Newtonsoft.Json;

    /// <summary>
    /// Manager save Load Local data
    /// </summary>
    public class HandleLocalDataServices
    {
        private readonly ILogService logger;
        public HandleLocalDataServices(ILogService logger) { this.logger = logger; }

        public void SaveLocalDataToString<T>(T data, bool saveToFile = false)
        {
            var json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(typeof(T).Name, json);
            if (saveToFile)
            {
                this.SaveLocalDataToFile();
            }

            this.logger.Log("Save " + typeof(T).Name + ": " + json);
        }

        //Load Any model
        public T LoadModel<T>() where T : new()
        {
            var json = PlayerPrefs.GetString(typeof(T).Name);
            var data = JsonConvert.DeserializeObject<T>(json);
            if (string.IsNullOrEmpty(json))
                data = new T();
            return data;
        }

        public void SaveLocalDataToFile()
        {
            PlayerPrefs.Save();
            this.logger.Log("Save Data To File");
        }
        
        public string GetKey<T>(){ return PlayerPrefs.GetString(typeof(T).Name); }
    }
}