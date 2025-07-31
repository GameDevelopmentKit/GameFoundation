namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.DI;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using R3;
    using TheOne.Logging;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = TheOne.Logging.ILogger;

    public interface IAudioService
    {
        void        PlaySound(string        name,      AudioSource sender);
        void        PlaySound(string        name,      bool        isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool isPausePlaylist = false);
        void        PlaySound(AudioClip     audioClip, bool        isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool isPausePlaylist = false);
        AudioSource GetLoopingSound(string  name);
        void        StopLoopingSound(string name);
        void        StopAllSound();
        void        StopAll();
        void        PlayPlayList(string    musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, float fadeProgressThreshold = 0f, bool persist = false);
        void        PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, float fadeProgressThreshold = 0f, bool persist = false);
        void        StopPlayList(float     fadeSeconds = 1f);
        void        SetPlayListTime(float  time);
        float       GetPlayListTime();
        void        SetPlayListPitch(float pitch);
        void        SetPlayListLoop(bool   isLoop);
        void        PausePlayList();
        void        ResumePlayList();
        bool        IsPlayingPlayList();
        void        StopAllPlayList();
        void        PauseEverything();
        void        ResumeEverything();
    }

    public class AudioService : IAudioService, IInitializable, IDisposable
    {
        public static string       AudioSourceKey = "AudioSource";
        public static AudioService Instance { get; private set; }

        private readonly SignalBus         signalBus;
        private readonly SoundSetting      soundSetting;
        private readonly IGameAssets       gameAssets;
        private readonly ObjectPoolManager objectPoolManager;
        private readonly ILogger           logger;

        private CompositeDisposable             compositeDisposable;
        private Dictionary<string, AudioSource> loopingSoundNameToSources = new();
        private AudioSource                     MusicAudioSource;

        [Preserve]
        public AudioService(
            SignalBus         signalBus,
            SoundSetting      soundSetting,
            IGameAssets       gameAssets,
            ObjectPoolManager objectPoolManager,
            ILoggerManager    loggerManager
        )
        {
            this.signalBus         = signalBus;
            this.soundSetting      = soundSetting;
            this.gameAssets        = gameAssets;
            this.objectPoolManager = objectPoolManager;
            this.logger            = loggerManager.GetLogger(this);
            Instance               = this;
        }

        public void Initialize()
        {
            this.signalBus.Subscribe<UserDataLoadedSignal>(this.SubscribeMasterAudio);
        }

        private void SubscribeMasterAudio()
        {
            this.compositeDisposable = new()
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

        public virtual async void PlaySound(string name, AudioSource sender)
        {
            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(name);
            sender.PlayOneShotSoundManaged(audioClip);
        }

        public virtual async void PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool isPausePlaylist = false)
        {
            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(name);
            this.PlaySound(audioClip, isLoop, volumeScale, fadeSeconds, isAverage, name, isPausePlaylist);
        }

        public virtual void PlaySound(AudioClip audioClip, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false, bool isPausePlaylist = false)
        {
            this.PlaySound(audioClip, isLoop, volumeScale, fadeSeconds, isAverage, audioClip.name, isPausePlaylist);
        }

        private async void PlaySound(AudioClip audioClip, bool isLoop, float volumeScale, float fadeSeconds, bool isAverage, string name, bool isPausePlaylist = false)
        {
            var audioSource = await this.GetAudioSource();
            if (isLoop)
            {
                if (this.loopingSoundNameToSources.ContainsKey(name))
                {
                    this.logger.Warning($"You already played  looping - {name}!!!!, do you want to play it again?");
                    return;
                }

                audioSource.clip = audioClip;
                audioSource.PlayLoopingSoundManaged(volumeScale, fadeSeconds);
                this.loopingSoundNameToSources.Add(name, audioSource);
            }
            else
            {
                if (isPausePlaylist) this.PausePlayList();
                audioSource.PlayOneShotSoundManaged(audioClip, volumeScale, isAverage);
                await UniTask.Delay(TimeSpan.FromSeconds(audioClip.length));
                audioSource.Recycle();
                if (isPausePlaylist) this.ResumePlayList();
            }
        }

        public AudioSource GetLoopingSound(string name) => this.loopingSoundNameToSources.GetValueOrDefault(name);

        public void StopLoopingSound(string name)
        {
            var audioSource = this.GetLoopingSound(name);
            if (!audioSource) return;

            audioSource.StopLoopingSoundManaged();
            audioSource.gameObject.Recycle();
            this.loopingSoundNameToSources.Remove(name);
        }

        public void StopAllSound()
        {
            SoundManager.StopAllLoopingSounds();
            SoundManager.StopAllNonLoopingSounds();

            foreach (var audioSource in this.loopingSoundNameToSources.Values) audioSource.gameObject.Recycle();

            this.loopingSoundNameToSources.Clear();
        }

        public void StopAll()
        {
            this.StopAllSound();
            this.StopAllPlayList();
        }

        /// <summary>
        /// Play a music track and loop it until stopped, using the global music volume as a modifier
        /// </summary>
        /// <param name="audioClip">Audio clip to play</param>
        /// <param name="random">Whether to play a random track</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">The number of seconds to fade in and out</param>
        /// <param name="fadeProgressThreshold">The percent to fade in and out</param>
        /// <param name="persist">Whether to persist the looping music between scene changes</param>
        public virtual async void PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, float fadeProgressThreshold = 0f, bool persist = false)
        {
            this.StopPlayList(fadeSeconds);

            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;

            var delayTime = fadeSeconds * Mathf.Clamp01(fadeProgressThreshold / 100f);
            if (delayTime > 0f) await UniTask.Delay(TimeSpan.FromSeconds(delayTime));

            this.MusicAudioSource.PlayLoopingMusicManaged(volumeScale, fadeSeconds, persist);
        }

        public virtual async void PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, float fadeProgressThreshold = 0f, bool persist = false)
        {
            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(musicName);
            this.PlayPlayList(audioClip, random, volumeScale, fadeSeconds, fadeProgressThreshold, persist);
        }

        /// <summary>
        /// Stop play list music
        /// </summary>
        /// <param name="fadeSeconds">Fade time to turn off</param>
        public void StopPlayList(float fadeSeconds = 1f)
        {
            var audioSource = this.MusicAudioSource;
            if (audioSource == null) return;
            audioSource.DOFade(0f, fadeSeconds).OnComplete(() =>
            {
                audioSource.StopLoopingMusicManaged();
                audioSource.clip = null;
                audioSource.Recycle();
            });
            this.MusicAudioSource = null;
        }

        public void SetPlayListTime(float time)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.time = time;
        }

        /// <summary>
        /// Get playlist time
        /// </summary>
        /// <returns>Return playlist time, -1 if no playlist is playing</returns>
        public float GetPlayListTime()
        {
            if (this.MusicAudioSource == null) return -1f;
            return this.MusicAudioSource.time;
        }

        public void SetPlayListPitch(float pitch)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.pitch = pitch;
        }

        public void SetPlayListLoop(bool isLoop)
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.loop = isLoop;
        }

        public void PausePlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.Pause();
        }

        public void ResumePlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.Play();
        }

        public bool IsPlayingPlayList()
        {
            if (this.MusicAudioSource == null) return false;
            return this.MusicAudioSource.isPlaying;
        }

        public void StopAllPlayList()
        {
            this.StopPlayList();
        }

        public void PauseEverything()
        {
            SoundManager.PauseAll();
            AudioListener.pause = true;
        }

        public void ResumeEverything()
        {
            AudioListener.pause = false;
            SoundManager.ResumeAll();
        }

        protected virtual void SetSoundValue(float value)
        {
            SoundManager.SoundVolume = value;
        }

        protected virtual void SetMusicValue(float value)
        {
            SoundManager.MusicVolume = value;
        }

        public void Dispose()
        {
            this.compositeDisposable?.Dispose();
        }
    }
}