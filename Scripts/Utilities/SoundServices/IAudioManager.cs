namespace Utilities.SoundServices
{
    public interface IAudioManager
    {
        void PlaySound(string name, bool isLoop = false);
        void StopAllSound(string name);
        void PlayPlayList(string playlist, bool random = false);
        void StopPlayList(string playlist);
        void StopAllPlayList();
    }
}