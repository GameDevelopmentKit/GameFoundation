namespace DataManager.LocalSave
{
    using System;

    /// <summary>
    /// Mark an <see cref="IUserData"/> class as excluded from cloud backup.
    /// By default, ALL ILocalData classes in a profile are backed up.
    /// Use this for data that is device-specific, ephemeral, or server-derived.
    /// </summary>
    /// <example>
    /// <code>
    /// [BackupExclude("Device-specific analytics")]
    /// public class AnalyticData : ILocalData { ... }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class BackupExcludeAttribute : Attribute
    {
        /// <summary>
        /// Optional reason for excluding this data from backup.
        /// Useful for documentation purposes.
        /// </summary>
        public string Reason { get; }

        public BackupExcludeAttribute(string reason = null)
        {
            this.Reason = reason;
        }
    }
}
