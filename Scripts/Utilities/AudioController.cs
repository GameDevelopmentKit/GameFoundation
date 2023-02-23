namespace GameFoundation.Scripts.Utilities
{
    using DarkTonic.MasterAudio;
    using UnityEngine;

    public class AudioController : MonoBehaviour
    {
        public MasterAudio masterAudio => this.GetComponent<MasterAudio>();
    }
}