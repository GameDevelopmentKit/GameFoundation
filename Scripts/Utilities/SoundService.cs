namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using GameFoundation.Scripts.Utilities.UserData;
    using UniRx;
    using UnityEngine;
    using Zenject;

    public interface IAudioService
    {
        void PlaySound(string    name, bool isLoop = false);
        void StopAllSound(string name);
        void PlayPlayList(string playlist, bool random = false);
        void StopPlayList(string playlist);
        void StopAllPlayList();
        void PauseEverything();
        void ResumeEverything();
    }

    public class AudioService : IAudioService, IInitializable, IDisposable
    {
        public static AudioService Instance { get; private set; }

        private readonly SignalBus         signalBus;
        private readonly SoundSetting      soundSetting;
        private readonly IGameAssets       gameAssets;
        private readonly ObjectPoolManager objectPoolManager;
        
        private CompositeDisposable compositeDisposable;

        public AudioService(SignalBus signalBus, SoundSetting SoundSetting, IGameAssets gameAssets, ObjectPoolManager objectPoolManager)
        {
            this.signalBus         = signalBus;
            this.soundSetting      = SoundSetting;
            this.gameAssets        = gameAssets;
            this.objectPoolManager = objectPoolManager;
            Instance               = this;
        }

        public void Initialize()
        {
            this.signalBus.Subscribe<UserDataLoadedSignal>(this.SubscribeMasterAudio);
        }

        private async void SubscribeMasterAudio()
        {
            this.soundSetting.MuteSound.Value = false;
            this.soundSetting.MuteMusic.Value = false;

            this.compositeDisposable = new CompositeDisposable
                                       {
                                           //TODO uncomment this when we have a proper solution
                                           // this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Subscribe(this.CheckToMuteMusic),
                                           // this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Subscribe(this.CheckToMuteSound),
                                           this.soundSetting.MusicValue.Subscribe(this.SetMusicValue),
                                           this.soundSetting.SoundValue.Subscribe(this.SetSoundValue),
                                           this.soundSetting.MasterVolume.Subscribe(this.SetMasterVolume)
                                       };
        }

        private void SetMasterVolume(bool value)
        {
            var finalValue = value ? 1 : 0;
            SoundManager.MusicVolume = finalValue;
            SoundManager.SoundVolume = finalValue;
        }

        private UniTask<AudioSource> GetAudioSource() => this.objectPoolManager.Spawn<AudioSource>("AudioSource");

        public virtual async void PlaySound(string name, bool isLoop = false)
        {
            var audioClip   = await this.gameAssets.LoadAssetAsync<AudioClip>(name);
            var audioSource = await this.GetAudioSource();
            audioSource.PlayOneShotMusicManaged(audioClip);
            await UniTask.Delay(TimeSpan.FromSeconds(audioClip.length));
            audioSource.Recycle();
        }

        public void StopAllSound(string name)
        {
            SoundManager.PauseAll();
        }

        private Dictionary<string, AudioSource> PlayListToAudioSource { get; set; } = new();
        public virtual async void PlayPlayList(string playlist, bool random = false)
        {
            if(this.PlayListToAudioSource.ContainsKey(playlist)) return;
            
            var audioClip   = await this.gameAssets.LoadAssetAsync<AudioClip>(playlist);
            var audioSource = await this.GetAudioSource();
            audioSource.clip = audioClip;
            this.PlayListToAudioSource.Add(playlist, audioSource);
            audioSource.PlayLoopingMusicManaged();
        }

        public void StopPlayList(string playlist)
        {
            SoundManager.StopLoopingMusic(this.PlayListToAudioSource[playlist]);
        }

        public void StopAllPlayList()
        {
            foreach (var audioSource in this.PlayListToAudioSource.Values)
            {
                SoundManager.StopLoopingMusic(audioSource);
            }
        }

        public void PauseEverything()
        {
            SoundManager.PauseAll();
        }
        public void ResumeEverything()
        {
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
            this.compositeDisposable.Dispose();
        }
    }
}