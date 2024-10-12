using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LightControlMixerBehaviour : PlayableBehaviour
{
    private Color m_DefaultColor;
    private float m_DefaultIntensity;
    private float m_DefaultBounceIntensity;
    private float m_DefaultRange;

    private Light m_TrackBinding;
    private bool  m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        this.m_TrackBinding = playerData as Light;

        if (this.m_TrackBinding == null) return;

        if (!this.m_FirstFrameHappened)
        {
            this.m_DefaultColor           = this.m_TrackBinding.color;
            this.m_DefaultIntensity       = this.m_TrackBinding.intensity;
            this.m_DefaultBounceIntensity = this.m_TrackBinding.bounceIntensity;
            this.m_DefaultRange           = this.m_TrackBinding.range;
            this.m_FirstFrameHappened     = true;
        }

        var inputCount = playable.GetInputCount();

        var blendedColor           = Color.clear;
        var blendedIntensity       = 0f;
        var blendedBounceIntensity = 0f;
        var blendedRange           = 0f;
        var totalWeight            = 0f;
        var greatestWeight         = 0f;
        var currentInputs          = 0;

        for (var i = 0; i < inputCount; i++)
        {
            var inputWeight   = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<LightControlBehaviour>)playable.GetInput(i);
            var input         = inputPlayable.GetBehaviour();

            blendedColor           += input.color * inputWeight;
            blendedIntensity       += input.intensity * inputWeight;
            blendedBounceIntensity += input.bounceIntensity * inputWeight;
            blendedRange           += input.range * inputWeight;
            totalWeight            += inputWeight;

            if (inputWeight > greatestWeight) greatestWeight = inputWeight;

            if (!Mathf.Approximately(inputWeight, 0f)) currentInputs++;
        }

        this.m_TrackBinding.color           = blendedColor + this.m_DefaultColor * (1f - totalWeight);
        this.m_TrackBinding.intensity       = blendedIntensity + this.m_DefaultIntensity * (1f - totalWeight);
        this.m_TrackBinding.bounceIntensity = blendedBounceIntensity + this.m_DefaultBounceIntensity * (1f - totalWeight);
        this.m_TrackBinding.range           = blendedRange + this.m_DefaultRange * (1f - totalWeight);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        this.m_FirstFrameHappened = false;

        if (this.m_TrackBinding == null) return;

        this.m_TrackBinding.color           = this.m_DefaultColor;
        this.m_TrackBinding.intensity       = this.m_DefaultIntensity;
        this.m_TrackBinding.bounceIntensity = this.m_DefaultBounceIntensity;
        this.m_TrackBinding.range           = this.m_DefaultRange;
    }
}