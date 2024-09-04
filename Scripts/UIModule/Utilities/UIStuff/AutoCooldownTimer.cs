﻿namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using System;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using R3;
    using Zenject;

    /// <summary>
    /// A timer cooldown by cycle automatically, mainly use for UI
    /// </summary>
    public class AutoCooldownTimer : IDisposable
    {
        private readonly int  minDays;
        private readonly int  minHours;
        private readonly int  minMinutes;
        private          long currentCooldownTime;

        private IDisposable observableTimer;

        private Action<long> onEveryCycle;
        private Action       onComplete;

        public AutoCooldownTimer(SignalBus signalBus, int minMinutes = 60, int minHours = 24, int minDays = 30)
        {
            this.minDays    = minDays;
            this.minHours   = minHours;
            this.minMinutes = minMinutes;

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
            if (timeSpan.TotalDays > this.minDays)
            {
                cycle = 86400;
            }
            else if (timeSpan.TotalHours > this.minHours)
            {
                cycle = 3600;
            }
            else if (timeSpan.TotalMinutes > this.minMinutes)
            {
                cycle = 60;
            }
            else if (timeSpan.Minutes <= this.minMinutes)
            {
                cycle = 1;
            }

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