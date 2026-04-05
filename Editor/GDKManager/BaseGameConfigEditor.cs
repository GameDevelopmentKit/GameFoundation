
    using System.Text.RegularExpressions;
    using GameConfigs;
    using UnityEngine;
    using UnityEngine.UIElements;

    public interface IGameConfigEditor
    {
        string        TabName { get; }
        void          InitConfig(GDKConfig gdkConfig);
        VisualElement LoadView();
    }

    public abstract class BaseGameConfigEditor<T> : VisualElement, IGameConfigEditor where T : ScriptableObject, IGameConfig
    {
        protected          T      Config;
        protected abstract string ConfigName { get; }
        protected abstract string ConfigPath { get; }

        public virtual string TabName
        {
            get
            {
                var raw = this.ConfigName.Replace("Config", "").Trim();
                return Regex.Replace(raw, "(?<=[a-z])([A-Z])", " $1");
            }
        }

        public virtual void InitConfig(GDKConfig gdkConfig)
        {
            if (!gdkConfig.HasGameConfig<T>())
            {
                this.Config = this.CreateInstanceInResource<T>(this.ConfigName, this.ConfigPath);
                gdkConfig.AddGameConfig(this.Config);
            }
            else
            {
                this.Config = gdkConfig.GetGameConfig<T>();
            }
        }
        public abstract VisualElement LoadView();
    }
