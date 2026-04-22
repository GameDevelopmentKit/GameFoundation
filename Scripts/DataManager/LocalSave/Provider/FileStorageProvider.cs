namespace DataManager.LocalSave.Provider
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;

    /// <summary>
    /// File-based storage provider.
    /// Stores JSON files on disk at Application.persistentDataPath.
    /// 
    /// Directory structure:
    ///   {persistentDataPath}/{saveDataFolder}/
    ///     _global/           ← Profile registry and global data (profileId == null)
    ///       __ProfileRegistry__.json
    ///     {profileId}/       ← Per-profile data
    ///       ProfileMetadata.json
    ///       LD-UserCommonData.json
    ///       LD-InventoryData.json
    ///       ...
    /// 
    /// Features:
    /// - Atomic writes (write to .tmp → .bak → rename) to prevent corruption on crash
    /// - At least one valid copy always exists on disk during saves
    /// - Automatic recovery of interrupted saves on startup (.tmp/.bak promotion)
    /// - Auto-creates directories as needed
    /// </summary>
    public class FileStorageProvider : IStorageProvider
    {
        private const string GlobalFolderName = "_global";
        private const string JsonExtension = ".json";
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Per-key locks to prevent concurrent writes to the same file.
        /// Multiple saves to different keys still run in parallel.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();

        private readonly string rootPath;
        private readonly ILogService logService;

        public string ProviderName => "File";
        public bool IsAvailable => true;
        public bool UseEncryption { get; }

        /// <summary>
        /// Create from a StorageProviderConfig.
        /// </summary>
        public FileStorageProvider(StorageProviderConfig config, ILogService logService)
            : this(logService,
                !string.IsNullOrEmpty(config?.SaveDataFolderName) ? config.SaveDataFolderName : "SaveData",
                config?.EnableEncryption ?? true)
        {
        }

        /// <summary>
        /// Create directly (used by editor tools and migration utilities).
        /// </summary>
        public FileStorageProvider(ILogService logService, string saveDataFolderName = "SaveData", bool useEncryption = true)
        {
            this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
            this.UseEncryption = useEncryption;
            this.rootPath = Path.Combine(Application.persistentDataPath, saveDataFolderName);
            this.logService.Log($"[FileStorage] Root path: {this.rootPath}");
        }

        public UniTask InitializeAsync()
        {
            // Ensure root directory exists (CreateDirectory is idempotent)
            Directory.CreateDirectory(this.rootPath);

            // Recover from any interrupted saves
            this.RecoverOrphanedTempFiles();

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Save raw JSON string to a file (atomic write: .tmp → .bak → rename).
        /// At least one valid copy always exists on disk.
        /// </summary>
        public async UniTask SaveAsync(string profileId, string key, string json)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Storage key must not be null or empty.", nameof(key));
            }

            var filePath = this.GetFilePath(profileId, key);

            // Acquire per-key lock to prevent concurrent writes to the same file.
            // This fixes "Sharing violation" errors when multiple callers save the same key simultaneously.
            var semaphore = this.fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                await this.SaveAsyncInternal(filePath, key, json);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Internal save implementation. Must be called under the per-key semaphore.
        /// All disk I/O (write, fsync, atomic rename) runs on a background thread to avoid
        /// blocking the main thread during OnApplicationPause/OnApplicationFocus, which was
        /// the root cause of ANR cluster A1 in v0.2.2.69.
        /// </summary>
        private async UniTask SaveAsyncInternal(string filePath, string key, string json)
        {
            var tempPath = filePath + TempExtension;
            var backupPath = filePath + BackupExtension;

            // Move all blocking I/O off the main thread.
            // fs.Flush(flushToDisk: true) is a synchronous fsync syscall that can block
            // 5-200 ms per file on Android flash; calling it on the main thread during
            // app pause causes the ANR seen in cluster A1.
            await UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    // Ensure directory exists (CreateDirectory is idempotent)
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Write to temp file with explicit flush to disk for mobile durability
                    using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                        FileShare.None, 4096, FileOptions.WriteThrough))
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(json);
                        writer.Flush();
                        fs.Flush(flushToDisk: true);
                    }

                    // Safe atomic rename: original -> .bak -> move temp -> delete .bak
                    // At every step, at least one valid file exists on disk.
                    if (File.Exists(filePath))
                    {
                        // Remove stale .bak if it exists, then move original to .bak
                        this.TryDeleteFile(backupPath);
                        File.Move(filePath, backupPath);
                    }

                    File.Move(tempPath, filePath);

                    // Clean up backup after successful rename
                    this.TryDeleteFile(backupPath);
                }
                catch (Exception ex)
                {
                    // Attempt recovery: if original was moved to .bak but temp->target failed,
                    // restore the backup so we don't lose the previous save.
                    if (!File.Exists(filePath) && File.Exists(backupPath))
                    {
                        try
                        {
                            File.Move(backupPath, filePath);
                        }
                        catch (Exception restoreEx)
                        {
                            this.logService.Error($"[FileStorage] Failed to restore backup for {key}: {restoreEx.Message}");
                        }
                    }

                    // Clean up temp file on failure
                    this.TryDeleteFile(tempPath);
                    this.logService.Error($"[FileStorage] Failed to save {key}: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Load raw JSON string from a file.
        /// Returns null only when the file genuinely does not exist.
        /// Throws on I/O errors to prevent the caller from silently overwriting real data.
        /// Disk read runs on a background thread to avoid blocking the main thread.
        /// </summary>
        public async UniTask<string> LoadAsync(string profileId, string key)
        {
            var filePath = this.GetFilePath(profileId, key);

            try
            {
                return await UniTask.RunOnThreadPool(() =>
                {
                    if (!File.Exists(filePath))
                    {
                        return (string)null;
                    }
                    return File.ReadAllText(filePath);
                });
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[FileStorage] Failed to load {key}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete a JSON file by key.
        /// Also cleans up any associated .tmp and .bak files.
        /// </summary>
        public UniTask DeleteAsync(string profileId, string key)
        {
            var filePath = this.GetFilePath(profileId, key);
            this.TryDeleteFile(filePath);
            this.TryDeleteFile(filePath + TempExtension);
            this.TryDeleteFile(filePath + BackupExtension);
            return UniTask.CompletedTask;
        }

        #region Public Helpers (used by legacy migrator)

        /// <summary>
        /// Get the directory path for a profile's data folder.
        /// Used by LegacyPlayerPrefsMigrator to write files directly.
        /// </summary>
        public string GetProfileDirectoryPath(string profileId)
        {
            return Path.Combine(this.rootPath, GetProfileFolder(profileId));
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Resolve the folder name for a profile ID.
        /// Null/empty profileId maps to the global folder.
        /// </summary>
        private static string GetProfileFolder(string profileId)
        {
            return string.IsNullOrEmpty(profileId) ? GlobalFolderName : profileId;
        }

        /// <summary>
        /// Build the full file path for a given profile and key.
        /// </summary>
        private string GetFilePath(string profileId, string key)
        {
            var safeKey = SanitizeFileName(key);
            return Path.Combine(this.rootPath, GetProfileFolder(profileId), safeKey + JsonExtension);
        }

        /// <summary>
        /// Remove characters that are invalid in file names.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            foreach (var c in InvalidFileNameChars)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }

        /// <summary>
        /// Safely delete a file, ignoring errors.
        /// </summary>
        private void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                this.logService.Error($"[FileStorage] Failed to delete {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// On startup, recover from interrupted saves:
        /// - If a .tmp exists without a matching .json → promote the .tmp (previous save completed write but not rename)
        /// - If a .bak exists without a matching .json → restore the .bak (rename to target failed)
        /// - Otherwise, clean up orphaned .tmp and .bak files
        /// </summary>
        private void RecoverOrphanedTempFiles()
        {
            try
            {
                if (!Directory.Exists(this.rootPath))
                {
                    return;
                }

                // Process .tmp files
                foreach (var tmpFile in Directory.EnumerateFiles(this.rootPath, "*" + TempExtension, SearchOption.AllDirectories))
                {
                    var targetFile = tmpFile.Substring(0, tmpFile.Length - TempExtension.Length);
                    if (!File.Exists(targetFile))
                    {
                        // Target is missing — the .tmp is the only copy, promote it
                        try
                        {
                            File.Move(tmpFile, targetFile);
                            this.logService.Log($"[FileStorage] Recovered orphaned temp file: {Path.GetFileName(tmpFile)}");
                        }
                        catch (Exception ex)
                        {
                            this.logService.Error($"[FileStorage] Failed to recover {tmpFile}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Target exists — the .tmp is stale, remove it
                        this.TryDeleteFile(tmpFile);
                    }
                }

                // Process .bak files
                foreach (var bakFile in Directory.EnumerateFiles(this.rootPath, "*" + BackupExtension, SearchOption.AllDirectories))
                {
                    var targetFile = bakFile.Substring(0, bakFile.Length - BackupExtension.Length);
                    if (!File.Exists(targetFile))
                    {
                        // Target is missing — restore from backup
                        try
                        {
                            File.Move(bakFile, targetFile);
                            this.logService.Log($"[FileStorage] Restored from backup: {Path.GetFileName(bakFile)}");
                        }
                        catch (Exception ex)
                        {
                            this.logService.Error($"[FileStorage] Failed to restore {bakFile}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Target exists — the .bak is leftover, remove it
                        this.TryDeleteFile(bakFile);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logService.Error($"[FileStorage] Error during orphan recovery: {ex.Message}");
            }
        }

        #endregion
    }
}
