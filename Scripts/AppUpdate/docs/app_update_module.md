# App Update Module

> Canonical technical reference for the reusable Game Foundation app update gate.
>
> This document is project-neutral. Host projects own their remote-config provider, store URLs, remote-config dashboard values, and localization table content.

## 1. Purpose

`GameFoundation.Scripts.AppUpdate` provides a reusable startup gate for force updates and optional update notifications. The module reads a provider-agnostic config, evaluates the current app version/build against platform thresholds, and shows the common notification popup when the player should update.

The module intentionally keeps player-facing text on the client. Remote config only controls policy data such as minimum version, latest version, build thresholds, cooldown, and store URLs.

## 2. Design Goals

- Support both force updates and optional update notifications.
- Keep Game Foundation independent from Firebase, UGS, ByteBrew, or any other remote-config provider.
- Use the existing common notification popup instead of introducing an AppUpdate-specific popup prefab.
- Keep all title, body, and button text client-side through localization keys with hardcoded English fallback.
- Let host projects rebind only the config provider while reusing the policy, prompt, text, and build-info logic.
- Avoid trapping players if remote config is malformed or the store URL is missing.

## 3. Source Layout

```text
Packages/com.gdk.core/Scripts/AppUpdate/
|-- GameFoundation.AppUpdate.asmdef
|-- AppUpdateConfig.cs
|-- AppUpdateContracts.cs
|-- AppUpdateGateService.cs
|-- AppUpdateInstaller.cs
|-- AppUpdatePolicy.cs
|-- AppUpdateTextProvider.cs
|-- DisabledAppUpdateConfigProvider.cs
|-- NotificationAppUpdatePromptService.cs
|-- Tests/
|   `-- Editor/
`-- docs/
    `-- app_update_module.md

Packages/com.gdk.core/Plugins/iOS/
`-- GFAppUpdateBuildInfo.mm

Packages/com.gdk.core/Scripts/Utilities/
`-- VersionComparer.cs

Packages/com.gdk.core/Scripts/Utilities/ApplicationServices/
|-- ApplicationBuildInfo.cs
`-- ApplicationVersionService.cs
```

## 4. Assembly And Dependencies

Runtime assembly: `GameFoundation.AppUpdate`

Runtime references used by this module:

- `UnityEngine`
- `UniTask`
- `Zenject-Source`
- `GameFoundation.Utilities`
- `GameFoundation.UIModule`
- `GameFoundation.Localization`

Editor test assembly: `GameFoundation.AppUpdate.Tests`

The module does not reference any remote-config SDK. A host project supplies an `IAppUpdateConfigProvider` adapter from its game assembly. Host assemblies that use `GameFoundation.Scripts.AppUpdate` types must reference `GameFoundation.AppUpdate`.

## 5. Architecture

```text
Host startup
  -> fetch remote config
  -> initialize localization
  -> AppUpdateGateService.CheckForUpdateAsync()

AppUpdateGateService
  -> IAppUpdateConfigProvider
  -> IApplicationBuildInfoProvider
  -> AppUpdatePolicy
  -> IAppUpdateTextProvider
  -> IAppUpdatePromptService
     -> NotificationPopupPresenter
```

### Main Types

| Type | Role |
|---|---|
| `AppUpdateGateService` | Orchestrates config loading, policy evaluation, prompt display, optional cooldown, and store URL opening. |
| `IAppUpdateConfigProvider` | Provider boundary for loading `AppUpdateConfig`. Game Foundation binds a disabled default; host projects rebind this. |
| `DisabledAppUpdateConfigProvider` | Safe default provider that returns `null`, so the module is inactive unless a host project opts in. |
| `AppUpdateConfig` | Root remote-config data model. Contains optional cooldown and platform configs. |
| `AppUpdatePlatformConfig` | Per-platform policy thresholds and store URL. |
| `AppUpdatePolicy` | Pure decision logic that returns `None`, `Optional`, or `Force`. |
| `VersionComparer` | Shared utility comparer used for app versions, build numbers, and data migrations. |
| `ApplicationBuildInfo` | Shared runtime app identity data from `ApplicationServices`. |
| `IApplicationBuildInfoProvider` | Shared runtime app build info boundary from `ApplicationServices`. |
| `UnityApplicationBuildInfoProvider` | Reads `Application.version`, package id, Android `versionCode`, iOS `CFBundleVersion`, and Editor active-target build number. |
| `IAppUpdateTextProvider` | Supplies localized prompt text from client-side keys. |
| `LocalizedAppUpdateTextProvider` | Uses `ILocalizationService`, with English fallback when keys are missing. |
| `IAppUpdatePromptService` | UI boundary for showing the update prompt. |
| `NotificationAppUpdatePromptService` | Opens the common `NotificationPopupPresenter`. |

## 6. Installation

When a host project uses the full Game Foundation installer, shared application services are installed before AppUpdate:

```csharp
ApplicationServiceInstaller.Install(this.Container);
AppUpdateInstaller.Install(this.Container);
```

ApplicationServices default bindings:

```csharp
this.Container.Bind<IApplicationBuildInfoProvider>().To<UnityApplicationBuildInfoProvider>().AsSingle().IfNotBound();
this.Container.Bind<ApplicationVersionService>().AsSingle().IfNotBound();
```

AppUpdate default bindings:

```csharp
this.Container.Bind<AppUpdateTextSettings>().AsSingle().IfNotBound();
this.Container.Bind<AppUpdatePolicy>().AsSingle().IfNotBound();
this.Container.Bind<IApplicationBuildInfoProvider>().To<UnityApplicationBuildInfoProvider>().AsSingle().IfNotBound();
this.Container.Bind<IAppUpdateConfigProvider>().To<DisabledAppUpdateConfigProvider>().AsSingle().IfNotBound();
this.Container.Bind<IAppUpdateTextProvider>().To<LocalizedAppUpdateTextProvider>().AsSingle().IfNotBound();
this.Container.Bind<IAppUpdatePromptService>().To<NotificationAppUpdatePromptService>().AsSingle().IfNotBound();
this.Container.Bind<AppUpdateGateService>().AsSingle().IfNotBound();
```

The AppUpdate installer also binds `IApplicationBuildInfoProvider` with `IfNotBound()` so the module can be installed without the full Game Foundation installer.

Host projects should rebind `IAppUpdateConfigProvider` after installing the concrete remote-config SDK and before startup calls `CheckForUpdateAsync()`.

## 7. Host Project Responsibilities

The module expects the host project to provide:

- A remote-config adapter that implements `IAppUpdateConfigProvider`.
- Remote-config values for each platform that should be gated.
- Valid platform store URLs when an update prompt can be shown.
- Localization table entries if the host wants translated AppUpdate copy.
- A startup call to `AppUpdateGateService.CheckForUpdateAsync()` after remote config and localization are initialized.

The module does not fetch remote config by itself, does not own store identifiers, and does not edit localization tables.

## 8. Remote Config Format

Recommended remote-config key:

```text
app_update_config_v1
```

Recommended JSON:

```json
{
  "softPromptCooldownHours": 24,
  "android": {
    "minVersion": "1.2.0",
    "minBuild": "120",
    "latestVersion": "1.3.0",
    "latestBuild": "130",
    "storeUrl": "market://details?id=com.company.game"
  },
  "ios": {
    "minVersion": "1.2.0",
    "minBuild": "120",
    "latestVersion": "1.3.0",
    "latestBuild": "130",
    "storeUrl": "itms-apps://itunes.apple.com/app/idXXXXXXXX"
  }
}
```

### Field Reference

| Field | Type | Meaning |
|---|---|---|
| `softPromptCooldownHours` | `int` | Cooldown after an optional prompt is shown. `0` disables cooldown. |
| `android` | `AppUpdatePlatformConfig` | Android config. |
| `ios` | `AppUpdatePlatformConfig` | iOS config. |
| `webGL` | `AppUpdatePlatformConfig` | WebGL config. |
| `editor` | `AppUpdatePlatformConfig` | Editor config, useful for manual testing. |

`AppUpdatePlatformConfig` fields:

| Field | Type | Meaning |
|---|---|---|
| `minVersion` | `string` | Force update if current `Application.version` is below this value. |
| `minBuild` | `string` | Force update if current platform build number is below this value. |
| `latestVersion` | `string` | Optional update if current `Application.version` is below this value and minimum checks pass. |
| `latestBuild` | `string` | Optional update if current platform build number is below this value and minimum checks pass. |
| `storeUrl` | `string` | URL opened when the player chooses update. Android falls back to `market://details?id={Application.identifier}` when empty. |

To disable AppUpdate globally, delete the remote-config key or make it empty. To disable it for one platform, omit that platform block or leave all thresholds empty. Empty threshold fields are ignored.

## 9. Policy Behavior

`AppUpdatePolicy.Evaluate()` returns:

| Decision | Condition | Player behavior |
|---|---|---|
| `None` | Config is missing, platform config is missing, no threshold matches, or current build is up to date. | Startup continues. |
| `Force` | Current version/build is below `minVersion` or `minBuild`. | Player sees an update-only prompt. Returning to the game shows the prompt again. |
| `Optional` | Current version/build passes minimum checks but is below `latestVersion` or `latestBuild`. | Player sees an update/later prompt, respecting cooldown. |

If a decision requires a prompt but no store URL can be resolved, `AppUpdateGateService` logs an error and continues startup to avoid trapping the player.

## 10. Build Info

`UnityApplicationBuildInfoProvider` supplies:

- `Platform`: compile-time platform enum.
- `Version`: `Application.version`.
- `BuildNumber`: Android `versionCode` or iOS `CFBundleVersion`.
- `PackageIdentifier`: `Application.identifier`.

Android build number is read through JNI. iOS build number is read through the small native plugin in `Packages/com.gdk.core/Plugins/iOS/GFAppUpdateBuildInfo.mm`.

In Editor, `BuildNumber` returns the active build target's Android `bundleVersionCode` or iOS build number when the active build target is Android or iOS. For other active targets it is empty. Use `editor` config for local manual testing, or bind a test `IApplicationBuildInfoProvider`.

## 11. Client Text And Localization

Default table name:

```text
Default Table
```

Default localization keys:

| Key | English fallback |
|---|---|
| `app_update_force_title` | `Update Required` |
| `app_update_force_message` | `A new version is required to continue playing. Please update the game to keep playing.` |
| `app_update_optional_title` | `Update Available` |
| `app_update_optional_message` | `A new version is available. Update now for the latest fixes and improvements, or continue playing for now.` |
| `app_update_button_update` | `Update` |
| `app_update_button_later` | `Later` |

The text provider treats an empty value, the key itself, or Unity's no-translation message as missing and falls back to English. Host projects may add these keys to their localization tables when translations are ready. Remote config should not contain title, message, or button text.

`AppUpdateTextSettings` can be rebound if a host project uses a different table name or key naming convention.

## 12. UI Behavior

`NotificationAppUpdatePromptService` uses the common notification popup:

- Force update uses `NotificationType.Close`, which shows the update/OK style.
- Optional update uses `NotificationType.Option`, which shows update and later buttons.
- The popup model supports optional `OkButtonText` and `CancelButtonText`.
- `NotificationPopupPresenter` caches the prefab's default button labels and restores them when a later popup does not provide custom labels.
- Popup click listeners are removed before being added, so reused popup instances do not accumulate duplicate callbacks.

## 13. Optional Prompt Cooldown

Optional prompt cooldown is stored in `PlayerPrefs` using this prefix:

```text
GameFoundation.AppUpdate.OptionalPromptNextUtcTicks.
```

The key includes platform and `{latestVersion}_{latestBuild}`. Changing either latest threshold creates a new cooldown identity, so players can see the next optional update campaign without any extra config field.

## 14. Host Adapter Example

Firebase/UGS/ByteBrew adapters should stay in the host game layer. Example:

```csharp
using Cysharp.Threading.Tasks;
using GameFoundation.Scripts.AppUpdate;
using Newtonsoft.Json;

public class RemoteConfigAppUpdateConfigProvider : IAppUpdateConfigProvider
{
    public const string RemoteConfigKey = "app_update_config_v1";

    private readonly IRemoteConfig remoteConfig;

    public RemoteConfigAppUpdateConfigProvider(IRemoteConfig remoteConfig)
    {
        this.remoteConfig = remoteConfig;
    }

    public UniTask<AppUpdateConfig> GetConfigAsync()
    {
        var json = this.remoteConfig.GetRemoteConfigStringValue(RemoteConfigKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return UniTask.FromResult<AppUpdateConfig>(null);
        }

        return UniTask.FromResult(JsonConvert.DeserializeObject<AppUpdateConfig>(json));
    }
}
```

Host installer:

```csharp
this.Container.Rebind<IAppUpdateConfigProvider>()
    .To<RemoteConfigAppUpdateConfigProvider>()
    .AsSingle()
    .NonLazy();
```

Startup:

```csharp
await (
    remoteConfigHandler.SetupConfig(fetchRemoteConfigTimeout),
    localizationService.InitializeAsync()
);

await appUpdateGateService.CheckForUpdateAsync();
```

## 15. Extending The Module

To customize behavior without changing Game Foundation:

- Rebind `IAppUpdateConfigProvider` for a different remote-config source.
- Rebind `IApplicationBuildInfoProvider` for tests, special build channels, or custom version metadata.
- Rebind `IAppUpdateTextProvider` for a different localization system.
- Rebind `IAppUpdatePromptService` if a game needs a branded update UI while keeping the same policy.
- Rebind `AppUpdateTextSettings` for table/key changes.

Keep provider-specific SDK dependencies in the host project. Game Foundation should remain provider-agnostic.

## 16. Testing Checklist

| Scenario | Expected result |
|---|---|
| Remote-config key missing or empty | Startup continues with no prompt. |
| Platform config missing | Startup continues with no prompt. |
| Platform thresholds empty | Startup continues with no prompt. |
| Current version below `minVersion` | Force prompt appears. |
| Current build below `minBuild` | Force prompt appears. |
| Current version/build below latest only | Optional prompt appears. |
| Optional prompt dismissed | Prompt is skipped until cooldown expires. |
| Optional prompt update clicked | Store URL opens and startup continues. |
| Force prompt update clicked | Store URL opens; returning to game shows prompt again. |
| Missing store URL | Error is logged and startup continues. |
| Missing localization keys | English fallback text appears. |
| Popup reused after AppUpdate | Button labels return to prefab defaults unless overridden by the new model. |
| Android runtime build | `versionCode` is read. |
| iOS runtime build | `CFBundleVersion` is read. |

## 17. Agent Notes

- Treat this file as the reusable module source of truth.
- Keep provider-specific remote-config instructions in the host project's agent docs and link back here.
- Do not add remote-config fields for player-facing text unless product direction explicitly changes.
- Update this doc when the module API, JSON format, decision policy, installer behavior, text fallback, build-info provider, or prompt behavior changes.
