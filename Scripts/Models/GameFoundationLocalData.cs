namespace GameFoundation.Scripts.Models
{
    using GameFoundation.Scripts.Interfaces;
    using UniRx;

    public class SoundSetting : ILocalData
    {
        public FloatReactiveProperty MusicValue   { get; set; } = new(1);
        public FloatReactiveProperty SoundValue   { get; set; } = new(1);
        
        public void Init()
        {
        }
    }
}