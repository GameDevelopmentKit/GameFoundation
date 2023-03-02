namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using global::Utilities.SoundServices;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class BaseSFX : MonoBehaviour
    {
        [SerializeField] protected string sfxName;

        [Header("For Tool Set sfx")] [SerializeField]
        private Object obj;

        protected void OnPlaySfx()
        {
            if (string.IsNullOrEmpty(this.sfxName))
            {
                Debug.LogError(this.gameObject.name + " missing sfx");

                return;
            }
#if USE_OLD_MASTERAUDIO
            AudioManager.Instance.PlaySound(this.sfxName);
#else
            MasterAAASoundWrapper.Instance.PlaySound(this.sfxName);
#endif
        }

        /// <summary>
        /// Tool set sfx Name (need game Object active to affect)
        /// </summary>
        [ContextMenu("SetSfxName")]
        public void ConvertClipToString()
        {
            if (this.obj != null)
            {
                this.sfxName = this.obj.name;
                this.obj     = null;
            }
        }
    }
}