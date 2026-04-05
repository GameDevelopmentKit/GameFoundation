namespace DataManager.LocalSave.Migration
{
    using DataManager.LocalSave.Handler;

    /// <summary>
    /// Context provided to migrations for accessing other data types.
    /// Allows type-safe cross-data migrations.
    /// </summary>
    public interface IMigrationContext
    {
        IHandleLocalDataServices DataServices { get; }

        /// <summary>
        /// Log a message during migration (for debugging)
        /// </summary>
        void Log(string message);
    }
}
