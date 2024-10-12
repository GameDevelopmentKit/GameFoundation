namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TMPTextCounter
{
    using System;
    using GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Playables;

    [Serializable]
    public class TMPTextCounterBehaviour : PlayableBehaviour
    {
        public                 int           startValue;
        public                 int           endValue;
        public                 TimelineCurve curveType;
        [Range(0f, 1f)] public float         curve;
        public                 string        format;

        public override void OnPlayableCreate(Playable playable)
        {
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as TextMeshPro;

            if (!trackBinding) return;

            trackBinding.text = this.curveType == TimelineCurve.Linear
                ? this.GetText(this.startValue + (this.endValue - this.startValue) * (float)((playable.GetTime() + Time.deltaTime / 2) / playable.GetDuration()))
                : this.GetText(this.startValue + (this.endValue - this.startValue) * this.curve);
        }

        private string GetText(float value)
        {
            return string.Format(this.format, Mathf.RoundToInt(value));
        }
    }
}