namespace DataManager.LocalSave.Profile
{
    using System;
    using System.Collections.Generic;
    using DataManager.UserData;
    using Newtonsoft.Json;

    /// <summary>
    /// Global registry of all profiles and current active profile.
    /// Stored separately from individual profiles.
    /// </summary>
    [Serializable]
    public class ProfileRegistry : IUserData
    {
        /// <summary>
        /// ID of the currently active profile
        /// </summary>
        [JsonProperty("currentProfileId")]
        public string CurrentProfileId { get; set; }

        /// <summary>
        /// List of all profile IDs that exist
        /// </summary>
        [JsonProperty("profileIds")]
        public HashSet<string> ProfileIds { get; set; } = new HashSet<string>();

        /// <summary>
        /// Create default registry with a default profile
        /// </summary>
        public static ProfileRegistry CreateDefault(string defaultProfileId)
        {
            return new ProfileRegistry
            {
                CurrentProfileId = defaultProfileId,
                ProfileIds = new HashSet<string> { defaultProfileId }
            };
        }

        /// <summary>
        /// Add a profile to the registry
        /// </summary>
        public void AddProfile(string profileId)
        {
            this.ProfileIds.Add(profileId);
        }

        /// <summary>
        /// Remove a profile from the registry
        /// </summary>
        public bool RemoveProfile(string profileId)
        {
            return this.ProfileIds.Remove(profileId);
        }

        /// <summary>
        /// Check if a profile exists
        /// </summary>
        public bool HasProfile(string profileId)
        {
            return this.ProfileIds.Contains(profileId);
        }

        /// <summary>
        /// Get all profile IDs
        /// </summary>
        public IReadOnlyCollection<string> GetAllProfileIds()
        {
            return this.ProfileIds;
        }
    }
}
