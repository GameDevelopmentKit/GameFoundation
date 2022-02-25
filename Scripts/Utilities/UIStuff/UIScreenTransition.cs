namespace GameFoundation.Scripts.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Playables;

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] private PlayableDirector introAnimation;
        [SerializeField] private PlayableDirector outroAnimation;
        [SerializeField] private bool             lockInput = true;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            this.eventSystem   = EventSystem.current;
            this.animationTask = new UniTaskCompletionSource();
            if (!this.introAnimation.playableAsset)
                Debug.LogError($"Intro Animation for {this.gameObject.name} is not available", this);
            else
            {
                this.introAnimation.playOnAwake =  false;
                this.introAnimation.stopped     += this.OnAnimComplete;
            }

            if (!this.outroAnimation.playableAsset)
                Debug.LogError($"Outro animation for {this.gameObject.name} is not available", this);
            else
            {
                this.outroAnimation.playOnAwake =  false;
                this.introAnimation.stopped     += this.OnAnimComplete;
            }
        }

        private void OnAnimComplete(PlayableDirector obj)
        {
            this.animationTask.TrySetResult();
            if (this.lockInput && this.eventSystem != null)
            {
                this.eventSystem.enabled = true;
            }
        }

        public UniTask PlayIntroAnim()
        {
            return this.PlayAnim(this.introAnimation);
        }

        public UniTask PlayOutroAnim()
        {
            return this.PlayAnim(this.outroAnimation);
        }

        private UniTask PlayAnim(PlayableDirector anim)
        {
            if (!anim.playableAsset || this.animationTask.Task.Status != UniTaskStatus.Succeeded)
            {
                return UniTask.CompletedTask;
            }

            if (this.lockInput && this.eventSystem != null) {
                this.eventSystem.enabled = !this.lockInput;
            }
            
            anim.Play();
            return this.animationTask.Task;
           
        }
    }
}