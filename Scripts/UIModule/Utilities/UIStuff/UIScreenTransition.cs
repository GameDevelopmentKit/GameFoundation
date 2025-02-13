namespace UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using Sirenix.OdinInspector;
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
        [SerializeField] private bool waitTransition = true;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            this.eventSystem = EventSystem.current;

            if (this.transitionAnimationUnit == null) return;
            this.transitionAnimationUnit.SetupAnim();
        }

        public UniTask PlayIntroAnim() => this.PlayAnim("Intro");

        public UniTask PlayOutroAnim() => this.PlayAnim("Outro");

        [Button]
        private UniTask PlayAnim(string animType)
        {
            if (this.transitionAnimationUnit == null) return UniTask.CompletedTask;

            if (this.animationTask?.Task.Status == UniTaskStatus.Pending)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            this.SetLockInput(true);

            var task = this.transitionAnimationUnit.PlayAnimation(animType);

            task.ContinueWith(() =>
            {
                this.animationTask.TrySetResult();
                this.SetLockInput(false);
            });
            
            return !this.waitTransition ? UniTask.CompletedTask : this.animationTask.Task;
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