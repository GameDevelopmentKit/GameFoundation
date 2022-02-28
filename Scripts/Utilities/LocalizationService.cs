namespace GameFoundation.Scripts.Utilities
{
    using I2.Loc;

    public class LocalizationService
    {
        public static LocalizationService Instance { get; private set; }
        public LocalizationService()
        {
            Instance = this;
        }
        public string GetTextWithKey(string key) { return LocalizationManager.TryGetTranslation(key, out var localization) ? localization : key; }
    }
}