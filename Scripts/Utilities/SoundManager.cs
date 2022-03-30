namespace GameFoundation.Scripts.Utilities
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.GameManager;
    using UnityEngine;
    using Zenject;
    using UniRx;
    using Vector2 = System.Numerics.Vector2;

    public interface IMechSoundManager
    {
        void PlaySound(string name, bool isLoop = false);
        void StopSound(string name);
        void PlayPlayList(string playlist, bool random = false);
        void StopPlaylist(string playlist);
        void StopAllPlaylist();
        void MutePlaylist(string playlist);
        void MuteAllPlaylist();
        void SetVolumePlaylist(float value);
        void CheckToMuteSound(bool value);
        void CheckToMuteMusic(bool value);
        void SetSoundValue(float value);
        void SetMusicValue(float value);
    }

    public class MasterMechSoundManager : IMechSoundManager, IInitializable, IDisposable
    {
        public static MasterMechSoundManager Instance { get; private set; }

        private readonly PlaylistController       playlistController;
        private readonly GameFoundationLocalData  gameFoundationLocalData;
        private readonly MasterAudio              masterAudio;
        private readonly DynamicSoundGroupCreator groupCreator;

        private          CompositeDisposable    compositeDisposable;
        private readonly List<MasterAudioGroup> listSFXGroups = new List<MasterAudioGroup>();

        public MasterMechSoundManager(PlaylistController playlistController, GameFoundationLocalData gameFoundationLocalData, MasterAudio masterAudio)
        {
            this.playlistController      = playlistController;
            this.gameFoundationLocalData = gameFoundationLocalData;
            this.masterAudio             = masterAudio;
            Instance                     = this;
        }

        public void Initialize() { this.SubscribeMasterAudio(); }

        private async void SubscribeMasterAudio()
        {
            await UniTask.WaitUntil(() => this.playlistController.ControllerIsReady);
            var groups = this.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();
            foreach (var t in groups)
            {
                this.listSFXGroups.Add(t);
            }

            this.compositeDisposable = new CompositeDisposable
            {
                this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Subscribe(this.CheckToMuteMusic),
                this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Subscribe(this.CheckToMuteSound),
                this.gameFoundationLocalData.IndexSettingRecord.MusicValue.Subscribe(this.SetMusicValue),
                this.gameFoundationLocalData.IndexSettingRecord.SoundValue.Subscribe(this.SetSoundValue)
            };
        }

        public virtual void PlaySound(string name, bool isLoop = false)
        {
            if (this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Value) return;
            MasterAudio.PlaySound(name, isChaining: isLoop);
        }
        public void StopSound(string name) { MasterAudio.StopAllOfSound(name); }

        public virtual void PlayPlayList(string playlist, bool random = false)
        {
            this.playlistController.isShuffle = random;
            var check = !this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Value;
            MasterAudio.StartPlaylist(playlist);
            if (check)
            {
                MasterAudio.PauseAllPlaylists();
            }
            else
            {
                MasterAudio.UnpauseAllPlaylists();
            }
        }

        public virtual void StopPlaylist(string playlist) { MasterAudio.StopPlaylist(playlist); }

        public virtual void MutePlaylist(string playlist) { MasterAudio.MutePlaylist(); }

        public virtual void StopAllPlaylist() { MasterAudio.StopAllPlaylists(); }

        public virtual void MuteAllPlaylist() { MasterAudio.MuteAllPlaylists(); }

        public virtual void SetVolumePlaylist(float value) { MasterAudio.PlaylistMasterVolume = value; }

        public virtual void CheckToMuteSound(bool value)
        {
            foreach (var t in this.listSFXGroups)
            {
                t.isMuted = value;
            }
        }

        public virtual void CheckToMuteMusic(bool value)
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