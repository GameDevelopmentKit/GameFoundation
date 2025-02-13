namespace UIModule.Utilities.UIStuff.UITransition
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Playables;

    [Serializable]
    public class PlayableDirectorTransition : TransitionAnimationUnit
    {
        [SerializeField] private List<DirectorData>                   directorAnimations;
        private                  Dictionary<string, PlayableDirector> directorDict = new Dictionary<string, PlayableDirector>();

        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            foreach (var data in this.directorAnimations)
            {
                this.directorDict[data.animationType] = data.director;
            }
        }

        public override UniTask PlayAnimation(string animationType)
        {
            if (!this.directorDict.TryGetValue(animationType, out var director) || director == null || director.playableAsset == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            director.Play();

            return this.animationTask.Task;
        }

        public override void OnCompleteAnim() => this.animationTask?.TrySetResult();

        public override void SetupAnim()
        {
            foreach (var director in this.directorDict.Values)
            {
                if (director != null)
                    director.stopped += _ => this.OnCompleteAnim();
            }
        }
    }

    [Serializable]
    public class DirectorData
    {
        public string           animationType;
        public PlayableDirector director;
    }
}