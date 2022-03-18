namespace GameFoundation.Scripts.Utilities
{
    using DarkTonic.MasterAudio;

    public interface IMechSoundManager
    {
        void PlaySound(string name);
        void StopSound(string name);
        void PlayPlayList(string playlist, bool random = false);
        void StopPlaylist(string playlist);
        void StopAllPlaylist();
        void MutePlaylist(string playlist);
        void MuteAllPlaylist();
        void SetVolumePlaylist(float value);
    }

    public class MasterMechSoundManager : IMechSoundManager
    {
        protected readonly PlaylistController playlistController;

        public static MasterMechSoundManager Instance { get; private set; }

        public MasterMechSoundManager(PlaylistController playlistController)
        {
            this.playlistController = playlistController;
            Instance                = this;
        }

        public virtual void PlaySound(string name) { MasterAudio.PlaySound(name); }
        public virtual void StopSound(string name) { }
        public virtual void PlayPlayList(string playlist, bool random = false)
        {
            this.playlistController.isShuffle = random;
            MasterAudio.StartPlaylist(playlist);
        }

        public virtual void StopPlaylist(string playlist)  { MasterAudio.StopPlaylist(playlist); }
        public virtual void MutePlaylist(string playlist)  { MasterAudio.MutePlaylist(); }
        public virtual void StopAllPlaylist()              { MasterAudio.StopAllPlaylists(); }
        public virtual void MuteAllPlaylist()              { MasterAudio.MuteAllPlaylists(); }
        public virtual void SetVolumePlaylist(float value) { MasterAudio.MasterVolumeLevel = value; }
    }

}