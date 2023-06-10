namespace GameFoundation.Scripts.Models
{
    using GameFoundation.Scripts.Interfaces;

    public class SoundSetting : ILocalData
    {
        public bool  IsMuteMusic    { get; set; } = false;
        public bool  IsMuteSound    { get; set; } = false;
        public float MusicVolume   { get; set; } = 1;
        public float SoundVolume   { get; set; } = 1;
        
        public void Init()
        {
        }
    }
}