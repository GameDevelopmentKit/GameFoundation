namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using System;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Signals;
    using R3;
    using UnityEngine.Scripting;

    /// <summary>
    /// A timer cooldown by cycle automatically, mainly use for UI
    /// </summary>
    public class AutoCooldownTimer : IDisposable
    {
        private const int MIN_DAYS    = 30;
        private const int MIN_HOURS   = 24;
        private const int MIN_MINUTES = 60;

        private long currentCooldownTime;

        private IDisposable observableTimer;

        private Action<long> onEveryCycle;
        private Action       onComplete;

        [Preserve]
        public AutoCooldownTimer(SignalBus signalBus)
        {
            signalBus.Subscribe<UpdateTimeAfterFocusSignal>(this.OnUpdateTimeAfterFocus);
        }

        private void OnUpdateTimeAfterFocus(UpdateTimeAfterFocusSignal signal)
        {
            this.currentCooldownTime -= (long)signal.MinimizeTime;
            if (this.currentCooldownTime <= 0)
            {
                this.onComplete?.Invoke();
            }
            else
            {
                this.observableTimer?.Dispose();
                this.CountDown(this.currentCooldownTime, this.onEveryCycle, this.onComplete);
            }
        }

        public IDisposable CountDown(long cooldownTime, Action<long> onEveryCycleParam, Action onCompleteParam = null, int depth = 0)
        {
            this.currentCooldownTime = cooldownTime;
            this.onEveryCycle        = onEveryCycleParam;
            this.onComplete          = onCompleteParam;

            var currentCycle = this.GetCycleByTime(this.currentCooldownTime);
            //        Debug.Log("Create count down with depth = " + Depth);

            this.observableTimer = Observable.Interval(TimeSpan.FromSeconds(currentCycle)).Subscribe(_ =>
            {
                // Debug.Log($"count down = {cooldownTime} - depth = {Depth}");
                this.onEveryCycle?.Invoke(this.currentCooldownTime);
                if (this.currentCooldownTime <= 0)
                {
                    this.onComplete?.Invoke();
                    this.Dispose();
                    return;
                }

                if (currentCycle != 1)
                {
                    var nextCycle = this.GetCycleByTime(this.currentCooldownTime);

                    if (nextCycle != currentCycle)
                    {
                        this.observableTimer?.Dispose();
                        this.CountDown(this.currentCooldownTime, this.onEveryCycle, this.onComplete, depth + 1);
                        return;
                    }
                }

                this.currentCooldownTime -= currentCycle;
            });

            return this;
        }

        private long GetCycleByTime(long time)
        {
            var  timeSpan = TimeSpan.FromSeconds(time);
            long cycle    = 0;
            if (timeSpan.TotalDays > MIN_DAYS)
                cycle = 86400;
            else if (timeSpan.TotalHours > MIN_HOURS)
                cycle = 3600;
            else if (timeSpan.TotalMinutes > MIN_MINUTES)
                cycle                                       = 60;
            else if (timeSpan.Minutes <= MIN_MINUTES) cycle = 1;

            return cycle;
        }

        public void Dispose()
        {
            this.onComplete   = null;
            this.onEveryCycle = null;
            this.observableTimer?.Dispose();
        }
    }
}