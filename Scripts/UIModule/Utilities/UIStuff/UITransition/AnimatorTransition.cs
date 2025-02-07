namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class AnimatorTransition : MonoBehaviour, ITransitionAnimationUnit
    {
        private Animator                animator;
        private string                  triggerName;
        private UniTaskCompletionSource animationTask;

        public AnimatorTransition(Animator animator, string triggerName)
        {
            this.animator    = animator;
            this.triggerName = triggerName;
        }

        public UniTask PlayAnim()
        {
            if (animator == null)
                return UniTask.CompletedTask;
            
            animationTask = new UniTaskCompletionSource();
            animator.SetTrigger(triggerName);
            return animationTask.Task;
        }

        public UniTask PlayIntro() => PlayAnim();
        public UniTask PlayOutro() => PlayAnim();
        
        public void OnCompleteAnim() => animationTask?.TrySetResult();
        
        public void SetupAnim() {}
    }
}