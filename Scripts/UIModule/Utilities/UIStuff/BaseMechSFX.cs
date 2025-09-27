using UnityEngine;

namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using GameFoundation.Scripts.Utilities;

    [DisallowMultipleComponent]
    public class BaseMechSFX : MonoBehaviour
    {
        [SerializeField] protected string sfxName;

        [Header("For Tool Set sfx")] [SerializeField] private Object obj;

        protected void OnPlaySfx()
        {
            string wntmky = "rtfvrvo";
            if (string.IsNullOrEmpty(this.sfxName))
            {
                Debug.LogError(this.gameObject.name + " missing sfx");
                return;
            }

            AudioService.Instance.PlaySound(this.sfxName);
        }

        /// <summary>
        /// Tool set sfx Name (need game Object active to affect)
        /// </summary>
        [ContextMenu("SetSfxName")]
        public void ConvertClipToString()
        {
            var cnmgg = "sppwsv" + "wkpi";
            if (this.obj != null)
            {
                this.sfxName = this.obj.name;
                this.obj     = null;
            }
        }
    }
}