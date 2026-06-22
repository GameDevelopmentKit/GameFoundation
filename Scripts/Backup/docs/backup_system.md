# Backup System Spec

> Canonical technical reference for the current Backup module.
> Updated 2026-06-18.
>
> Deep technical design, diagrams, and implementation rationale live in [TDD_BackupSystem.md](TDD_BackupSystem.md).

## 1. Purpose

`Backup` provides cloud backup and recovery for local save data. It is deliberately not a storage provider and does not replace `LocalSave`. Local data remains primary; Backup is a separate orchestration layer that pushes profile data to cloud and restores it when needed.

The current cloud provider is Unity Gaming Services Cloud Save through `UGSBackupProvider`.

Backup is auth-gated and non-fatal. If the player is not authenticated, has no network, or UGS Cloud Save is unavailable, local gameplay continues.

## 2. Source Layout

```text
Packages/com.gdk.core/Scripts/Backup/
|-- IBackupService.cs
|-- IBackupProvider.cs
|-- BackupManager.cs
|-- BackupManifest.cs
|-- BackupConfig.cs
|-- BackupInstaller.cs
|-- BackupLifecycleHandler.cs
|-- BackupConflictSignal.cs
|-- BackupExcludeAttribute.cs
|-- ConflictInfo.cs
|-- RecoveryResult.cs
|-- GameFoundation.Backup.asmdef
|-- UGS/
|   `-- UGSBackupProvider.cs
`-- Editor/
    |-- BackupConfigEditor.cs
    |-- BackupConfigEditor.uxml
    `-- GameFoundation.Backup.Editor.asmdef
```

Primary dependencies and integrations:

| Module | Role |
|---|---|
| Auth | `IAuthService` gates all cloud operations. |
| LocalSave | `IHandleLocalDataServices` reads and applies profile data. |
| UGS Cloud Save | `UGSBackupProvider` reads/writes cloud keys when `UGS_CLOUD_SAVE` is defined. |
| Game UI | `ConflictResolutionService` handles `BackupConflictSignal`. |
| Game startup | `GameInitializer` calls `CheckAndRecoverAsync()` before data managers load. |

## 3. System Architecture

```text
GameInitializer / AccountLinkingService / BackupLifecycleHandler
  -> IBackupService
     -> BackupManager
        -> IAuthService
        -> IHandleLocalDataServices
        -> IBackupProvider
           -> UGSBackupProvider
              -> Unity.Services.CloudSave
        -> SignalBus
           -> BackupConflictSignal
              -> game-side ConflictResolutionService
```

The module is split into two interfaces:

- `IBackupService`: orchestration API used by startup, account linking, lifecycle, and UI flows.
- `IBackupProvider`: profile-level cloud I/O API. It knows cloud keys and payloads, but not recovery policy.

This split keeps cloud storage mechanics separate from local data comparison, conflict handling, and auth guards.

## 4. Cloud Key Design

UGS Cloud Save stores one lightweight manifest plus one data payload per profile.

| Key | Content | Purpose |
|---|---|---|
| `backup_manifest` | `BackupManifest` JSON with `ProfileRegistry` and metadata map. | One read can compare all profiles. |
| `backup_data_{profileId}` | `Dictionary<string, string>` of LocalSave keys to JSON strings. | Heavy profile data payload. |

`BackupConfig.ManifestCloudKey` and `BackupConfig.ProfileDataKeyPrefix` can override these names. Changing them after release orphans existing cloud data unless a migration is written.

`UGSBackupProvider` uses `TypeNameHandling.None`, `MissingMemberHandling.Ignore`, `NullValueHandling.Ignore`, and `StringEnumConverter` for cloud JSON.

## 5. Public API

### `IBackupService`

| Member | Purpose |
|---|---|
| `Status` | Current `BackupStatus`: `Idle`, `BackingUp`, `Restoring`, `Offline`, or `Error`. |
| `BackupCurrentProfileAsync()` | Pushes the current local profile to cloud. |
| `CheckAndRecoverAsync()` | Compares local metadata to cloud manifest and restores/backs up/resolves conflicts. |
| `RestoreProfileAsync(profileId)` | Restores one cloud profile payload to the current local profile. |
| `OnRecoveryCompleted` | Event fired after a recovery check completes. |

### `IBackupProvider`

| Member | Purpose |
|---|---|
| `ProviderName` | Human-readable provider name. |
| `IsAvailable` | Provider availability, currently based on UGS package and network reachability. |
| `FetchManifestAsync()` | Reads the cloud manifest. |
| `SaveManifestAsync(manifest)` | Writes the cloud manifest. |
| `BackupProfileDataAsync(profileId, data)` | Writes profile data payload. |
| `RestoreProfileDataAsync(profileId)` | Reads profile data payload. |
| `DeleteProfileDataAsync(profileId)` | Deletes profile data payload. |

## 6. Runtime Flows

### Startup Recovery Check

```text
GameInitializer
  -> auth completes
  -> MasterDataManager.Initialize()
  -> backupService.CheckAndRecoverAsync()
     -> skip if backup disabled, signed out, offline, or already busy
     -> fetch backup_manifest
     -> compare local ProfileMetadata.BackupVersion to cloud metadata
     -> restore cloud data before data managers load, when cloud is newer and safe
  -> MasterDataManager.InitializeAllRegisteredDataManagers()
```

### Recovery Outcomes

| Case | Behavior |
|---|---|
| No manifest | Run first-time backup and return `NoBackup`. |
| Current profile missing in manifest | Run first backup and return `NoBackup`. |
| Versions match | Return `InSync`. |
| Local version newer | Back up local data and return `BackedUp`. |
| Cloud version newer, no local changes | Restore cloud payload and return `Restored`. |
| Cloud version newer, local changes exist | Fire `BackupConflictSignal`, apply chosen resolution, return `ConflictResolved`. |
| Guard failure or network unavailable | Return `Offline` or no-op depending on API. |
| Exception | Log, set status `Error`, return `Failed` for recovery checks. |

### Backup Flow

```text
BackupCurrentProfileAsync()
  -> skip if disabled, signed out, offline, or already backing up
  -> GetProfileDataForBackupAsync()
  -> BackupProfileDataAsync("default", data)
  -> IncrementBackupVersion()
  -> SaveCurrentProfileMetadataAsync()
  -> update BackupManifest metadata and registry
  -> SaveManifestAsync(manifest)
```

The current `BackupManager.GetCurrentProfileId()` returns `"default"`. Multi-profile cloud restore needs a future profile-id resolution pass.

### Manual Restore

`RestoreProfileAsync(profileId)` fetches `backup_data_{profileId}` and applies it through `IHandleLocalDataServices.ApplyBackupDataAsync(data)`. This is used by account-switch flows.

## 7. Conflict Resolution

`BackupManager` creates a `BackupConflictSignal` with `ConflictInfo` containing local and cloud `ProfileMetadata`. It fires the signal through `SignalBus` and waits for the signal's `UniTaskCompletionSource<ConflictChoice>`.

Resolution behavior:

- `KeepLocal`: back up the local profile to cloud.
- `KeepCloud`: restore cloud profile data locally.
- No handler or timeout: default to `KeepLocal`.

The current code waits up to 300 seconds in `RequestConflictResolutionAsync()`. `BackupConfig.ConflictTimeoutSeconds` exists but is not currently used by that method.

## 8. Lifecycle Hooks

`BackupInstaller` creates `BackupLifecycleHandler` as a persistent MonoBehaviour named `[BackupLifecycle]`.

| Trigger | Current behavior |
|---|---|
| `OnApplicationPause(true)` | Save current profile, then backup if `BackupConfig.BackupOnAppPause` allows it. |
| `OnApplicationPause(false)` | Check and recover if `BackupConfig.CheckOnAppResume` allows it. |
| Startup | `GameInitializer` calls `CheckAndRecoverAsync()` when authenticated. |
| Account link | `AccountLinkingService` requests backup after successful link. |
| Account switch | `AccountLinkingService` restores profile data before reload. |
| Force save/logout | `AccountLinkingService` saves locally and requests backup. |

## 9. LocalSave Integration

Backup reads and writes through `IHandleLocalDataServices`:

| Method | Role |
|---|---|
| `GetProfileDataForBackupAsync()` | Reads saved provider data as key-to-JSON pairs. |
| `ApplyBackupDataAsync(data)` | Writes restored key-to-JSON pairs to the current local profile and updates cache. |
| `GetCurrentProfileMetadata()` | Provides local backup version and timestamps for comparison. |
| `IncrementBackupVersion()` | Updates metadata after successful backup or accepted cloud restore. |
| `SaveCurrentProfileMetadataAsync()` | Persists metadata changes. |

Important: backup reads saved provider data. If gameplay changes are still only in LocalSave memory cache, cloud backup may upload older data. Callers that need a strong checkpoint should save locally before backup.

## 10. Configuration

`BackupConfig` is an optional `ScriptableObject` registered in `GDKConfig`.

| Field | Default | Current runtime use |
|---|---|---|
| `enableBackup` | true | Guards backup and recovery. |
| `backupOnAppPause` | true | Used by lifecycle handler. |
| `checkOnAppResume` | true | Used by lifecycle handler. |
| `checkOnLaunch` | true | Exposed; startup currently calls backup from `GameInitializer` directly. |
| `manifestCloudKey` | `backup_manifest` | Used by `UGSBackupProvider`. |
| `profileDataKeyPrefix` | `backup_data_` | Used by `UGSBackupProvider`. |
| `maxSnapshotSizeWarningKB` | 1024 | Used by `UGSBackupProvider` to log large payload warnings. |
| `conflictTimeoutSeconds` | 120 | Exposed but current conflict wait is hard-coded to 300 seconds. |

## 11. Extension Guide

When adding a new cloud backend:

1. Implement `IBackupProvider`.
2. Bind it in `BackupInstaller` or a project-specific installer.
3. Preserve the manifest-plus-data separation unless the new backend requires a different storage model.
4. Keep `BackupManager` responsible for comparison and conflict policy.

When adding server-authoritative writes:

1. Keep client reads and comparison behavior stable.
2. Move cloud writes behind Cloud Code or a backend provider implementation.
3. Update `BackupConfig` and access-tier docs.
4. Add migration notes for existing client-writable keys.

## 12. Known Limits

- UGS Cloud Save operations are no-op/log-only when `UGS_CLOUD_SAVE` is not defined.
- Startup does not currently check `BackupConfig.CheckOnLaunch` before calling `CheckAndRecoverAsync()`.
- Conflict wait uses a hard-coded 300 seconds instead of `BackupConfig.ConflictTimeoutSeconds`.
- Current profile id is hard-coded as `default` in `BackupManager`.
- Backup is not a durability scheduler; it depends on LocalSave having flushed current data to the provider.
- Client currently writes cloud data directly. Server-authoritative writes are future work.

## 13. Testing Checklist

| Scenario | Expected result |
|---|---|
| Signed out startup | Backup check is skipped as offline/non-authenticated. |
| No network | Backup/recovery logs offline and does not block gameplay. |
| No cloud manifest | First-time backup creates manifest and data key. |
| Local version newer | Local data is backed up and manifest metadata updates. |
| Cloud version newer with no local changes | Cloud data is applied before data managers load. |
| Cloud and local both changed | `BackupConflictSignal` fires and chosen resolution is applied. |
| App pause | Local save is requested, then backup runs when enabled. |
| Missing UGS Cloud Save package | Provider logs skipped operations and returns null/no-op safely. |
| Large payload | Warning logs when payload exceeds `MaxSnapshotSizeWarningKB`. |
