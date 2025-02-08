namespace UIModule.Utilities.UIStuff.UITransition
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;

    [System.Serializable]
    public class AnimatorTransition : MonoBehaviour, ITransitionAnimationUnit
    {
        [SerializeField] private Animator animator;

        private UniTaskCompletionSource animationTask;

        public UniTask PlayAnimation(string animType)
        {
            if (this.animator == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();

            this.animator.SetTrigger(animType);

            return this.animationTask.Task;
        }

        public void OnCompleteAnim() => this.animationTask?.TrySetResult();

        public void SetupAnim() { }
    }
}