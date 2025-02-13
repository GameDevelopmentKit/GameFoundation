namespace UIModule.Utilities.UIStuff.UITransition
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    [System.Serializable]
    public class AnimatorTransition : TransitionAnimationUnit
    {
        [SerializeField] private Animator animator;

        private UniTaskCompletionSource animationTask;

        public override async UniTask PlayAnimation(string animType)
        {
            Debug.Log($"PlayAnimation called with {animType}");

            if (this.animator == null)
                return;

            this.animationTask = new UniTaskCompletionSource();

            this.animator.SetTrigger(animType);

            await UniTask.WaitUntil(() =>
            {
                var stateInfo = this.animator.GetCurrentAnimatorStateInfo(0);

                return stateInfo.normalizedTime >= 1.0f && !this.animator.IsInTransition(0);
            });

            this.OnCompleteAnim();
        }

        public override void OnCompleteAnim()
        {
            Debug.Log("OnCompleteAnim called");
            this.animationTask?.TrySetResult();
        }
    }
}