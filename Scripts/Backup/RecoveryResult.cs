namespace GameFoundation.Scripts.Backup
{
    /// <summary>
    /// Result of a recovery check performed by <see cref="IBackupService.CheckAndRecoverAsync"/>.
    /// </summary>
    public class RecoveryResult
    {
        public RecoveryOutcome Outcome { get; set; }
        public string ErrorMessage { get; set; }

        public static RecoveryResult NoBackup() => new() { Outcome = RecoveryOutcome.NoBackup };
        public static RecoveryResult InSync() => new() { Outcome = RecoveryOutcome.InSync };
        public static RecoveryResult BackedUp() => new() { Outcome = RecoveryOutcome.BackedUp };
        public static RecoveryResult Restored() => new() { Outcome = RecoveryOutcome.Restored };
        public static RecoveryResult ConflictResolved() => new() { Outcome = RecoveryOutcome.ConflictResolved };
        public static RecoveryResult Offline() => new() { Outcome = RecoveryOutcome.Offline };
        public static RecoveryResult Failed(string msg) => new() { Outcome = RecoveryOutcome.Failed, ErrorMessage = msg };
    }

    public enum RecoveryOutcome
    {
        /// <summary>No backup existed on cloud. First-time backup was performed.</summary>
        NoBackup,

        /// <summary>Local and cloud are already in sync. Nothing to do.</summary>
        InSync,

        /// <summary>Local was newer. Backup was pushed to cloud.</summary>
        BackedUp,

        /// <summary>Cloud was newer. Data was restored from cloud.</summary>
        Restored,

        /// <summary>Conflict detected and resolved (either KeepLocal or KeepCloud).</summary>
        ConflictResolved,

        /// <summary>Recovery failed due to an error.</summary>
        Failed,

        /// <summary>Not authenticated or no network. Recovery skipped.</summary>
        Offline,
    }

    public enum BackupStatus
    {
        Idle,
        BackingUp,
        Restoring,
        Offline,
        Error,
    }
}
