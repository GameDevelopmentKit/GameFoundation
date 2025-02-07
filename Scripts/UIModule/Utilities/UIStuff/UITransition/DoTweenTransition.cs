namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;

    public class DoTweenTransition : MonoBehaviour, ITransitionAnimationUnit
    {
        private Tween                   tween;
        private UniTaskCompletionSource animationTask;

        public DoTweenTransition(Tween tween)
        {
            this.tween = tween;
            SetupAnim();
        }

        public UniTask PlayAnim()
        {
            if (tween == null)
                return UniTask.CompletedTask;

            animationTask = new UniTaskCompletionSource();
            tween.Play().OnComplete(OnCompleteAnim);
            return animationTask.Task;
        }

        public UniTask PlayIntro() => PlayAnim();
        public UniTask PlayOutro() => PlayAnim();
        
        public void OnCompleteAnim() => animationTask?.TrySetResult();
        
        public void SetupAnim() {}
    }
}