using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Animations
{
    [Serializable]
    public class UserAnimationsCancellingEvents : AnimationCancellingEvents
    {
        [SerializeField] [Tooltip("Whether to cancel on OSA.SmoothScrollTo")] private bool _OnBeginSmoothScroll = true;

        /// <summary>Whether to cancel on <see cref="OSA{TParams, TItemViewsHolder}.SmoothScrollTo(int, float, float, float, Func{float, bool}, Action, bool)</summary>
        public bool OnBeginSmoothScroll { get => this._OnBeginSmoothScroll; set => this._OnBeginSmoothScroll = value; }
    }
}