namespace GameFoundation.Scripts.Utilities
{
    using System;
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
        void StopPlayList(string playlist);
        void StopAllPlayList();
    }

    public class AudioManager : IAudioManager, IInitializable, IDisposable
    {
        public static AudioManager Instance { get; private set; }

        private readonly PlaylistController playlistController;
        private readonly AudioController    audioController;

        private readonly SoundSetting soundSetting;

        private CompositeDisposable compositeDisposable;

        public AudioManager(PlaylistController playlistController, AudioController audioController, SoundSetting soundSetting)
        {
            this.playlistController = playlistController;
            this.audioController    = audioController;
            this.soundSetting       = soundSetting;
            Instance                = this;
        }

        public void Initialize() { this.SubscribeMasterAudio(); }

        private async void SubscribeMasterAudio()
        {
            await UniTask.WaitUntil(() => this.playlistController.ControllerIsReady);
            this.soundSetting.MuteSound.Value = false;
            this.soundSetting.MuteMusic.Value = false;

            this.compositeDisposable = new CompositeDisposable
            {
                this.soundSetting.MusicValue.Subscribe(this.SetMusicValue),
                this.soundSetting.SoundValue.Subscribe(this.SetSoundValue),
                this.soundSetting.MasterVolume.Subscribe(this.SetMasterVolume)
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

        protected virtual void SetSoundValue(float value)
        {
            var groups = this.audioController.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();

            foreach (var transform in groups)
            {
                transform.groupMasterVolume = value;
            }
        }

        protected virtual void SetMusicValue(float value)    { MasterAudio.PlaylistMasterVolume = value; }
        public virtual    void StopPlayList(string playlist) => MasterAudio.StopPlaylist(playlist);
        public virtual    void StopAllPlayList()             => MasterAudio.StopAllPlaylists();

        public void Dispose() { this.compositeDisposable.Dispose(); }
    }
}