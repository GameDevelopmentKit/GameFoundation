namespace DataManager.LocalSave.Migration
{
    using Cysharp.Threading.Tasks;
    using DataManager.UserData;

    /// <summary>
    /// Base class for data migrations providing common functionality.
    /// Inherit from this for easier migration implementation.
    /// </summary>
    /// <typeparam name="T">The data type this migration targets</typeparam>
    public abstract class BaseDataMigration<T> : IDataMigration<T>, IDataMigration where T : class, IUserData
    {
        public System.Type TargetDataType => typeof(T);

        /// <summary>
        /// Implement this to perform the actual migration logic
        /// </summary>
        public abstract UniTask Migrate(T data, string rawJson, IMigrationContext context);

        /// <summary>
        /// Non-generic implementation for reflection support
        /// </summary>
        UniTask IDataMigration.Migrate(IUserData data, string rawJson, IMigrationContext context)
        {
            if (data is T typedData)
            {
                return this.Migrate(typedData, rawJson, context);
            }
            else
            {
                throw new System.InvalidCastException($"Expected {typeof(T).Name} but got {data?.GetType().Name}");
            }
        }
    }
}
