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
            // Re-check after the async clip load: StopLoop may have been called while we were loading
            if (loopingSources.ContainsKey(name)) return;
            await PlayLoopInternal(name, clip, volumeScale);
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

            PlayLoopInternal(name, clip, volumeScale).Forget();
        }

        // Changed from async void to async UniTask to prevent AudioSource pool leaks.
        // The old async void version had a race: StopLoop() called between GetSource() await
        // and the loopingSources assignment would leave the pooled AudioSource unreturned.
        // Now the sentinel entry is set before GetSource() awaits so StopLoop() can cancel it.
        private async UniTask PlayLoopInternal(string name, AudioClip clip, float volumeScale)
        {
            if (clip == null) return;

            // Reserve the slot before awaiting GetSource so that a concurrent StopLoop(name)
            // can see this entry and recycle the source when it arrives.
            loopingSources[name] = null;

            var source = await GetSource();

            // If StopLoop was called while we were waiting for a pooled source, clean up and bail.
            if (!loopingSources.ContainsKey(name))
            {
                source.Recycle();
                return;
            }

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

            // Remove the key first — PlayLoopInternal checks ContainsKey after GetSource() to detect cancellation
            loopingSources.Remove(name);

            // src is null when PlayLoopInternal is still awaiting GetSource(); it will recycle itself
            if (src == null) return;

            src.Stop();
            src.Recycle();
        }

        public void StopAll()
        {
            foreach (var src in loopingSources.Values)
            {
                // src is null when PlayLoopInternal is still awaiting GetSource(); it will recycle itself
                if (src == null) continue;
                src.Stop();
                src.Recycle();
            }

            loopingSources.Clear();
        }
    }
}
