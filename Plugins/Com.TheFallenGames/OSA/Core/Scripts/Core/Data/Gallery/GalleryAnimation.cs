using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Core.Data.Gallery
{
    /// <summary>
    /// Parameters for a gallery effect. The term 'value' can mean something else depending on which property of the items is affected.
    /// For example, it can mean localScale or localEulerAngles
    /// </summary>
    [Serializable]
    public abstract class GalleryAnimation
    {
        public const float MAX_EFFECT_EXPONENT = 50f;

        [Range(0f, 1f)]
        [SerializeField]
        [Tooltip("Applies 'biggest' value for the item in the middle and gradually 'lowers' the value of the side items. \n" + "0=no effect, 1=the most sideways items will have the 'lowest' value. \n" + "ViewportPivot can be used to set the weight in other place than the middle.")]
        private float _Amount = 0f;

        /// <summary>
        /// Applies max value for the item in the middle and gradually 'lowers' the value of the side items. 
        /// 0=no effect, 1=the most sideways items will have the 'lowest' value. 
        /// <see cref="ViewportPivot"/> can be used to set the weight in other place than the middle
        /// </summary>
        public float Amount { get => this._Amount; set => this._Amount = value; }

        [Range(-1f, 2f)]
        [SerializeField]
        [Tooltip("0=start; 1=end; -1=point situated at <viewportSize> units before start; 2=point situated at <viewportSize> units after end")]
        private float _ViewportPivot = .5f;

        /// <summary>0=start; 1=end; -1=point situated at 'viewportSize' units before start; 2=point situated at 'viewportSize' units after end</summary>
        public float ViewportPivot { get => this._ViewportPivot; set => this._ViewportPivot = value; }

        [Range(1f, MAX_EFFECT_EXPONENT)]
        [SerializeField]
        [Tooltip("1=linear. The bigger the exponent, the faster the value is lowered for the items that are far from pivot (see ViewportPivot)")]
        private float _Exponent = 1f;

        /// <summary>1=linear. The bigger the exponent, the faster the value is lowered for the items that are far from pivot (see <see cref="ViewportPivot"/>)</summary>
        public float Exponent { get => this._Exponent; set => this._Exponent = value; }

        /// <summary>From which to which value to interpolate the value, per component</summary>
        public abstract Vector3Space TransformSpace { get; set; }

        [Serializable]
        public class Vector3Space
        {
            [SerializeField] [Tooltip("Value for the furthest items from pivot, per component. In other words, the 'lowest' value")] private Vector3 _From = Vector3.zero;

            /// <summary>Value for the furthest items from pivot, per component. In other words, the 'lowest' value</summary>
            public Vector3 From { get => this._From; set => this._From = value; }

            [SerializeField] [Tooltip("Value for the closest items to pivot, per component. In other words, the 'biggest' value")] private Vector3 _To = Vector3.zero;

            /// <summary>Value for the closest items to pivot, per component. In other words, the 'biggest' value</summary>
            public Vector3 To { get => this._To; set => this._To = value; }

            public Vector3Space()
            {
            }

            public Vector3Space(Vector3 from, Vector3 to)
            {
                this._From = from;
                this._To   = to;
            }

            public Vector3 Transform(float amount01)
            {
                return Vector3.Lerp(this._From, this._To, amount01);
            }
        }
    }
}