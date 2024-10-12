using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Util
{
    /// <summary>
    /// Important note: if used with ScrollbarFixer8 (which is true in the most cases, 
    /// make sure <see cref="ScrollbarFixer8.minSize"/> is not too small
    /// </summary>
    public class DiscreteScrollbar : MonoBehaviour
    {
        public RectTransform slotPrefab;
        public RectTransform slotsParent;
        public UnityIntEvent OnSlotSelected;
        public Func<int>     getItemsCountFunc;

        private Scrollbar        _Scrollbar;
        private RectTransform[]  slots = new RectTransform[0];
        private RectTransform    _ScrollbarPanelRT;
        private IScrollRectProxy _ScrollRectProxy;
        private int              _OneIfVert_ZeroIfHor;

        private const int  MAX_COUNT = 100;
        private       bool _UpdatePending;

        private void Awake()
        {
            // Get in parent, but ignore self
            this._ScrollRectProxy = this.transform.parent.GetComponentInParent<IScrollRectProxy>();
            if (this._ScrollRectProxy == null) throw new OSAException(this.GetType().Name + ": No IScrollRectProxy component found in parent");

            this._Scrollbar           = this.GetComponent<Scrollbar>();
            this._ScrollbarPanelRT    = this._Scrollbar.transform as RectTransform;
            this._OneIfVert_ZeroIfHor = this._ScrollRectProxy.IsHorizontal ? 0 : 1;
        }

        private void OnEnable()
        {
            this._UpdatePending = false;
        }

        public void OnScrollbarSizeChanged()
        {
            this.StartCoroutine(this.UpdateSize());
        }

        private IEnumerator UpdateSize()
        {
            while (this._UpdatePending) // wait for prev request to complete
                yield return null;

            this._UpdatePending = true;
            yield return null;

            if (this.getItemsCountFunc == null) throw new OSAException(this.GetType().Name + "getItemsCountFunc==null. Please specify a count provider");

            this._UpdatePending = true;
            var count = this.getItemsCountFunc();
            if (count > MAX_COUNT) throw new OSAException(this.GetType().Name + ": count is " + count + ". Bigger than MAX_COUNT=" + MAX_COUNT + ". Are you sure you want to use a discrete scrollbar?");

            this.Rebuild(count);
            this._UpdatePending = false;
        }

        public void Rebuild(int numSlots)
        {
            this.slotPrefab.gameObject.SetActive(true);

            // Clear prev
            if (this.slots != null)
                foreach (var slot in this.slots)
                    Destroy(slot.gameObject);

            // Add new
            this.slots = new RectTransform[numSlots];
            float sizesCumu       = 0;
            var   slotSize        = this._ScrollbarPanelRT.rect.size[this._OneIfVert_ZeroIfHor] / numSlots; // not using the handle's size because of rounding errors with higher <numSlots>
            var   edgeToInsetFrom = this._OneIfVert_ZeroIfHor == 1 ? RectTransform.Edge.Top : RectTransform.Edge.Left;
            for (var i = 0; i < numSlots; i++)
            {
                var slot = (Instantiate(this.slotPrefab.gameObject) as GameObject).GetComponent<RectTransform>();
                this.slots[i] = slot;
                slot.SetParent(this.slotsParent, false);
                slot.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(edgeToInsetFrom, sizesCumu, slotSize);
                sizesCumu += slotSize;
                var copyOfI = i;
                slot.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    if (this.OnSlotSelected != null) this.OnSlotSelected.Invoke(copyOfI);
                });
            }
            this.slotPrefab.gameObject.SetActive(false);
        }

        [Serializable]
        public class UnityIntEvent : UnityEvent<int>
        {
        }
    }
}