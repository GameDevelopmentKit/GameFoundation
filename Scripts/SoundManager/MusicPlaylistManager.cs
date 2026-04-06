namespace SoundManager
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities;
    using UnityEngine;
    using Zenject;
    using Object = UnityEngine.Object;

    public interface IMusicPlaylistManager
    {
        void Stop();
        void Pause();
        void Resume();
        bool IsPlaying();

        void  SetTime(float time);
        float GetTime();

        void SetPitch(float pitch);
        void SetLoop(bool loop);

        void StopAll();

        // Context BGM (async only for asset loading)
        UniTask PushContextBGM(string id, AudioClip clip, int priority,
            float fadeSeconds = 0.8f, float volumeScale = 1f,
            List<AudioClip> playlist = null, bool loopPlaylist = false);

        void RemoveContextBGM(string id, float fadeSeconds = 0.8f);

        UniTask SetBaseBGM(string name, int priority = 0);

        void AdjustContextPriority(string id, int newPriority);
        void UpdateVolume(float globalVolume);
    }

    public interface IHasId
    {
        string Id { get; }
    }

    /// <summary>
    /// PRIORITY QUEUE (MAX-HEAP)
    /// </summary>
    public class PriorityQueue<T> where T : IComparable<T>, IHasId
    {
        private readonly List<T> heap = new();

        public int  Count   => heap.Count;
        public void Clear() => heap.Clear();
        public T    Peek()  => heap.Count > 0 ? heap[0] : default;

        public void Push(T item)
        {
            heap.Add(item);
            HeapifyUp(heap.Count - 1);
        }

        public bool Remove(T item)
        {
            int index = heap.IndexOf(item);

            if (index < 0) return false;

            int last = heap.Count - 1;

            if (index == last)
            {
                heap.RemoveAt(last);
                return true;
            }

            heap[index] = heap[last];
            heap.RemoveAt(last);

            HeapifyUp(index);
            HeapifyDown(index);

            return true;
        }

        public void Resort(T item)
        {
            int index = heap.IndexOf(item);

            if (index < 0) return;

            HeapifyUp(index);
            HeapifyDown(index);
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int p = (index - 1) / 2;

                if (heap[index].CompareTo(heap[p]) <= 0) break;

                (heap[index], heap[p]) = (heap[p], heap[index]);
                index                  = p;
            }
        }

        private void HeapifyDown(int index)
        {
            int last = heap.Count - 1;

            while (true)
            {
                int left    = index * 2 + 1;
                int right   = index * 2 + 2;
                int largest = index;

                if (left <= last && heap[left].CompareTo(heap[largest]) > 0) largest   = left;
                if (right <= last && heap[right].CompareTo(heap[largest]) > 0) largest = right;

                if (largest == index) break;

                (heap[index], heap[largest]) = (heap[largest], heap[index]);
                index                        = largest;
            }
        }

        public bool TryGet(string id, out T result)
        {
            foreach (var item in heap)
            {
                if (item.Id == id)
                {
                    result = item;
                    return true;
                }
            }

            result = default;
            return false;
        }

    }

    /// <summary>
    /// BGM CONTEXT ENTRY
    /// </summary>
    class BgmContextEntry : IComparable<BgmContextEntry>, IHasId
    {
        public int    Priority;

        public List<AudioClip> Playlist;
        public bool            LoopPlaylist;
        public int             PlaylistIndex;

        public float Volume;
        public float FadeSeconds;

        public int CompareTo(BgmContextEntry other)
            => Priority.CompareTo(other.Priority); // max priority first

        public string Id { get; set; }
    }

    /// <summary>
    /// Tick-based state machine for BGM crossfading.
    /// No async loops, no CancellationTokenSource.
    /// </summary>
    public class MusicPlaylistManager : IMusicPlaylistManager, IDisposable, ITickable
    {
        private readonly IGameAssets assets;

        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private bool initialized;
        private bool disposed;

        private readonly PriorityQueue<BgmContextEntry> queue = new();

        /// <summary>
        /// baseBgm is the permanent fallback — it is NEVER in the queue.
        /// When the queue empties, we always fall back to baseBgm.
        /// </summary>
        private BgmContextEntry baseBgm;

        private float globalVolume = 1f;

        // ── Fade state machine ──────────────────────────────────────────

        private enum FadeMode { None, FadeIn, CrossFade, FadeOut }

        private FadeMode fadeMode;
        private float    fadeElapsed;
        private float    fadeDuration;
        private float    fadeFromVolume;   // active source start volume
        private float    fadeToVolume;     // active source target volume
        private float    fadeNewFromVolume; // inactive source start (crossfade)
        private float    fadeNewToVolume;   // inactive source target (crossfade)

        // ── Constructor ─────────────────────────────────────────────────

        public MusicPlaylistManager(IGameAssets assets)
        {
            this.assets = assets;
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            var prefab = await assets.LoadAssetAsync<GameObject>(AudioManager.AudioSourceKey);

            var go1 = Object.Instantiate(prefab);
            var go2 = Object.Instantiate(prefab);

            Object.DontDestroyOnLoad(go1);
            Object.DontDestroyOnLoad(go2);

            activeSource   = go1.GetComponent<AudioSource>();
            inactiveSource = go2.GetComponent<AudioSource>();

            activeSource.playOnAwake   = false;
            inactiveSource.playOnAwake = false;

            activeSource.loop   = true;
            inactiveSource.loop = true;

            activeSource.volume   = 0f;
            inactiveSource.volume = 0f;

            initialized = true;
        }

        private async UniTask WaitInit()
        {
            while (!initialized)
                await UniTask.Yield();
        }

        // ── ITickable ───────────────────────────────────────────────────

        public void Tick()
        {
            if (disposed || !initialized) return;
            if (activeSource == null || inactiveSource == null) return;
            if (fadeMode == FadeMode.None) return;

            fadeElapsed += Time.deltaTime;
            float t = fadeDuration > 0f ? Mathf.Clamp01(fadeElapsed / fadeDuration) : 1f;

            switch (fadeMode)
            {
                case FadeMode.FadeIn:
                    activeSource.volume = Mathf.Lerp(fadeFromVolume, fadeToVolume, t);
                    break;

                case FadeMode.CrossFade:
                    // Fade out old (active), fade in new (inactive)
                    activeSource.volume   = Mathf.Lerp(fadeFromVolume, 0f, t);
                    inactiveSource.volume = Mathf.Lerp(fadeNewFromVolume, fadeNewToVolume, t);
                    break;

                case FadeMode.FadeOut:
                    activeSource.volume = Mathf.Lerp(fadeFromVolume, 0f, t);
                    break;
            }

            if (t >= 1f) CompleteFade();
        }

        /// <summary>
        /// Finalize the current fade — swap sources if crossfade, stop old source.
        /// </summary>
        private void CompleteFade()
        {
            switch (fadeMode)
            {
                case FadeMode.FadeIn:
                    if (activeSource != null) activeSource.volume = fadeToVolume;
                    break;

                case FadeMode.CrossFade:
                    if (activeSource != null)
                    {
                        activeSource.Stop();
                        activeSource.volume = 0f;
                    }
                    if (inactiveSource != null) inactiveSource.volume = fadeNewToVolume;

                    // Swap: the new source becomes active
                    (activeSource, inactiveSource) = (inactiveSource, activeSource);
                    break;

                case FadeMode.FadeOut:
                    if (activeSource != null)
                    {
                        activeSource.Stop();
                        activeSource.volume = 0f;
                    }
                    break;
            }

            fadeMode = FadeMode.None;
        }

        /// <summary>
        /// If a fade is in progress, snap it to completion immediately.
        /// This ensures clean state before starting a new fade.
        /// </summary>
        private void SnapCurrentFade()
        {
            if (fadeMode != FadeMode.None) CompleteFade();
        }

        // ── Crossfade / Fade helpers (sync — state machine driven) ──────

        private void CrossFadeTo(AudioClip newClip, float duration, float targetVolume)
        {
            if (activeSource == null || inactiveSource == null) return;

            // Snap any in-progress fade first
            SnapCurrentFade();

            targetVolume *= globalVolume;

            // No old clip playing → simple fade in
            if (activeSource.clip == null || !activeSource.isPlaying)
            {
                activeSource.clip   = newClip;
                activeSource.time   = 0f;
                activeSource.volume = 0f;
                activeSource.Play();

                fadeMode       = FadeMode.FadeIn;
                fadeElapsed    = 0f;
                fadeDuration   = duration;
                fadeFromVolume = 0f;
                fadeToVolume   = targetVolume;
                return;
            }

            // Same clip already playing → just adjust volume
            if (activeSource.clip == newClip && activeSource.isPlaying)
            {
                fadeMode       = FadeMode.FadeIn;
                fadeElapsed    = 0f;
                fadeDuration   = duration;
                fadeFromVolume = activeSource.volume;
                fadeToVolume   = targetVolume;
                return;
            }

            // Different clip → crossfade
            inactiveSource.clip   = newClip;
            inactiveSource.time   = 0f;
            inactiveSource.volume = 0f;
            inactiveSource.Play();

            fadeMode          = FadeMode.CrossFade;
            fadeElapsed       = 0f;
            fadeDuration      = duration;
            fadeFromVolume    = activeSource.volume;   // old → 0
            fadeToVolume      = 0f;
            fadeNewFromVolume = 0f;                    // new → target
            fadeNewToVolume   = targetVolume;
        }

        private void StartFadeOut(float duration)
        {
            if (activeSource == null) return;

            SnapCurrentFade();

            fadeMode       = FadeMode.FadeOut;
            fadeElapsed    = 0f;
            fadeDuration   = duration;
            fadeFromVolume = activeSource.volume;
        }

        // ── Queue evaluation ────────────────────────────────────────────

        /// <summary>
        /// Check the top of the priority queue and crossfade to it if needed.
        /// Falls back to baseBgm when queue is empty.
        /// </summary>
        private void EvaluateTopAndPlay(float fadeOverride = -1f)
        {
            var top = queue.Count > 0 ? queue.Peek() : baseBgm;

            if (top == null || top.Playlist == null || top.Playlist.Count == 0)
            {
                // Nothing to play — fade out current
                if (activeSource != null && activeSource.isPlaying)
                    StartFadeOut(0.5f);
                return;
            }

            float fade = fadeOverride >= 0f ? fadeOverride : top.FadeSeconds;
            var targetClip = top.Playlist[top.PlaylistIndex];
            CrossFadeTo(targetClip, fade, top.Volume);
        }

        // ── Public API: Context BGM ─────────────────────────────────────

        /// <summary>
        /// Set the base (fallback) BGM. Stored separately from the queue.
        /// Played when the queue is empty.
        /// </summary>
        public async UniTask SetBaseBGM(string name, int priority = 0)
        {
            var clip = await assets.LoadAssetAsync<AudioClip>(name);
            if (clip == null) return;
            await WaitInit();

            baseBgm = new BgmContextEntry
            {
                Id            = name,
                Priority      = priority,
                Playlist      = new List<AudioClip>() { clip },
                PlaylistIndex = 0,
                LoopPlaylist  = false,
                Volume        = 1f,
                FadeSeconds   = 1f
            };

            // If nothing in the queue, start playing baseBgm
            if (queue.Count == 0)
                EvaluateTopAndPlay();
        }

        /// <summary>
        /// Push or update a context BGM entry in the priority queue.
        /// </summary>
        public async UniTask PushContextBGM(
            string id, AudioClip clip, int priority,
            float fadeSeconds = 0.8f, float volumeScale = 1f,
            List<AudioClip> playlist = null, bool loopPlaylist = false)
        {
            await WaitInit();

            List<AudioClip> list =
                playlist != null && playlist.Count > 0
                    ? playlist
                    : new List<AudioClip>() { clip };

            if (!queue.TryGet(id, out var entry))
            {
                entry = new BgmContextEntry
                {
                    Id            = id,
                    Priority      = priority,
                    Playlist      = list,
                    LoopPlaylist  = loopPlaylist,
                    PlaylistIndex = 0,
                    Volume        = volumeScale,
                    FadeSeconds   = fadeSeconds
                };

                queue.Push(entry);
            }
            else
            {
                entry.Priority      = priority;
                entry.Playlist      = list;
                entry.LoopPlaylist  = loopPlaylist;
                entry.PlaylistIndex = 0;
                entry.Volume        = volumeScale;
                entry.FadeSeconds   = fadeSeconds;

                queue.Resort(entry);
            }

            EvaluateTopAndPlay(fadeSeconds);
        }

        /// <summary>
        /// Remove a context BGM entry from the queue and play the next top.
        /// </summary>
        public void RemoveContextBGM(string id, float fadeSeconds = 0.8f)
        {
            if (!queue.TryGet(id, out var entry)) return;

            queue.Remove(entry);
            EvaluateTopAndPlay(fadeSeconds);
        }

        /// <summary>
        /// Adjust priority of a context BGM entry.
        /// Priority ≤ 0 is treated as removal (the caller is done with this BGM).
        /// </summary>
        public void AdjustContextPriority(string id, int newPriority)
        {
            if (!queue.TryGet(id, out var entry)) return;

            if (newPriority <= 0)
            {
                // Treat as removal — caller is done with this BGM
                RemoveContextBGM(id, entry.FadeSeconds);
                return;
            }

            entry.Priority = newPriority;
            queue.Resort(entry);
            EvaluateTopAndPlay();
        }

        public void UpdateVolume(float newGlobalVolume)
        {
            globalVolume = newGlobalVolume;

            // If currently fading, adjust the target volume
            if (fadeMode == FadeMode.FadeIn)
            {
                var top = queue.Count > 0 ? queue.Peek() : baseBgm;
                if (top != null) fadeToVolume = top.Volume * globalVolume;
            }
            else if (fadeMode == FadeMode.CrossFade)
            {
                var top = queue.Count > 0 ? queue.Peek() : baseBgm;
                if (top != null) fadeNewToVolume = top.Volume * globalVolume;
            }
            else if (fadeMode == FadeMode.None && activeSource != null && activeSource.isPlaying)
            {
                // No fade in progress — set volume directly with a quick fade
                var top = queue.Count > 0 ? queue.Peek() : baseBgm;
                if (top != null)
                {
                    float targetVol = top.Volume * globalVolume;
                    fadeMode       = FadeMode.FadeIn;
                    fadeElapsed    = 0f;
                    fadeDuration   = 0.2f;
                    fadeFromVolume = activeSource.volume;
                    fadeToVolume   = targetVol;
                }
            }
        }

        // ── Public API: Playback control ────────────────────────────────

        public void Stop()
        {
            SnapCurrentFade();

            if (activeSource != null) activeSource.Stop();
            if (inactiveSource != null) inactiveSource.Stop();
        }

        public void Pause()
        {
            if (activeSource != null) activeSource.Pause();
            if (inactiveSource != null) inactiveSource.Pause();
        }

        public void Resume()
        {
            if (activeSource != null) activeSource.UnPause();
            if (inactiveSource != null) inactiveSource.UnPause();
        }

        public bool IsPlaying() => activeSource != null && activeSource.isPlaying;

        public void SetTime(float time)
        {
            if (activeSource != null && activeSource.clip != null)
                activeSource.time = Mathf.Clamp(time, 0, activeSource.clip.length);
        }

        public float GetTime() => activeSource != null && activeSource.clip != null
            ? activeSource.time
            : 0;

        public void SetPitch(float pitch)
        {
            if (activeSource != null) activeSource.pitch   = pitch;
            if (inactiveSource != null) inactiveSource.pitch = pitch;
        }

        public void SetLoop(bool loop)
        {
            if (activeSource != null) activeSource.loop   = loop;
            if (inactiveSource != null) inactiveSource.loop = loop;
        }

        public void StopAll() => Stop();

        // ── Dispose ─────────────────────────────────────────────────────

        public void Dispose()
        {
            disposed = true;
            fadeMode = FadeMode.None;

            if (activeSource != null)
            {
                activeSource.Stop();
                Object.Destroy(activeSource.gameObject);
                activeSource = null;
            }

            if (inactiveSource != null)
            {
                inactiveSource.Stop();
                Object.Destroy(inactiveSource.gameObject);
                inactiveSource = null;
            }

            queue.Clear();
            baseBgm     = null;
            initialized = false;
        }
    }
}