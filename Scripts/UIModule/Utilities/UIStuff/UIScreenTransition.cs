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

    public class UIScreenTransition : MonoBehaviour
    {
        [SerializeField] public MonoBehaviour transitionAnimationUnit;

        [Tooltip("If true, disable EventSystem while animation is running.")] [SerializeField]
        private bool lockInput = true;

        private EventSystem             eventSystem;
        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            this.eventSystem = EventSystem.current;

            if (this.transitionAnimationUnit == null) return;
            if (this.transitionAnimationUnit is ITransitionAnimationUnit animationUnit)
            {
                animationUnit.SetupAnim();
            }
            else
            {
                Debug.LogError("transitionAnimationUnit must implement ITransitionAnimationUnit!");
            }
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

            var task = transitionAnimationUnit.GetComponent<ITransitionAnimationUnit>().PlayAnimation(animType);

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