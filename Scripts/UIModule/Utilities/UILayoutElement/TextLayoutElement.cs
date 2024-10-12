using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Scripts.UIModule.Utilities.UILayoutElement
{
    [ExecuteInEditMode]
    public class TextLayoutElement : UIBehaviour, ILayoutSelfController, ILayoutController
    {
        [SerializeField] private bool isValid;

        //[HideInInspector]
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private bool fitWidth;
        [SerializeField] private bool fitHeight;

        [SerializeField] private Vector2 maxValue;
        [SerializeField] private Vector2 minValue;

        private DrivenRectTransformTracker m_Tracker;
        private RectTransform              m_Rect;

        private RectTransform rectTransform
        {
            get
            {
                if (this.m_Rect == null) this.m_Rect = this.GetComponent<RectTransform>();
                return this.m_Rect;
            }
        }

        private WaitForEndOfFrame waitForEndOfFrame;

        protected override void Start()
        {
            base.Start();
            this.waitForEndOfFrame = new();
        }

        protected override void OnEnable()
        {
            this.isValid = this.text != null && this.rectTransform != null;
            base.OnEnable();
            this.SetDirty();
        }

        protected override void OnDisable()
        {
            this.m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            this.SetDirty();
        }

        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            this.SetDirty();
        }

        #endif

        protected void SetDirty()
        {
            if (!this.IsActive()) return;
            if (!CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            else
                this.StartCoroutine(this.DelayedSetDirty(this.rectTransform));
        }

        private IEnumerator DelayedSetDirty(RectTransform rectTransform)
        {
            yield return this.waitForEndOfFrame;
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
        }

        public void SetLayoutHorizontal()
        {
            this.m_Tracker.Clear();
            if (this.isValid && this.fitWidth)
            {
                var preferredValuesX = this.text.GetPreferredValues().x;
                if (Mathf.Approximately(preferredValuesX, this.rectTransform.sizeDelta.x)) return;
                this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaX);
                var maxX = this.maxValue.x > 0 ? this.maxValue.x : preferredValuesX;
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(preferredValuesX, this.minValue.x, maxX));
            }
        }

        public void SetLayoutVertical()
        {
            if (this.isValid && this.fitHeight)
            {
                this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaY);
                var preferredValues = this.text.GetPreferredValues();
                var maxY            = this.maxValue.y > 0 ? this.maxValue.y : preferredValues.y;
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(preferredValues.y, this.minValue.y, maxY));
            }
        }
    }
}