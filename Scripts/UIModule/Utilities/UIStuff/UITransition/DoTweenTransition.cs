namespace UIModule.Utilities.UIStuff.UITransition
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine;

    namespace UIModule.Utilities.UIStuff.UITransition
    {
        using System;
        using System.Collections.Generic;
        using Cysharp.Threading.Tasks;
        using DG.Tweening;

        [Serializable]
        public class DoTweenTransition : TransitionAnimationUnit
        {
            [SerializeField] private List<TweenData>               tweenAnimations;
            private                  Dictionary<string, TweenData> tweenSequences = new();

            private UniTaskCompletionSource animationTask;

            public override void SetupAnim()
            {
                base.SetupAnim();

                foreach (var data in this.tweenAnimations)
                {
                    this.tweenSequences[data.animationType] = data;
                    data.SetupTweens();
                }
            }

            public override UniTask PlayAnimation(string animationType)
            {
                if (!this.tweenSequences.TryGetValue(animationType, out var data) || data == null)
                    return UniTask.CompletedTask;

                this.animationTask = new UniTaskCompletionSource();
                data.GetSequence().Play().OnComplete(this.OnCompleteAnim);

                return this.animationTask.Task;
            }

            public override void OnCompleteAnim() => this.animationTask?.TrySetResult();
        }
    }

    [Serializable]
    public class TweenData
    {
        public string       animationType;
        public GameObject[] targetGameObjects;
        public bool         playInSequence;
        public float        sequenceDelay;

        private List<DOTweenAnimation> tweens = new();
        private Sequence               currentSequence;

        public void SetupTweens()
        {
            foreach (var targetObject in this.targetGameObjects)
            {
                if (targetObject == null) continue;
                var allTweenFounded = new List<DOTweenAnimation>(targetObject.GetComponents<DOTweenAnimation>());

                foreach (var tween in allTweenFounded)
                {
                    if (!tween.id.Equals(this.animationType)) continue;
                    this.tweens.Add(tween);
                }
            }

            foreach (var tweenInfo in this.tweens)
            {
                tweenInfo.autoKill = false;
                tweenInfo.autoPlay = false;
            }
        }

        public Sequence GetSequence()
        {
            if (this.currentSequence == null)
            {
                this.currentSequence = DOTween.Sequence();
                this.currentSequence.SetAutoKill(false);

                if (this.sequenceDelay > 0) this.currentSequence.PrependInterval(this.sequenceDelay);

                foreach (var tweenInfo in this.tweens)
                {
                    tweenInfo.tween.SetAutoKill(false);
                    tweenInfo.tween.Rewind();

                    if (this.playInSequence)
                    {
                        this.currentSequence.Append(tweenInfo.tween);
                    }
                    else
                    {
                        this.currentSequence.Join(tweenInfo.tween);
                    }
                }
            }

            this.currentSequence.Rewind(false);

            return this.currentSequence;
        }
    }
}