using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Animations
{
    [Serializable]
    public abstract class AnimationCancellingEvents
    {
        [SerializeField] [Tooltip("Whether to cancel on any count change event: Insert/Remove/Reset")] private bool _OnCountChanges = true;

        /// <summary>Whether to cancel on any count change event: Insert/Remove/Reset</summary>
        public bool OnCountChanges { get => this._OnCountChanges; set => this._OnCountChanges = value; }

        [SerializeField] [Tooltip("Whether to cancel on any event that changes sizes of the items or content")] private bool _OnSizeChanges = true;

        /// <summary>Whether to cancel on any event that changes sizes of the items or content</summary>
        public bool OnSizeChanges { get => this._OnSizeChanges; set => this._OnSizeChanges = value; }

        [SerializeField] [Tooltip("Whether to cancel on OSA.ScrollTo")] private bool _OnScrollTo = true;

        /// <summary>Whether to cancel on <see cref="OSA{TParams, TItemViewsHolder}.ScrollTo(int, float, float)"/></summary>
        public bool OnScrollTo { get => this._OnScrollTo; set => this._OnScrollTo = value; }
    }
}