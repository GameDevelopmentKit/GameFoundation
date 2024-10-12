using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Animations
{
    /// <summary>
    /// Parameters for animations in general
    /// </summary>
    [Serializable]
    public class AnimationParams
    {
        [SerializeField] private AnimationFunctionType _SmoothScrollType = AnimationFunctionType.SLOW_OUT;
        public                   AnimationFunctionType SmoothScrollType { get => this._SmoothScrollType; set => this._SmoothScrollType = value; }

        [SerializeField] private AnimationCancelling _Cancel = new();
        public                   AnimationCancelling Cancel { get => this._Cancel; set => this._Cancel = value; }

        public bool CallDoneOnScrollCancel { get => this._OnDoneWhenCancelled; set => this._OnDoneWhenCancelled = value; }

        [SerializeField] private bool _OnDoneWhenCancelled;
    }
}