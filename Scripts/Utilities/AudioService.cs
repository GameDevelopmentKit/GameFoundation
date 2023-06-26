namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using UniRx;
    using UnityEngine;
    using Zenject;

    public interface IAudioService
    {
        void PlaySound(string name, bool isLoop = false);
        void StopAllSound();
        void StopAll();
        void PlayPlayList(string musicName, bool random = false);
        void StopPlayList();
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

        public AudioService(SignalBus signalBus, SoundSetting SoundSetting, IGameAssets gameAssets, ObjectPoolManager objectPoolManager, ILogService logService)
        {
            this.signalBus         = signalBus;
            this.soundSetting      = SoundSetting;
            this.gameAssets        = gameAssets;
            this.objectPoolManager = objectPoolManager;
            this.logService        = logService;
            Instance               = this;
        }

        public void Initialize() { this.signalBus.Subscribe<UserDataLoadedSignal>(this.SubscribeMasterAudio); }

        private async void SubscribeMasterAudio()
        {
            this.compositeDisposable = new CompositeDisposable
            {
                this.soundSetting.MusicValue.Subscribe(this.SetMusicValue),
                this.soundSetting.SoundValue.Subscribe(this.SetSoundValue),
            };
            SoundManager.MusicVolume = this.soundSetting.MusicValue.Value;
            SoundManager.SoundVolume = this.soundSetting.SoundValue.Value;
        }

        private UniTask<AudioSource> GetAudioSource() => this.objectPoolManager.Spawn<AudioSource>(AudioSourceKey);

        public virtual async void PlaySound(string name, bool isLoop = false)
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
                audioSource.PlayLoopingSoundManaged();
                this.loopingSoundNameToSources.Add(name, audioSource);
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

            foreach (var audioSource in this.loopingSoundNameToSources.Values)
            {
                audioSource.gameObject.Recycle();
            }

            this.loopingSoundNameToSources.Clear();
        }

        public void StopAll()
        {
            this.StopAllSound();
            this.StopAllPlayList();
        }

        public virtual async void PlayPlayList(string musicName, bool random = false)
        {
            this.StopPlayList();

            var audioClip = await this.gameAssets.LoadAssetAsync<AudioClip>(musicName);
            this.MusicAudioSource      = await this.GetAudioSource();
            this.MusicAudioSource.clip = audioClip;
            this.MusicAudioSource.PlayLoopingMusicManaged();
        }

        public void StopPlayList()
        {
            if (this.MusicAudioSource == null) return;
            this.MusicAudioSource.StopLoopingMusicManaged();
            this.MusicAudioSource.Recycle();
            this.MusicAudioSource = null;
        }

        public void StopAllPlayList()
        {
            this.StopPlayList();
        }

        public void PauseEverything()  { SoundManager.PauseAll(); }
        public void ResumeEverything() { SoundManager.ResumeAll(); }

        protected virtual void SetSoundValue(float value) { SoundManager.SoundVolume = value; }

        protected virtual void SetMusicValue(float value) { SoundManager.MusicVolume = value; }

        public void Dispose() { this.compositeDisposable.Dispose(); }
    }
}