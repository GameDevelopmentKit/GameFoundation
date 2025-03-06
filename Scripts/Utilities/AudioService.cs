namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.DI;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using R3;
    using UnityEngine;
    using UnityEngine.Scripting;

    public interface IAudioService
    {
        void PlaySound(string name, AudioSource sender);

        void PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false);

        void PlayAudioClip(AudioClip audioClip, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false);

        void StopAllSound();

        void StopAll();

        void PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false);

        void PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false);

        void StopPlayList();

        void SetPlayListTime(float time);

        float GetPlayListTime();

        void SetPlayListPitch(float pitch);

        void SetPlayListLoop(bool isLoop);

        void PausePlayList();

        void ResumePlayList();

        bool IsPlayingPlayList();

        void StopAllPlayList();

        void PauseEverything();

        void ResumeEverything();
    }

    public class AudioService : IAudioService, IInitializable, IDisposable
    {
        public static string       AudioSourceKey = "AudioSource";
        public static AudioService Instance { get; private set; }

        private readonly SignalBus         signalBus;
        private readonly SoundSetting      soundSetting;
        private readonly IGameAssets       gameAssets;
        private readonly ObjectPoolManager objectPoolManager;
        private readonly ILogService       logService;

        private CompositeDisposable             compositeDisposable;
        private Dictionary<string, AudioSource> loopingSoundNameToSources = new();
        private AudioSource                     MusicAudioSource;

        [Preserve]
        public AudioService(
            SignalBus signalBus,
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

        public virtual async void PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false)
        {
            var audioClip   = await this.gameAssets.LoadAssetAsync<AudioClip>(name);
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
                audioSource.PlayOneShotSoundManaged(audioClip);
                await UniTask.Delay(TimeSpan.FromSeconds(audioClip.length));
                audioSource.Recycle();
            }
        }

        public async void PlayAudioClip(AudioClip audioClip, bool isLoop = false, float volumeScale = 1, float fadeSeconds = 1, bool isAverage = false)
        {
            var audioSource = await this.GetAudioSource();

            if (isLoop)
            {
                audioSource.clip = audioClip;
                audioSource.PlayLoopingSoundManaged(volumeScale, fadeSeconds);
            }
            else
            {
                audioSource.PlayOneShotSoundManaged(audioClip);
                await UniTask.Delay(TimeSpan.FromSeconds(audioClip.length));
                audioSource.Recycle();
            }
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
        /// <param name="source">Audio source to play</param>
        /// <param name="volumeScale">Additional volume scale</param>
        /// <param name="fadeSeconds">The number of seconds to fade in and out</param>
        /// <param name="persist">Whether to persist the looping music between scene changes</param>
        public virtual async void PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false)
        {
            this.StopPlayList();

            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(musicName);
            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;
            this.MusicAudioSource.PlayLoopingMusicManaged(volumeScale, fadeSeconds, persist);
        }

        public virtual async void PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false)
        {
            this.StopPlayList();

            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;
            this.MusicAudioSource.PlayLoopingMusicManaged(volumeScale, fadeSeconds, persist);
        }

        public void StopPlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.StopLoopingMusicManaged();
            this.MusicAudioSource.clip = null;
            this.MusicAudioSource.Recycle();
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

        public void StopAllPlayList() { this.StopPlayList(); }

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

        protected virtual void SetSoundValue(float value) { SoundManager.SoundVolume = value; }

        protected virtual void SetMusicValue(float value) { SoundManager.MusicVolume = value; }

        public void Dispose() { this.compositeDisposable?.Dispose(); }
    }
}