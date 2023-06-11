/*! \cond PRIVATE */
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    public class AudioTransformTracker : MonoBehaviour {
        public int _frames;

        private Transform _trans;

        public Transform Trans {
            get {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (_trans == null) {
                    _trans = transform;
                }

                return _trans;
            }
        }

        void Update() {
            _frames++;
        }
    }

}
/*! \endcond */