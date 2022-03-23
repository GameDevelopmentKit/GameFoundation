namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.GameManager;
    using UnityEngine;
    using Zenject;
    using UniRx;

    public interface IMechSoundManager
    {
        void PlaySound(string name);
        void PlayPlayList(string playlist, bool random = false);
        void StopPlaylist(string playlist);
        void StopAllPlaylist();
        void MutePlaylist(string playlist);
        void MuteAllPlaylist();
        void SetVolumePlaylist(float value);
        void MuteSound(bool value);
        void MuteMusic(bool value);
        void SetSoundValue(float value);
        void SetMusicValue(float value);
    }

    public class MasterMechSoundManager : IMechSoundManager, IInitializable, IDisposable
    {
        private readonly PlaylistController      playlistController;
        private readonly GameFoundationLocalData gameFoundationLocalData;
        public static    MasterMechSoundManager  Instance { get; private set; }
        private          CompositeDisposable     compositeDisposable;

        public MasterMechSoundManager(PlaylistController playlistController, GameFoundationLocalData gameFoundationLocalData)
        {
            this.playlistController      = playlistController;
            this.gameFoundationLocalData = gameFoundationLocalData;
            Instance                     = this;
        }

        public void Initialize() { this.SubscribeMasterAudio(); }

        private async void SubscribeMasterAudio()
        {
            await UniTask.WaitUntil(() => this.playlistController.ControllerIsReady);
            this.compositeDisposable = new CompositeDisposable
            {
                this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Subscribe(this.MuteMusic),
                this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Subscribe(this.MuteSound),
                this.gameFoundationLocalData.IndexSettingRecord.MusicValue.Subscribe(this.SetMusicValue),
                this.gameFoundationLocalData.IndexSettingRecord.SoundValue.Subscribe(this.SetSoundValue)
            };
        }

        public virtual void PlaySound(string name)
        {
            if (this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Value) return;
            MasterAudio.PlaySound(name);
        }

        public virtual void PlayPlayList(string playlist, bool random = false)
        {
            if (this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Value) return;
            this.playlistController.isShuffle = random;
            MasterAudio.StartPlaylist(playlist);
        }

        public virtual void StopPlaylist(string playlist) { MasterAudio.StopPlaylist(playlist); }

        public virtual void MutePlaylist(string playlist) { MasterAudio.MutePlaylist(); }

        public virtual void StopAllPlaylist() { MasterAudio.StopAllPlaylists(); }

        public virtual void MuteAllPlaylist() { MasterAudio.MuteAllPlaylists(); }

        public virtual void SetVolumePlaylist(float value) { MasterAudio.PlaylistMasterVolume = value; }

        public virtual void MuteSound(bool value) { }

        public virtual void MuteMusic(bool value)
        {
            if (value)
            {
                MasterAudio.PauseAllPlaylists();
            }
            else
            {
                MasterAudio.UnpauseAllPlaylists();
            }
        }

        public virtual void SetSoundValue(float value) { MasterAudio.MasterVolumeLevel = value; }

        public virtual void SetMusicValue(float value) { MasterAudio.PlaylistMasterVolume = value; }

        public void Dispose() { this.compositeDisposable.Dispose(); }
    }
}