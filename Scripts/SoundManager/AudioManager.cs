namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using DataManager.MasterData;
    using DigitalRuby.SoundManagerNamespace;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Models;
    using SoundManager;
    using UniRx;
    using UnityEngine;
    using Zenject;

    public interface IAudioManager
    {
        void  PlaySound(string name, AudioSource sender);
        void  PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false);
        void  StopSound(string name);
        void  StopAllSound();
        void  StopAll();
        void  PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false);
        void  PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false);
        void  StopPlayList();
        void  SetPlayListTime(float time);
        float GetPlayListTime();
        void  SetPlayListPitch(float pitch);
        void  SetPlayListLoop(bool isLoop);
        void  PausePlayList();
        void  ResumePlayList();
        bool  IsPlayingPlayList();
        void  StopAllPlayList();
        void  PauseEverything();
        void  ResumeEverything();

        UniTask PushContextBGM(string name, int priority, float fade = 1f, float volume = 1f);

        //UniTask RestoreBGM();
        UniTask SetBaseBGM(string name, int priority =0);
        UniTask AdjustContextPriority(string id, int newPriority);
    }

    public class AudioManager : IAudioManager, IInitializable, IDisposable
    {
        public static string       AudioSourceKey = "AudioSource";
        public static AudioManager Instance { get; private set; }

        private readonly SignalBus            signalBus;
        private readonly SoundSetting         soundSetting;
        private readonly IGameAssets          gameAssets;
        private readonly SoundEffectManager   sfx;
        private readonly MusicPlaylistManager music;

        private CompositeDisposable compositeDisposable;

        public AudioManager(
            SignalBus signalBus,
            SoundSetting soundSetting,
            IGameAssets gameAssets,
            SoundEffectManager sfx,
            MusicPlaylistManager music
        )
        {
            this.signalBus    = signalBus;
            this.soundSetting = soundSetting;
            this.gameAssets   = gameAssets;
            this.sfx          = sfx;
            this.music        = music;
            Instance          = this;
        }

        public void Initialize() { this.signalBus.Subscribe<MasterDataReadySignal>(this.SubscribeMasterAudio); }

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

        public void PlaySound(string name, AudioSource sender)
        {
            UniTask.Void(async () =>
            {
                var clip = await gameAssets.LoadAssetAsync<AudioClip>(name).ToUniTask();
                if (clip != null) sender.PlayOneShot(clip);
            });
        }

        public void PlaySound(string name, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false)
        {
            if (isLoop)
                sfx.PlayLoop(name, volumeScale, fadeSeconds).Forget();
            else
                sfx.PlayOneShot(name, volumeScale).Forget();
        }

        public void PlaySound(AudioClip clip, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false)
        {
            if (clip != null)
                sfx.PlayOneShot(clip, volumeScale).Forget();
        }

        public void PlaySound(AudioClip clip, float pitch)
        {
            if (clip != null)
                sfx.PlayOneShot(clip, pitch, 1f).Forget();
        }

        public void StopSound(string name) => sfx.StopLoop(name);

        public void StopAllSound() => sfx.StopAll();

        public void StopAll()
        {
            StopAllSound();
            StopAllPlayList();
        }

        #region Playlist / Music

        public void PlayPlayList(string musicName, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false)
        {
            music.Play(musicName, volumeScale, fadeSeconds, persist).Forget();
        }

        public void PlayPlayList(AudioClip audioClip, bool random = false, float volumeScale = 1f, float fadeSeconds = 1f, bool persist = false)
        {
            if (audioClip != null)
                music.Play(audioClip, volumeScale, fadeSeconds, persist).Forget();
        }

        public void StopPlayList() => music.Stop();

        public void SetPlayListTime(float time) => music.SetTime(time);

        public float GetPlayListTime() => music.GetTime();

        public void SetPlayListPitch(float pitch) => music.SetPitch(pitch);

        public void SetPlayListLoop(bool isLoop) => music.SetLoop(isLoop);

        public void PausePlayList() => music.Pause();

        public void ResumePlayList() => music.Resume();

        public bool IsPlayingPlayList() => music.IsPlaying();

        public void StopAllPlayList() => StopPlayList();

        #endregion

        #region Everything

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

        #endregion

        #region Context BGM

        public async UniTask PushContextBGM(string name, int priority, float fade = 1f, float volume = 1f)
        {
            var clip = await gameAssets.LoadAssetAsync<AudioClip>(name).ToUniTask();

            if (clip == null) return;

            await PushContextBGM(clip, priority, fade, volume, name);
        }

        public async UniTask PushContextBGM(AudioClip clip, int priority, float fade = 1f, float volume = 1f, string name = "")
        {
            if (clip == null) return;

            await music.PushContextBGM(
                name,
                clip: clip,
                priority: priority, // context BGM priority
                fadeSeconds: fade,
                volumeScale: volume
            );
        }

        // public async UniTask RestoreBGM()
        // {
        //     // update the priority to back to last bgm
        // }

        public UniTask SetBaseBGM(string name, int priority =0) => music.SetBaseBGM(name, priority);

        public UniTask AdjustContextPriority(string id, int newPriority)
        {
            music.AdjustContextPriority(id, newPriority);

            return UniTask.CompletedTask;
        }

        #endregion

        #region Sound Settings

        protected void SetSoundValue(float value) => SoundManager.SoundVolume = value;

        protected void SetMusicValue(float value) => SoundManager.MusicVolume = value;

        #endregion

        public void Dispose() => compositeDisposable?.Dispose();
    }
}