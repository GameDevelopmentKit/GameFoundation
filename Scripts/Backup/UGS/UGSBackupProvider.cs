namespace GameFoundation.Scripts.Backup.UGS
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;

#if UGS_CLOUD_SAVE
    using Unity.Services.CloudSave;
#endif

    /// <summary>
    /// UGS Cloud Save implementation of <see cref="IBackupProvider"/>.
    ///
    /// Cloud key layout:
    ///   "backup_manifest"             → BackupManifest JSON (~2-5 KB)
    ///   "backup_data_{profileId}"     → Dictionary&lt;string, string&gt; JSON (~50-200 KB)
    ///
    /// All UGS API calls are wrapped in #if UGS_CLOUD_SAVE.
    /// When the package is not installed, all operations return null / no-op.
    /// </summary>
    public class UGSBackupProvider : IBackupProvider
    {
        private readonly BackupConfig config;
        private readonly ILogService logService;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            // IMPORTANT: Use TypeNameHandling.None for cloud-sourced data to prevent
            // deserialization attacks via $type metadata in untrusted JSON.
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
        };

        public string ProviderName => "UGS Cloud Save";
        public bool IsAvailable =>
#if UGS_CLOUD_SAVE
            Application.internetReachability != NetworkReachability.NotReachable;
#else
            false;
#endif

        public UGSBackupProvider(BackupConfig config, ILogService logService)
        {
            this.config = config;
            this.logService = logService;
        }

        #region Manifest (lightweight registry + all metadata)

        public async UniTask<BackupManifest> FetchManifestAsync()
        {
#if UGS_CLOUD_SAVE
            try
            {
                var manifestKey = this.GetManifestKey();
                var keys = new HashSet<string> { manifestKey };
                var cloudData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (cloudData.TryGetValue(manifestKey, out var cloudItem))
                {
                    var json = cloudItem.Value.GetAsString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        var manifest = JsonConvert.DeserializeObject<BackupManifest>(json, JsonSettings);
                        this.logService.Log($"[Backup] Fetched manifest: {manifest?.ProfileMetadataMap?.Count ?? 0} profiles");
                        return manifest;
                    }
                }

                this.logService.Log("[Backup] No manifest found on cloud.");
                return null;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Failed to fetch manifest: {ex.Message}");
                throw;
            }
#else
            this.logService.Log("[Backup] FetchManifest skipped: com.unity.services.cloudsave not installed.");
            return null;
#endif
        }

        public async UniTask SaveManifestAsync(BackupManifest manifest)
        {
#if UGS_CLOUD_SAVE
            try
            {
                manifest.LastUpdatedAt = DateTime.UtcNow;

                var manifestKey = this.GetManifestKey();
                var json = JsonConvert.SerializeObject(manifest, JsonSettings);
                var data = new Dictionary<string, object> { { manifestKey, json } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);

                this.logService.LogWithColor(
                    $"[Backup] Saved manifest ({manifest.ProfileMetadataMap?.Count ?? 0} profiles, {json.Length / 1024} KB)",
                    Color.green);
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Failed to save manifest: {ex.Message}");
                throw;
            }
#else
            this.logService.Log("[Backup] SaveManifest skipped: com.unity.services.cloudsave not installed.");
            await UniTask.CompletedTask;
#endif
        }

        #endregion

        #region Profile Data (heavy payload)

        public async UniTask BackupProfileDataAsync(string profileId, Dictionary<string, string> profileData)
        {
#if UGS_CLOUD_SAVE
            try
            {
                var dataKey = this.GetProfileDataKey(profileId);
                var json = JsonConvert.SerializeObject(profileData, JsonSettings);

                // Size check
                var sizeKB = json.Length / 1024;
                var maxWarningKB = this.config != null ? this.config.MaxSnapshotSizeWarningKB : 1024;
                if (sizeKB > maxWarningKB)
                {
                    this.logService.Warning($"[Backup] Profile '{profileId}' data size ({sizeKB} KB) exceeds warning threshold ({maxWarningKB} KB)!");
                }

                var data = new Dictionary<string, object> { { dataKey, json } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);

                this.logService.LogWithColor(
                    $"[Backup] Backed up profile '{profileId}' ({profileData.Count} keys, {sizeKB} KB)",
                    Color.green);
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Failed to backup profile '{profileId}': {ex.Message}");
                throw;
            }
#else
            this.logService.Log($"[Backup] BackupProfileData skipped: com.unity.services.cloudsave not installed.");
            await UniTask.CompletedTask;
#endif
        }

        public async UniTask<Dictionary<string, string>> RestoreProfileDataAsync(string profileId)
        {
#if UGS_CLOUD_SAVE
            try
            {
                var dataKey = this.GetProfileDataKey(profileId);
                var keys = new HashSet<string> { dataKey };
                var cloudData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (cloudData.TryGetValue(dataKey, out var cloudItem))
                {
                    var json = cloudItem.Value.GetAsString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        var profileData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json, JsonSettings);
                        this.logService.Log($"[Backup] Restored profile '{profileId}' ({profileData?.Count ?? 0} keys)");
                        return profileData;
                    }
                }

                this.logService.Log($"[Backup] No data found for profile '{profileId}'.");
                return null;
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Failed to restore profile '{profileId}': {ex.Message}");
                throw;
            }
#else
            this.logService.Log($"[Backup] RestoreProfileData skipped: com.unity.services.cloudsave not installed.");
            return null;
#endif
        }

        public async UniTask DeleteProfileDataAsync(string profileId)
        {
#if UGS_CLOUD_SAVE
            try
            {
                var dataKey = this.GetProfileDataKey(profileId);
                await CloudSaveService.Instance.Data.Player.DeleteAsync(dataKey);
                this.logService.Log($"[Backup] Deleted backup data for profile '{profileId}'.");
            }
            catch (Exception ex)
            {
                this.logService.Error($"[Backup] Failed to delete profile '{profileId}': {ex.Message}");
                throw;
            }
#else
            this.logService.Log($"[Backup] DeleteProfileData skipped: com.unity.services.cloudsave not installed.");
            await UniTask.CompletedTask;
#endif
        }

        #endregion

        #region Key Helpers

        private string GetManifestKey()
        {
            return this.config != null ? this.config.ManifestCloudKey : "backup_manifest";
        }

        private string GetProfileDataKey(string profileId)
        {
            var prefix = this.config != null ? this.config.ProfileDataKeyPrefix : "backup_data_";
            return $"{prefix}{profileId}";
        }

        #endregion
    }
}
