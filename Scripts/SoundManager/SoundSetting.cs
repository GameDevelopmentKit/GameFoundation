namespace GameFoundation.Scripts.Models
{
    using DataManager.UserData;
    using UniRx;

    public class SoundSetting : IUserData
    {
        public BoolReactiveProperty  MasterVolume { get; set; } = new(true);
        public BoolReactiveProperty  MuteMusic    { get; set; } = new(false);
        public BoolReactiveProperty  MuteSound    { get; set; } = new(false);
        public FloatReactiveProperty MusicValue   { get; set; } = new(1);
        public FloatReactiveProperty SoundValue   { get; set; } = new(1);
    }
}