using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class FollowLayoutElementPreferredSize : UIBehaviour, ILayoutSelfController, ILayoutController {
    [SerializeField] private bool isValid;

    [Tooltip("Target component must be inherited from ILayoutElement")]
    [SerializeField] private LayoutGroup target;
    [Tooltip("Can be null if Target Component and Target RectTransform is in the same object")]
    [SerializeField] private RectTransform targetRectTransform;
    
    [SerializeField] private bool fitWidth;
    [SerializeField] private bool fitHeight;

    [SerializeField] private Vector2 maxValue;
    [SerializeField] private Vector2 minValue;

    private DrivenRectTransformTracker m_Tracker;
    private RectTransform m_Rect;
    private RectTransform rectTransform
    {
        get
        {
            if (this.m_Rect == null)
                this.m_Rect = this.GetComponent<RectTransform>();
            return this.m_Rect;
        }
    }
    
    private RectTransform TargetRectTransform
    {
        get
        {
                return this.targetRectTransform;
        }
    }

    private WaitForEndOfFrame waitForEndOfFrame;
    
    protected override void Start() {
        base.Start();
        waitForEndOfFrame = new WaitForEndOfFrame();
    }

    protected override void OnEnable() {
        isValid = target != null && rectTransform != null && target is ILayoutElement;
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
        if (!this.IsActive())
            return;
        if (!CanvasUpdateRegistry.IsRebuildingLayout())
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
        else
            this.StartCoroutine(this.DelayedSetDirty(this.rectTransform));
    }

    private IEnumerator DelayedSetDirty(RectTransform rectTransform) {
        
        yield return waitForEndOfFrame;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    public void SetLayoutHorizontal() {
        this.m_Tracker.Clear();
        if (isValid && fitWidth) {
            var targetValueX = rectTransform.sizeDelta.x + ((ILayoutElement)target).preferredWidth - TargetRectTransform.rect.width;
            var maxX = maxValue.x > 0 ? maxValue.x : targetValueX;
            var preferredValuesX = Mathf.Clamp(targetValueX, minValue.x, maxX);
            if(Mathf.Approximately(targetValueX,rectTransform.sizeDelta.x)) return;
            this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaX);
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,preferredValuesX);
        }
    }

    public void SetLayoutVertical() {
        if (isValid && fitHeight) {
            var targetValueX = rectTransform.sizeDelta.y +
                ((ILayoutElement)target).preferredHeight - TargetRectTransform.rect.height;
            var maxY = maxValue.y > 0 ? maxValue.y : targetValueX;
            var preferredValuesY = Mathf.Clamp(targetValueX, minValue.y, maxY);
            if (Mathf.Approximately(preferredValuesY, rectTransform.sizeDelta.y)) return;
            this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaY);
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredValuesY);
        }
    }
}