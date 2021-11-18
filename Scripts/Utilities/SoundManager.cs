namespace GameFoundation.Scripts.Utilities
{
    using DarkTonic.MasterAudio;

    public interface IMechSoundManager
    {
        void PlaySound(string name);
        void StopSound(string name);
    }

    public class MasterMechSoundManager : IMechSoundManager
    {
        public void PlaySound(string name) { MasterAudio.PlaySound(name); }
        //To DO
        public void StopSound(string name) { }
    }
}