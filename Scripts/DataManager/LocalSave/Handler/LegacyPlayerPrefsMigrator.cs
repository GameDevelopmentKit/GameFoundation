namespace DataManager.LocalSave.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Encryption;
    using DataManager.LocalSave.Profile;
    using DataManager.LocalSave.Provider;
    using DataManager.UserData;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>
    /// One-time migration utility to bridge legacy PlayerPrefs data (old system, no profiles)
    /// to the new data system (profile-scoped, file-based or new PlayerPrefs format).
    ///
    /// CRITICAL: This must run BEFORE HandleLocalDataServices.InitializeAsync()!
    ///
    /// The old system stored data as:
    ///   PlayerPrefs key: "LD-{ClassName}" → JSON string
    ///   Example: "LD-WalletData" → {"Balances": {...}}
    ///
    /// The new system expects:
    ///   FileBased: SaveData/default/LD-WalletData.json
    ///   PlayerPrefs: "default_LD-WalletData" → JSON string
    ///   Plus: ProfileRegistry and ProfileMetadata must exist
    ///
    /// This migrator:
    /// 1. Detects old-format keys (no profile prefix) in PlayerPrefs
    /// 2. Copies raw JSON to the new backend format via IStorageProvider directly
    /// 3. Creates ProfileRegistry and ProfileMetadata with the correct legacy version
    /// 4. Does NOT delete old keys (kept as safety net for rollback)
    /// </summary>
    public static class LegacyPlayerPrefsMigrator
    {
        private const string MigrationCompleteKey = "__LegacyPlayerPrefsMigrationDone__";
        private const string LogTag = "[LegacyMigrator]";
        private const string UserDataPrefix = "LD-";
        private const string DefaultProfileId = "default";

        /// <summary>
        /// Known legacy data keys as a safety net.
        /// Dynamic reflection scan will also find keys, but this list ensures
        /// we catch keys even if assemblies fail to load.
        /// </summary>
        private static readonly string[] KnownLegacyKeys =
        {
            "LD-UserProfile", "LD-UserCommonData", "LD-WalletData", "LD-UserInventory",
            "LD-GameSettingData", "LD-SoundSetting", "LD-AnalyticData", "LD-UserSynergy",
            "LD-UserTown", "LD-UserWorker", "LD-UserMiner", "LD-UserResourceGenerator",
            "LD-TutorialLocalData", "LD-QuestJournal", "LD-TrackingQuestData",
            "LD-ShopData", "LD-InterstitialData", "LD-BlueprintInfoData",
            "LD-BattlePassMileStoneData",
        };

        /// <summary>
        /// Main entry point. Call BEFORE HandleLocalDataServices.InitializeAsync().
        /// Uses the primary IStorageProvider directly (not through the manager).
        /// </summary>
        /// <param name="config">DataManagerConfig with lastKnownLegacyVersion and provider configs</param>
        /// <param name="primaryProvider">The primary storage provider (already constructed, not yet initialized)</param>
        /// <param name="encryptionService">Optional encryption service. If non-null and provider uses encryption, data will be encrypted before writing.</param>
        public static async UniTask MigrateIfNeeded(
            LocalSaveConfig config,
            IStorageProvider primaryProvider,
            IEncryptionService encryptionService = null)
        {
            // 1. Already migrated? Skip immediately.
            if (PlayerPrefs.HasKey(MigrationCompleteKey))
            {
                Debug.Log($"{LogTag} Legacy migration already completed. Skipping.");
                return;
            }

            // 2. Detect legacy keys
            var legacyData = DetectLegacyKeys();

            if (legacyData.Count == 0)
            {
                Debug.Log($"{LogTag} No legacy PlayerPrefs data found. Fresh install or already migrated.");
                MarkMigrationComplete();
                return;
            }

            Debug.Log($"<color=yellow>{LogTag} Legacy data detected! Found {legacyData.Count} key(s). Starting migration...</color>");
            foreach (var kvp in legacyData)
            {
                Debug.Log($"{LogTag}   Found: {kvp.Key} ({kvp.Value.Length} chars)");
            }

            // 3. Determine legacy version
            var legacyVersion = config != null ? config.LastKnownLegacyVersion : "0.1.0";
            Debug.Log($"{LogTag} Legacy version stamp: {legacyVersion}");

            // 4. Ensure primary provider is initialized before writing
            await primaryProvider.InitializeAsync();

            // 5. Write data to target provider directly
            // Encrypt user data if encryption is enabled for this provider
            var shouldEncrypt = primaryProvider.UseEncryption && encryptionService != null;
            foreach (var kvp in legacyData)
            {
                var json = shouldEncrypt ? encryptionService.Encrypt(kvp.Value) : kvp.Value;
                await primaryProvider.SaveAsync(DefaultProfileId, kvp.Key, json);
                Debug.Log($"{LogTag}   Wrote: {kvp.Key} (encrypted: {shouldEncrypt})");
            }

            // 6. Create ProfileRegistry
            var registry = ProfileRegistry.CreateDefault(DefaultProfileId);
            var registryJson = JsonConvert.SerializeObject(registry);
            await primaryProvider.SaveAsync(null, "__ProfileRegistry__", registryJson);
            Debug.Log($"{LogTag}   Wrote ProfileRegistry");

            // 7. Create ProfileMetadata with LEGACY version (NOT Application.version!)
            var metadata = ProfileMetadata.CreateNew(
                profileId: DefaultProfileId,
                displayName: "Default",
                gameVersion: legacyVersion, // ← THIS IS THE CRITICAL FIX
                deviceId: SystemInfo.deviceUniqueIdentifier);

            // Track all migrated data keys in the metadata
            foreach (var key in legacyData.Keys)
            {
                metadata.SetDataKey(key);
            }

            var metadataJson = JsonConvert.SerializeObject(metadata);
            await primaryProvider.SaveAsync(DefaultProfileId, "ProfileMetadata", metadataJson);
            Debug.Log($"{LogTag}   Wrote ProfileMetadata (version={legacyVersion})");

            // 8. Mark migration complete (but DO NOT delete old keys — kept as safety net)
            MarkMigrationComplete();

            Debug.Log($"<color=green>{LogTag} Legacy migration complete! " +
                      $"Migrated {legacyData.Count} keys. " +
                      $"Profile version stamped as '{legacyVersion}'. " +
                      $"InitializeAsync() will detect mismatch and run migration chain.</color>");
        }

        #region Detection

        /// <summary>
        /// Detect legacy PlayerPrefs keys using both hardcoded list and reflection.
        /// Returns dictionary of key → raw JSON value.
        /// </summary>
        private static Dictionary<string, string> DetectLegacyKeys()
        {
            var found = new Dictionary<string, string>();

            // Phase A: Check known keys (hardcoded safety net)
            foreach (var key in KnownLegacyKeys)
            {
                TryAddLegacyKey(found, key);
            }

            // Phase B: Dynamic scan via reflection — find ALL ILocalData types
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var localDataTypes = assembly.GetTypes()
                            .Where(t => t.IsClass && !t.IsAbstract
                                && typeof(IUserData).IsAssignableFrom(t)
                                && t != typeof(ProfileMetadata)   // Exclude new system types
                                && t != typeof(ProfileRegistry)); // Exclude new system types

                        foreach (var type in localDataTypes)
                        {
                            var key = $"{UserDataPrefix}{type.Name}";
                            TryAddLegacyKey(found, key);
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Some assemblies can't be reflected, skip them
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LogTag} Reflection scan failed (using hardcoded list only): {ex.Message}");
            }

            return found;
        }

        /// <summary>
        /// Try to read an old-format key from PlayerPrefs and add to dictionary.
        /// Old format: key is stored WITHOUT profile prefix (e.g., "LD-WalletData", not "default_LD-WalletData").
        /// </summary>
        private static void TryAddLegacyKey(Dictionary<string, string> found, string key)
        {
            if (found.ContainsKey(key)) return;

            var json = PlayerPrefs.GetString(key, null);
            if (!string.IsNullOrEmpty(json))
            {
                found[key] = json;
            }
        }

        #endregion

        #region Helpers

        private static void MarkMigrationComplete()
        {
            PlayerPrefs.SetInt(MigrationCompleteKey, 1);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
