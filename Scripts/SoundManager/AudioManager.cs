namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using DataManager.MasterData;
    using DataManager.UserData;

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
        void  PlaySound(AudioClip clip, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false);
        void  StopSound(string name);
        void  StopAllSound();
        void  StopAll();

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
        UniTask SetBaseBGM(string name, int priority = 0);
        UniTask AdjustContextPriority(string id, int newPriority);

        void  SetSoundValue(float value);
        void  SetMusicValue(float value);
        float SoundVolume { get; }
        float MusicVolume { get; }
    }

    public class AudioManager : BaseDataManager<SoundSetting>, IAudioManager
    {
        public static string       AudioSourceKey = "AudioSource";
        public static AudioManager Instance { get; private set; }

        private readonly IGameAssets          gameAssets;
        private readonly SoundEffectManager   sfx;
        private readonly MusicPlaylistManager music;

        public AudioManager(
            SignalBus signalBus,
            IGameAssets gameAssets,
            SoundEffectManager sfx,
            MusicPlaylistManager music
        ) : base(signalBus)
        {
            this.gameAssets = gameAssets;
            this.sfx        = sfx;
            this.music      = music;
            Instance        = this;
        }

        public override void OnDataInitialized()
        {
            this.music.UpdateVolume(this.MusicVolume);
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
            // Debug.Log("Using volume: " + Data.SoundValue.Value);
            if (isLoop)
                sfx.PlayLoop(name, volumeScale * SoundVolume, fadeSeconds).Forget();
            else
                sfx.PlayOneShot(name, volumeScale * SoundVolume).Forget();
        }

        public void PlaySound(AudioClip clip, bool isLoop = false, float volumeScale = 1f, float fadeSeconds = 1f, bool isAverage = false)
        {
            if (clip != null)
                sfx.PlayOneShot(clip, volumeScale * SoundVolume).Forget();
        }

        public void PlaySound(AudioClip clip, float pitch)
        {
            if (clip != null)
                sfx.PlayOneShot(clip, pitch, SoundVolume).Forget();
        }

        public void StopSound(string name) => sfx.StopLoop(name);

        public void StopAllSound() => sfx.StopAll();

        public void StopAll()
        {
            StopAllSound();
            StopAllPlayList();
        }

        #region Playlist / Music

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
            this.music.Pause();
            this.sfx.StopAll();
            AudioListener.pause = true;
        }

        public void ResumeEverything()
        {
            AudioListener.pause = false;
            this.music.Resume();
        }
        #endregion

        #region Context BGM
        public async UniTask PushContextBGM(string name, int priority, float fade = 1f, float volume = 1f)
        {
            var clip = await gameAssets.LoadAssetAsync<AudioClip>(name).ToUniTask();

            if (clip == null) return;

            await PushContextBGM(clip, priority, fade, volume * this.MusicVolume, name);
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

        public UniTask SetBaseBGM(string name, int priority = 0) => music.SetBaseBGM(name, priority);

        public UniTask AdjustContextPriority(string id, int newPriority)
        {
            music.AdjustContextPriority(id, newPriority);

            return UniTask.CompletedTask;
        }
        #endregion

        #region Sound Settings
        public float SoundVolume => Data != null ? Data.SoundValue.Value : 0f;

        public float MusicVolume => Data != null ? Data.MusicValue.Value : 0f;

        public void SetSoundValue(float value)
        {
            this.Data.SoundValue.Value = value;
        }

        public void SetMusicValue(float value)
        {
            Data.MusicValue.Value = value;
            this.music.UpdateVolume(value);
        }
        #endregion
    }
}
