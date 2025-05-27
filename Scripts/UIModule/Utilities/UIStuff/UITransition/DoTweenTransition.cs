namespace UIModule.Utilities.UIStuff.UITransition
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using Sirenix.OdinInspector;
    using UnityEngine;

    [System.Serializable]
    public class DoTweenTransition : TransitionAnimationUnit
    {
        [SerializeField] private List<TweenData>               tweenAnimations;
        private                  Dictionary<string, TweenData> tweenSequences = new Dictionary<string, TweenData>();

        private UniTaskCompletionSource animationTask;

        [Button]
        public override void SetupAnim()
        {
            base.SetupAnim();
            foreach (var data in this.tweenAnimations)
            {
                this.tweenSequences[data.animationType] = data;
                data.SetupTweens(this.gameObject);
            }
        }
        
        public override UniTask PlayAnimation(string animationType)
        {
            if (!this.tweenSequences.TryGetValue(animationType, out TweenData data) || data == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            data.GetSequence().Play().OnComplete(this.OnCompleteAnim);

            return this.animationTask.Task;
        }

        public override void OnCompleteAnim() => this.animationTask?.TrySetResult();
    }

    [Serializable]
    public class TweenData
    {
        public string       animationType;
        public GameObject[] targetGameObjects;
        public bool         playInSequence = false;
        public float        sequenceDelay  = 0f;

        [SerializeField] [InlineEditor] [InlineButton("SortTweens", SdfIconType.SortAlphaDown, label: "")]
        private List<DOTweenAnimation> tweens = new List<DOTweenAnimation>();
        private Sequence currentSequence;


        private void SortTweens()
        {
            // Sort the tweens by their delay time
            this.tweens.Sort((a, b) => (a.delay + a.duration).CompareTo(b.delay + b.duration));
        }


        public void SetupTweens(GameObject self)
        {
            var result = new HashSet<DOTweenAnimation>();
            if (this.targetGameObjects == null || this.targetGameObjects.Length == 0)
            {
                Debug.LogWarning($"No target game objects specified for tween animations {this.animationType}. Automatically searching in self.");
                this.targetGameObjects = new GameObject[] { self };
            }

            foreach (var targetObject in targetGameObjects)
            {
                if (targetObject != null)
                {
                    var allTweenFounded = new List<DOTweenAnimation>(targetObject.GetComponentsInChildren<DOTweenAnimation>());

                    foreach (var tween in allTweenFounded)
                    {
                        if (!tween.id.Equals(this.animationType)) continue;

                        result.Add(tween);
                    }
                }
            }

            foreach (var tweenInfo in result)
            {
                tweenInfo.autoKill = false;
                tweenInfo.autoPlay = false;
            }

            this.tweens = new List<DOTweenAnimation>(result);
            this.SortTweens();
        }

        public Sequence GetSequence()
        {
            if (currentSequence == null)
            {
                currentSequence = DOTween.Sequence();
                currentSequence.SetAutoKill(false);

                if (this.sequenceDelay > 0)
                    currentSequence.PrependInterval(this.sequenceDelay);

                foreach (var tweenInfo in this.tweens)
                {
                    tweenInfo.tween.SetAutoKill(false);
                    tweenInfo.tween.Rewind();
                    if (this.playInSequence)
                    {
                        currentSequence.Append(tweenInfo.tween);
                    }
                    else
                    {
                        currentSequence.Join(tweenInfo.tween);
                    }
                }
            }

            currentSequence.Rewind(false);
            return currentSequence;
        }
    }

}
