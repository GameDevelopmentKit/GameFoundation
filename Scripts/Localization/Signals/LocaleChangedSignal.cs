namespace Localization.Signals
{
    using Localization.Interfaces;
    /// <summary>
    /// Fired when the active localization language is successfully changed.
    /// </summary>
    public class LocaleChangedSignal
    {
        public LanguageInfo NewLanguage { get; }

        public LocaleChangedSignal(LanguageInfo newLanguage)
        {
            this.NewLanguage = newLanguage;
        }
    }
}
