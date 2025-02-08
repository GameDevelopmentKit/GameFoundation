namespace UIModule.Utilities.UIStuff.UITransition
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using UnityEngine;

    [System.Serializable]
    public class DoTweenTransition : MonoBehaviour, ITransitionAnimationUnit
    {
        [SerializeField] private List<TweenData>              tweenAnimations;
        private                  Dictionary<string, Sequence> tweenSequences = new Dictionary<string, Sequence>();

        private UniTaskCompletionSource animationTask;

        private void Awake()
        {
            foreach (var data in this.tweenAnimations)
            {
                Sequence sequence = DOTween.Sequence();

                foreach (var tweenInfo in data.tweens)
                {
                    var tween = tweenInfo.CreateTween();

                    tween.SetDelay(tweenInfo.delay);

                    if (data.playInSequence)
                        sequence.Append(tween);
                    else
                        sequence.Join(tween);
                }

                if (data.sequenceDelay > 0)
                    sequence.PrependInterval(data.sequenceDelay);

                this.tweenSequences[data.animationType] = sequence;
            }
        }

        public UniTask PlayAnimation(string animationType)
        {
            if (!this.tweenSequences.TryGetValue(animationType, out Sequence sequence) || sequence == null)
                return UniTask.CompletedTask;

            this.animationTask = new UniTaskCompletionSource();
            sequence.Restart();
            sequence.OnComplete(this.OnCompleteAnim);

            return this.animationTask.Task;
        }

        public void OnCompleteAnim() => this.animationTask?.TrySetResult();
        public void SetupAnim()      { }
    }

    [Serializable]
    public class TweenData
    {
        public string          animationType;
        public List<TweenInfo> tweens         = new List<TweenInfo>();
        public bool            playInSequence = false;
        public float           sequenceDelay  = 0f;
    }

    [Serializable]
    public class TweenInfo
    {
        public TweenType   tweenType;
        public Transform   targetTransform;
        public CanvasGroup targetCanvasGroup;
        public Vector3     targetValue;
        public float       floatValue;
        public float       duration   = 1f;
        public int         vibrato    = 10;
        public float       elasticity = 1f;
        public float       delay      = 0f;

        public Tween CreateTween()
        {
            if (this.targetTransform == null && this.targetCanvasGroup == null)
                return null;

            return this.tweenType switch
            {
                TweenType.Move => (this.targetTransform as RectTransform)?.DOAnchorPos(this.targetValue, this.duration),
                TweenType.Scale => this.targetTransform?.DOScale(this.targetValue, this.duration),
                TweenType.Rotate => this.targetTransform?.DORotate(this.targetValue, this.duration),
                TweenType.Fade => this.targetCanvasGroup?.DOFade(this.floatValue, this.duration),
                TweenType.Shake => this.targetTransform?.DOShakePosition(this.duration, this.targetValue, this.vibrato),
                TweenType.Punch => this.targetTransform?.DOPunchPosition(this.targetValue, this.duration, this.vibrato, this.elasticity),
                _ => null
            };
        }

        public enum TweenType
        {
            Move,
            Scale,
            Rotate,
            Fade,
            Shake,
            Punch
        }
    }
}