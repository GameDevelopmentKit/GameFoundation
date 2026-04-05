namespace GameFoundation.Scripts.Backup
{
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Signal fired by <see cref="BackupManager"/> when a save data conflict is detected
    /// (both local and cloud have diverged changes).
    ///
    /// The <see cref="Resolution"/> TCS is the callback channel: whoever handles this signal
    /// (e.g., the game-side ConflictResolutionService) must call
    /// <c>Resolution.TrySetResult(ConflictChoice.KeepLocal|KeepCloud)</c> to unblock the backup flow.
    ///
    /// If no handler resolves it within the timeout, BackupManager defaults to KeepLocal.
    /// </summary>
    public class BackupConflictSignal
    {
        /// <summary>Conflict metadata for display in the UI.</summary>
        public ConflictInfo Info { get; }

        /// <summary>
        /// Callback TCS. The signal handler must resolve this to unblock <see cref="BackupManager"/>.
        /// </summary>
        public UniTaskCompletionSource<ConflictChoice> Resolution { get; }

        public BackupConflictSignal(ConflictInfo info)
        {
            this.Info       = info;
            this.Resolution = new UniTaskCompletionSource<ConflictChoice>();
        }
    }
}
