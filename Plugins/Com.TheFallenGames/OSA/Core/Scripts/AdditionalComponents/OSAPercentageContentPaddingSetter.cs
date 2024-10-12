using System.Collections;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.AdditionalComponents
{
    /// <summary>
    /// Use this when the content padding should be a function of the viewport size, rather than a constant decided at edit-time. 
    /// In other words, use if you want to specify the padding as a percentage rather than in pixels. It also allows for fine adjustments of the first/last item, mostly useful for centering them.
    /// A use case is keeping the last/first element in the middle when the content's extremity is reached. This can be done by setting a constant padding, 
    /// but having a percentage-specified padding allows for seamless screen size changes
    /// </summary>
    public class OSAPercentageContentPaddingSetter : MonoBehaviour
    {
        [SerializeField] [Range(0f, 1f)] [Tooltip("0 = none, .5f = half of the viewport, 1f = the entire viewport's size will be used for padding")] private float _PaddingStartPercent = .5f;

        [SerializeField] [Range(0f, 1f)] [Tooltip("Same rules as for PaddingStartPercent")] private float _PaddingEndPercent = .5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("After setting the padding, how much will this item approach the viewport's edge based on its size?. 0=none, i.e. full padding. 1=a distance equal to its size. \n" + "For example, a 0.5 value could be used along with PaddingStartPercent and PaddingEndPercent also set to 0.5, resulting in first/last items arriving exactly in the middle when you scroll in the extremities.\n" + "ItemSizeCustomSource must also be set for this to be accurate. Otherwise, OSA.Parameters.DefaultItemSize will be used")]
        private float _FirstLastItemsInsidePercent = .5f;

        [SerializeField]
        [Tooltip("This object's width or height will be used to calculate the most accurate position to satisfy the FirstLastItemsInsidePercent property")]
        private RectTransform _ItemSizeCustomSource = null;

        private IOSA _IOSA;
        private bool _UseDefaultItemSize;

        private bool _IsHorizontal;
        //float _LastItemSize = float.MinValue * 1.1f;
        //float _LastVPSize = float.MinValue * 1.1f;

        #region Unity

        private void Awake()
        {
            this.enabled = false;
            this._IOSA   = this.GetComponent(typeof(IOSA)) as IOSA;
            if (this._IOSA == null)
            {
                Debug.Log(typeof(OSAPercentageContentPaddingSetter).Name + " needs to be attached to a game object containing an OSA component");
                return;
            }

            if (this._IOSA.IsInitialized)
            {
                Debug.Log(typeof(OSAPercentageContentPaddingSetter).Name + " needs the OSA component to not be initialized before it");
                return;
            }

            var parameters = this._IOSA.BaseParameters;
            this._UseDefaultItemSize = this._ItemSizeCustomSource == null;
            this._IsHorizontal       = this._IOSA.BaseParameters.IsHorizontal;

            this._IOSA.ScrollViewSizeChanged += this.OnScrollViewSizeChanged;
            //_IOSA.ItemsRefreshed += OnItemsRefreshed;

            parameters.PrepareForInit(true);
            parameters.InitIfNeeded(this._IOSA);

            this.UpdatePadding();
        }

        #endregion

        /// <summary>Each time the ScrollView's size changes, the padding needs to be recalculated. <see cref="OSA{TParams, TItemViewsHolder}.ScrollViewSizeChanged"/></summary>
        private void OnScrollViewSizeChanged()
        {
            if (!this._IOSA.IsInitialized)
            {
                Debug.LogError(typeof(OSAPercentageContentPaddingSetter).Name + ".OnScrollViewSizeChanged() called, but OSA not initialized. This shouldn't happen if implemented correctly");
                return;
            }

            this._IOSA.BaseParameters.PrepareForInit(false);
            this._IOSA.BaseParameters.InitIfNeeded(this._IOSA);

            this.UpdatePadding();
        }

        //void OnItemsRefreshed(int _, int __)
        //{
        //	float curSize;
        //	if (_UseDefaultItemSize)
        //		curSize = _IOSA.BaseParameters.DefaultItemSize;
        //	else
        //		curSize = GetSourceItemSize();

        //	if (Mathf.Abs(curSize - _LastItemSize) < .01f)
        //		return;

        //	float curVPSize = GetVPSize();
        //	if (Mathf.Abs(curVPSize - _LastVPSize) < .01f)
        //		return;

        //	SetPaddingWith(curVPSize, curSize);

        //	_IOSA.asdasdas
        //	_IOSA.Refresh(false, true);
        //}

        private void UpdatePadding()
        {
            if (this._UseDefaultItemSize)
                this.SetPaddingFromDefaultItemSize();
            else
                this.SetPaddingFromCustomItemSource();
        }

        private void SetPaddingFromCustomItemSource()
        {
            this.SetPaddingWith(this.GetVPSize(), this.GetSourceItemSize());
        }

        private void SetPaddingFromDefaultItemSize()
        {
            this.SetPaddingWith(this.GetVPSize(), this._IOSA.BaseParameters.DefaultItemSize);
        }

        private void SetPaddingWith(float vpSize, float itemSizeToUse)
        {
            var parameters                = this._IOSA.BaseParameters;
            var firstLastItemInsideAmount = itemSizeToUse * this._FirstLastItemsInsidePercent;
            var pad                       = parameters.ContentPadding;
            var padStart                  = (int)(vpSize * this._PaddingStartPercent - firstLastItemInsideAmount + .5f);
            var padEnd                    = (int)(vpSize * this._PaddingEndPercent - firstLastItemInsideAmount + .5f);
            if (parameters.IsHorizontal)
            {
                pad.left  = padStart;
                pad.right = padEnd;
            }
            else
            {
                pad.top    = padStart;
                pad.bottom = padEnd;
            }

            //_LastItemSize = itemSizeToUse;
            //_LastVPSize = vpSize;
        }

        private float GetSourceItemSize()
        {
            var itemRect = this._ItemSizeCustomSource.rect;
            return this._IsHorizontal ? itemRect.width : itemRect.height;
        }

        private float GetVPSize()
        {
            var vpRect = this._IOSA.BaseParameters.Viewport.rect;
            return this._IsHorizontal ? vpRect.width : vpRect.height;
        }
    }
}