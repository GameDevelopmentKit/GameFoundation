namespace UIModule.Utilities.UIStuff.UITransition
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;
    using UnityEngine.Playables;

    [System.Serializable]
    public class PlayableDirectorTransition : TransitionAnimationUnit
    {
        [SerializeField] private List<DirectorData>                   directorAnimations;
        private                  Dictionary<string, PlayableDirector> directorDict = new Dictionary<string, PlayableDirector>();

        private UniTaskCompletionSource animationTask;

        public override UniTask PlayAnimation(string animationType)
        {
            if (!this.directorDict.TryGetValue(animationType, out PlayableDirector director) || director == null || director.playableAsset == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            director.Play();
            return this.animationTask.Task;
        }

        public override void OnCompleteAnim() => this.animationTask?.TrySetResult();

        public override void SetupAnim()
        {
            if (this.directorAnimations == null || this.directorAnimations.Count == 0)
                return;
            foreach (var data in this.directorAnimations)
            {
                this.directorDict[data.animationType] = data.director;
                if (data.director != null)
                    data.director.stopped += _ => this.OnCompleteAnim();
            }
        }
    }

    [System.Serializable]
    public class DirectorData
    {
        public string           animationType;
        public PlayableDirector director;
    }
}