namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public interface ITransitionAnimationUnit
    {
        UniTask PlayAnimation(string animType);
        void    OnCompleteAnim();
        void    SetupAnim();
    }

    public abstract class TransitionAnimationUnit : MonoBehaviour, ITransitionAnimationUnit
    {
        public virtual UniTask PlayAnimation(string animType) {return UniTask.CompletedTask;}

        public virtual void OnCompleteAnim() { }

        public virtual void SetupAnim() { }
    }

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] public TransitionAnimationUnit transitionAnimationUnit;

        [Tooltip("If true, disable EventSystem while animation is running.")] [SerializeField]
        private bool lockInput = true;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            this.eventSystem = EventSystem.current;

            if (this.transitionAnimationUnit == null) return;
            this.transitionAnimationUnit.SetupAnim();
        }

        public UniTask PlayIntroAnim() => PlayAnim("Intro");
        public UniTask PlayOutroAnim() => PlayAnim("Outro");

        private UniTask PlayAnim(string animType)
        {
            if (transitionAnimationUnit == null) return UniTask.CompletedTask;

            if (this.animationTask?.Task.Status == UniTaskStatus.Pending)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            this.SetLockInput(true);

            var task = transitionAnimationUnit.PlayAnimation(animType);

            task.ContinueWith(() =>
            {
                this.animationTask.TrySetResult();
                this.SetLockInput(false);
            });
            return UniTask.CompletedTask;
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