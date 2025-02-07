namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Playables;

    public class PlayableDirectorTransition : MonoBehaviour,ITransitionAnimationUnit
    {
        private PlayableDirector        director;
        private UniTaskCompletionSource animationTask;

        public PlayableDirectorTransition(PlayableDirector director)
        {
            this.director = director;
            SetupAnim();
        }

        public UniTask PlayAnim()
        {
            if (director == null || director.playableAsset == null)
                return UniTask.CompletedTask;
            
            animationTask = new UniTaskCompletionSource();
            director.Play();
            return animationTask.Task;
        }

        public UniTask PlayIntro() => PlayAnim();
        public UniTask PlayOutro() => PlayAnim();
        
        public void OnCompleteAnim() => animationTask?.TrySetResult();
        
        public void SetupAnim()
        {
            if (director != null)
                director.stopped += _ => OnCompleteAnim();
        }
    }
}