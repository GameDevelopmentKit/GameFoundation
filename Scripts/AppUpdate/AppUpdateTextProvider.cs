namespace GameFoundation.Scripts.AppUpdate
{
    using Localization.Interfaces;
    using Localization.UnityLocalization;

    public class AppUpdateTextSettings
    {
        public string TableName = "Default Table";

        public string ForceTitleKey       = "app_update_force_title";
        public string ForceMessageKey     = "app_update_force_message";
        public string OptionalTitleKey    = "app_update_optional_title";
        public string OptionalMessageKey  = "app_update_optional_message";
        public string UpdateButtonKey     = "app_update_button_update";
        public string LaterButtonKey      = "app_update_button_later";

        public string ForceTitleFallback      = "Update Required";
        public string ForceMessageFallback    = "A new version is required to continue playing.\n Please update the game to keep playing.";
        public string OptionalTitleFallback   = "Update Available";
        public string OptionalMessageFallback = "A new version is available. Update now for the latest fixes and improvements, or continue playing for now.";
        public string UpdateButtonFallback    = "Update";
        public string LaterButtonFallback     = "Later";
    }

    public class AppUpdateTexts
    {
        public string Title;
        public string Message;
        public string UpdateButton;
        public string LaterButton;
    }

    public class LocalizedAppUpdateTextProvider : IAppUpdateTextProvider
    {
        private readonly ILocalizationService localizationService;
        private readonly AppUpdateTextSettings settings;

        public LocalizedAppUpdateTextProvider(ILocalizationService localizationService, AppUpdateTextSettings settings)
        {
            this.localizationService = localizationService;
            this.settings            = settings;
        }

        public AppUpdateTexts GetTexts(AppUpdateDecision decision)
        {
            var isForce = decision != null && decision.IsForceUpdate;

            return new AppUpdateTexts
            {
                Title        = isForce ? this.Localize(this.settings.ForceTitleKey, this.settings.ForceTitleFallback) : this.Localize(this.settings.OptionalTitleKey, this.settings.OptionalTitleFallback),
                Message      = isForce ? this.Localize(this.settings.ForceMessageKey, this.settings.ForceMessageFallback) : this.Localize(this.settings.OptionalMessageKey, this.settings.OptionalMessageFallback),
                UpdateButton = this.Localize(this.settings.UpdateButtonKey, this.settings.UpdateButtonFallback),
                LaterButton  = this.Localize(this.settings.LaterButtonKey, this.settings.LaterButtonFallback),
            };
        }

        private string Localize(string key, string fallback)
        {
            if (this.localizationService == null || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            var localized = this.localizationService.GetLocalizedString(this.settings.TableName, key);
            if (string.IsNullOrWhiteSpace(localized) || localized == key || LocalizationHelper.IsDefaultNoTranslationMsg(localized))
            {
                return fallback;
            }

            return localized;
        }
    }
}
