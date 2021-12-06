namespace GameFoundation.Scripts.Utilities
{
    using DarkTonic.MasterAudio;

    public interface IMechSoundManager
    {
        void PlaySound(string name);
        void StopSound(string name);
        void PlayPlayList(string playlist);
        void StopPlaylist(string playlist, bool stopAll = false);
    }

    public class MasterMechSoundManager : IMechSoundManager
    {
        public void PlaySound(string name) { MasterAudio.PlaySound(name); }
        //To DO
        public void StopSound(string name)        { }
        public void PlayPlayList(string playlist) { MasterAudio.StartPlaylist(playlist); }
        public void StopPlaylist(string playlist, bool stopAll = false)
        {
            if (stopAll)
            {
                MasterAudio.StopAllPlaylists();
                return;
            }

            MasterAudio.StopPlaylist(playlist);
        }
    }
}