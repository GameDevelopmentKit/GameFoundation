namespace GameFoundation.Scripts.Utilities
{
    using I2.Loc;

    public class LocalizationService
    {
        public string GetTextWithKey(string key) { return LocalizationManager.TryGetTranslation(key, out var localization) ? localization : ""; }
    }
}