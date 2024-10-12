using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Scripts.UIModule.Utilities.UILayoutElement
{
    [ExecuteAlways]
    public class FollowLayoutElementPreferredSize : UIBehaviour, ILayoutSelfController, ILayoutController
    {
        [SerializeField] private bool isValid;

        [Tooltip("Target component must be inherited from ILayoutElement")] [SerializeField]                         private LayoutGroup   target;
        [Tooltip("Can be null if Target Component and Target RectTransform is in the same object")] [SerializeField] private RectTransform targetRectTransform;

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

        private RectTransform TargetRectTransform => this.targetRectTransform;

        private WaitForEndOfFrame waitForEndOfFrame;

        protected override void Start()
        {
            base.Start();
            this.waitForEndOfFrame = new();
        }

        protected override void OnEnable()
        {
            this.isValid = this.target != null && this.rectTransform != null && this.target is ILayoutElement;
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

        protected void OnTransformChildrenChanged()
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
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void SetLayoutHorizontal()
        {
            this.m_Tracker.Clear();
            if (this.isValid && this.fitWidth)
            {
                var targetValueX     = this.rectTransform.sizeDelta.x + ((ILayoutElement)this.target).preferredWidth - this.TargetRectTransform.rect.width;
                var maxX             = this.maxValue.x > 0 ? this.maxValue.x : targetValueX;
                var preferredValuesX = Mathf.Clamp(targetValueX, this.minValue.x, maxX);
                if (Mathf.Approximately(targetValueX, this.rectTransform.sizeDelta.x)) return;
                this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaX);
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredValuesX);
            }
        }

        public void SetLayoutVertical()
        {
            if (this.isValid && this.fitHeight)
            {
                var targetValueX     = this.rectTransform.sizeDelta.y + ((ILayoutElement)this.target).preferredHeight - this.TargetRectTransform.rect.height;
                var maxY             = this.maxValue.y > 0 ? this.maxValue.y : targetValueX;
                var preferredValuesY = Mathf.Clamp(targetValueX, this.minValue.y, maxY);
                if (Mathf.Approximately(preferredValuesY, this.rectTransform.sizeDelta.y)) return;
                this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaY);
                this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredValuesY);
            }
        }
    }
}