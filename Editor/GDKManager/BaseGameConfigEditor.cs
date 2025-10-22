using System;
using System.IO;
using Models;
using UnityEditor;
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

    protected string FindAssetPath(string fileName)
    {
        var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (Path.GetFileName(assetPath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return assetPath;
            }
        }

        var packagesDirs = Directory.GetDirectories("Packages", "*", SearchOption.AllDirectories);

        foreach (var dir in packagesDirs)
        {
            var files = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                var fullPath     = files[0].Replace("\\", "/");
                var projectPath  = Path.GetFullPath(".").Replace("\\", "/");
                var relativePath = fullPath.Replace(projectPath + "/", "");

                return relativePath;
            }
        }

        Debug.LogWarning($"❌ Không tìm thấy file: {fileName}");

        return null;
    }
}