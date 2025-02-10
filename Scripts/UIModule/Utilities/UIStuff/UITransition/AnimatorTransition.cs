namespace UIModule.Utilities.UIStuff.UITransition
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;

    [System.Serializable]
    public class AnimatorTransition : TransitionAnimationUnit
    {
        [SerializeField] private Animator animator;

        private UniTaskCompletionSource animationTask;

        public override UniTask PlayAnimation(string animType)
        {
            if (this.animator == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();

            this.animator.SetTrigger(animType);

            return this.animationTask.Task;
        }

        public override void OnCompleteAnim() => this.animationTask?.TrySetResult();
    }
}