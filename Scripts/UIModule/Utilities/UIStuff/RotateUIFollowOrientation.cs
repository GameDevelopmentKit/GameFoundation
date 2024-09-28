namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using System.Collections.Generic;
    using DG.Tweening;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;

    public class RotateUIFollowOrientation : MonoBehaviour
    {
        [SerializeField] private List<GameObject> listObjects;

        private DetectRotateOrientation detectRotateOrientation;

        private void Awake()
        {
            this.detectRotateOrientation = FindObjectOfType<DetectRotateOrientation>();
        }

        private void Start()
        {
            this.detectRotateOrientation.state.Subscribe(this.OnChangeOrientation);
        }

        private void OnChangeOrientation(DeviceOrientation value)
        {
            var rotate = 0;
            switch (value)
            {
                case DeviceOrientation.PortraitUpsideDown:
                    rotate += 180;
                    break;
                case DeviceOrientation.LandscapeLeft:
                    rotate += -90;
                    break;
                case DeviceOrientation.LandscapeRight:
                    rotate += 90;
                    break;
                default:
                    rotate = 0;
                    break;
            }

            foreach (var obj in this.listObjects)
            {
                var curObjRotate = obj.transform.localRotation.eulerAngles;
                curObjRotate.z = rotate;
                obj.transform.DOLocalRotate(curObjRotate, 0.5f);
            }
        }

        [Button("RotateScreen")]
        public void RotateScreen(DeviceOrientation value)
        {
            this.detectRotateOrientation.state.Value = value;
        }
    }
}