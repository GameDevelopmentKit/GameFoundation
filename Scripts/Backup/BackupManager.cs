namespace GameFoundation.Scripts.Backup
{
    using System;
    using Cysharp.Threading.Tasks;
    using Auth;
    using DataManager.LocalSave.Handler;
    using DataManager.LocalSave.Profile;
    using Utilities.LogService;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Core backup orchestrator. Implements <see cref="IBackupService"/>.
    ///
    /// Coordinates between:
    ///   - <see cref="IBackupProvider"/> (cloud I/O)
    ///   - <see cref="IHandleLocalDataServices"/> (local data access)
    ///   - <see cref="IAuthService"/> (authentication gate)
    ///   - <see cref="SignalBus"/> for conflict resolution (fires <see cref="BackupConflictSignal"/>)
    ///
    /// Recovery flow (CheckAndRecoverAsync):
    ///   1. Fetch manifest from cloud (1 API call)
    ///   2. Compare local BackupVersion vs cloud BackupVersion
    ///   3. No backup → first-time backup
    ///   4. In sync → no-op
    ///   5. Local newer → push backup
    ///   6. Cloud newer + no local changes → restore
    ///   7. Cloud newer + local changes → fires BackupConflictSignal; awaits resolution TCS
    /// </summary>
    public class BackupManager : IBackupService
    {
        private readonly IBackupProvider backupProvider;
        private readonly IHandleLocalDataServices localDataServices;
        private readonly IAuthService authService;
        private readonly BackupConfig config;
        private readonly ILogService logService;
        private readonly SignalBus signalBus;

        private BackupStatus status = BackupStatus.Idle;
        private bool isBackingUp;

        /// <summary>Cached manifest from the last FetchManifestAsync call.</summary>
        private BackupManifest cachedManifest;

        public BackupStatus Status => this.status;
        public event Action<RecoveryResult> OnRecoveryCompleted;

        public BackupManager(
            IBackupProvider backupProvider,
            IHandleLocalDataServices localDataServices,
            IAuthService authService,
            BackupConfig config,
            ILogService logService,
            SignalBus signalBus)
        {
            this.backupProvider = backupProvider;
            this.localDataServices = localDataServices;
            this.authService = authService;
            this.config = config;
            this.logService = logService;
            this.signalBus = signalBus;
        }

        #region IBackupService

        public async UniTask<RecoveryResult> CheckAndRecoverAsync()
        {
            // Guard: backup disabled
            if (this.config != null && !this.config.EnableBackup)
            {
                this.logService.Log("[Backup] Backup disabled in config.");
                return RecoveryResult.Offline();
            }

            // Guard: not authenticated
            if (!this.authService.IsSignedIn)
            {
                this.logService.Log("[Backup] Not authenticated. Skipping recovery check.");
                this.status = BackupStatus.Offline;
                return RecoveryResult.Offline();
            }

            // Guard: no network
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                this.logService.Log("[Backup] No network. Skipping recovery check.");
                this.status = BackupStatus.Offline;
                return RecoveryResult.Offline();
            }

            // Guard: already in progress
            if (this.status == BackupStatus.BackingUp || this.status == BackupStatus.Restoring)
            {
                this.logService.Log("[Backup] Operation already in progress. Skipping.");
                return RecoveryResult.InSync();
            }

            this.status = BackupStatus.Restoring;
            RecoveryResult result;

            try
            {
                result = await this.PerformRecoveryCheckInternal();
                this.status = BackupStatus.Idle;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Recovery check failed: {ex.Message}");
                this.status = BackupStatus.Error;
                result = RecoveryResult.Failed(ex.Message);
            }

            this.logService.LogWithColor($"[Backup] Recovery result: {result.Outcome}", Color.cyan);
            this.OnRecoveryCompleted?.Invoke(result);
            return result;
        }

        public async UniTask BackupCurrentProfileAsync()
        {
            // Guard: backup disabled
            if (this.config != null && !this.config.EnableBackup)
            {
                this.logService.Log("[Backup] Backup disabled in config.");
                return;
            }

            // Guard: not authenticated
            if (!this.authService.IsSignedIn)
            {
                this.logService.Warning("[Backup] Cannot backup: not authenticated.");
                return;
            }

            // Guard: no network
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                this.logService.Log("[Backup] No network. Skipping backup.");
                return;
            }

            // Guard: concurrent backup
            if (this.isBackingUp)
            {
                this.logService.Log("[Backup] Backup already in progress. Skipping.");
                return;
            }

            this.isBackingUp = true;
            this.status = BackupStatus.BackingUp;

            try
            {
                await this.PerformBackupInternal();
                this.status = BackupStatus.Idle;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Backup failed: {ex.Message}");
                this.status = BackupStatus.Error;
            }
            finally
            {
                this.isBackingUp = false;
            }
        }

        public async UniTask RestoreProfileAsync(string profileId)
        {
            this.logService.Log($"[Backup] Manual restore requested for profile '{profileId}'.");

            // Guard checks
            if (!this.authService.IsSignedIn)
            {
                this.logService.Warning("[Backup] Cannot restore: not authenticated.");
                return;
            }

            this.status = BackupStatus.Restoring;

            try
            {
                var cloudMetadata = await this.TryFetchCloudMetadataAsync(profileId);
                var restoredCount = await this.RestoreCloudProfileDataAsync(profileId, cloudMetadata);
                if (restoredCount >= 0)
                {
                    this.logService.LogWithColor($"[Backup] Restored profile '{profileId}' ({restoredCount} keys).",
                        Color.green);
                }
                else
                {
                    this.logService.Warning($"[Backup] No backup data found for profile '{profileId}'.");
                }

                this.status = BackupStatus.Idle;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Restore failed: {ex.Message}");
                this.status = BackupStatus.Error;
            }
        }

        #endregion

        #region Core Logic

        private async UniTask<RecoveryResult> PerformRecoveryCheckInternal()
        {
            // Step 1: Fetch manifest from cloud (1 API call)
            this.cachedManifest = await this.backupProvider.FetchManifestAsync();

            // Step 2: Get local metadata for current profile
            var localMetadata = this.localDataServices.GetCurrentProfileMetadata();
            var localVersion = localMetadata?.BackupVersion ?? 0;
            var currentProfileId = this.GetCurrentProfileId(localMetadata);

            // Case A: No manifest exists (first time)
            if (this.cachedManifest == null)
            {
                this.logService.Log("[Backup] No cloud backup found. Performing first-time backup...");
                await this.PerformBackupInternal();
                return RecoveryResult.NoBackup();
            }

            // Case B: Profile not in manifest
            var cloudMetadata = this.cachedManifest.GetMetadata(currentProfileId);
            if (cloudMetadata == null)
            {
                this.logService.Log(
                    $"[Backup] Profile '{currentProfileId}' not in manifest. Performing first backup...");
                await this.PerformBackupInternal();
                return RecoveryResult.NoBackup();
            }

            var cloudVersion = cloudMetadata.BackupVersion;

            // Case C: In sync
            if (localVersion == cloudVersion)
            {
                this.logService.Log($"[Backup] In sync (v{localVersion}).");
                return RecoveryResult.InSync();
            }

            // Case D: Local is newer → push backup
            if (localVersion > cloudVersion)
            {
                this.logService.Log($"[Backup] Local is newer (v{localVersion} > v{cloudVersion}). Backing up...");
                await this.PerformBackupInternal();
                return RecoveryResult.BackedUp();
            }

            // Case E: Cloud is newer → check for local changes
            var lastBackup = localMetadata?.LastBackupTimestamp;
            var localHasChanges = lastBackup.HasValue && localMetadata.LastSavedAt > lastBackup.Value;

            if (!localHasChanges)
            {
                // No local changes since last backup → safe to restore
                this.logService.Log($"[Backup] Cloud is newer (v{cloudVersion} > v{localVersion}). Restoring...");
                await this.RestoreCloudProfileDataAsync(currentProfileId, cloudMetadata);

                return RecoveryResult.Restored();
            }

            // Case F: CONFLICT — both sides have changes → fire signal, await resolution
            this.logService.LogWithColor("[Backup] Conflict detected! Both sides have changes.", Color.yellow);

            var conflictInfo = new ConflictInfo
            {
                LocalMetadata = localMetadata,
                CloudMetadata = cloudMetadata
            };

            var choice = await this.RequestConflictResolutionAsync(conflictInfo);

            // Execute resolution
            switch (choice)
            {
                case ConflictChoice.KeepLocal:
                    this.logService.Log("[Backup] Conflict resolved: Keep Local. Backing up...");
                    await this.PerformBackupInternal();
                    break;

                case ConflictChoice.KeepCloud:
                    this.logService.Log("[Backup] Conflict resolved: Keep Cloud. Restoring...");
                    await this.RestoreCloudProfileDataAsync(currentProfileId, cloudMetadata);

                    break;
            }

            return RecoveryResult.ConflictResolved();
        }

        private async UniTask<int> RestoreCloudProfileDataAsync(string profileId, ProfileMetadata cloudMetadata)
        {
            var data = await this.backupProvider.RestoreProfileDataAsync(profileId);
            if (data == null)
            {
                return -1;
            }

            await this.localDataServices.ApplyBackupDataAsync(data, cloudMetadata == null);

            if (cloudMetadata != null)
            {
                await this.localDataServices.ApplyCloudProfileMetadataAsync(cloudMetadata);
            }
            else
            {
                this.logService.Warning(
                    $"[Backup] Restored profile '{profileId}' without cloud metadata. Future recovery may need another comparison.");
            }

            return data.Count;
        }

        private async UniTask<ProfileMetadata> TryFetchCloudMetadataAsync(string profileId)
        {
            try
            {
                this.cachedManifest = await this.backupProvider.FetchManifestAsync();
                return this.cachedManifest?.GetMetadata(profileId);
            }
            catch (Exception ex)
            {
                this.logService.Warning(
                    $"[Backup] Failed to fetch cloud metadata for profile '{profileId}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fires <see cref="BackupConflictSignal"/> and awaits the resolution TCS.
        /// Falls back to KeepLocal if no handler resolves within the timeout or if
        /// the SignalBus has no subscribers for the signal.
        /// </summary>
        private async UniTask<ConflictChoice> RequestConflictResolutionAsync(ConflictInfo info)
        {
            var signal = new BackupConflictSignal(info);

            try
            {
                this.signalBus.Fire(signal);
            }
            catch (Exception ex)
            {
                // SignalBus throws if no subscribers are declared — safe default
                this.logService.Warning(
                    $"[Backup] BackupConflictSignal has no handler ({ex.Message}). Defaulting to KeepLocal.");
                return ConflictChoice.KeepLocal;
            }

            // Wait for the handler to resolve (with a generous timeout)
            var timeout = UniTask.Delay(TimeSpan.FromSeconds(300)); // 5 min max
            var resolved = signal.Resolution.Task;

            var (isResolved, choice) = await UniTask.WhenAny(resolved, timeout);
            if (isResolved)
            {
                return choice;
            }

            this.logService.Warning("[Backup] Conflict resolution timed out. Defaulting to KeepLocal.");
            return ConflictChoice.KeepLocal;
        }

        /// <summary>
        /// Perform a full backup of the current profile.
        /// Writes data key + updates manifest in 2 API calls.
        /// </summary>
        private async UniTask PerformBackupInternal()
        {
            var localMetadata = this.localDataServices.GetCurrentProfileMetadata();
            var currentProfileId = this.GetCurrentProfileId(localMetadata);

            // Step 1: Get all data for this profile
            var profileData = await this.localDataServices.GetProfileDataForBackupAsync();

            // Step 2: Upload data payload (1 write — heavy)
            await this.backupProvider.BackupProfileDataAsync(currentProfileId, profileData);

            // Step 3: Update local metadata
            this.localDataServices.IncrementBackupVersion();
            await this.localDataServices.SaveCurrentProfileMetadataAsync();

            // Re-read metadata after increment
            localMetadata = this.localDataServices.GetCurrentProfileMetadata();

            // Step 4: Update manifest and save (1 write — lightweight)
            if (this.cachedManifest == null)
            {
                this.cachedManifest = new BackupManifest
                {
                    Registry = ProfileRegistry.CreateDefault(currentProfileId)
                };
            }

            this.cachedManifest.SetMetadata(currentProfileId, localMetadata);
            if (!this.cachedManifest.Registry.HasProfile(currentProfileId))
            {
                this.cachedManifest.Registry.AddProfile(currentProfileId);
            }

            await this.backupProvider.SaveManifestAsync(this.cachedManifest);

            this.logService.LogWithColor(
                $"[Backup] Backed up profile '{currentProfileId}' (v{localMetadata?.BackupVersion}, {profileData.Count} keys).",
                Color.green);
        }

        /// <summary>
        /// Get the current profile ID. Falls back to "default" if metadata is unavailable.
        /// </summary>
        private string GetCurrentProfileId(ProfileMetadata metadata)
        {
            // ProfileMetadata doesn't carry its own ID — use the local data services
            // to determine current profile. For now, default to "default".
            // TODO: When multi-profile is fully supported, resolve from ProfileRegistry.
            return "default";
        }

        #endregion
    }
}
