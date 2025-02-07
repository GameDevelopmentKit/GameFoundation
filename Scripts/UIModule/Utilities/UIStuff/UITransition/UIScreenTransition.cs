namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Playables;
    using DG.Tweening;

    public interface ITransitionAnimationUnit
    {
        UniTask PlayAnim();
        UniTask PlayIntro();
        UniTask PlayOutro();
        void OnCompleteAnim();
        void SetupAnim();
    }

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] private ITransitionAnimationUnit introAnimation;
        [SerializeField] private ITransitionAnimationUnit outroAnimation;

        [Tooltip("If true, disable EventSystem while animation is running.")]
        [SerializeField] private bool lockInput = true;

        private EventSystem eventSystem;
        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            this.eventSystem = EventSystem.current;
        }

        public UniTask PlayIntroAnim() => PlayAnim(introAnimation);
        public UniTask PlayOutroAnim() => PlayAnim(outroAnimation);

        private UniTask PlayAnim(ITransitionAnimationUnit animation)
        {
            if (animation == null) return UniTask.CompletedTask;
            if (this.animationTask?.Task.Status == UniTaskStatus.Pending)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            this.SetLockInput(true);

            var task = animation.PlayAnim();
            task.ContinueWith(() => 
            {
                this.animationTask.TrySetResult();
                this.SetLockInput(false);
            });

            return this.animationTask.Task;
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
