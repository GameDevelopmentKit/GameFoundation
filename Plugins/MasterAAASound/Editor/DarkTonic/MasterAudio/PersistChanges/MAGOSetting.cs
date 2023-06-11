using System.Collections.Generic;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    // ReSharper disable once InconsistentNaming
    public class MAGOSetting
    {
        private const string IgnoredComponentNames = ";Transform;AudioSource;SoundGroupVariationUpdater;";

        private List<MAComponentPatch> _componentSettings;

        public MAGOSetting(GameObject gameObj)
        {
            GameObj = gameObj;

            CreateComponentSettings();
        }

        public GameObject GameObj { get; set; }

        public List<MAComponentPatch> ComponentSettings {
            get { return _componentSettings; }
        }

        public void CreateComponentSettings()
        {
            _componentSettings = new List<MAComponentPatch>();

            var components = GameObj.GetComponents(typeof(Component));

            foreach (var c in components)
            {
                var setting = new MAComponentPatch(c);

                if (c == null)
                {
                    continue;
                }

                if (IgnoredComponentNames.Contains(";" + setting.ComponentName + ";"))
                {
                    continue;
                }

                _componentSettings.Add(setting);
            }
        }

        public void StoreAllSelectedSettings()
        {
            _componentSettings.ForEach(setting => setting.StoreSettings());
        }

        public List<Component> RestoreAllSelectedSettings()
        {
            var listOfChangedComponents = new List<Component>();
            foreach (var setting in _componentSettings)
            {
                var resultChangedComponent = setting.RestoreSettings();
                if (resultChangedComponent != null)
                {
                    listOfChangedComponents.Add(resultChangedComponent);
                }
            }

            return listOfChangedComponents;
        }
    }
}