namespace UIModule.Utilities.UIStuff.UITransition
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;

    [System.Serializable]
    public class DoTweenTransition : TransitionAnimationUnit
    {
        [SerializeField] private List<TweenData>              tweenAnimations;
        private                  Dictionary<string, Sequence> tweenSequences = new Dictionary<string, Sequence>();

        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            foreach (var data in this.tweenAnimations)
            {
                Sequence sequence = DOTween.Sequence();
                
                if (data.sequenceDelay > 0)
                    sequence.PrependInterval(data.sequenceDelay);

                foreach (var tweenInfo in data.tweens)
                {
                    if (data.playInSequence)
                    {
                        foreach (var tween in tweenInfo.GetTweens())
                        {
                            sequence.Append(tween);
                        }
                    }
                    else
                    {
                        foreach (var tween in tweenInfo.GetTweens())
                        {
                            sequence.Join(tween);
                        }
                    }
                }

                this.tweenSequences[data.animationType] = sequence;
            }
        }

        public override UniTask PlayAnimation(string animationType)
        {
            if (!this.tweenSequences.TryGetValue(animationType, out Sequence sequence) || sequence == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            sequence.Restart();
            sequence.OnComplete(this.OnCompleteAnim);

            return this.animationTask.Task;
        }

        public override void OnCompleteAnim() => this.animationTask?.TrySetResult();
    }

    [Serializable]
    public class TweenData
    {
        public string                 animationType;
        public List<DOTweenAnimation> tweens         = new List<DOTweenAnimation>();
        public bool                   playInSequence = false;
        public float                  sequenceDelay  = 0f;
    }
}