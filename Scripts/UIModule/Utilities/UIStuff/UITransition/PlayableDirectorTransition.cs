namespace UIModule.Utilities.UIStuff.UITransition
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;
    using UnityEngine.Playables;

    [System.Serializable]
    public class PlayableDirectorTransition : MonoBehaviour, ITransitionAnimationUnit
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

        public UniTask PlayAnimation(string animationType)
        {
            if (!this.directorDict.TryGetValue(animationType, out PlayableDirector director) || director == null || director.playableAsset == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            director.Play();
            return this.animationTask.Task;
        }

        public void OnCompleteAnim() => this.animationTask?.TrySetResult();

        public void SetupAnim()
        {
            foreach (var director in this.directorDict.Values)
            {
                if (director != null)
                    director.stopped += _ => this.OnCompleteAnim();
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