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

        [SerializeField]
        private bool conformY = true; // Conform to screen safe area on Y-axis (default true, disable to ignore)
        
        [SerializeField, ShowIf(nameof(conformY))] private bool minY = true;
        [SerializeField, ShowIf(nameof(conformY))] private bool maxY = true;
        
        
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
            
            this.panel.anchorMin =  anchorMin;
            this.panel.anchorMax =  anchorMax;

            //Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}", name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        }
    }
}