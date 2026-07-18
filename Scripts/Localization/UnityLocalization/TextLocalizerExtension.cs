namespace Localization.UnityLocalization
{
    using Localization.Blueprint;
    using TMPro;

    /// <summary>
    /// Extension methods for <see cref="TextMeshProUGUI"/> to simplify localization initialization.
    /// Provides backward-compatible helpers that work with both the new <see cref="TextLocalizer"/>
    /// and the legacy <see cref="LocalizationElementData"/> pattern.
    /// </summary>
    public static class TextLocalizerExtension
    {
        /// <summary>
        /// Initializes localization on a <see cref="TextMeshProUGUI"/> using the <see cref="TextLocalizer"/> component.
        /// If no <see cref="TextLocalizer"/> exists on the GameObject, one will be added.
        /// </summary>
        /// <param name="txt">The TextMeshProUGUI to localize</param>
        /// <param name="tableName">The string table collection name</param>
        /// <param name="entryKey">The entry key in the string table</param>
        public static void Localize(this TextMeshProUGUI txt, string tableName, string entryKey)
        {
            if (txt == null) return;

            if (!txt.TryGetComponent<TextLocalizer>(out var localizer))
                localizer = txt.gameObject.AddComponent<TextLocalizer>();

            var localizedString = LocalizationHelper.CreateLocalizedString(tableName, entryKey);
            localizer.SetStringReference(localizedString);
        }

        /// <summary>
        /// Initializes localization on a <see cref="TextMeshProUGUI"/> using the <see cref="TextLocalizer"/> component.
        /// Uses the first element's table and key for the string reference, with formatting arguments.
        /// </summary>
        public static void Localize(this TextMeshProUGUI txt, string tableName, string entryKey, params object[] args)
        {
            if (txt == null) return;

            if (!txt.TryGetComponent<TextLocalizer>(out var localizer))
                localizer = txt.gameObject.AddComponent<TextLocalizer>();

            var localizedString = LocalizationHelper.CreateLocalizedString(tableName, entryKey);
            localizer.SetStringReference(localizedString, args);
        }

        /// <summary>
        /// Sets the localized text directly on a <see cref="TextMeshProUGUI"/> using the <see cref="TextLocalizer"/> component.
        /// </summary>
        /// <param name="txt"></param>
        /// <param name="localizedText"></param>
        public static void SetLocalizedText(this TextMeshProUGUI txt, string localizedText)
        {
            if (txt == null) return;

            if (!txt.TryGetComponent<TextLocalizer>(out var localizer))
                localizer = txt.gameObject.AddComponent<TextLocalizer>();

            localizer.SetText(localizedText);
        }
    }
}
