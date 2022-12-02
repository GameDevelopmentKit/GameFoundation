/*! \cond PRIVATE */
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [AddComponentMenu("Dark Tonic/Master Audio/Button Clicker")]
    // ReSharper disable once CheckNamespace
    public class ButtonClicker : MonoBehaviour {
        public const float SmallSizeMultiplier = 0.9f;
        public const float LargeSizeMultiplier = 1.1f;

        // ReSharper disable InconsistentNaming
        public bool resizeOnClick = true;
        public bool resizeClickAllSiblings = false;
        public bool resizeOnHover = false;
        public bool resizeHoverAllSiblings = false;
        public string mouseDownSound = string.Empty;
        public string mouseUpSound = string.Empty;
        public string mouseClickSound = string.Empty;
        public string mouseOverSound = string.Empty;
        public string mouseOutSound = string.Empty;
        // ReSharper restore InconsistentNaming

        private Vector3 _originalSize;
        private Vector3 _smallerSize;
        private Vector3 _largerSize;
        private Transform _trans;

        private readonly Dictionary<Transform, Vector3> _siblingClickScaleByTransform =
            new Dictionary<Transform, Vector3>();

        private readonly Dictionary<Transform, Vector3> _siblingHoverScaleByTransform =
            new Dictionary<Transform, Vector3>();

        // This script can be triggered from NGUI clickable elements only. 
        // ReSharper disable once UnusedMember.Local
        private void Awake() {
            _trans = transform;
            _originalSize = _trans.localScale;
            _smallerSize = _originalSize * SmallSizeMultiplier;
            _largerSize = _originalSize * LargeSizeMultiplier;

            var holder = _trans.parent;

            if (resizeOnClick && resizeClickAllSiblings && holder != null) {
                for (var i = 0; i < holder.transform.childCount; i++) {
                    var aChild = holder.transform.GetChild(i);
                    _siblingClickScaleByTransform.Add(aChild, aChild.localScale);
                }
            }

            if (!resizeOnHover || !resizeHoverAllSiblings || holder == null) {
                return;
            }
            for (var i = 0; i < holder.transform.childCount; i++) {
                var aChild = holder.transform.GetChild(i);
                _siblingHoverScaleByTransform.Add(aChild, aChild.localScale);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPress(bool isDown) {
            if (isDown) {
                if (!enabled) {
                    return;
                }
                MasterAudio.PlaySoundAndForget(mouseDownSound);

                if (!resizeOnClick) {
                    return;
                }
                _trans.localScale = _smallerSize;

                var scales = _siblingClickScaleByTransform.GetEnumerator();

                while (scales.MoveNext()) {
                    scales.Current.Key.localScale = scales.Current.Value * SmallSizeMultiplier;
                }
            } else {
                if (enabled) {
                    MasterAudio.PlaySoundAndForget(mouseUpSound);
                }
                // still want to restore size if disabled

                if (!resizeOnClick) {
                    return;
                }
                _trans.localScale = _originalSize;

                var scales = _siblingClickScaleByTransform.GetEnumerator();

                while (scales.MoveNext()) {
                    scales.Current.Key.localScale = scales.Current.Value;
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnClick() {
            if (enabled) {
                MasterAudio.PlaySoundAndForget(mouseClickSound);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnHover(bool isOver) {
            if (isOver) {
                if (!enabled) {
                    return;
                }
                MasterAudio.PlaySoundAndForget(mouseOverSound);

                if (!resizeOnHover) {
                    return;
                }
                _trans.localScale = _largerSize;

                var scales = _siblingHoverScaleByTransform.GetEnumerator();

                while (scales.MoveNext()) {
                    scales.Current.Key.localScale = scales.Current.Value * LargeSizeMultiplier;
                }
            } else {
                if (enabled) {
                    MasterAudio.PlaySoundAndForget(mouseOutSound);
                }
                // still want to restore size if disabled

                if (!resizeOnHover) {
                    return;
                }
                _trans.localScale = _originalSize;

                var scales = _siblingHoverScaleByTransform.GetEnumerator();

                while (scales.MoveNext()) {
                    scales.Current.Key.localScale = scales.Current.Value;
                }
            }
        }
    }
}
/*! \endcond */