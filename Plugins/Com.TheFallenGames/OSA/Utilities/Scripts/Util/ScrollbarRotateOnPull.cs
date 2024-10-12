using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Visual.UI;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Util
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollbarRotateOnPull : MonoBehaviour
    {
        [SerializeField] private float _DegreesOfFreedom  = 5f;
        [SerializeField] private float _RotationSensivity = .5f;

        private IOSA          _Adapter;
        private RectTransform _HandleRT;
        private Vector2       _SrollbarPivotOnInit;

        private void Start()
        {
            this._Adapter  = this.GetComponentInParent<IOSA>();
            this._HandleRT = this.transform as RectTransform;
            //_HandleRT = _Scrollbar.handleRect;
            this._SrollbarPivotOnInit = this._HandleRT.pivot;
        }

        private void Update()
        {
            if (this._Adapter == null) return;

            var pullAmount01 = 0f;
            var piv          = this._SrollbarPivotOnInit;
            var sign         = 1;
            if (this._Adapter.GetContentSizeToViewportRatio() > 1d)
            {
                var insetStart = this._Adapter.ContentVirtualInsetFromViewportStart;
                if (insetStart > 0d)
                {
                    if (this._Adapter.IsHorizontal)
                    {
                        pullAmount01 = (float)(insetStart / this._Adapter.BaseParameters.Viewport.rect.width);
                        piv.x        = 0f;
                    }
                    else
                    {
                        pullAmount01 = (float)(insetStart / this._Adapter.BaseParameters.Viewport.rect.height);
                        piv.y        = 1f;
                    }
                }
                else
                {
                    var insetEnd = this._Adapter.ContentVirtualInsetFromViewportEnd;
                    if (insetEnd > 0d)
                    {
                        sign         = -1;
                        pullAmount01 = (float)(insetEnd / this._Adapter.GetContentSize());
                        if (this._Adapter.IsHorizontal)
                        {
                            pullAmount01 = (float)(insetEnd / this._Adapter.BaseParameters.Viewport.rect.width);
                            piv.x        = 1f;
                        }
                        else
                        {
                            pullAmount01 = (float)(insetEnd / this._Adapter.BaseParameters.Viewport.rect.height);
                            piv.y        = 0f;
                        }
                    }
                }
            }
            if (this._HandleRT.pivot != piv) this._HandleRT.pivot = piv;

            var euler = this._HandleRT.localEulerAngles;
            // Multiplying argument by _Speed to speed up sine function growth
            euler.z                         = Mathf.Sin(pullAmount01 * this._RotationSensivity * Mathf.PI) * this._DegreesOfFreedom * sign;
            this._HandleRT.localEulerAngles = euler;
        }
    }
}