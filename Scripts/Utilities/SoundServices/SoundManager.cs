namespace GameFoundation.Scripts.Utilities
{
    using System;
    using Cysharp.Threading.Tasks;
    using DarkTonic.MasterAudio;
    using GameFoundation.Scripts.Models;
    using global::Utilities.SoundServices;
    using UniRx;
    using Zenject;

    [Obsolete("Please use MasterAAASoundWrapper instead")]
    public class AudioManager : IAudioManager, IInitializable, IDisposable
    {
        public static AudioManager Instance { get; private set; }

        private readonly PlaylistController playlistController;
        private readonly MasterAudio        masterAudio;

        private readonly SoundSetting soundSetting;

        private CompositeDisposable compositeDisposable;

        public AudioManager(PlaylistController playlistController, MasterAudio masterAudio, SoundSetting soundSetting)
        {
            this.playlistController = playlistController;
            this.masterAudio        = masterAudio;
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
            var groups = this.masterAudio.transform.GetComponentsInChildren<MasterAudioGroup>();

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