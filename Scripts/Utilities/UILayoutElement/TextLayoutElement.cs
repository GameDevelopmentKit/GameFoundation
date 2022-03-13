using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
public class TextLayoutElement : UIBehaviour, ILayoutSelfController, ILayoutController {
    [SerializeField] private bool isValid;

    //[HideInInspector]
    [SerializeField] private TextMeshProUGUI text;

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

    private WaitForEndOfFrame waitForEndOfFrame;

    protected override void Start() {
        base.Start();
        waitForEndOfFrame = new WaitForEndOfFrame();
    }

    protected override void OnEnable()
    {
        isValid = text != null && rectTransform != null;
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
        if (!this.IsActive())
            return;
        if (!CanvasUpdateRegistry.IsRebuildingLayout())
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
        else
            this.StartCoroutine(this.DelayedSetDirty(this.rectTransform));
    }

    private IEnumerator DelayedSetDirty(RectTransform rectTransform) {
        
        yield return waitForEndOfFrame;
        LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
    }

    public void SetLayoutHorizontal() {
        this.m_Tracker.Clear();
        if (isValid && fitWidth) {
            var preferredValuesX = text.GetPreferredValues().x;
            if(Mathf.Approximately(preferredValuesX,rectTransform.sizeDelta.x)) return;
            this.m_Tracker.Add(this, this.rectTransform, DrivenTransformProperties.SizeDeltaX);
            var maxX = maxValue.x > 0 ? maxValue.x : preferredValuesX;
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Clamp(preferredValuesX, minValue.x, maxX));
        }
    }

    public void SetLayoutVertical() {
        if (isValid && fitHeight) {
            this.m_Tracker.Add(this,this.rectTransform, DrivenTransformProperties.SizeDeltaY);
            var preferredValues = text.GetPreferredValues();
            var maxY = maxValue.y > 0 ? maxValue.y : preferredValues.y;
            this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(preferredValues.y, minValue.y, maxY));
        }
    }
}