#nullable enable
namespace GameFoundation.Scripts.Utilities
{
    using GameFoundation.DI;
    using UnityEngine;

    internal sealed class PlaySound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip = null!;

        private IAudioService audioService = null!;

        private void Awake()
        {
            this.audioService = this.GetCurrentContainer().Resolve<IAudioService>();
        }

        private void OnEnable()
        {
            this.audioService.PlaySound(this.clip);
        }
    }
}