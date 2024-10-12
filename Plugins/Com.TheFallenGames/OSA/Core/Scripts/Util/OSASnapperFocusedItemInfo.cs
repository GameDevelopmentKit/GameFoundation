using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Visual.UI;
using Com.ForbiddenByte.OSA.Core;
using System;
using UnityEngine.Events;

namespace Com.ForbiddenByte.OSA.Util
{
    /// <summary>
    /// Utility providing a way of getting the currently focused item by a <see cref="Snapper8"/> on OSA. 
    /// Attach it to the same game object containing your OSA implementation.
    /// </summary>
    public class OSASnapperFocusedItemInfo : MonoBehaviour
    {
        [SerializeField] private Snapper8 _Snapper = null;

        /// <summary>Fired when the currently focused item changes. It passes the actual ViewsHolder instance as <see cref="AbstractViewsHolder"/> - simply cast it to your known VH type, if needed</summary>
        public FocusedItemChangedUnityEvent FocusedItemChanged;

        /// <summary>Fired when the currently focused item changes</summary>
        public FocusedItemIndexChangedUnityEvent FocusedItemIndexChanged;

        /// <summary>
        /// The "ItemIndex" of the focused item, if any. -1 if none focused
        /// </summary>
        public int FocusedIndex => this._FocusedIndex;

        private int  _FocusedIndex = -1;
        private IOSA _OSA;

        private void Start()
        {
            this._OSA                =  this.GetComponent(typeof(IOSA)) as IOSA;
            this._OSA.ItemsRefreshed += this.OnItemsRefreshed;
        }

        private void Update()
        {
            float _;
            var   vh = this._Snapper.GetMiddleVH(out _);
            int   newIndex;
            if (vh == null)
                newIndex = -1;
            else
                newIndex = vh.ItemIndex;

            if (this._FocusedIndex != newIndex) this.ChangeFocusedItem(vh);
        }

        private void OnItemsRefreshed(int _, int __)
        {
            if (this._FocusedIndex != -1) this.ChangeFocusedItem(null);
        }

        private void ChangeFocusedItem(AbstractViewsHolder vh)
        {
            if (vh == null)
                this._FocusedIndex = -1;
            else
                this._FocusedIndex = vh.ItemIndex;

            // Normally, it's only 1 item with this index, so this is called only once in the loop
            if (this.FocusedItemChanged != null) this.FocusedItemChanged.Invoke(vh);

            if (this.FocusedItemIndexChanged != null) this.FocusedItemIndexChanged.Invoke(this._FocusedIndex);
        }

        [Serializable]
        public class FocusedItemChangedUnityEvent : UnityEvent<AbstractViewsHolder>
        {
        }

        [Serializable]
        public class FocusedItemIndexChangedUnityEvent : UnityEvent<int>
        {
        }
    }
}