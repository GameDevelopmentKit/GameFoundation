using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ScreenFaderMixerBehaviour : PlayableBehaviour
{
    private Color m_DefaultColor;

    private Image m_TrackBinding;
    private bool  m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        this.m_TrackBinding = playerData as Image;

        if (this.m_TrackBinding == null) return;

        if (!this.m_FirstFrameHappened)
        {
            this.m_DefaultColor       = this.m_TrackBinding.color;
            this.m_FirstFrameHappened = true;
        }

        var inputCount = playable.GetInputCount();

        var blendedColor   = Color.clear;
        var totalWeight    = 0f;
        var greatestWeight = 0f;
        var currentInputs  = 0;

        for (var i = 0; i < inputCount; i++)
        {
            var inputWeight   = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<ScreenFaderBehaviour>)playable.GetInput(i);
            var input         = inputPlayable.GetBehaviour();

            blendedColor += input.color * inputWeight;
            totalWeight  += inputWeight;

            if (inputWeight > greatestWeight) greatestWeight = inputWeight;

            if (!Mathf.Approximately(inputWeight, 0f)) currentInputs++;
        }

        this.m_TrackBinding.color = blendedColor + this.m_DefaultColor * (1f - totalWeight);
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        this.m_FirstFrameHappened = false;

        if (this.m_TrackBinding == null) return;

        this.m_TrackBinding.color = this.m_DefaultColor;
    }
}