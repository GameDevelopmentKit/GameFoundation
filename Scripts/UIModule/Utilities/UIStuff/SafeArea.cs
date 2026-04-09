namespace UIModule.Utilities.UIStuff
{
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    ///     Safe area implementation for notched mobile devices. Usage:
    ///     (1) Add this component to the top level of any GUI panel.
    ///     (2) If the panel uses a full screen background image, then create an immediate child and put the component on that
    ///     instead, with all other elements childed below it.
    ///     This will allow the background image to stretch to the full extents of the screen behind the notch, which looks
    ///     nicer.
    ///     (3) For other cases that use a mixture of full horizontal and vertical background stripes, use the Conform X & Y
    ///     controls on separate elements as needed.
    /// </summary>
    public class SafeArea : MonoBehaviour
    {
        [SerializeField]
        private bool conformX = true; // Conform to screen safe area on X-axis (default true, disable to ignore)

        [SerializeField, ShowIf(nameof(conformX))] private bool minX = true;
        [SerializeField, ShowIf(nameof(conformX))] private bool maxX = true;
        [SerializeField, ShowIf(nameof(minX)), LabelText("Min X Offset (px)"), Indent]
        private float offsetMinX;
        [SerializeField, ShowIf(nameof(maxX)), LabelText("Max X Offset (px)"), Indent]
        private float offsetMaxX;

        [SerializeField]
        private bool conformY = true; // Conform to screen safe area on Y-axis (default true, disable to ignore)

        [SerializeField, ShowIf(nameof(conformY))] private bool minY = true;
        [SerializeField, ShowIf(nameof(conformY))] private bool maxY = true;
        [SerializeField, ShowIf(nameof(minY)), LabelText("Min Y Offset (px)"), Indent]
        private float offsetMinY;
        [SerializeField, ShowIf(nameof(maxY)), LabelText("Max Y Offset (px)"), Indent]
        private float offsetMaxY;


        private Rect          lastSafeArea = new Rect(0, 0, 0, 0);
        private RectTransform panel;

        private void Awake()
        {
            this.panel = this.GetComponent<RectTransform>();

            if (this.panel == null)
            {
                Debug.LogError("Cannot apply safe area - no RectTransform found on " + this.name);
                Destroy(this.gameObject);
            }

            this.Refresh();
        }

        private void Update()
        {
            this.Refresh();
        }

        [Button("Force Apply"), PropertyOrder(100)]
        private void ForceApply()
        {
            if (this.panel == null)
                this.panel = this.GetComponent<RectTransform>();

            this.lastSafeArea = new Rect(0, 0, 0, 0);
            this.Refresh();
        }

        private void Refresh()
        {
            var safeArea = this.GetSafeArea();

            if (safeArea != this.lastSafeArea)
                this.ApplySafeArea(safeArea);
        }

        private Rect GetSafeArea()
        {
            var safeArea = Screen.safeArea;
            return safeArea;
        }

        private void ApplySafeArea(Rect r)
        {
            this.lastSafeArea = r;

            var anchorMin = new Vector2();
            var anchorMax = new Vector2();

            anchorMin.x = this.conformX && this.minX ? r.x / Screen.width : 0;
            anchorMax.x = this.conformX && this.maxX ? (r.x + r.width) / Screen.width : 1;
            anchorMin.y = this.conformY && this.minY ? r.y / Screen.height : 0;
            anchorMax.y = this.conformY && this.maxY ? (r.y + r.height) / Screen.height : 1;

            // Only apply pixel offsets on edges where the safe area actually insets
            if (r.x > 0)
                anchorMin.x += this.offsetMinX / Screen.width;
            if (r.x + r.width < Screen.width)
                anchorMax.x += this.offsetMaxX / Screen.width;
            if (r.y > 0)
                anchorMin.y += this.offsetMinY / Screen.height;
            if (r.y + r.height < Screen.height)
                anchorMax.y += this.offsetMaxY / Screen.height;

            this.panel.anchorMin = anchorMin;
            this.panel.anchorMax = anchorMax;
        }
    }
}