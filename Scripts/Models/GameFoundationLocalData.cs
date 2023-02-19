namespace GameFoundation.Scripts.Models
{
    using UniRx;

    public class SoundSetting
    {
        public BoolReactiveProperty  MasterVolume { get; set; } = new(true);
        public BoolReactiveProperty  MuteMusic    { get; set; } = new(false);
        public BoolReactiveProperty  MuteSound    { get; set; } = new(false);
        public FloatReactiveProperty MusicValue   { get; set; } = new(1);
        public FloatReactiveProperty SoundValue   { get; set; } = new(1);
    }
}