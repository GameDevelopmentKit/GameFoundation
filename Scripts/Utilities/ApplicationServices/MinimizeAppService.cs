namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using System;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using UnityEngine;

    /// <summary>Catch application event ex pause, focus and more.... </summary>
    public class MinimizeAppService : MonoBehaviour
    {
        private SignalBus               signalBus;
        private IHandleUserDataServices handleUserDataServices;

        public void Construct(SignalBus signalBus, IHandleUserDataServices handleUserDataServices)
        {
            this.signalBus              = signalBus;
            this.handleUserDataServices = handleUserDataServices;
        }

        private readonly ApplicationPauseSignal     applicationPauseSignal     = new(false);
        private readonly ApplicationQuitSignal      applicationQuitSignal      = new();
        private readonly UpdateTimeAfterFocusSignal updateTimeAfterFocusSignal = new();

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
                this.handleUserDataServices.SaveAll();
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

        private void OnApplicationQuit()
        {
            this.signalBus.Fire(this.applicationQuitSignal);
            this.handleUserDataServices.SaveAll();
        }
    }
}