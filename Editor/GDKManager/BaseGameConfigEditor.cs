using Models;
using UnityEngine;
using UnityEngine.UIElements;

public interface IGameConfigEditor
{
    void          InitConfig(GDKConfig gdkConfig);
    VisualElement LoadView();
}

public abstract class BaseGameConfigEditor<T> : VisualElement, IGameConfigEditor where T : ScriptableObject, IGameConfig
{
    protected          T      Config;
    protected abstract string ConfigName { get; }
    protected abstract string ConfigPath { get; }

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