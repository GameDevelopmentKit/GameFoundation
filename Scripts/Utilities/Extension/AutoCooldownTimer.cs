namespace Mech.Utils
{
    using System;
    using UniRx;

    /// <summary>
    /// A timer cooldown by cycle automatically, mainly use for UI
    /// </summary>
    public class AutoCooldownTimer : IDisposable
    {
        private IDisposable observableTimer;
        private int         minDays, minHours, minMinutes;

        public AutoCooldownTimer(int minMinutes = 60, int minHours = 24, int minDays = 30)
        {
            this.minDays    = minDays;
            this.minHours   = minHours;
            this.minMinutes = minMinutes;
        }

        public IDisposable CountDown(long cooldownTime, Action<long> onEveryCyle, int Depth = 0)
        {
            var currentCycle = GetCycleByTime(cooldownTime);
//        Debug.Log("Create count down with depth = " + Depth);
            observableTimer = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(currentCycle))
                .Subscribe(l =>
                {
                    // Debug.Log($"count down = {cooldownTime} - depth = {Depth}");
                    onEveryCyle?.Invoke(cooldownTime);
                    if (cooldownTime <= 0)
                    {
                        Dispose();
                        return;
                    }

                    if (currentCycle != 1)
                    {
                        var nextCycle = GetCycleByTime(cooldownTime);

                        if (nextCycle != currentCycle)
                        {
                            Dispose();
                            CountDown(cooldownTime, onEveryCyle, Depth + 1);
                            return;
                        }
                    }

                    cooldownTime = cooldownTime - currentCycle;
                });
            return observableTimer;
        }

        private long GetCycleByTime(long time)
        {
            var  timeSpan = TimeSpan.FromSeconds(time);
            long cycle    = 0;
            if (timeSpan.TotalDays > minDays)
            {
                cycle = 86400;
            }
            else if (timeSpan.TotalHours > minHours)
            {
                cycle = 3600;
            }
            else if (timeSpan.TotalMinutes > minMinutes)
            {
                cycle = 60;
            }
            else if (timeSpan.Minutes <= minMinutes)
            {
                cycle = 1;
            }

            return cycle;
        }

        public void Dispose() { observableTimer?.Dispose(); }
    }
}