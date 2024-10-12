using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Animations
{
    /// <summary>
    /// Parameters for cancelling certain animations on certain events
    /// </summary>
    [Serializable]
    public class AnimationCancelling
    {
        [SerializeField] [Tooltip("This decides whether or not to stop an existing SmoothScrollTo animation on certain events")] private SmoothScrollCancellingEvents _SmoothScroll = new();

        /// <summary>
        /// This decides whether or not to stop an existing <see cref="OSA{TParams, TItemViewsHolder}.SmoothScrollTo(int, float, float, float, Func{float, bool}, Action, bool)"/> animation on certain events
        /// </summary>
        public SmoothScrollCancellingEvents SmoothScroll { get => this._SmoothScroll; set => this._SmoothScroll = value; }

        [SerializeField]
        [Tooltip("Custom animations you may have. This decides whether or not to call CancelUserAnimations() on certain events, which you can override to comply")]
        private UserAnimationsCancellingEvents _UserAnimations = new();

        /// <summary>
        /// Custom animations you may have. This decides whether or not to call <see cref="OSA{TParams, TItemViewsHolder}.CancelUserAnimations"/>, which you can override to comply
        /// </summary>
        public UserAnimationsCancellingEvents UserAnimations { get => this._UserAnimations; set => this._UserAnimations = value; }
    }
}