namespace DataManager.LocalSave.Migration
{
    using System;

    /// <summary>
    /// Attribute to mark and describe data migrations.
    /// Used for auto-discovery and building migration chains.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class MigrationAttribute : Attribute
    {
        /// <summary>
        /// Game version this migration migrates FROM (e.g., "1.0.0")
        /// </summary>
        public string FromVersion { get; }

        /// <summary>
        /// Game version this migration migrates TO (e.g., "1.1.0")
        /// </summary>
        public string ToVersion { get; }

        /// <summary>
        /// Human-readable description of what this migration does
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Optional: Migration priority (lower = runs first within same version)
        /// </summary>
        public int Priority { get; set; }

        public MigrationAttribute(string fromVersion, string toVersion, string description)
        {
            this.FromVersion = fromVersion ?? throw new ArgumentNullException(nameof(fromVersion));
            this.ToVersion = toVersion ?? throw new ArgumentNullException(nameof(toVersion));
            this.Description = description ?? throw new ArgumentNullException(nameof(description));
            this.Priority = 0;
        }
    }
}
