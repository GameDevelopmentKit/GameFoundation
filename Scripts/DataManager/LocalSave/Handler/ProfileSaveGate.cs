namespace DataManager.LocalSave.Handler
{
    using System;
    using Cysharp.Threading.Tasks;

    /// <summary>
    /// Coalesces overlapping profile save requests into one active drain.
    /// The drain keeps saving until it has caught up with the latest requested version.
    /// </summary>
    internal sealed class ProfileSaveGate
    {
        private readonly object gate = new();
        private long requestedVersion;
        private bool isDraining;
        private UniTaskCompletionSource completionSource;

        public bool IsSaving
        {
            get
            {
                lock (this.gate)
                {
                    return this.isDraining;
                }
            }
        }

        public UniTask RequestSave(Func<UniTask> saveOnce)
        {
            if (saveOnce == null) throw new ArgumentNullException(nameof(saveOnce));

            UniTaskCompletionSource activeCompletionSource;
            lock (this.gate)
            {
                this.requestedVersion++;

                if (this.isDraining)
                {
                    return this.completionSource.Task;
                }

                this.isDraining = true;
                this.completionSource = new UniTaskCompletionSource();
                activeCompletionSource = this.completionSource;
            }

            this.DrainSaveRequests(saveOnce, activeCompletionSource).Forget();
            return activeCompletionSource.Task;
        }

        private async UniTask DrainSaveRequests(Func<UniTask> saveOnce, UniTaskCompletionSource activeCompletionSource)
        {
            try
            {
                while (true)
                {
                    long versionToSave;
                    lock (this.gate)
                    {
                        versionToSave = this.requestedVersion;
                    }

                    await saveOnce();

                    lock (this.gate)
                    {
                        if (this.requestedVersion != versionToSave)
                        {
                            continue;
                        }

                        this.isDraining = false;
                        this.completionSource = null;
                    }

                    activeCompletionSource.TrySetResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                lock (this.gate)
                {
                    this.isDraining = false;
                    this.completionSource = null;
                }

                activeCompletionSource.TrySetException(ex);
            }
        }
    }
}
