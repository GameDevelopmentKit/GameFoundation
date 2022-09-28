using System;

namespace GameFoundation.Scripts.Utilities
{
    using Cysharp.Threading.Tasks;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.Models;
    using UniRx;
    using Zenject;

    public interface IAudioManager
    {
        void PlaySound(string name, bool isLoop = false);
        void StopAllSound(string name);
        void PlayPlayList(string playlist, bool random = false);
    }

    public class AudioManager : IAudioManager, IInitializable, IDisposable
    {
        public static AudioManager Instance { get; private set; }

        private readonly PlaylistController       playlistController;
        private readonly GameFoundationLocalData  gameFoundationLocalData;
        private readonly MasterAudio              masterAudio;
        private readonly DynamicSoundGroupCreator groupCreator;

        private CompositeDisposable compositeDisposable;

        public AudioManager(PlaylistController playlistController, GameFoundationLocalData gameFoundationLocalData, MasterAudio masterAudio)
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
            this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Value = false;
            this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Value = false;
            this.compositeDisposable = new CompositeDisposable
            {
                //TODO uncomment this when we have a proper solution
                // this.gameFoundationLocalData.IndexSettingRecord.MuteMusic.Subscribe(this.CheckToMuteMusic),
                // this.gameFoundationLocalData.IndexSettingRecord.MuteSound.Subscribe(this.CheckToMuteSound),
                this.gameFoundationLocalData.IndexSettingRecord.MusicValue.Subscribe(this.SetMusicValue),
                this.gameFoundationLocalData.IndexSettingRecord.SoundValue.Subscribe(this.SetSoundValue),
                this.gameFoundationLocalData.IndexSettingRecord.MasterVolume.Subscribe(this.SetMasterVolume)
            };
        }

        private void SetMasterVolume(bool value)
        {
            var finalValue = value ? 1 : 0;
            MasterAudio.MasterVolumeLevel = finalValue;
            if (value)
            {
                MasterAudio.UnmuteAllPlaylists();
            }
            else
            {
                MasterAudio.MuteAllPlaylists();
            }
        }

        public virtual void PlaySound(string name, bool isLoop = false) => MasterAudio.PlaySound(name, isChaining: isLoop);

        public void StopAllSound(string name) => MasterAudio.StopAllOfSound(name);

        public virtual void PlayPlayList(string playlist, bool random = false)
        {
            this.playlistController.isShuffle = random;
            MasterAudio.StartPlaylist(playlist);
        }

        public virtual void CheckToMuteSound(bool isMute)
        {
            var groups = this.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();
            foreach (var transform in groups)
            {
                transform.groupMasterVolume = isMute ? 0 : 1;
            }
        }

        public virtual void CheckToMuteMusic(bool value)
        {
            if (value)
            {
                MasterAudio.MuteAllPlaylists();
            }
            else
            {
                MasterAudio.UnmuteAllPlaylists();
            }
        }

        protected virtual void SetSoundValue(float value)
        {
            var groups = this.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();
            foreach (var transform in groups)
            {
                transform.groupMasterVolume = value;
            }
        }

        protected virtual void SetMusicValue(float value) { MasterAudio.PlaylistMasterVolume = value; }

        public void Dispose() { this.compositeDisposable.Dispose(); }
    }
}