namespace DataManager.LocalSave
{
    using System;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Handler;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Zenject;

    /// <summary>
    /// Catches application lifecycle events (pause, focus, quit, scene load) and requests local data saves.
    /// Save coalescing is owned by HandleLocalDataServices so lifecycle events cannot drop requests.
    /// </summary>
    public class ApplicationService : MonoBehaviour
    {
        [Inject] private SignalBus signalBus;
        [Inject] private IHandleLocalDataServices handleLocalDataServices;

        private readonly ApplicationPauseSignal applicationPauseSignal = new ApplicationPauseSignal(false);
        private readonly UpdateTimeAfterFocusSignal updateTimeAfterFocusSignal = new UpdateTimeAfterFocusSignal();

        private DateTime timeBeforeAppPause = DateTime.Now;

        /// <summary>
        /// True while the central LocalSave service is draining a profile save.
        /// </summary>
        public bool IsSaving => this.handleLocalDataServices?.IsSavingCurrentProfile == true;

        //Todo need
        private const int MinimizeTimeToReload = 5;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            RequestSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Only save when pausing (going to background), not when resuming
                RequestSave();
                this.timeBeforeAppPause = DateTime.Now;
            }
            else
            {
                var intervalTimeMinimize = DateTime.Now - this.timeBeforeAppPause;

                if (MinimizeTimeToReload > 0 && intervalTimeMinimize.TotalMinutes >= MinimizeTimeToReload)
                {
                    // TODO: Reload when open minimized game
                }

                this.updateTimeAfterFocusSignal.MinimizeTime = intervalTimeMinimize.TotalSeconds;
                this.signalBus.Fire(this.updateTimeAfterFocusSignal);
            }

            this.applicationPauseSignal.PauseStatus = pauseStatus;
            this.signalBus.Fire(this.applicationPauseSignal);
        }

        private void OnApplicationQuit()
        {
            RequestSave();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Only save when losing focus, not when gaining focus
                RequestSave();
            }
        }

        private void OnDestroy()
        {
            RequestSave();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Request a profile save. Overlapping requests are coalesced by HandleLocalDataServices.
        /// </summary>
        private void RequestSave()
        {
            if (!this.handleLocalDataServices.IsInitialized) return;

            SaveAsync().Forget();
        }

        private async UniTaskVoid SaveAsync()
        {
            try
            {
                await this.handleLocalDataServices.SaveCurrentProfile();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ApplicationService] Save failed: {ex.Message}");
            }
        }
    }
}
