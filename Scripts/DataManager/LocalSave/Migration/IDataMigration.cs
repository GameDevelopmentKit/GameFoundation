namespace DataManager.LocalSave.Migration
{
    using Cysharp.Threading.Tasks;
    using DataManager.UserData;
    /// <summary>
    /// Interface for type-safe data migrations.
    /// Each migration focuses on one data type but can access others via context.
    /// </summary>
    /// <typeparam name="T">The primary data type this migration targets</typeparam>
    /// <summary>
    /// Interface for type-safe data migrations.
    /// Each migration focuses on one data type but can access others via context.
    /// </summary>
    /// <typeparam name="T">The primary data type this migration targets</typeparam>
    public interface IDataMigration<T> where T : class, IUserData
    {
        /// <summary>
        /// Perform migration on the specified data.
        /// </summary>
        /// <param name="data">The data to migrate (already deserialized with lenient settings)</param>
        /// <param name="rawJson">Raw JSON string for accessing renamed/removed fields</param>
        /// <param name="context">Context for accessing other data types</param>
        UniTask Migrate(T data, string rawJson, IMigrationContext context);
    }

    /// <summary>
    /// Non-generic base interface for reflection/discovery purposes.
    /// </summary>
    public interface IDataMigration
    {
        /// <summary>
        /// The primary data type this migration targets
        /// </summary>
        System.Type TargetDataType { get; }

        /// <summary>
        /// Perform migration (non-generic version)
        /// </summary>
        UniTask Migrate(IUserData data, string rawJson, IMigrationContext context);
    }
}
