namespace Localization.UnityLocalization
{
    using System;
    using System.Collections;
    using Cysharp.Threading.Tasks;
    using Localization.Config;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Events;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Localization.Tables;
    using UnityEngine.Scripting.APIUpdating;

    /// <summary>
    /// Self-contained text localization component that handles string, font, and material
    /// localization on a single <see cref="TextMeshProUGUI"/> GameObject.
    ///
    /// Follows the <c>GameObjectLocalizer</c> pattern: subscribes to
    /// <see cref="LocalizationSettings.SelectedLocaleChanged"/> once, then loads all
    /// localized assets from Asset Tables via Addressables on each locale change.
    ///
    /// Default font and material references are read from <see cref="TextLocalizerFontMetadata"/>
    /// attached to a Locale. Per-object overrides are supported via <see cref="UseCustomFont"/>.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DisallowMultipleComponent]
    [MovedFrom(true, "Localization.UnityLocalization", "Game.Scripts", "TextLocalizer")]
    public class TextLocalizer : MonoBehaviour
    {
        #region Inspector/Serialized Variables
        [SerializeField] private TextMeshProUGUI text;

        [TitleGroup("String")] [Tooltip("The localized string reference (table + entry key).")] [SerializeField]
        private LocalizedString stringReference = new();

        [SerializeField] private UnityEventString onStringChanged;

        [TitleGroup("Font & Material")]
        [Tooltip("When true, this object uses custom font/material instead of the metadata defaults.")]
        [SerializeField]
        private bool useCustomFont;

        [TitleGroup("Font & Material")]
        [Tooltip("Localized font reference.")]
        [EnableIf("useCustomFont")]
        [SerializeField]
        private LocalizedTmpFont fontRef;

        [TitleGroup("Font & Material")]
        [Tooltip("Localized material reference.")]
        [EnableIf("useCustomFont")]
        [SerializeField]
        private LocalizedMaterial materialRef;
        #endregion

        #region Runtime State
        private string                        fallbackText = string.Empty;
        private Locale                        currentLocale;
        private LocalizedString.ChangeHandler stringChangedHandler;
        #endregion

        #region Properties
        /// <summary>
        /// The <see cref="TextMeshProUGUI"/> component on this GameObject.
        /// </summary>
        public TextMeshProUGUI Text => this.text;

        /// <summary>
        /// The <see cref="LocalizedString"/> reference for string localization.
        /// </summary>
        public LocalizedString StringReference
        {
            get => this.stringReference;
            set
            {
                this.ClearStringHandler();
                this.stringReference = value;
                if (this.isActiveAndEnabled)
                    this.RegisterStringHandler();
            }
        }

        /// <summary>
        /// Whether a string reference has been set.
        /// </summary>
        public bool HasStringRefSet => this.stringReference != null && !this.stringReference.IsEmpty;

        /// <summary>
        /// When true, uses custom font/material refs instead of metadata defaults.
        /// </summary>
        public bool UseCustomFont
        {
            get => this.useCustomFont;
            set => this.useCustomFont = value;
        }
        #endregion

        #region Public API
        /// <summary>
        ///  Sets the text directly, bypassing localization. Clears any existing string reference.
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            this.ClearStringHandler();
            if (this.text != null) this.text.text = text;
        }

        /// <summary>
        /// Changes the <see cref="TableReference"/> value of a LocalizeString.
        /// </summary>
        /// <param name="tableReference">A reference to the table that will be set to StringReference of a LocalizeString</param>
        public void SetTable(string tableReference)
        {
            if (StringReference == null)
                StringReference = new LocalizedString();
            if (StringReference.TableReference == tableReference) return;
            StringReference.TableReference = tableReference;
        }

        /// <summary>
        /// Changes the <see cref="TableEntry"/> value of a LocalizeString.
        /// </summary>
        /// <param name="entryName">A reference to the entry in the table that will be set to StringReference of a LocalizeString</param>
        /// <param name="args"></param>
        public void SetEntry(string entryName, params object[] args)
        {
            if (StringReference == null)
                StringReference = new LocalizedString();
            StringReference.Arguments           = args;
            StringReference.TableEntryReference = entryName;

            if (args != null && args.Length > 0)
                StringReference.RefreshString();
        }

        /// <summary>
        /// Clears the current string reference and sets the text to empty.
        /// </summary>
        public void SetStringEmpty()
        {
            this.ClearStringHandler();
            this.stringReference = new LocalizedString();
            if (this.text != null) this.text.text = string.Empty;
        }

        /// <summary>
        /// Sets the string reference by table name and entry key.
        /// </summary>
        public void SetStringReference(string tableName, string entryKey, params object[] args)
        {
            if (HasStringRefSet)
            {
                this.SetTable(tableName);
                this.SetEntry(entryKey, args);
            }
            else
            {
                this.SetStringReference(new LocalizedString(tableName, entryKey), args);
            }
        }

        /// <summary>
        /// Sets the string reference by table name and entry key ID.
        /// </summary>
        public void SetStringReference(string tableName, long keyId)
        {
            var localizedString = new LocalizedString
            {
                TableReference      = tableName,
                TableEntryReference = keyId
            };
            this.SetStringReference(localizedString);
        }

        /// <summary>
        /// Sets the string reference to a <see cref="LocalizedString"/>.
        /// </summary>
        public void SetStringReference(LocalizedString stringRef)
        {
            if (stringRef == null)
            {
                Debug.LogError($"[TextLocalizer] StringReference is null on '{this.gameObject.name}'");
                return;
            }

            this.StringReference = stringRef;
        }

        /// <summary>
        /// Sets the string reference with formatting arguments.
        /// </summary>
        public void SetStringReference(LocalizedString stringRef, params object[] args)
        {
            if (stringRef == null)
            {
                Debug.LogError($"[TextLocalizer] StringReference is null on '{this.gameObject.name}'");
                return;
            }

            try
            {
                stringRef.Arguments = args;
                this.SetStringReference(stringRef);
            }
            catch (FormatException fe)
            {
                Debug.LogException(fe);
                stringRef.Arguments = null;
                if (this.text != null)
                    this.text.text = string.Format(stringRef.GetLocalizedString(), args);
            }
        }

        /// <summary>
        /// Sets the formatting arguments on the current string reference.
        /// </summary>
        public void SetArguments(params object[] args)
        {
            if (!this.HasStringRefSet) return;
            try
            {
                this.StringReference.Arguments = args;
                this.StringReference.RefreshString();
            }
            catch (FormatException fe)
            {
                Debug.LogException(fe);
                this.StringReference.Arguments = null;
                if (this.text != null)
                    this.text.text = string.Format(this.StringReference.GetLocalizedString(), args);
            }

        }
        #endregion

        #region Lifecycle
        private void Awake()
        {
            if (this.text == null) this.text = this.GetComponent<TextMeshProUGUI>();

            if (this.text != null)
            {
                this.fallbackText = string.IsNullOrEmpty(this.text.text) ? string.Empty : this.text.text;
            }
        }

        private IEnumerator Start()
        {
            this.currentLocale = null;
            var localeOp = LocalizationSettings.SelectedLocaleAsync;
            if (!localeOp.IsDone)
                yield return localeOp;

            this.OnSelectedLocaleChanged(localeOp.Result);
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += this.OnSelectedLocaleChanged;
            this.RegisterStringHandler();

            // Re-apply if locale changed while disabled
            if (this.currentLocale != null)
            {
                var locale = LocalizationSettings.SelectedLocale;
                if (!ReferenceEquals(this.currentLocale, locale))
                {
                    this.OnSelectedLocaleChanged(locale);
                }
            }
        }

        private void OnDisable()
        {
            this.ClearStringHandler();
            LocalizationSettings.SelectedLocaleChanged -= this.OnSelectedLocaleChanged;
        }

        private void OnDestroy()
        {
            this.ClearStringHandler();
            LocalizationSettings.SelectedLocaleChanged -= this.OnSelectedLocaleChanged;
        }
        #endregion

        #region Locale Change Handler
        /// <summary>
        /// Single entry point for locale changes. Loads font and material from Asset Tables.
        /// String is handled separately via <see cref="LocalizedString.StringChanged"/>.
        /// </summary>
        private async void OnSelectedLocaleChanged(Locale locale)
        {
            this.currentLocale = locale;
            if (locale == null || this.text == null) return;

            await this.ApplyFontAndMaterialAsync();
        }
        #endregion

        #region String Handling
        private void RegisterStringHandler()
        {
            if (this.StringReference == null) return;

            this.stringChangedHandler          ??= this.OnStringChanged;
            this.StringReference.StringChanged +=  this.stringChangedHandler;
        }

        private void ClearStringHandler()
        {
            if (this.StringReference != null)
            {
                this.StringReference.StringChanged -= this.stringChangedHandler;
                this.stringChangedHandler          =  null;
            }
        }

        private void OnStringChanged(string value)
        {
            if (this.text == null) return;

            if (string.IsNullOrEmpty(value) || LocalizationHelper.IsDefaultNoTranslationMsg(value))
            {
                if (this.HasStringRefSet && !string.IsNullOrEmpty(this.StringReference.TableEntryReference.Key))
                {
                    value = this.StringReference.TableEntryReference.Key;
                }
                else
                {
                    value = this.fallbackText;
                }

#if DEBUG_MODULE
                value += " [LOC_MISSING!!!]";

                if (this.HasStringRefSet)
                {
                    Debug.LogWarning(
                        $"[TextLocalizer] No translation found for '{this.StringReference.TableEntryReference}' " +
                        $"in table '{this.StringReference.TableReference}' on '{this.gameObject.name}'.", this);
                }
#endif
            }

            this.onStringChanged?.Invoke(value);
            this.text.text = value;
        }
        #endregion

        #region Font & Material Handling
        /// <summary>
        /// Resolves the active font and material references.
        /// When <see cref="useCustomFont"/> is false, reads from <see cref="TextLocalizerFontMetadata"/>.
        /// When true, uses the per-object <see cref="fontRef"/> and <see cref="materialRef"/>.
        /// </summary>
        private void ResolveFontAndMaterialRefs()
        {
            if (this.useCustomFont)
                return;

            if (ValidateAssetRef(this.fontRef) && ValidateAssetRef(this.materialRef)) return;

            var metadata = LocalizationSettings.Metadata.GetMetadata<TextLocalizerFontMetadata>();
            if (metadata != null)
            {
                this.fontRef = metadata.DefaultFont;

                // Guard: fontSharedMaterial can be null on iOS when Addressables
                // releases the previous locale's font atlas during a locale switch.
                if (this.text == null || this.text.fontSharedMaterial == null) return;

                var currentMaterial = this.text.fontSharedMaterial.name;
                foreach (var materialPreset in metadata.MaterialPresets)
                {
                    if (materialPreset.Keywords.Contains(currentMaterial))
                    {
                        this.materialRef = materialPreset.Material;
                        break;
                    }
                }
            }
        }

        private bool ValidateAssetRef(LocalizedReference assetRef)
        {
            return assetRef != null && !assetRef.IsEmpty;
        }

        /// <summary>
        /// Loads font and material from Asset Tables based on metadata defaults or per-object overrides.
        /// Called once per locale change via <see cref="OnSelectedLocaleChanged"/>.
        /// </summary>
        private async UniTask ApplyFontAndMaterialAsync()
        {
            this.ResolveFontAndMaterialRefs();

            // Guard: the GameObject may have been destroyed during the sync resolve
            // or between async loads (e.g. screen transition during locale change).
            if (this == null) return;
            await this.LoadAndApplyFontAsync(this.fontRef);
            if (this == null) return;
            await this.LoadAndApplyMaterialAsync(this.materialRef);
        }

        private async UniTask LoadAndApplyFontAsync(LocalizedAsset<TMP_FontAsset> activeFontRef)
        {
            if (!ValidateAssetRef(activeFontRef)) return;

            try
            {
                var op   = activeFontRef.LoadAssetAsync();
                var font = await op;
                if (this.text != null && font != null)
                {
                    this.text.font = font;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        private async UniTask LoadAndApplyMaterialAsync(LocalizedAsset<Material> activeMaterialRef)
        {
            if (!ValidateAssetRef(activeMaterialRef)) return;

            try
            {
                var op       = activeMaterialRef.LoadAssetAsync();
                var material = await op;
                if (this.text != null && material != null)
                {
                    this.text.fontSharedMaterial = material;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
        #endregion

        #region Editor
#if UNITY_EDITOR

        [TitleGroup("Font & Material")]
        [ShowInInspector, HideIf(nameof(useCustomFont)), LabelText("Global Font Metadata"), DisplayAsString(false)]
        [InlineButton(nameof(OpenLocalizationSettings), SdfIconType.Gear, label: "Configure")]
        [InfoBox("TextLocalizerFontMetadata is missing. Please add it to Project Settings -> Localization -> Metadata.",
            InfoMessageType.Warning, nameof(IsMissingMetadata))]
        private string GlobalFontMetadataStatus
        {
            get => IsMissingMetadata ? "Not Configured" : "Configured in Project Settings";
            set { }
        }

        private void OpenLocalizationSettings() =>
            UnityEditor.SettingsService.OpenProjectSettings("Project/Localization");

        private bool IsMissingMetadata => !this.useCustomFont && (!LocalizationSettings.HasSettings ||
                                                                  LocalizationSettings.Metadata
                                                                      .GetMetadata<TextLocalizerFontMetadata>() ==
                                                                  null);

        private void OnValidate()
        {
            if (this.text == null) this.text = this.GetComponent<TextMeshProUGUI>();
            this.ResolveFontAndMaterialRefs();
        }


        private void Reset()
        {
            this.text = this.GetComponent<TextMeshProUGUI>();
            this.ResolveFontAndMaterialRefs();
        }
#endif
        #endregion
    }
}
