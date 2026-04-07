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
        private readonly IGameAssets       assets;
        private readonly ILogService       logger;

        private readonly Dictionary<string, AudioSource> loopingSources = new();
        private readonly Dictionary<string, AudioClip>   clipCache      = new();
        private readonly Dictionary<AudioClip, int>      playingCounts  = new();

        /// <summary>
        /// Maximum number of the same AudioClip playing concurrently.
        /// </summary>
        public static int MaxConcurrentSameClip = 4;

        public SoundEffectManager(ObjectPoolManager objectPool, IGameAssets assets, ILogService logger)
        {
            this.objectPool = objectPool;
            this.assets     = assets;
            this.logger     = logger;
        }

        private async UniTask<AudioSource> GetSource()
        {
            var source = await objectPool.Spawn<AudioSource>(AudioManager.AudioSourceKey);
            source.clip   = null;
            source.pitch  = 1f;
            source.loop   = false;
            source.volume = 1f;
            return source;
        }

        private async UniTask<AudioClip> LoadClipCached(string name)
        {
            if (clipCache.TryGetValue(name, out var cached))
                return cached;

            var clip = await assets.LoadAssetAsync<AudioClip>(name);
            if (clip != null)
                clipCache[name] = clip;
            return clip;
        }

        private bool CanPlayClip(AudioClip clip)
        {
            if (clip == null) return false;
            playingCounts.TryGetValue(clip, out var count);
            return count < MaxConcurrentSameClip;
        }

        private void IncrementPlayCount(AudioClip clip)
        {
            playingCounts.TryGetValue(clip, out var count);
            playingCounts[clip] = count + 1;
        }

        private void DecrementPlayCount(AudioClip clip)
        {
            if (clip == null) return;
            if (playingCounts.TryGetValue(clip, out var count))
            {
                if (count <= 1)
                    playingCounts.Remove(clip);
                else
                    playingCounts[clip] = count - 1;
            }
        }

        public async UniTask PlayOneShot(string name, float volumeScale = 1f)
        {
            var clip = await LoadClipCached(name);
            PlayOneShot(clip, volumeScale).Forget();
        }

        public async UniTask PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (!CanPlayClip(clip)) return;

            IncrementPlayCount(clip);
            var source = await GetSource();
            source.PlayOneShot(clip, volumeScale);
            await UniTask.Delay((int)(clip.length * 1000f));
            DecrementPlayCount(clip);
            source.Recycle();
        }

        public async UniTask PlayOneShot(AudioClip clip, float pitch, float volumeScale = 1f)
        {
            if (!CanPlayClip(clip)) return;

            IncrementPlayCount(clip);
            var source = await GetSource();
            source.pitch = pitch;
            source.PlayOneShot(clip, volumeScale);
            await UniTask.Delay((int)(clip.length / Mathf.Abs(pitch) * 1000f));
            DecrementPlayCount(clip);
            source.Recycle();
        }

        public async UniTask PlayLoop(string name, float volumeScale = 1f, float fadeSeconds = 1f)
        {
            if (loopingSources.ContainsKey(name))
            {
                logger.Warning($"Loop sound already playing: {name}");
                return;
            }

            var clip = await LoadClipCached(name);
            PlayLoopInternal(name, clip, volumeScale);
        }

        public void PlayLoop(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            var name = clip.name;

            if (loopingSources.ContainsKey(name))
            {
                logger.Warning($"Loop sound already playing: {name}");
                return;
            }

            PlayLoopInternal(name, clip, volumeScale);
        }

        private async void PlayLoopInternal(string name, AudioClip clip, float volumeScale)
        {
            var source = await GetSource();

            source.clip   = clip;
            source.loop   = true;
            source.volume = volumeScale;
            source.Play();

            loopingSources[name] = source;
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
