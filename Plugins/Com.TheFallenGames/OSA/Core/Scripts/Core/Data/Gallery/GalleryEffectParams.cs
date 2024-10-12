using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Gallery
{
    /// <summary>
    /// Parameters for a gallery effect
    /// </summary>
    [Serializable]
    public class GalleryEffectParams
    {
        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("The amount of the gallery effect itself, independent of the amounts of individual animation types. 0=disabled")]
        private float _OverallAmount = 0f;

        /// <summary>
        /// The amount of the gallery effect itself, independent of the amounts of individual animation types. 0=disabled
        /// </summary>
        public float OverallAmount { get => this._OverallAmount; set => this._OverallAmount = value; }

        [SerializeField] private GalleryScale _Scale = new();
        public                   GalleryScale Scale { get => this._Scale; set => this._Scale = value; }

        [SerializeField] private GalleryRotation _Rotation = new();
        public                   GalleryRotation Rotation { get => this._Rotation; set => this._Rotation = value; }
    }
}