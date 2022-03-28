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
        void MuteSound(bool value);
        void MuteMusic(bool value);
        void SetSoundValue(float value);
        void SetMusicValue(float value);
    }

    public class MasterMechSoundManager : IMechSoundManager, IInitializable, IDisposable
    {
        public static    MasterMechSoundManager   Instance { get; private set; }
        
        private readonly PlaylistController       playlistController;
        private readonly GameFoundationLocalData  gameFoundationLocalData;
        private readonly MasterAudio              masterAudio;
        private readonly DynamicSoundGroupCreator groupCreator;
        
        private  CompositeDisposable compositeDisposable;
        private  List<Transform>          listSfxSoundMecha = new List<Transform>();
        
        public MasterMechSoundManager(PlaylistController playlistController, GameFoundationLocalData gameFoundationLocalData, MasterAudio masterAudio)
        {
            this.playlistController      = playlistController;
            this.gameFoundationLocalData = gameFoundationLocalData;
            this.masterAudio             = masterAudio;
            Instance                     = this;
            var AllsoundTrans = masterAudio.transform.GetComponentsInChildren<Transform>();
            foreach (var t in AllsoundTrans)
            {
                this.listSfxSoundMecha.Add(t);
            }
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

        public virtual void PlaySound(string name, bool isLoop = false)
        {
            if (this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Value) return;
            MasterAudio.PlaySound(name, isChaining: isLoop);
        }
        public void StopSound(string name) { MasterAudio.StopAllOfSound(name); }

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

        public virtual void MuteSound(bool value)
        {
            if (value)
            {
                foreach (var t in this.listSfxSoundMecha)
                {
                    MasterAudio.StopAllSoundsOfTransform(t);
                }
            }
        }

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