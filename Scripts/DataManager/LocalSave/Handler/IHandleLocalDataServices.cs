namespace DataManager.LocalSave.Handler
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Profile;
    using DataManager.UserData;

    public interface IHandleLocalDataServices
    {
        /// <summary>
        /// Initialize the service - loads manifest and executes migrations if needed.
        /// Must be called before any data operations!
        /// </summary>
        UniTask InitializeAsync();

        bool IsInitialized { get; }

        /// <summary>
        /// Save a class data to local
        /// </summary>
        /// <param name="data">class data</param>
        /// <param name="force"> if true, save data immediately to local</param>
        /// <typeparam name="T"> type of class</typeparam>
        public UniTask SaveData<T>(T data, bool force = false) where T : class, IUserData;

        /// <summary>
        /// Load data from local
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<T> LoadData<T>() where T : class, IUserData;

        /// <summary>
        ///  Load data from local
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public UniTask<IUserData> LoadData(Type type);

        public UniTask<IUserData[]> LoadDatas(params Type[] types);

        public UniTask<string> GetRawDataJson<T>() where T : class, IUserData;

        public UniTask<string> GetRawDataJson(Type type);

        public bool HasData<T>() where T : class, IUserData;

        public bool HasData(Type type);

        public UniTask SaveCurrentProfile();

        public UniTask DeleteCurrentProfile();

        #region Profile Management

        /// <summary>
        /// Check if profile exists
        /// </summary>
        bool ProfileExists(string profileId);

        /// <summary>
        /// Get metadata for specific profile
        /// </summary>
        UniTask<ProfileMetadata> LoadProfileMetadataAsync(string profileId);

        /// <summary>
        /// Get current active profile ID
        /// </summary>
        string GetCurrentProfileId();

        /// <summary>
        /// List all available profile IDs
        /// </summary>
        IReadOnlyCollection<string> GetAllProfiles();

        /// <summary>
        /// Create a new profile
        /// </summary>
        /// <param name="profileId">Unique profile ID</param>
        /// <param name="displayName">Human-readable name (optional, defaults to profileId)</param>
        UniTask<ProfileMetadata> CreateProfileAsync(string profileId, string displayName = null);

        /// <summary>
        /// Switch to a different profile
        /// </summary>
        /// <param name="profileId">Target profile ID</param>
        /// <param name="saveCurrentProfile">Save current profile before switching</param>
        UniTask SwitchProfileAsync(string profileId, bool saveCurrentProfile = true);

        /// <summary>
        /// Delete a profile and all its data
        /// </summary>
        /// <param name="profileId">Profile to delete</param>
        /// <param name="canDeleteCurrent">Allow deleting current profile (will switch to default)</param>
        UniTask DeleteProfileAsync(string profileId, bool canDeleteCurrent = false);

        /// <summary>
        /// Save profile metadata
        /// </summary>
        UniTask SaveCurrentProfileMetadataAsync();

        #endregion

        #region Backup Support

        /// <summary>
        /// Get all profile data as key→JSON pairs for backup. Reads from disk.
        /// Includes ALL <see cref="IUserData"/> classes by default,
        /// except those marked with <see cref="BackupExcludeAttribute"/>.
        /// </summary>
        /// <returns>Dictionary of data key → raw JSON string for all backup-eligible data.</returns>
        UniTask<Dictionary<string, string>> GetProfileDataForBackupAsync();

        /// <summary>
        /// Apply backup data to the current profile, overwriting local storage.
        /// Writes to disk and refreshes the in-memory cache so subsequent
        /// LoadData calls return the new values.
        /// </summary>
        /// <param name="keyValuePairs">Data key → raw JSON pairs from the backup.</param>
        UniTask ApplyBackupDataAsync(Dictionary<string, string> keyValuePairs);

        /// <summary>
        /// Increment the backup version counter and update the last backup timestamp.
        /// Called after a successful backup operation.
        /// </summary>
        void IncrementBackupVersion();

        /// <summary>
        /// Get the current profile metadata. Used by the backup manager to read backup version.
        /// </summary>
        ProfileMetadata GetCurrentProfileMetadata();

        #endregion
    }
}
