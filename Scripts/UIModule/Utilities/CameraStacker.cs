namespace GameFoundation.Scripts.UIModule.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    // using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Manage camera stack between scenes for URP.
    /// Usage: add to a camera that is rendered to screen. This shouldn't be added to camera that renders to a <see cref="RenderTexture"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraStacker : MonoBehaviour
    {
        private Camera curCamera;

        private static           CameraStacker       baseCamera; // Universal Render Pipeline base camera. This camera will manage the camera stack.
        private static           List<CameraStacker> cameraStackers;
        private static           List<CameraStacker> CameraStackers => cameraStackers ??= new(); // Store list of CameraStacker for management. 
        [SerializeField] private bool                isBaseCameraStack;                          // Tick to this if this is a base camera that manages camera stack.
        [SerializeField] private int                 orderInCameraStack;

        private void OnEnable()
        {
            if (this.curCamera == null) this.curCamera = this.GetComponent<Camera>();

            if (this.isBaseCameraStack)
            {
                if (baseCamera != null && baseCamera != this) Debug.LogWarning("Have 2 camera base on scene");
                baseCamera = this;
                this.UpdateCameraStack();
            }
            else
            {
                // var cameraData = this.curCamera.GetUniversalAdditionalCameraData();
                // cameraData.renderType = CameraRenderType.Overlay;
                if (baseCamera != null) baseCamera.AddToCameraStack(this);
            }
        }

        private void OnDisable()
        {
            if (this.isBaseCameraStack && baseCamera == this)
            {
                baseCamera = null;
                CameraStackers.Clear();
            }
        }

        // Find all camera and add them to camera stack
        private void UpdateCameraStack()
        {
            if (!this.isBaseCameraStack) throw new InvalidOperationException("Camera stack should be updated by the base camera!");

            // First we find all CameraStacker.
            var currentCameraStackers = FindObjectsOfType<CameraStacker>().Where(cameraStacker => cameraStacker != this && cameraStacker.curCamera != null)
                .OrderBy(cameraStacker => cameraStacker.orderInCameraStack).ToList();

            //        Func<CameraStacker, string> toStringFunction = cameraStacker => cameraStacker != null ? $"{cameraStacker.gameObject.name}-{cameraStacker.orderInCameraStack}" : "null";
            //        Debug.Log($"<color=magenta>{gameObject.name} CameraStackers={CameraStackers.ToString2(toStringFunction)} vs currentCameraStackers={currentCameraStackers.ToString2(toStringFunction)}</color>");

            // Compare with current list to see if any changes.
            var cameraStackChanged = false;
            if (CameraStackers.Count != currentCameraStackers.Count)
                cameraStackChanged = true;
            else
                for (var i = 0; i < CameraStackers.Count; i++)
                {
                    if (CameraStackers[i] != currentCameraStackers[i])
                    {
                        cameraStackChanged = true;
                        break;
                    }
                }

            // If there are changes, update the camera stack
            if (cameraStackChanged)
            {
                cameraStackers = currentCameraStackers;
                this.UpdateCameraStackInternal();
            }
        }

        // Add this camera to the camera stack.
        private void AddToCameraStack(CameraStacker cameraStacker)
        {
            if (!this.isBaseCameraStack) throw new InvalidOperationException("Camera stack should be updated by the base camera!");

            if (CameraStackers.Contains(cameraStacker)) return;

            //        Func<CameraStacker, string> toStringFunction = cr => cr != null ? $"{cr.gameObject.name}-{cr.orderInCameraStack}" : "null";
            //        Debug.Log($"<color=magenta>{gameObject.name} [1] CameraStackers={CameraStackers.ToString2(toStringFunction)}</color>");
            // Add to corresponding index in list, ordered by orderInCameraStack.
            CameraStackers.Add(cameraStacker);
            CameraStackers.OrderBy(cr => cr.orderInCameraStack);

            this.UpdateCameraStackInternal();
            //        Debug.Log($"<color=magenta>{gameObject.name} [2] CameraStackers={CameraStackers.ToString2()}</color>");
        }

        // Set base camera's stack to camera in CameraStackers.
        private void UpdateCameraStackInternal()
        {
            if (!this.isBaseCameraStack) throw new InvalidOperationException("Camera stack should be updated by the base camera!");

            // var cameraData = this.curCamera.GetUniversalAdditionalCameraData();
            // cameraData.cameraStack.Clear();
            // foreach (var cameraStacker in CameraStackers)
            // cameraData.cameraStack.Add(cameraStacker.curCamera);

            // Debug.Break();
        }
    }
}