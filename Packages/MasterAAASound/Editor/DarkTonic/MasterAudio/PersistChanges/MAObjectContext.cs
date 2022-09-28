using System.Collections.Generic;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public class MAObjectContext
    {
        private static readonly Dictionary<GameObject, MAGOSetting> PersistentSettings = new Dictionary<GameObject, MAGOSetting>();

        public GameObject GameObj { get; private set; }

        public MAGOSetting GameObjectSetting { get; private set; }

        private MAGOSetting GetStoredGameObjectSetting()
        {
            MAGOSetting setting;

            if (PersistentSettings.ContainsKey(GameObj))
            {
                setting = PersistentSettings[GameObj];
            }
            else
            {
                setting = new MAGOSetting(GameObj);
                PersistentSettings.Add(GameObj, setting);
            }

            return setting;
        }

        public void SetContext(Object target)
        {
            GameObj = ((Transform)target).gameObject;

            if (GameObjectSetting == null)
            {
                GameObjectSetting = GetStoredGameObjectSetting();
            }
        }
    }
}