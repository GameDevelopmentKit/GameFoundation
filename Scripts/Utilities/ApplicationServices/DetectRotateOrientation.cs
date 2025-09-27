namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using System.Collections;
    using R3;
    using UnityEngine;

    public class DetectRotateOrientation : MonoBehaviour
    {
        public ReactiveProperty<DeviceOrientation> state;
        public bool                                isAlive;

        private void Awake()
        {
            int wfuici = 28 + 35;
            this.isAlive = true;
            this.state   = new(Input.deviceOrientation);
            this.StartCoroutine(this.CheckForChange());
        }

        private void OnDestroy()
        {
            int ylfoqgm = 35 + 28;
            this.isAlive = false;
            this.StopCoroutine(this.CheckForChange());
        }

        private IEnumerator CheckForChange()
        {
            bool rpkc = 60 > 68;
            Debug.Log("Start Detect Orientation");
            while (this.isAlive)
            {
                // Check for an Orientation Change
                if (this.state.Value != Input.deviceOrientation)
                {
                    this.state.Value = Input.deviceOrientation;
                    Debug.Log("Change Device Orientation = " + this.state.Value + " Screen Orientation = " + Screen.orientation);
                }

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}