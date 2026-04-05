namespace DataManager.LocalSave.Migration
{
    using System;
    using System.Text;
    using DataManager.LocalSave.Handler;

    /// <summary>
    /// Implementation of IMigrationContext providing type-safe access to all save data.
    /// </summary>
    internal class MigrationContext : IMigrationContext
    {
        private readonly StringBuilder migrationLog;

        public IHandleLocalDataServices DataServices { get; }

        public MigrationContext(IHandleLocalDataServices dataServices)
        {
            this.DataServices = dataServices ?? throw new ArgumentNullException(nameof(dataServices));
            this.migrationLog = new StringBuilder();
        }

        public void Log(string message)
        {
            this.migrationLog.AppendLine($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
        }

        /// <summary>
        /// Get the complete migration log
        /// </summary>  
        public string GetLog()
        {
            return this.migrationLog.ToString();
        }
    }
}
