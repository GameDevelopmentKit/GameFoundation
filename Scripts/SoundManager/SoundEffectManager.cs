namespace SoundManager
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using UnityEngine;

    public interface ISoundEffectManager
    {
        UniTask PlayOneShot(string soundName, float volumeScale = 1f);
        UniTask PlayLoop(string soundName, float volumeScale = 1f, float fadeSeconds = 1f);
        void    StopLoop(string soundName);
        void    StopAll();
    }

    public class SoundEffectManager : ISoundEffectManager
{
    private readonly ObjectPoolManager objectPool;
    private readonly IGameAssets assets;
    private readonly ILogService logger;

    private readonly Dictionary<string, AudioSource> loopingSources = new();

    public SoundEffectManager(ObjectPoolManager objectPool, IGameAssets assets, ILogService logger)
    {
        this.objectPool = objectPool;
        this.assets     = assets;
        this.logger     = logger;
    }

    private async UniTask<AudioSource> GetSource()
    {
        var source = await objectPool.Spawn<AudioSource>(AudioManager.AudioSourceKey);
        source.clip = null;
        return source;
    }

    public async UniTask PlayOneShot(string name, float volumeScale = 1f)
    {
        var clip   = await assets.LoadAssetAsync<AudioClip>(name);
        PlayOneShot(clip, volumeScale).Forget();
    }
    
    public async UniTask PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        var source = await GetSource();
        source.PlayOneShot(clip, volumeScale);
        await UniTask.Delay((int)(clip.length * 1000f));
        source.Recycle();
    }
    
    public async UniTask PlayOneShot(AudioClip clip, float pitch, float volumeScale = 1f)
    {
        var source = await GetSource();
        source.pitch = pitch;
        source.PlayOneShot(clip, volumeScale);
        await UniTask.Delay((int)(clip.length * 1000f));
        source.Recycle();
    }

    public async UniTask PlayLoop(string name, float volumeScale = 1f, float fadeSeconds = 1f)
    {
        if (loopingSources.ContainsKey(name))
        {
            logger.Warning($"Loop sound already playing: {name}");
            return;
        }

        var clip   = await assets.LoadAssetAsync<AudioClip>(name);
        var source = await GetSource();

        source.clip = clip;
        source.Play();

        loopingSources.Add(name, source);
    }

    public void StopLoop(string name)
    {
        if (!loopingSources.TryGetValue(name, out var src))
            return;

        src.Stop();
        src.Recycle();
        loopingSources.Remove(name);
    }

    public void StopAll()
    {
        foreach (var src in loopingSources.Values)
        {
            src.Stop();
            src.Recycle();
        }

        loopingSources.Clear();
    }
}

}