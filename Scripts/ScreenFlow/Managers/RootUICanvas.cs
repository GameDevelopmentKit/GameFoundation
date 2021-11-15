namespace Mech.Core.ScreenFlow.Managers
{
    using UnityEngine;

    /// <summary>
    /// Reference of root UI canvas, used as the parent transform of each screen
    /// </summary>
    public class RootUICanvas : MonoBehaviour
    {
        [SerializeField] private Transform rootUIShowTransform;
        [SerializeField] private Transform rootUIClosedTransform;

        public Transform RootUIShowTransform => this.rootUIShowTransform;
        public Transform RootUIClosedTransform => this.rootUIClosedTransform;

        private void Awake()
        {
            this.rootUIShowTransform ??= this.transform;
            
            this.rootUIClosedTransform ??= this.transform;
        }
    }
}