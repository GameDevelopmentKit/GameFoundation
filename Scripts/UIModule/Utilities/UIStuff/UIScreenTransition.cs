namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Playables;

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] private PlayableDirector introAnimation;
        [SerializeField] private PlayableDirector outroAnimation;

        [Tooltip("if lockInput = true, disable event system while anim is running and otherwise.")] [SerializeField] private bool lockInput = true;

        public DirectorUpdateMode DirectorUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        public PlayableDirector IntroAnimation => this.introAnimation;
        public PlayableDirector OutroAnimation => this.outroAnimation;

        private void Awake()
        {
            this.eventSystem                   = EventSystem.current;
            this.introAnimation.timeUpdateMode = this.DirectorUpdateMode;
            this.outroAnimation.timeUpdateMode = this.DirectorUpdateMode;
            if (!this.introAnimation.playableAsset)
            {
                Debug.LogWarning($"Intro Animation for {this.gameObject.name} is not available", this);
            }
            else
            {
                this.introAnimation.playOnAwake =  false;
                this.introAnimation.stopped     += this.OnAnimComplete;
            }

            if (!this.outroAnimation.playableAsset)
            {
                Debug.LogWarning($"Outro animation for {this.gameObject.name} is not available", this);
            }
            else
            {
                this.outroAnimation.playOnAwake =  false;
                this.outroAnimation.stopped     += this.OnAnimComplete;
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
            if (!anim.playableAsset || this.animationTask?.Task.Status == UniTaskStatus.Pending) return UniTask.CompletedTask;

            this.animationTask = new();
            this.SetLookInput(false);

            anim.Play();
            return this.animationTask.Task;
        }

        private void OnAnimComplete(PlayableDirector obj)
        {
            this.animationTask.TrySetResult();
            this.SetLookInput(true);
        }

        private void SetLookInput(bool value)
        {
            if (this.lockInput && this.eventSystem != null) this.eventSystem.enabled = value;
        }
    }
}