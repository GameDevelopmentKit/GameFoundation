namespace DataManager.LocalData
{
    using System;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using UnityEngine;
    using Zenject;

    /// <summary>Catch application event ex pause, focus and more.... </summary>
    public class MinimizeAppService : MonoBehaviour
    {
        [Inject] private SignalBus               signalBus;
        [Inject] private IHandleLocalDataServices handleLocalDataServices;

        private readonly ApplicationPauseSignal     applicationPauseSignal     = new ApplicationPauseSignal(false);
        private readonly UpdateTimeAfterFocusSignal updateTimeAfterFocusSignal = new UpdateTimeAfterFocusSignal();

        private DateTime timeBeforeAppPause = DateTime.Now;

        //Todo need
        private const int MinimizeTimeToReload = 5;

        private void OnApplicationPause(bool pauseStatus)
        {
            this.applicationPauseSignal.PauseStatus = pauseStatus;
            this.signalBus.Fire(this.applicationPauseSignal); // Active this signal later, when need

            if (pauseStatus)
            {
                this.timeBeforeAppPause = DateTime.Now;

                // save local data to storage
                this.handleLocalDataServices.SaveAll();
            }
            else
            {
                //TODO: Reload when open minimized game
                
                var intervalTimeMinimize = DateTime.Now - this.timeBeforeAppPause;

                if (MinimizeTimeToReload > 0 && intervalTimeMinimize.TotalMinutes >= MinimizeTimeToReload)
                {
                    // Reload game
                }

                this.updateTimeAfterFocusSignal.MinimizeTime = intervalTimeMinimize.TotalSeconds;
                this.signalBus.Fire(this.updateTimeAfterFocusSignal); // temporary disable this function, re-active later when game specs require
            }
        }

        private void OnApplicationQuit() { this.handleLocalDataServices.SaveAll(); }
    }
}