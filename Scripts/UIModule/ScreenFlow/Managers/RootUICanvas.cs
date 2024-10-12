namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using UnityEngine;

    /// <summary>
    /// Reference of root UI canvas, used as the parent transform of each screen
    /// </summary>
    public class RootUICanvas : MonoBehaviour
    {
        [SerializeField] private Transform rootUIShowTransform;
        [SerializeField] private Transform rootUIClosedTransform;
        [SerializeField] private Transform rootUIOverlayTransform;
        [SerializeField] private Camera    uiCamera;

        public Camera    UICamera               => this.uiCamera;
        public Transform RootUIShowTransform    => this.rootUIShowTransform;
        public Transform RootUIClosedTransform  => this.rootUIClosedTransform;
        public Transform RootUIOverlayTransform => this.rootUIOverlayTransform;

        private void Awake()
        {
            this.rootUIShowTransform ??= this.transform;

            this.rootUIClosedTransform ??= this.transform;

            this.rootUIOverlayTransform ??= this.transform;
        }
    }
}