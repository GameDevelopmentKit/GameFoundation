namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using R3;
    using UnityEngine;
    using Zenject;

    public interface IAudioService
    {
        UniTask PlaySound(string name, AudioSource sender);
        UniTask PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool autoUnload = true);
        UniTask PlaySoundFrequency(string name, float frequency, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool autoUnload = true);
        void    StopSoundFrequency(string name);
        void    StopSound(string name);
        void    StopAllSound();
        void    StopAllFrequencySound();
        void    StopAll();
        UniTask PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false, bool autoUnload = true);
        UniTask PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false);
        void    StopPlayList();
        void    SetPlayListTime(float time);
        float   GetPlayListTime();
        void    SetPlayListPitch(float pitch);
        void    SetPlayListLoop(bool isLoop);
        void    PausePlayList();
        void    ResumePlayList();
        bool    IsPlayingPlayList();
        void    StopAllPlayList();
        void    PauseEverything();
        void    ResumeEverything();
        void    PauseSound(string s);
        void    ResumeSound(string s);
    }

    public class AudioService : IAudioService, IInitializable, IDisposable
    {
        public static string       AudioSourceKey = "AudioSource";
        public static AudioService Instance { get; private set; }

        private readonly ISignalBus        signalBus;
        private readonly SoundSetting      soundSetting;
        private readonly IGameAssets       gameAssets;
        private readonly ObjectPoolManager objectPoolManager;
        private readonly ILogService       logService;

        private CompositeDisposable                         compositeDisposable;
        private Dictionary<string, AudioSource>             loopingSoundNameToSources = new();
        private Dictionary<string, CancellationTokenSource> frequencySoundDic         = new();
        private AudioSource                                 MusicAudioSource;

        public AudioService(
            ISignalBus signalBus,
            SoundSetting SoundSetting,
            IGameAssets gameAssets,
            ObjectPoolManager objectPoolManager,
            ILogService logService
        )
        {
            this.signalBus         = signalBus;
            this.soundSetting      = SoundSetting;
            this.gameAssets        = gameAssets;
            this.objectPoolManager = objectPoolManager;
            this.logService        = logService;
            Instance               = this;
        }

        public void Initialize() { this.signalBus.Subscribe<UserDataLoadedSignal>(this.SubscribeMasterAudio); }

        private void SubscribeMasterAudio()
        {
            this.compositeDisposable = new CompositeDisposable
            {
                this.soundSetting.MusicValue.Subscribe(this.SetMusicValue),
                this.soundSetting.SoundValue.Subscribe(this.SetSoundValue),
            };

            SoundManager.MusicVolume = this.soundSetting.MusicValue.Value;
            SoundManager.SoundVolume = this.soundSetting.SoundValue.Value;
        }

        private async UniTask<AudioSource> GetAudioSource()
        {
            var audioSource = await this.objectPoolManager.Spawn<AudioSource>(AudioSourceKey);
            audioSource.clip   = null;
            audioSource.volume = 1;

            return audioSource;
        }

        public virtual async UniTask PlaySound(string name, AudioSource sender)
        {
            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(name);
            sender.PlayOneShotSoundManaged(audioClip);
        }

        public virtual async UniTask PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool autoUnload = true)
        {
            var audioClip   = await this.gameAssets.LoadAssetAsync<AudioClip>(name, isAutoUnload: autoUnload);
            var audioSource = await this.GetAudioSource();

            if (isLoop)
            {
                if (this.loopingSoundNameToSources.ContainsKey(name))
                {
                    this.logService.Warning($"You already played  looping - {name}!!!!, do you want to play it again?");

                    return;
                }

                audioSource.clip = audioClip;
                audioSource.PlayLoopingSoundManaged(volumeScale, fadeSeconds);
                this.loopingSoundNameToSources.Add(name, audioSource);
            }
            else
            {
                audioSource.PlayOneShotSoundManaged(audioClip, volumeScale);
                await UniTask.Delay(TimeSpan.FromSeconds(audioClip.length) + TimeSpan.FromSeconds(0.5f));
                this.RecycleAudioSource(audioSource);
            }
        }

        public async UniTask PlaySoundFrequency(
            string name,
            float frequency,
            float volumeScale = 1,
            float fadeSeconds = 1,
            bool isAverage = false,
            bool autoUnload = true)
        {
            if (!this.frequencySoundDic.TryGetValue(name, out var token))
            {
                token = new CancellationTokenSource();
                this.frequencySoundDic.Add(name, token);
            }
            else
            {
                token.Cancel();
                token                        = new CancellationTokenSource();
                this.frequencySoundDic[name] = token;
            }

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await this.PlaySound(
                        name,
                        isLoop: false,
                        volumeScale: volumeScale,
                        fadeSeconds: fadeSeconds,
                        isAverage: isAverage,
                        autoUnload: autoUnload
                    );

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(frequency),
                        cancellationToken: token.Token
                    );
                }
            }
            catch (OperationCanceledException)
            {
                this.logService.Log($"[Audio] Frequency sound '{name}' cancelled.");
            }
            finally
            {
                if (this.frequencySoundDic.TryGetValue(name, out var current)
                    && current == token)
                {
                    this.frequencySoundDic.Remove(name);
                }
            }
        }

        public void StopSoundFrequency(string name) { this.StopFrequencySound(name); }

        private void StopFrequencySound(string name)
        {
            if (this.frequencySoundDic.TryGetValue(name, out var token))
            {
                token.Cancel();
                this.frequencySoundDic.Remove(name);
            }
        }

        public void StopAllFrequencySound()
        {
            foreach (var token in this.frequencySoundDic.Values)
            {
                token.Cancel();
            }

            this.frequencySoundDic.Clear();
        }

        public void StopSound(string name)
        {
            var audioSource = this.loopingSoundNameToSources.GetValueOrDefault(name);
            SoundManager.StopOneShotSound(name);

            if (audioSource == null)
            {
                return;
            }

            audioSource.StopLoopingSoundManaged();
            this.loopingSoundNameToSources.Remove(name);
            this.RecycleAudioSource(audioSource);
        }

        public virtual void StopAllSound()
        {
            SoundManager.StopAllLoopingSounds();
            SoundManager.StopAllNonLoopingSounds();
            SoundManager.StopAllOneShotSound();

            foreach (var audioSource in this.loopingSoundNameToSources.Values)
            {
                this.RecycleAudioSource(audioSource);
            }

            this.StopAllFrequencySound();

            this.frequencySoundDic.Clear();
            this.loopingSoundNameToSources.Clear();
        }

        public virtual void StopAll()
        {
            this.StopAllSound();
            this.StopAllPlayList();
            this.StopAllFrequencySound();
        }

        /// <summary>
        /// Play a music track and loop it until stopped, using the global music volume as a modifier
        /// </summary>
        /// <param name="source">Audio source to play</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">The number of seconds to fade in and out</param>
        /// <param name="persist">Whether to persist the looping music between scene changes</param>
        public virtual async UniTask PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false, bool autoUnload = true)
        {
            this.StopPlayList();

            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(musicName, isAutoUnload: autoUnload);
            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;
            this.MusicAudioSource.PlayLoopingMusicManaged(volumeScale, fadeSeconds, persist);
        }

        public virtual async UniTask PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false)
        {
            this.StopPlayList();

            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;
            this.MusicAudioSource.PlayLoopingMusicManaged(volumeScale, fadeSeconds, persist);
        }

        public virtual void StopPlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.StopLoopingMusicManaged();
            this.RecycleAudioSource(this.MusicAudioSource);
            this.MusicAudioSource = null;
        }

        public virtual void SetPlayListTime(float time)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.time = time;
        }

        /// <summary>
        /// Get playlist time
        /// </summary>
        /// <returns>Return playlist time, -1 if no playlist is playing</returns>
        public virtual float GetPlayListTime()
        {
            if (this.MusicAudioSource == null) return -1f;

            return this.MusicAudioSource.time;
        }

        public virtual void SetPlayListPitch(float pitch)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.pitch = pitch;
        }

        public virtual void SetPlayListLoop(bool isLoop)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.loop = isLoop;
        }

        public virtual void PausePlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.Pause();
        }

        public virtual void ResumePlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.UnPause();
        }

        public virtual bool IsPlayingPlayList()
        {
            if (this.MusicAudioSource == null) return false;

            return this.MusicAudioSource.isPlaying;
        }

        public virtual void StopAllPlayList() { this.StopPlayList(); }

        public virtual void PauseEverything()
        {
            SoundManager.PauseAll();
            AudioListener.pause = true;
        }

        public virtual void ResumeEverything()
        {
            AudioListener.pause = false;
            SoundManager.ResumeAll();
        }

        public void PauseSound(string s)
        {
            var audioSource = this.loopingSoundNameToSources.GetValueOrDefault(s);

            if (audioSource == null) return;
            audioSource.Pause();
        }

        public void ResumeSound(string s)
        {
            var audioSource = this.loopingSoundNameToSources.GetValueOrDefault(s);

            if (audioSource == null) return;
            audioSource.UnPause();
        }

        protected virtual void SetSoundValue(float value) { SoundManager.SoundVolume = value; }

        protected virtual void SetMusicValue(float value) { SoundManager.MusicVolume = value; }

        public void Dispose()
        {
            this.compositeDisposable?.Dispose();
            Instance = null;
        }

        private async UniTask RecycleAudioSource(AudioSource audioSource)
        {
            if (!audioSource) return;

            audioSource.clip   = null;
            audioSource.volume = 1;
            audioSource.loop   = false;
            await UniTask.Delay(100);
            audioSource.gameObject.Recycle();
        }
    }
}