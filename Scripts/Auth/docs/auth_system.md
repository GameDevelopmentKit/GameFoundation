# Auth System Spec

> Canonical technical reference for the current Auth module.
> Updated 2026-06-18.
>
> Deep technical design, diagrams, and implementation rationale live in [TDD_AuthSystem.md](TDD_AuthSystem.md).

## 1. Purpose

`Auth` provides a reusable authentication boundary for GameFoundation and the game layer. It hides the concrete provider behind `IAuthService`, exposes a small set of auth state models, and publishes auth changes through both a C# event and Zenject signal.

The current production provider is Unity Gaming Services Authentication. The current editor/default development provider is `EditorAuthService`, a deterministic local mock.

Auth is intentionally local-first friendly: if real auth fails during startup, gameplay should continue using local data and skip cloud backup.

## 2. Source Layout

```text
Packages/com.gdk.core/Scripts/Auth/
|-- IAuthService.cs
|-- AuthState.cs
|-- AuthSignals.cs
|-- AuthConfig.cs
|-- AuthInstaller.cs
|-- EditorAuthService.cs
|-- GameFoundation.Auth.asmdef
|-- UGS/
|   `-- UGSAuthService.cs
`-- Editor/
    |-- AuthConfigEditor.cs
    |-- AuthConfigEditor.uxml
    `-- GameFoundation.Auth.Editor.asmdef
```

Primary game-layer integration points:

| File | Responsibility |
|---|---|
| `Assets/Scripts/UIFeatures/LoadingScene/GameInitializer.cs` | Initializes auth before data managers and backup checks. |
| `Assets/Scripts/UIFeatures/PlayerProfile/AccountLinkingService.cs` | Owns link, unlink, switch, logout, force-save, and provider-token UI flows. |
| `Assets/Scripts/UIFeatures/PlayerProfile/SettingTabView.cs` | Settings UI delegates account operations to `AccountLinkingService`. |
| `Assets/Scripts/DIContexts/GameProjectInstaller.cs` | Calls `AuthInstaller.Install(this.Container)`. |

## 3. System Architecture

```text
GameInitializer
  -> IAuthService.InitializeAsync()
  -> IAuthService.SignInAnonymouslyAsync() when no cached session exists

AuthInstaller
  -> resolves AuthConfig from GDKConfig
  -> binds IAuthService
     -> EditorAuthService in Unity Editor when UseMockInEditor is true
     -> UGSAuthService when UGS_AUTH is defined
     -> EditorAuthService fallback when UGS_AUTH is unavailable
  -> bridges IAuthService.OnAuthStateChanged to AuthStateChangedSignal

Game UI / Services
  -> consume IAuthService for current PlayerId and linked providers
  -> consume AuthStateChangedSignal for state propagation
```

### Assembly Defines

`GameFoundation.Auth.asmdef` references `Unity.Services.Core` and `Unity.Services.Authentication`. Its `versionDefines` create `UGS_AUTH` when `com.unity.services.authentication` is installed.

`UGSAuthService` is compiled only under `#if UGS_AUTH`. Without the package/define, the installer falls back to the editor mock so the project can still compile and run local flows.

## 4. Public API

### `IAuthService`

| Member | Purpose |
|---|---|
| `State` | Current `AuthState`. |
| `IsSignedIn` | True when any authenticated session exists. |
| `IsAnonymous` | True for guest sessions without linked providers. |
| `PlayerId` | Provider-assigned opaque player id, or null when signed out. |
| `LinkedProviders` | Providers linked to the current account. |
| `InitializeAsync()` | Initializes provider SDK and restores cached session if present. |
| `SignInAnonymouslyAsync()` | Creates or restores a guest account. |
| `SignInWithProviderAsync(provider, token)` | Signs in to an existing linked account. |
| `LinkAccountAsync(provider, token)` | Links current session to a provider account. |
| `UnlinkAccountAsync(provider)` | Removes a provider link from the current account. |
| `SignOutAsync()` | Ends the local session and clears local linked-provider state. |
| `OnAuthStateChanged` | C# event carrying `AuthSessionInfo`. |

### State And Result Types

| Type | Current values / role |
|---|---|
| `AuthState` | `Uninitialized`, `Initializing`, `SignedOut`, `Authenticated`, `Error`. |
| `AuthResult` | Sign-in result, including `IsSuccess`, `PlayerId`, `IsAnonymous`, `ErrorMessage`, and `IsSessionRestored`. |
| `LinkResult` | `Success`, `AlreadyLinked`, `Failed`. |
| `AuthProvider` | `Anonymous`, `GooglePlayGames`, `Apple`, `UnityPlayerAccount`, `AppleGameCenter`. |
| `AuthSessionInfo` | Signal/event payload with state, player id, anonymous flag, linked providers, and optional error. |

## 5. Runtime Flows

### Startup

```text
GameInitializer.Initialize()
  -> Open loading screen
  -> InitializeAuthAsync()
     -> authService.InitializeAsync()
        -> provider SDK init
        -> cached session restore if available
     -> if no restored session: SignInAnonymouslyAsync()
     -> exceptions are logged as non-fatal
  -> MasterDataManager.Initialize()
  -> Backup check may run only when authService.IsSignedIn is true
```

`GameInitializer` does not currently consult `AuthConfig.AutoSignInAnonymous`; it always attempts anonymous sign-in when initialization does not restore a session.

### Editor Mock

`EditorAuthService.InitializeAsync()` generates a deterministic player id using `SystemInfo.deviceUniqueIdentifier.GetHashCode()` and the prefix `editor_`. This keeps local save paths stable across editor sessions.

Mock provider behavior:

- `InitializeAsync()` signs in immediately.
- `SignInWithProviderAsync()` marks the same editor id as linked.
- `LinkAccountAsync()` adds a provider unless already present.
- `UnlinkAccountAsync()` removes the provider and returns to anonymous if none remain.
- `SignOutAsync()` clears the local session.

### UGS Runtime

`UGSAuthService.InitializeAsync()` initializes `UnityServices` if needed, subscribes to SDK events, and reports an existing cached UGS session if one is already signed in.

Provider mappings:

| AuthProvider | UGS sign-in/link/unlink methods |
|---|---|
| `GooglePlayGames` | `SignInWithGooglePlayGamesAsync`, `LinkWithGooglePlayGamesAsync`, `UnlinkGooglePlayGamesAsync`. |
| `Apple` | `SignInWithAppleAsync`, `LinkWithAppleAsync`, `UnlinkAppleAsync`. |
| `AppleGameCenter` | `SignInWithAppleGameCenterAsync`, `LinkWithAppleGameCenterAsync`, `UnlinkAppleGameCenterAsync`. |
| `UnityPlayerAccount` | `SignInWithUnityAsync`, `LinkWithUnityAsync`, `UnlinkUnityAsync`. |

`RefreshLinkedProviders()` maps UGS identity type ids into `AuthProvider` values: `google-play-games`, `apple.com`/`apple`, `apple-game-center`, and `unity`.

## 6. Account Linking Integration

Provider token acquisition is not part of the reusable Auth package. The game layer owns token acquisition through `AccountLinkingService` because each provider may require UI, native SDK setup, or platform-specific flow.

Current game-layer behavior:

- Unity Player Account uses `PlayerAccountService.Instance.StartSignInAsync()` when `UGS_AUTH` is available.
- Google Play Games requests a server auth code from the GPGS plugin when available.
- Apple Game Center requests identity verification data from the platform Game Center API.
- Apple Sign-In still shows a coming-soon toast because the game currently displays Game Center on iOS.
- Successful link updates local `UserDataManager` account data and starts a backup.
- Already-linked provider accounts show a switch confirmation flow.
- Unlink preserves local data and updates the UI.
- Logout attempts local save plus backup, signs out, deletes the current local profile, and reloads the game.

## 7. Configuration

`AuthConfig` is an optional `ScriptableObject` registered in `GDKConfig`.

| Field | Default | Current runtime use |
|---|---|---|
| `autoSignInAnonymous` | true | Exposed but not currently read by `GameInitializer`. |
| `useMockInEditor` | true | Used by `AuthInstaller` to choose editor mock vs real UGS in the Editor. |
| `migrateDefaultProfile` | true | Exposed but not currently used by the Auth module. |

## 8. Extension Guide

When adding a new auth provider:

1. Add a value to `AuthProvider` only if it is a supported product requirement.
2. Add sign-in/link/unlink handling in `UGSAuthService`.
3. Extend `RefreshLinkedProviders()` with the UGS identity type id.
4. Add game-layer token acquisition in `AccountLinkingService`.
5. Add platform display rules in `AccountLinkingService.GetDisplayedProviders()`.
6. Update this doc and [TDD_AuthSystem.md](TDD_AuthSystem.md) if the architecture, flow, or provider contract changes.

When replacing UGS with another backend, keep `IAuthService` stable and create a new implementation bound by `AuthInstaller` or a project-specific installer.

## 9. Known Limits

- Facebook is not part of the current `AuthProvider` enum and is not supported by the current implementation.
- Google Play Games requires a GPGS plugin/runtime that exposes `RequestServerSideAccess(bool, Action<string>)`.
- Apple Game Center requires an iOS Game Center-authenticated local user and platform identity verification data.
- `AuthConfig.AutoSignInAnonymous` and `AuthConfig.MigrateDefaultProfile` are currently configuration surface without runtime behavior.
- There is no explicit auth timeout setting in the Auth module; startup relies on provider calls and `GameInitializer` exception handling.
- `EditorAuthService` uses `string.GetHashCode()`, which is stable enough for local editor convenience in this context but should not be used as a cross-machine/player identity scheme.

## 10. Testing Checklist

| Scenario | Expected result |
|---|---|
| Editor with `UseMockInEditor = true` | Mock signs in with `editor_XXXXXXXX`; `AuthStateChangedSignal` fires. |
| Editor with `UseMockInEditor = false` and UGS installed | Real UGS anonymous sign-in returns a UGS player id. |
| No UGS package/define | Project compiles and falls back to `EditorAuthService`. |
| Anonymous sign-in failure | Startup logs warning and continues without blocking local gameplay. |
| Link Unity Player Account | Provider appears in `LinkedProviders`; Settings UI refreshes; backup is requested. |
| Unlink last provider | Auth returns to anonymous; local data is preserved. |
| Already-linked provider | UI offers switch flow; current linked data is backed up before switch when possible. |
