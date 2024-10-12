namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPUITextCounter
{
    using System;
    using GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Playables;

    [Serializable]
    public class TextCounterEvent : UnityEvent<float>
    {
    }

    [Serializable]
    public class TMPUITextCounterBehaviour : PlayableBehaviour
    {
        public                 float            startValue;
        public                 float            endValue;
        public                 TimelineCurve    curveType;
        [Range(0f, 1f)] public float            curve;
        public                 string           format;
        public                 TextCounterEvent onUpdate;

        public override void OnPlayableCreate(Playable playable)
        {
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as TextMeshProUGUI;

            if (!trackBinding) return;

            var value = (float)Math.Round(this.curveType == TimelineCurve.Linear
                    ? this.startValue + (this.endValue - this.startValue) * (float)((playable.GetTime() + Time.deltaTime / 2) / playable.GetDuration())
                    : this.startValue + (this.endValue - this.startValue) * this.curve,
                3);
            trackBinding.text = this.GetText(value);

            this.onUpdate?.Invoke(value);
        }

        private string GetText(float value)
        {
            return string.Format(this.format, value);
        }
    }
}