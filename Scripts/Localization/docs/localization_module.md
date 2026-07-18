# Localization Module

> Canonical technical reference for the reusable Game Foundation localization module.
>
> This document is project-neutral. Host projects own their localization content, table names, locale assets, fonts, Addressables groups, and any editor sync tooling.

## 1. Purpose

`GameFoundation.Localization` provides a reusable runtime localization boundary on top of Unity Localization. It keeps consumer code behind `ILocalizationService`, supplies Zenject bindings and locale-change signaling, and includes a `TextLocalizer` component for TextMeshPro UI text, font, and material localization.

The module intentionally does not own game-specific localization content. A host project should keep its Unity Localization Settings, Locale assets, String Tables, Asset Tables, Addressables data, and spreadsheet/editor sync tools in the host project.

## 2. Design Goals

- Keep runtime localization reusable across games.
- Hide the concrete provider behind `ILocalizationService`.
- Let static UI text be configured on prefabs through `TextLocalizer`.
- Let dynamic UI and gameplay code request localized strings through a stable service API.
- Support per-locale font and material swaps for TextMeshPro.
- Provide an optional in-memory online override data model.
- Preserve the existing `Localization.*` namespaces for source and serialized-reference compatibility.

## 3. Source Layout

```text
Packages/com.gdk.core/Scripts/Localization/
|-- GameFoundation.Localization.asmdef
|-- LocalizationInstaller.cs
|-- Blueprint/
|   |-- LocalizationDataModel.cs
|   |-- LocalizationDataOnline.cs
|   `-- LocalizationElementData.cs
|-- Config/
|   `-- TextLocalizerFontMetadata.cs
|-- Interfaces/
|   |-- IBlueprintLocalizationData.cs
|   |-- ILocalizationElement.cs
|   `-- ILocalizationService.cs
|-- Signals/
|   `-- LocaleChangedSignal.cs
|-- UnityLocalization/
|   |-- LocalizationHelper.cs
|   |-- TextLocalizer.cs
|   |-- TextLocalizerExtension.cs
|   `-- UnityLocalizationServices.cs
|-- Tests/
|   `-- Editor/
`-- docs/
    `-- localization_module.md
```

## 4. Assembly And Dependencies

Assembly: `GameFoundation.Localization`

Runtime references:

- `Unity.Localization`
- `Unity.TextMeshPro`
- `Unity.Addressables`
- `Unity.ResourceManager`
- `UniTask`
- `UniTask.Addressables`
- `Zenject-Source`
- `Zenject-Signals`
- `Sirenix.OdinInspector.Attributes.dll`

The package hard-depends on Unity Localization. The module should not rely on a host-project scripting define such as `UNITY_LOCALIZATION` to compile.

## 5. Architecture

```text
Host startup
  -> GameFoundationInstaller or host installer
     -> SignalBusInstaller
     -> LocalizationInstaller

Consumer code
  -> ILocalizationService
     -> UnityLocalizationServices
        -> UnityEngine.Localization.Settings.LocalizationSettings
        -> LocalizationDataOnline
        -> LocaleChangedSignal

Prefab TMP text
  -> TextLocalizer
     -> LocalizedString
     -> TextLocalizerFontMetadata
     -> LocalizedTmpFont / LocalizedMaterial asset tables
```

### Main Types

| Type | Role |
|---|---|
| `ILocalizationService` | Provider-agnostic runtime API for initialization, locale switching, string lookup, and language enumeration. |
| `LanguageInfo` | Lightweight locale view containing `LanguageCode`, `DisplayName`, and locale index. |
| `UnityLocalizationServices` | Unity Localization-backed implementation of `ILocalizationService`. |
| `LocalizationInstaller` | Zenject installer for service, online data, and locale-change signal bindings. |
| `LocaleChangedSignal` | Signal fired after a successful locale switch. |
| `TextLocalizer` | Self-contained `TextMeshProUGUI` localizer for string, font, and material changes. |
| `TextLocalizerExtension` | Convenience extension methods for configuring `TextLocalizer` from code. |
| `LocalizationHelper` | Static helper methods for fallback strings, dynamic `LocalizedString` caching, and optional online fallback lookup. |
| `TextLocalizerFontMetadata` | Unity Localization metadata that stores default localized font and material preset references. |
| `LocalizationDataOnline` | In-memory data container for runtime string overrides supplied by the host project. |

## 6. Installation

When a host project uses the full Game Foundation installer, localization is installed by `GameFoundationInstaller`.

For a custom setup, install the signal bus before localization:

```csharp
using Localization;
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        SignalBusInstaller.Install(this.Container);
        LocalizationInstaller.Install(this.Container);
    }
}
```

Host assemblies that reference `Localization.*` types must reference `GameFoundation.Localization`.

## 7. Host Project Responsibilities

The module expects the host project to provide:

- A valid Unity Localization Settings asset.
- Locale assets and selected locale configuration.
- String Table collections for localized text.
- Optional Asset Table collections for `TMP_FontAsset` and `Material` localization.
- Addressables configuration required by Unity Localization asset tables.
- Optional `TextLocalizerFontMetadata` in `LocalizationSettings.Metadata`.
- Optional runtime data population for `LocalizationDataOnline`.

The module does not provide Google Sheets sync, CSV import, localization content, locale lists, or game-specific key naming rules.

## 8. Runtime Service Behavior

`UnityLocalizationServices.InitializeAsync()` waits for `LocalizationSettings.InitializationOperation` and captures the current selected locale as `CurrentLanguage`.

`SetLocaleAsync(LanguageInfo language)`:

1. Ignores requests for the already-selected language.
2. Waits for Unity Localization initialization.
3. Sets `LocalizationSettings.SelectedLocale`.
4. Fires `LocaleChangedSignal`.

`GetLocalizedString()` and `GetLocalizedStringAsync()` use this fallback order:

1. Unity Localization String Table value.
2. `LocalizationDataOnline` value when the local value is empty or Unity's no-translation message.
3. The input key.

`LocalizationHelper.GetLocalizedStringWithOnlineFallback()` is a separate helper that checks online data before falling back to Unity String Tables.

## 9. TextLocalizer Behavior

Attach `TextLocalizer` to the same GameObject as a `TextMeshProUGUI`.

For strings, it owns a `LocalizedString` and subscribes to `LocalizedString.StringChanged`. When the localized value is empty or Unity's no-translation message, it falls back to the table entry key when available, then to the original TMP text captured at `Awake()`.

For fonts and materials, it listens to `LocalizationSettings.SelectedLocaleChanged`. On each locale change it loads localized font/material assets through Unity Localization asset references.

Font/material reference resolution:

- If `UseCustomFont` is true, the component uses its serialized `fontRef` and `materialRef`.
- Otherwise it reads `TextLocalizerFontMetadata` from `LocalizationSettings.Metadata`.
- `DefaultFont` supplies the localized font reference.
- `MaterialPresets` are matched by checking whether a preset keyword contains the current material name.

If metadata is missing, string localization still works. Font and material localization are skipped until metadata or custom references are configured.

## 10. Usage

### Static UI Text

For labels, titles, and button text that are known in the prefab:

1. Add `TextLocalizer` to the `TextMeshProUGUI` GameObject.
2. Assign `String Reference` to a String Table and entry.
3. Leave presenter code alone unless the text is dynamic.

### Dynamic UI Text

For keys chosen at runtime:

```csharp
using Localization.UnityLocalization;
using TMPro;

public void SetTitle(TextMeshProUGUI title, string tableName, string entryKey)
{
    title.Localize(tableName, entryKey);
}
```

With formatting arguments:

```csharp
title.Localize("Default Table", "reward_amount", amount);
```

### Service Lookup

```csharp
using Cysharp.Threading.Tasks;
using Localization.Interfaces;

public class RewardFormatter
{
    private readonly ILocalizationService localizationService;

    public RewardFormatter(ILocalizationService localizationService)
    {
        this.localizationService = localizationService;
    }

    public UniTask<string> GetRewardTextAsync(string key)
    {
        return this.localizationService.GetLocalizedStringAsync("Default Table", key);
    }
}
```

### Locale Switching

```csharp
using Localization.Interfaces;

var languages = this.localizationService.GetAvailableLanguages();
var selectedLanguage = languages[index];
this.localizationService.SetLocaleAsync(selectedLanguage).Forget();
```

### Locale Change Signal

```csharp
using Localization.Signals;
using Zenject;

public class LocaleAwarePresenter : IInitializable, System.IDisposable
{
    private readonly SignalBus signalBus;

    public LocaleAwarePresenter(SignalBus signalBus)
    {
        this.signalBus = signalBus;
    }

    public void Initialize()
    {
        this.signalBus.Subscribe<LocaleChangedSignal>(this.OnLocaleChanged);
    }

    public void Dispose()
    {
        this.signalBus.Unsubscribe<LocaleChangedSignal>(this.OnLocaleChanged);
    }

    private void OnLocaleChanged(LocaleChangedSignal signal)
    {
        var language = signal.NewLanguage;
    }
}
```

## 11. Online Data Overrides

`LocalizationDataOnline` is registered as a singleton. The module does not fetch remote data by itself. A host project may populate it from remote config, backend data, downloaded CSV, or another source.

```csharp
using System.Collections.Generic;
using Localization.Blueprint;

localizationDataOnline.LocalizationDatas["en-US"] = new LocalizationDataModel
{
    LocalizedTexts = new Dictionary<string, string>
    {
        ["event_title"] = "Limited Event"
    }
};
```

When `UnityLocalizationServices` cannot find a valid local table value, it asks `LocalizationDataOnline` for the current language code and key.

## 12. Extending Or Replacing The Provider

To replace Unity Localization:

1. Implement `ILocalizationService`.
2. Preserve the public API behavior expected by consumers.
3. Update `LocalizationInstaller` or add a host-specific installer override.
4. Keep `LocaleChangedSignal` behavior stable.
5. Decide whether `TextLocalizer` remains Unity Localization-based or needs a provider-specific equivalent.

Avoid changing existing `Localization.*` namespaces or serialized type names in a minor migration. If a rename or assembly move is required, add Unity `MovedFrom` attributes and validate serialized assets.

## 13. Serialized Migration Notes

When moving this module between assemblies or packages:

- Preserve `.cs.meta` GUIDs when moving MonoBehaviour scripts so prefab references survive.
- Use `[MovedFrom]` for serialized classes that change assembly.
- Check managed-reference assets that serialize assembly names, especially metadata in Unity Localization Settings.
- Let Unity reserialize assets when possible instead of bulk-editing prefab YAML.

`TextLocalizer` prefab references are script-GUID based. The YAML `m_EditorClassIdentifier` may still show an old assembly name until Unity reserializes, but the important reference is the `m_Script` GUID.

## 14. Testing Checklist

| Scenario | Expected result |
|---|---|
| Unity compile | No duplicate localization type definitions and no missing assembly references. |
| Installer binding | `ILocalizationService`, `UnityLocalizationServices`, `LocalizationDataOnline`, and `LocaleChangedSignal` resolve/fire through Zenject. |
| Static prefab text | `TextLocalizer` displays table text and updates after locale changes. |
| Dynamic runtime text | `.Localize(table, key)` adds/configures `TextLocalizer` and refreshes text. |
| Missing local key | Service returns online override when present, otherwise the key. |
| Missing metadata | Text localization still works; font/material localization is skipped safely. |
| Font/material switch | Locale changes load the configured `TMP_FontAsset` and `Material` references. |
| Serialized migration | Existing prefabs with `TextLocalizer` do not show missing scripts after import. |

## 15. Agent Notes

- Treat this file as the reusable module source of truth.
- Keep host-project locale lists, font choices, table names, and key conventions outside this module doc.
- Update this doc when the module API, installer behavior, fallback order, assembly references, or migration strategy changes.
- Put host-project guidance in the host project's agent docs and link back here instead of duplicating the module design.
