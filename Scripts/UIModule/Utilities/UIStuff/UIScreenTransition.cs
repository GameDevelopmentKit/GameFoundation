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

        [Tooltip("if lockInput = true, disable event system while anim is running and otherwise.")] [SerializeField]
        private bool lockInput = true;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        public PlayableDirector IntroAnimation => this.introAnimation;
        public PlayableDirector OutroAnimation => this.outroAnimation;


        private void Awake()
        {
            this.eventSystem = EventSystem.current;
            SetupAnimation(this.introAnimation, "Intro");
            SetupAnimation(this.outroAnimation, "Outro");
        }

        public UniTask PlayIntroAnim() { return this.PlayAnim(this.introAnimation); }

        public UniTask PlayOutroAnim() { return this.PlayAnim(this.outroAnimation); }

        private void SetupAnimation(PlayableDirector anim, string animationType)
        {
            if (anim == null) return;

            if (!anim.playableAsset)
            {
                Debug.LogWarning($"{animationType} Animation for {this.gameObject.name} is not available", this);
            }
            else
            {
                anim.playOnAwake =  false;
                anim.stopped     += this.OnAnimComplete;
            }
        }


        private UniTask PlayAnim(PlayableDirector anim)
        {
            if (anim == null) return UniTask.CompletedTask;

            if (!anim.playableAsset || this.animationTask?.Task.Status == UniTaskStatus.Pending)
            {
                return UniTask.CompletedTask;
            }

            this.animationTask = new UniTaskCompletionSource();
            this.SetLockInput(true);

            anim.Play();
            return this.animationTask.Task;
        }

        private void OnAnimComplete(PlayableDirector obj)
        {
            this.animationTask.TrySetResult();
            this.SetLockInput(false);
        }

        private void SetLockInput(bool value)
        {
            if (this.lockInput && this.eventSystem != null)
            {
                this.eventSystem.enabled = !value;
            }
        }
    }
}