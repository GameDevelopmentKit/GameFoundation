namespace GameFoundation.Scripts.Backup
{
    // BackupExcludeAttribute lives in DataManager.LocalData namespace
    // (in DataManager/LocalData/BackupExcludeAttribute.cs) because
    // HandleLocalDataServices needs to reference it without a circular
    // assembly dependency.
    //
    // Re-export it here for convenience so consumers can reference it
    // via either namespace.
    using BackupExclude = DataManager.LocalSave.BackupExcludeAttribute;
}
