using System.Linq;

namespace Localization.Config
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Metadata;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Scripting.APIUpdating;

    /// <summary>
    /// Locale Metadata that stores default font and material Asset Table references
    /// for <see cref="Localization.UnityLocalization.TextLocalizer"/> components.
    ///
    /// Attach this to any Locale via the Locale inspector. TextLocalizer will
    /// search available locales to find this metadata automatically — no manual
    /// assignment needed on each component.
    /// </summary>
    [Metadata(AllowedTypes = MetadataType.LocalizationSettings, AllowMultiple = false,
        MenuItem = "GDK/Settings/TextLocalizerFontMetadata")]
    [Serializable]
    [MovedFrom(true, "Localization.Config", "Game.Scripts", "TextLocalizerFontMetadata")]
    public class TextLocalizerFontMetadata : IMetadata
    {
        [Header("Default Font")]
        [Tooltip("Localized font reference used by all TextLocalizer instances unless overridden.")]
        [SerializeField]
        private LocalizedTmpFont defaultFont;

        [Header("Material Presets")]
        [Tooltip("Material presets mapped by preset ID. Each TextLocalizer selects one preset.")]
        [SerializeField]
        private List<MaterialPreset> materialPresets = new();

        [Serializable]
        public class MaterialPreset
        {
            public List<string> Keywords;
            public LocalizedMaterial Material;
        }

        /// <summary>
        /// Default localized font asset reference.
        /// </summary>
        public LocalizedTmpFont DefaultFont => this.defaultFont;

        /// <summary>
        /// All configured material presets.
        /// </summary>
        public IReadOnlyList<MaterialPreset> MaterialPresets => this.materialPresets;

        /// <summary>
        /// Finds the localized material asset reference for a given preset ID.
        /// </summary>
        public LocalizedMaterial GetMaterialPreset(string presetId)
        {
            if (string.IsNullOrEmpty(presetId)) return null;

            return this.materialPresets.FirstOrDefault(x => x.Keywords.Contains(presetId))?.Material;
        }
    }
}
