namespace SoundManager
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.Extension;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public interface IMusicPlaylistManager
    {
        UniTask Play(string name, float volumeScale = 1f, float fadeSeconds = 0.8f, bool persist = false);
        UniTask Play(AudioClip clip, float volumeScale = 1f, float fadeSeconds = 0.8f, bool persist = false);

        void Stop();
        void Pause();
        void Resume();
        bool IsPlaying();

        void  SetTime(float time);
        float GetTime();

        void SetPitch(float pitch);
        void SetLoop(bool loop);

        void StopAll();

        // Priority context API
        UniTask PushContextBGM(string id, AudioClip clip, int priority,
            float fadeSeconds = 0.8f, float volumeScale = 1f,
            List<AudioClip> playlist = null, bool loopPlaylist = false);

        UniTask RemoveContextBGM(string id, float fadeSeconds = 0.8f);

        UniTask SetBaseBGM(string name, int priority =0);

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
    /// <typeparam name="T"></typeparam>
    public class PriorityQueue<T> where T : IComparable<T>,IHasId
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
            heap[index] = heap[last];
            heap.RemoveAt(last);

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
    class BgmContextEntry : IComparable<BgmContextEntry>,IHasId
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
    
    public class MusicPlaylistManager : IMusicPlaylistManager, IDisposable
    {
        private readonly IGameAssets assets;

        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private CancellationTokenSource fadeCts;
        private CancellationTokenSource playCts;

        private bool initialized = false;

        private readonly PriorityQueue<BgmContextEntry>      queue = new();
        //private readonly Dictionary<string, BgmContextEntry> dict  = new();

        private BgmContextEntry baseBgm;

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

        private void CancelFade()
        {
            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = null;
        }

        private void CancelPlayWait()
        {
            playCts?.Cancel();
            playCts?.Dispose();
            playCts = null;
        }

        /// <summary>
        /// simple play
        /// </summary>
        /// <param name="name"></param>
        /// <param name="volumeScale"></param>
        /// <param name="fadeSeconds"></param>
        /// <param name="persist"></param>
        public async UniTask Play(string name, float volumeScale = 1f, float fadeSeconds = 0.8f, bool persist = false)
        {
            var clip = await assets.LoadAssetAsync<AudioClip>(name);
            await Play(clip, volumeScale, fadeSeconds, persist);
        }

        public async UniTask Play(AudioClip clip, float volumeScale = 1f, float fadeSeconds = 0.8f, bool persist = false)
        {
            if (clip == null) return;
            await WaitInit();

            CancelFade();
            CancelPlayWait();
            
            queue.Clear();

            var entry = new BgmContextEntry
            {
                Id            = "temp",
                Priority      = int.MaxValue,
                Playlist      = new List<AudioClip>() { clip },
                LoopPlaylist  = false,
                PlaylistIndex = 0,
                Volume        = volumeScale,
                FadeSeconds   = fadeSeconds
            };
            
            queue.Push(entry);

            playCts = new CancellationTokenSource();
            await PlayContext(entry, playCts.Token);
        }

        /// <summary>
        /// set base bgm
        /// </summary>
        /// <param name="name"></param>
        public async UniTask SetBaseBGM(string name, int priority = 0)
        {
            var clip = await assets.LoadAssetAsync<AudioClip>(name);

            if (clip == null) return;
            await this.WaitInit();
            baseBgm = new BgmContextEntry
            {
                Id            = "base",
                Priority      = priority,
                Playlist      = new List<AudioClip>() { clip },
                PlaylistIndex = 0,
                LoopPlaylist  = false,
                Volume        = 1f,
                FadeSeconds   = 1f
            };
            
            {
                queue.Push(baseBgm);
            }
        }

        /// <summary>
        /// adjust priority of a playlist
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newPriority"></param>
        public void AdjustContextPriority(string id, int newPriority)
        {
            if(!this.queue.TryGet(id, out var entry)) return;
            entry.Priority = newPriority;
            queue.Resort(entry);

            var top        = queue.Peek();
            var targetClip = top.Playlist[top.PlaylistIndex];

            // Cancel any pending play operation
            playCts?.Cancel();
            playCts?.Dispose();
            playCts = null;

            
            if (activeSource.clip != targetClip)
            {
                playCts = new CancellationTokenSource();
                PlayContext(top, playCts.Token).Forget();
            }
            else
            {
                fadeCts?.Cancel();
                fadeCts?.Dispose();
                fadeCts = new CancellationTokenSource();
                FadeVolume(activeSource, activeSource.volume, top.Volume, top.FadeSeconds, fadeCts.Token).Forget();
            }
        }

        public void UpdateVolume(float globalVolume)
        {
            FadeVolume(this.activeSource, this.activeSource.volume, globalVolume, 0.2f, CancellationToken.None).Forget();
        }

        /// <summary>
        /// create or update entry of queue
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clip"></param>
        /// <param name="priority"></param>
        /// <param name="fadeSeconds"></param>
        /// <param name="volumeScale"></param>
        /// <param name="playlist"></param>
        /// <param name="loopPlaylist"></param>
        public async UniTask PushContextBGM(
            string id, AudioClip clip, int priority,
            float fadeSeconds = 0.8f, float volumeScale = 1f,
            List<AudioClip> playlist = null, bool loopPlaylist = false)
        {
            await WaitInit();

            CancelFade();
            CancelPlayWait();
            
            List<AudioClip> list =
                playlist != null && playlist.Count > 0
                    ? playlist
                    : new List<AudioClip>() { clip };

            if (!this.queue.TryGet(id, out var entry))
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

            var top        = queue.Peek();
            var targetClip = top.Playlist[top.PlaylistIndex];

            if (activeSource.clip != targetClip)
            {
                // new clip → replay
                playCts = new CancellationTokenSource();
                await PlayContext(top, playCts.Token);
            }
            else
            {
                // same clip -> update volume
                FadeVolume(activeSource, activeSource.volume, top.Volume, top.FadeSeconds, default).Forget();
            }
        }

        /// <summary>
        /// remove entry
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fadeSeconds"></param>
        public async UniTask RemoveContextBGM(string id, float fadeSeconds = 0.8f)
        {
            if (!this.queue.TryGet(id, out var entry))
                return;

            queue.Remove(entry);

            BgmContextEntry next = queue.Count > 0 ? queue.Peek() : baseBgm;

            if (next != null)
            {
                next.FadeSeconds = fadeSeconds;
                playCts          = new CancellationTokenSource();
                await PlayContext(next, playCts.Token);
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// play playlist of entry
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="ct"></param>
        private async UniTask PlayContext(BgmContextEntry entry, CancellationToken ct)
        {
            if (entry == null || entry.Playlist == null || entry.Playlist.Count == 0)
                return;

            await WaitInit();
            CancelFade();

            var clip = entry.Playlist[entry.PlaylistIndex];

            if (activeSource.clip == clip && activeSource.isPlaying)
                return;

            await PlayPlaylist(entry, ct);
        }

        private async UniTask PlayPlaylist(BgmContextEntry entry, CancellationToken ct)
        {
            int count = entry.Playlist.Count;

            while (!ct.IsCancellationRequested)
            {
                var clip = entry.Playlist[entry.PlaylistIndex];

                await CrossFadeTo(clip, entry.FadeSeconds, entry.Volume);
                await WaitForClipEnd(activeSource, ct);

                if (count == 1 || !entry.LoopPlaylist)
                    break;

                entry.PlaylistIndex = (entry.PlaylistIndex + 1) % count;
            }
        }
        
        private async UniTask CrossFadeTo(AudioClip newClip, float fade, float volume)
        {
            fadeCts = new CancellationTokenSource();
            var ct = fadeCts.Token;

            bool hasOld = activeSource.clip != null && activeSource.isPlaying;

            if (!hasOld)
            {
                activeSource.clip   = newClip;
                activeSource.time   = 0f;
                activeSource.volume = 0f;
                activeSource.PlayLoopingMusicManaged();

                await FadeVolume(activeSource, 0f, volume, fade, ct);

                return;
            }

            // Setup next clip
            inactiveSource.clip   = newClip;
            inactiveSource.time   = 0f;
            inactiveSource.volume = 0f;
            inactiveSource.PlayLoopingMusicManaged();

            float t        = 0f;
            float startOld = activeSource.volume;

            while (t < fade)
            {
                if (ct.IsCancellationRequested) return;

                t += Time.deltaTime;
                float k            = t / fade;

                inactiveSource.volume = k * volume;
                activeSource.volume   = (1f - k) * startOld;

                await UniTask.Yield();
            }

            inactiveSource.volume = volume;

            activeSource.Stop();
            (activeSource, inactiveSource) = (inactiveSource, activeSource);
        }

        private async UniTask FadeVolume(AudioSource src, float from, float to, float duration, CancellationToken ct)
        {
            float t = 0f;

            while (t < duration)
            {
                if (ct.IsCancellationRequested) return;

                t          += Time.deltaTime;
                var globalVolume = SoundManager.MusicVolume;
                src.volume =  Mathf.Lerp(from, to, t / duration)* globalVolume;

                await UniTask.Yield();
            }

            src.volume = to;
        }

        private async UniTask WaitForClipEnd(AudioSource src, CancellationToken ct)
        {
            float length = src.clip.length;

            float t = 0f;

            while (t < length)
            {
                if (!src.isPlaying || ct.IsCancellationRequested)
                    return;

                t          += Time.deltaTime;
                await UniTask.Yield();
            }
        }
        
        public void Stop()
        {
            activeSource.Stop();
            inactiveSource.Stop();
        }

        public void Pause()
        {
            activeSource.Pause();
            inactiveSource.Pause();
        }

        public void Resume()
        {
            activeSource.UnPause();
            inactiveSource.UnPause();
        }

        public bool IsPlaying() => activeSource.isPlaying;

        public void SetTime(float time)
        {
            if (activeSource.clip != null)
                activeSource.time = Mathf.Clamp(time, 0, activeSource.clip.length);
        }

        public float GetTime() => activeSource.clip != null ? activeSource.time : 0;

        public void SetPitch(float pitch)
        {
            activeSource.pitch   = pitch;
            inactiveSource.pitch = pitch;
        }

        public void SetLoop(bool loop)
        {
            activeSource.loop   = loop;
            inactiveSource.loop = loop;
        }

        public void StopAll() => Stop();

        public void Dispose() { Stop(); }
    }
}