using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core.Data.Animations;
using System;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Util.Animations
{
    /// <summary>
    /// Used for more control than what <see cref="Com.ForbiddenByte.OSA.Util.ExpandCollapseOnClick"/> offers.
    /// Holds all the required data for animating an item's size. The animation is done manually, using a MonoBehaviour's Update
    /// </summary>
    public class ExpandCollapseAnimationState
    {
        public readonly  float                expandingStartTime;
        public           float                initialExpandedAmount;
        public           float                targetExpandedAmount;
        public           float                duration;
        public           int                  itemIndex;
        private          bool                 _ForcefullyFinished;
        private readonly bool                 _UseUnscaledTime;
        private readonly Func<double, double> _Fn;

        /// <summary>
        /// Returns a value between <see cref="initialExpandedAmount"/> and <see cref="targetExpandedAmount"/> lerped by <see cref="Progress01"/>
        /// </summary>
        public float CurrentExpandedAmount => Mathf.Lerp(this.initialExpandedAmount, this.targetExpandedAmount, this.Progress01);

        /// <summary>
        /// Returns a value between <see cref="targetExpandedAmount"/> and <see cref="initialExpandedAmount"/> lerped by <see cref="Progress01"/>
        /// </summary>
        public float CurrentExpandedAmountInverse => Mathf.Lerp(this.targetExpandedAmount, this.initialExpandedAmount, this.Progress01);

        public float Progress01 => this.CurrentAnimationElapsedTimeSmooth01;

        public bool IsDone => this.CurrentAnimationElapsedTime01 == 1f;

        public bool IsExpanding => this.targetExpandedAmount > this.initialExpandedAmount;

        private float CurrentAnimationElapsedTime01
        {
            get
            {
                if (this._ForcefullyFinished) return 1f;

                // Prevent div by zero. Also, no duration means there's no animation over time
                if (this.duration == 0f) return 1f;

                var elapsed01 = (this.Time - this.expandingStartTime) / this.duration;

                if (elapsed01 >= 1f) elapsed01 = 1f;

                return elapsed01;
            }
        }

        private float CurrentAnimationElapsedTimeSmooth01
        {
            get
            {
                var t = this.CurrentAnimationElapsedTime01;
                if (t == 1f) return t;

                return (float)this._Fn(t);
            }
        }

        private float Time => this._UseUnscaledTime ? UnityEngine.Time.unscaledTime : UnityEngine.Time.time;

        public ExpandCollapseAnimationState(bool useUnscaledTime, AnimationFunctionType animationFunctionType = AnimationFunctionType.SLOW_OUT)
        {
            this._UseUnscaledTime   = useUnscaledTime;
            this._Fn                = OSAMath.GetLerpFunction(animationFunctionType);
            this.expandingStartTime = this.Time;
        }

        public void ForceFinish()
        {
            this._ForcefullyFinished = true;
        }
    }
}