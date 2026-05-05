namespace DataManager.LocalSave.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Encryption;
    using DataManager.LocalSave.Migration;
    using DataManager.LocalSave.Profile;
    using DataManager.LocalSave.Provider;
    using DataManager.UserData;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>
    /// Central manager for local data services with migration, encryption, profile management,
    /// and pluggable storage providers.
    /// 
    /// Architecture:
    ///   HandleLocalDataServices (this class — manager)
    ///     └─ primaryProvider: IStorageProvider (reads + writes, always local)
    ///
    /// Read: always from primary provider (fast, offline-safe)
    /// Write: primary provider only
    /// Encryption: handled per-provider via IStorageProvider.UseEncryption
    ///
    /// Cloud backup is handled by the separate Backup module (IBackupService/IBackupProvider).
    /// This class exposes GetProfileDataForBackupAsync() and ApplyBackupDataAsync() for that purpose.
    /// </summary>
    public sealed class HandleLocalDataServices : IHandleLocalDataServices
    {
        #region Constants & Static Members

        public const string DefaultProfileId = "default";
        public const string UserDataPrefix = "LD-";
        private const string ProfileRegistryKey = "__ProfileRegistry__";
        private const string ProfileMetadataKey = "ProfileMetadata";

        private static readonly JsonSerializerSettings JsonSetting = new()
        {
            // Handle type changes in JSON (polymorphism)
            TypeNameHandling = TypeNameHandling.Auto,

            // Ignore extra fields in JSON that don't exist in current class
            // (happens when fields are removed from code)
            MissingMemberHandling = MissingMemberHandling.Ignore,

            // Don't fail on type conversion errors, use default values instead
            // (handles: int→string, string→int, etc.)
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true; // Continue despite errors
            },

            // Use default values for missing fields
            // (handles: new fields added to class)
            NullValueHandling = NullValueHandling.Ignore,

            // Handle enum changes gracefully
            Converters = new List<JsonConverter>
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        #endregion

        #region Fields

        private readonly ILogService logService;
        private readonly IEncryptionService encryptionService;
        private readonly MigrationExecutor migrationExecutor;
        private readonly IStorageProvider primaryProvider;

        private bool isInitialized;
        private ProfileRegistry profileRegistry;
        private ProfileMetadata currentProfileMetadata;
        private readonly Dictionary<string, IUserData> localDataCache = new();

        /// <summary>
        /// Cached mapping of type name → Type for all ILocalData implementations.
        /// Built lazily on first use, avoids scanning all assemblies on every sync call.
        /// </summary>
        private Dictionary<string, Type> localDataTypeCache;

        /// <summary>
        /// Single source of truth for the active profile ID.
        /// Before InitializeAsync: returns DefaultProfileId.
        /// After InitializeAsync: delegates to profileRegistry.CurrentProfileId.
        /// </summary>
        private string CurrentProfileId
        {
            get => this.profileRegistry?.CurrentProfileId ?? DefaultProfileId;
            set
            {
                if (this.profileRegistry != null)
                    this.profileRegistry.CurrentProfileId = value;
            }
        }

        #endregion

        #region Constructor

        public HandleLocalDataServices(
            ILogService logService,
            IEncryptionService encryptionService,
            IStorageProvider primaryProvider)
        {
            this.logService = logService;
            this.encryptionService = encryptionService;
            this.primaryProvider = primaryProvider;
            this.migrationExecutor = new MigrationExecutor(logService);
        }

        #endregion

        #region Provider Access

        /// <summary>
        /// Get the primary storage provider. Useful for external code that needs
        /// direct provider access (e.g., legacy migrator needs FileStorageProvider).
        /// </summary>
        public IStorageProvider PrimaryProvider => this.primaryProvider;

        #endregion

        #region Common

        public bool IsInitialized => this.isInitialized;

        /// <summary>
        /// Initialize the service - loads profile registry, profile metadata, and executes migrations if needed.
        /// Call this before any data operations!
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.logService.Log("[LocalData] Initializing...");

            // Initialize providers
            await this.primaryProvider.InitializeAsync();

            // 1. Load global profile registry
            this.profileRegistry = await this.LoadProfileRegistryAsync();

            // 2. Create default profile if needed
            if (this.profileRegistry == null || !this.profileRegistry.HasProfile(DefaultProfileId))
            {
                this.logService.Log("[LocalData] Creating default profile...");
                this.profileRegistry = ProfileRegistry.CreateDefault(DefaultProfileId);
            }

            // 3. Finalize initialization
            this.isInitialized = true;
            this.logService.LogWithColor($"[LocalData] Initialization complete (Profile: {this.CurrentProfileId})",
                Color.green);

            // 4. Load current profile metadata (manifest) and execute migrations if needed
            await this.UseProfileInternal(this.CurrentProfileId);
        }

        public bool HasData<T>() where T : class, IUserData
        {
            return this.HasData(typeof(T));
        }

        public bool HasData(Type type)
        {
            return this.currentProfileMetadata.HasDataKey(DataKeyOf(type));
        }

        #endregion

        #region Profile Management

        public string GetCurrentProfileId()
        {
            return this.CurrentProfileId;
        }

        public IReadOnlyCollection<string> GetAllProfiles()
        {
            this.EnsureInitialized();
            return this.profileRegistry.GetAllProfileIds();
        }

        public bool ProfileExists(string profileId)
        {
            this.EnsureInitialized();
            return this.profileRegistry.HasProfile(profileId);
        }

        public async UniTask<ProfileMetadata> LoadProfileMetadataAsync(string profileId)
        {
            this.EnsureInitialized();
            if (!this.profileRegistry.HasProfile(profileId))
            {
                return null;
            }

            return (ProfileMetadata)await this.InternalLoadAsync(ProfileMetadataKey, typeof(ProfileMetadata), profileId,
                false);
        }

        /// <summary>
        /// Load profile metadata for current profile
        /// </summary>
        private UniTask<ProfileMetadata> LoadCurrentProfileMetadataAsync()
        {
            return this.LoadProfileMetadataAsync(this.CurrentProfileId);
        }

        /// <summary>
        /// Save profile metadata for current profile
        /// </summary>
        public async UniTask SaveCurrentProfileMetadataAsync()
        {
            this.currentProfileMetadata.RecordSave();
            await this.SaveJsonInternal(ProfileMetadataKey, this.currentProfileMetadata, this.CurrentProfileId);
        }

        /// <summary>
        /// Load global profile registry
        /// </summary>
        private async UniTask<ProfileRegistry> LoadProfileRegistryAsync()
        {
            return (ProfileRegistry)await this.InternalLoadAsync(ProfileRegistryKey, typeof(ProfileRegistry), null,
                false);
        }

        /// <summary>
        /// Save global profile registry
        /// </summary>
        private async UniTask SaveProfileRegistryAsync()
        {
            await this.SaveJsonInternal(ProfileRegistryKey, this.profileRegistry, null);
        }

        public async UniTask SwitchProfileAsync(string profileId, bool saveCurrentProfile = true)
        {
            this.EnsureInitialized();
            if (this.CurrentProfileId == profileId)
            {
                this.logService.Log($"Already on profile {profileId}");
                return;
            }

            // Validate target exists
            if (!this.profileRegistry.HasProfile(profileId))
            {
                throw new InvalidOperationException($"Profile {profileId} does not exist");
            }

            // Save current profile
            if (saveCurrentProfile)
            {
                await this.SaveCurrentProfile();
            }

            // Clear cache
            this.localDataCache.Clear();

            // Switch profile ID
            var oldProfileId = this.CurrentProfileId;
            this.CurrentProfileId = profileId;

            await this.UseProfileInternal(profileId);

            this.logService.LogWithColor($"Switched from {oldProfileId} to {profileId}", Color.green);
        }

        private async UniTask UseProfileInternal(string profileId)
        {
            // Update current profile (single source of truth)
            this.CurrentProfileId = profileId;
            this.logService.Log($"[LocalData] Current profile: {this.CurrentProfileId}");

            // Load profile metadata (per-profile manifest)
            this.currentProfileMetadata = await this.LoadCurrentProfileMetadataAsync();

            if (this.currentProfileMetadata == null)
            {
                // First time for this profile - create new metadata
                this.currentProfileMetadata = await this.CreateProfileAsync(profileId);
            }
            else
            {
                this.logService.Log(
                    $"[LocalData] Loaded profile metadata - Version: {this.currentProfileMetadata.GameVersion}");
            }

            // Check if migration needed
            var currentVersion = Application.version;
            if (this.currentProfileMetadata.GameVersion != currentVersion)
            {
                this.logService.LogWithColor(
                    $"[LocalData] Migration needed: {this.currentProfileMetadata.GameVersion} → {currentVersion}",
                    Color.yellow);

                await this.migrationExecutor.ExecuteMigrations(this, this.currentProfileMetadata, currentVersion);
            }

            // Update last played timestamp
            this.currentProfileMetadata.RecordPlay();
            await this.SaveCurrentProfileMetadataAsync();
            await this.SaveProfileRegistryAsync();
        }

        public async UniTask<ProfileMetadata> CreateProfileAsync(string profileId, string displayName = null)
        {
            this.EnsureInitialized();

            // If profile already exists in registry, try to load its metadata
            if (this.profileRegistry.HasProfile(profileId))
            {
                var existing = await this.LoadProfileMetadataAsync(profileId);
                if (existing != null)
                {
                    this.logService.LogWithColor($"Profile {profileId} already exists", Color.yellow);
                    return existing;
                }

                // Profile is in registry but metadata is missing/corrupt — recreate metadata below
                this.logService.Warning(
                    $"[LocalData] Profile {profileId} exists in registry but metadata is missing. Recreating metadata.");
            }

            // Create metadata
            var metadata = ProfileMetadata.CreateNew(profileId, displayName, Application.version,
                SystemInfo.deviceUniqueIdentifier);

            // Save metadata
            await this.SaveJsonInternal(ProfileMetadataKey, metadata, profileId);

            // Add to registry
            this.profileRegistry.AddProfile(profileId);
            await this.SaveProfileRegistryAsync();

            this.logService.LogWithColor($"Created profile: {profileId}", Color.green);
            return metadata;
        }

        public async UniTask DeleteProfileAsync(string profileId, bool canDeleteCurrent = false)
        {
            this.EnsureInitialized();

            // Check if deleting current profile
            if (profileId == this.CurrentProfileId && !canDeleteCurrent)
            {
                throw new InvalidOperationException(
                    $"Cannot delete current profile {profileId}. Switch to another profile first or set canDeleteCurrent=true");
            }

            // Load metadata to get data keys
            var metadata = await this.LoadProfileMetadataAsync(profileId);

            // Delete all data for this profile
            if (metadata != null && metadata.DataKeys != null)
            {
                foreach (var key in metadata.DataKeys)
                {
                    await this.DeleteJsonFromProviders(profileId, key);
                }

                metadata.DataKeys.Clear();
            }

            // Delete profile metadata
            await this.DeleteJsonFromProviders(profileId, ProfileMetadataKey);

            this.localDataCache.Clear();

            // If deleted current profile, switch to default
            if (profileId == this.CurrentProfileId)
            {
                // Remove deleted profile from registry first
                this.profileRegistry.RemoveProfile(profileId);
                await this.SaveProfileRegistryAsync();

                await this.CreateProfileAsync(DefaultProfileId, "Default");
                await this.SwitchProfileAsync(DefaultProfileId, saveCurrentProfile: false);
            }
            else
            {
                // Remove from registry
                this.profileRegistry.RemoveProfile(profileId);
                await this.SaveProfileRegistryAsync();
            }

            this.logService.LogWithColor($"Deleted profile: {profileId}", Color.yellow);
        }

        public async UniTask DeleteCurrentProfile()
        {
            this.EnsureInitialized();
            await this.DeleteProfileAsync(this.CurrentProfileId, canDeleteCurrent: true);
            this.logService.LogWithColor($"Deleted all data for profile {this.CurrentProfileId}", Color.yellow);
        }

        #endregion

        #region Save Data Handler

        public async UniTask SaveData<T>(T data, bool force = false) where T : class, IUserData
        {
            this.EnsureInitialized();

            var key = DataKeyOf(typeof(T));
            this.localDataCache[key] = data;

            if (!force) return;

            await this.SaveJsonInternal(key, data, CurrentProfileId);

            // Update manifest save time
            await this.SaveCurrentProfileMetadataAsync();

            this.logService.LogWithColor($"Saved {key}", Color.green);
        }

        public async UniTask SaveCurrentProfile()
        {
            this.logService.Log("[LocalData] Saving current profile...");
            this.EnsureInitialized();

            // Serialize all cached data concurrently (FileStorageProvider uses per-key locks)
            var saveTasks =
                this.localDataCache.Select(kvp => this.SaveJsonInternal(kvp.Key, kvp.Value, CurrentProfileId));
            await UniTask.WhenAll(saveTasks);

            // Update manifest
            await this.SaveCurrentProfileMetadataAsync();

            this.logService.LogWithColor($"Saved all data ({this.localDataCache.Count} items)", Color.green);
        }

        /// <summary>
        /// Internal save with per-provider encryption support.
        /// Serializes the object, then writes to primary + all secondary providers,
        /// applying encryption per each provider's UseEncryption setting.
        /// </summary>
        /// <param name="key">Key to save data for.</param>
        /// <param name="obj">Object to save.</param>
        /// <param name="profileId">Profile ID to save data for. If null, this data will not belong to any profile.</param>
        private async UniTask SaveJsonInternal(string key, object obj, string profileId)
        {
            // Only track user data keys in metadata — not system keys like ProfileRegistry/ProfileMetadata

            bool useEncryption = true;
            switch (key)
            {
                case ProfileRegistryKey:
                    profileId = null; // Global data doesn't belong to any profile
                    useEncryption = false; // Don't encrypt global registry
                    break;
                case ProfileMetadataKey:
                    useEncryption = false; // Don't encrypt profile metadata (must match load behavior)
                    break;
                default:
                    this.currentProfileMetadata?.SetDataKey(key);
                    break;
            }

            var plainJson = JsonConvert.SerializeObject(obj, JsonSetting);

            // Write to primary provider
            var primaryJson = this.MaybeEncrypt(plainJson, useEncryption && this.primaryProvider.UseEncryption);
            await this.primaryProvider.SaveAsync(profileId, key, primaryJson);
        }

        #endregion

        #region Load Data Handler

        public async UniTask<T> LoadData<T>() where T : class, IUserData
        {
            this.EnsureInitialized();
            return (T)await this.LoadData(typeof(T));
        }

        public async UniTask<IUserData> LoadData(Type type)
        {
            this.EnsureInitialized();
            var key = DataKeyOf(type);
            return await this.InternalLoadAsync(key, type, CurrentProfileId);
        }

        public async UniTask<IUserData[]> LoadDatas(params Type[] types)
        {
            this.EnsureInitialized();

            var keys = types.Select(DataKeyOf).ToArray();

            var results = new List<IUserData>();
            for (int i = 0; i < types.Length; i++)
            {
                results.Add(await this.InternalLoadAsync(keys[i], types[i], CurrentProfileId));
            }

            return results.ToArray();
        }

        public UniTask<string> GetRawDataJson<T>() where T : class, IUserData
        {
            return this.GetRawDataJson(typeof(T));
        }

        public UniTask<string> GetRawDataJson(Type type)
        {
            return this.LoadJsonFromPrimary(DataKeyOf(type), CurrentProfileId);
        }

        private async UniTask<IUserData> InternalLoadAsync(string key, Type type, string profileId,
            bool useEncryption = true)
        {
            // Check cache first
            if (this.localDataCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Deserialize data
            IUserData data = null;
            var json = await this.LoadJsonFromPrimary(key, profileId, useEncryption);
            if (!string.IsNullOrEmpty(json))
            {
                // Deserialize existing data
                try
                {
                    data = JsonConvert.DeserializeObject(json, type, JsonSetting) as IUserData;
                }
                catch (Exception ex)
                {
                    this.logService.Error($"Deserialization error for {key}: {ex.Message}");
                }
            }

            // If data is still null, create new instance
            if (data == null)
            {
                data = (IUserData)Activator.CreateInstance(type);
                this.logService.Log($"[LocalData] Created new {key}");
            }

            // Cache it
            this.localDataCache[key] = data;
            this.logService.LogWithColor($"[LocalData] Loaded {key}", Color.green);

            return data;
        }

        /// <summary>
        /// Load from primary provider with optional decryption.
        /// </summary>
        private async UniTask<string> LoadJsonFromPrimary(string key, string profileId = null,
            bool useEncryption = true)
        {
            var json = await this.primaryProvider.LoadAsync(profileId, key);

            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            // Decrypt if this provider uses encryption
            if (useEncryption && this.primaryProvider.UseEncryption && this.encryptionService != null)
            {
                try
                {
                    json = this.encryptionService.Decrypt(json);
                }
                catch (Exception ex)
                {
                    this.logService.Error($"Failed to decrypt {key}: {ex.Message}");
                    throw; // Decryption failure is critical
                }
            }

            return json;
        }

        #endregion

        #region Provider Dispatch (replaces abstract methods)

        /// <summary>
        /// Delete from primary provider.
        /// </summary>
        private async UniTask DeleteJsonFromProviders(string profileId, string key)
        {
            await this.primaryProvider.DeleteAsync(profileId, key);
        }

        #endregion

        #region Encryption Helpers

        /// <summary>
        /// Encrypt JSON if encryption is enabled and the encryption service is available.
        /// </summary>
        private string MaybeEncrypt(string json, bool shouldEncrypt)
        {
            if (shouldEncrypt && this.encryptionService != null)
            {
                return this.encryptionService.Encrypt(json);
            }

            return json;
        }

        #endregion

        #region Helper Methods

        private void EnsureInitialized()
        {
            if (!this.isInitialized)
            {
                throw new InvalidOperationException(
                    "HandleLocalDataServices not initialized! Call InitializeAsync() first.");
            }
        }

        /// <summary>
        /// Generate storage key for a data type
        /// </summary>
        private static string DataKeyOf(Type type) => $"{UserDataPrefix}{type.Name}";

        /// <summary>
        /// Lazily build and cache the typeName → Type mapping for all ILocalData implementations.
        /// Scanning all loaded assemblies is expensive (~50-100 KB transient allocations on mobile).
        /// Caching once eliminates GC pressure from repeated sync calls.
        /// </summary>
        private Dictionary<string, Type> GetLocalDataTypeLookup()
        {
            if (this.localDataTypeCache != null)
            {
                return this.localDataTypeCache;
            }

            this.localDataTypeCache = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IUserData).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                        {
                            this.localDataTypeCache[type.Name] = type;
                        }
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                    // Some assemblies may not be loadable; skip them
                }
            }

            return this.localDataTypeCache;
        }

        #endregion

        #region Backup Support

        /// <summary>
        /// Collect all profile data as key→JSON pairs for backup. Reads from disk.
        /// Includes ALL <see cref="IUserData"/> classes by default,
        /// except those marked with [BackupExclude].
        /// </summary>
        public async UniTask<Dictionary<string, string>> GetProfileDataForBackupAsync()
        {
            this.EnsureInitialized();

            var result = new Dictionary<string, string>();
            var metadata = this.GetCurrentProfileMetadata();

            if (metadata?.DataKeys == null || metadata.DataKeys.Count == 0)
            {
                this.logService.Log("[LocalData] GetProfileDataForBackup: No data keys in profile metadata.");
                return result;
            }

            var dataKeys = metadata.DataKeys.ToArray();

            // Use cached type lookup (avoids scanning all assemblies per call)
            var localDataTypes = this.GetLocalDataTypeLookup();

            foreach (var key in dataKeys)
            {
                // Extract type name from key (e.g., "LD-WalletData" → "WalletData")
                var typeName = key.StartsWith(UserDataPrefix) ? key.Substring(UserDataPrefix.Length) : key;

                if (!localDataTypes.TryGetValue(typeName, out var dataType))
                {
                    this.logService.Log($"[LocalData] Skipping '{key}' — type '{typeName}' not found.");
                    continue;
                }

                // Check for [BackupExclude] attribute — skip if present
                var excludeAttr = Attribute.GetCustomAttribute(dataType, typeof(BackupExcludeAttribute), inherit: true);
                if (excludeAttr != null)
                {
                    continue;
                }

                // Read raw JSON from disk
                var json = await this.LoadJsonFromPrimary(key, this.CurrentProfileId);
                if (string.IsNullOrEmpty(json))
                {
                    this.logService.Log($"[LocalData] Skipping '{key}' — no data on disk.");
                    continue;
                }

                result[key] = json;
                this.logService.Log($"[LocalData] Collected '{key}' ({typeName}) for backup ({json.Length} chars).");
            }

            this.logService.LogWithColor(
                $"[LocalData] Backup data: {result.Count}/{dataKeys.Length} keys collected.", Color.cyan);
            return result;
        }

        /// <summary>
        /// Apply backup data to the current profile, overwriting local storage.
        /// Deserializes each key's JSON, updates the in-memory cache, and persists to the primary provider.
        /// Keys not present in the backup are left untouched (we don't delete local-only data).
        /// </summary>
        public async UniTask ApplyBackupDataAsync(Dictionary<string, string> keyValuePairs)
        {
            this.EnsureInitialized();

            if (keyValuePairs == null || keyValuePairs.Count == 0)
            {
                this.logService.Log("[LocalData] ApplyBackupData: nothing to apply.");
                return;
            }

            foreach (var kvp in keyValuePairs)
            {
                var key = kvp.Key;
                var json = kvp.Value;

                if (string.IsNullOrEmpty(json)) continue;

                // Find the corresponding ILocalData type from the key
                var typeName = key.StartsWith(UserDataPrefix) ? key.Substring(UserDataPrefix.Length) : key;

                var localDataTypes = this.GetLocalDataTypeLookup();
                if (!localDataTypes.TryGetValue(typeName, out var dataType))
                {
                    this.logService.Warning($"[LocalData] ApplyBackupData: Unknown type for key '{key}'. Skipping.");
                    continue;
                }

                try
                {
                    // Deserialize and update cache
                    var data = JsonConvert.DeserializeObject(json, dataType, JsonSetting) as IUserData;
                    if (data != null)
                    {
                        this.localDataCache[key] = data;

                        // Persist to primary provider
                        await this.SaveJsonInternal(key, data, this.CurrentProfileId);

                        this.logService.Log($"[LocalData] Applied backup data: {key}");
                    }
                }
                catch (Exception ex)
                {
                    this.logService.Error($"[LocalData] Failed to apply backup data for {key}: {ex.Message}");
                }
            }

            // Save updated profile metadata
            await this.SaveCurrentProfileMetadataAsync();
            this.logService.LogWithColor("[LocalData] Backup data applied successfully.", Color.green);
        }

        /// <summary>
        /// Increment the backup version counter and update the last backup timestamp.
        /// Call after a successful backup operation.
        /// </summary>
        public void IncrementBackupVersion()
        {
            this.EnsureInitialized();
            this.currentProfileMetadata.BackupVersion++;
            this.currentProfileMetadata.LastBackupTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Get the current profile metadata. Used by the backup manager to read backup version.
        /// </summary>
        public ProfileMetadata GetCurrentProfileMetadata()
        {
            return this.currentProfileMetadata;
        }

        #endregion
    }
}
