namespace GameFoundation.Scripts.Backup
{
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave;
    using DataManager.LocalSave.Handler;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Persistent MonoBehaviour that triggers backup operations on application lifecycle events.
    /// Survives scene transitions via DontDestroyOnLoad.
    ///
    /// Created by <see cref="BackupInstaller"/> and marked DontDestroyOnLoad.
    ///
    /// Replaces the old <c>CloudSyncLifecycleHandler</c>.
    ///
    /// Triggers:
    ///   - OnApplicationPause(true)  -> Save local data FIRST, then backup to cloud
    ///   - OnApplicationPause(false) -> Recovery check to detect changes from other devices
    ///   - OnApplicationQuit         -> No-op on mobile (process killed before async work completes)
    ///
    /// IMPORTANT: This handler ensures local data is saved to disk BEFORE reading it for cloud
    /// upload. Without this sequencing, the upload could read stale or partially-written data.
    ///
    /// Coordinates with <see cref="ApplicationService"/> (when available) to avoid issuing a
    /// redundant parallel save on the same pause event - that double-save was an aggravating
    /// factor in ANR cluster A1 (v0.2.2.69).
    /// </summary>
    public class BackupLifecycleHandler : MonoBehaviour
    {
        [Inject]                            private IBackupService            backupService;
        [Inject]                            private IHandleLocalDataServices  localDataServices;
        [Inject(Optional = true)]           private ApplicationService        applicationService;

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (this.backupService == null) return;
            if (this.localDataServices == null || !this.localDataServices.IsInitialized) return;

            if (pauseStatus)
            {
                // App going to background -> save local data first (if not already in flight), then backup.
                Debug.Log("[Backup] App paused. Saving local data then backing up to cloud...");
                SaveThenBackupAsync().Forget();
            }
            else
            {
                // App resumed -> recovery check to detect changes from other devices
                Debug.Log("[Backup] App resumed. Checking for recovery...");
                this.backupService.CheckAndRecoverAsync().Forget();
            }
        }

        private void OnApplicationQuit()
        {
            // Intentionally a no-op on mobile.
            // On Android/iOS, OnApplicationQuit gives ~0 frames after returning.
            // Cloud upload reliability comes from OnApplicationPause(true) instead.
            Debug.Log("[Backup] App quitting. Relying on pause-backup for cloud sync.");
        }

        /// <summary>
        /// Sequence: ensure local data is on disk -> backup to cloud.
        /// If ApplicationService has already started a save (OnApplicationPause fires both handlers
        /// in the same frame), wait for it to finish instead of issuing a parallel save - that
        /// double-save doubled the I/O pressure during the surfaceDestroyed window and contributed
        /// to ANR cluster A1.
        /// </summary>
        private async UniTaskVoid SaveThenBackupAsync()
        {
            try
            {
                // Step 1: Ensure all cached data is written to disk
                if (this.applicationService != null && this.applicationService.IsSaving)
                {
                    // ApplicationService is already saving - wait for it instead of stacking another save.
                    // Bound the wait to avoid hanging the backup if the save stalls.
                    await UniTask.WaitUntil(() => !this.applicationService.IsSaving)
                        .Timeout(System.TimeSpan.FromSeconds(10));
                }
                else
                {
                    await this.localDataServices.SaveCurrentProfile();
                }

                // Step 2: Now safe to backup - disk has the latest data
                await this.backupService.BackupCurrentProfileAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Backup] Save-then-backup failed: {ex.Message}");
            }
        }
    }
}
