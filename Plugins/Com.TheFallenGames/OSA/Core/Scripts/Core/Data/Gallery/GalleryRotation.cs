using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Gallery
{
    /// <summary>
    /// See <see cref="GalleryAnimation"/>
    /// </summary>
    [Serializable]
    public class GalleryRotation : GalleryAnimation
    {
        [SerializeField] [Tooltip("From which to which value to interpolate the rotation, per component")] [FormerlySerializedAs("_ScaleSpace")] private Vector3Space _TransformSpace = new(Vector3.zero, Vector3.zero);

        /// <summary>From which to which value to interpolate the rotation, per component</summary>
        public override Vector3Space TransformSpace { get => this._TransformSpace; set => this._TransformSpace = value; }
    }
}