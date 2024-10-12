using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class TextSwitcherMixerBehaviour : PlayableBehaviour
{
    private Color  m_DefaultColor;
    private int    m_DefaultFontSize;
    private string m_DefaultText;

    private Text m_TrackBinding;
    private bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        this.m_TrackBinding = playerData as Text;

        if (this.m_TrackBinding == null) return;

        if (!this.m_FirstFrameHappened)
        {
            this.m_DefaultColor       = this.m_TrackBinding.color;
            this.m_DefaultFontSize    = this.m_TrackBinding.fontSize;
            this.m_DefaultText        = this.m_TrackBinding.text;
            this.m_FirstFrameHappened = true;
        }

        var inputCount = playable.GetInputCount();

        var blendedColor    = Color.clear;
        var blendedFontSize = 0f;
        var totalWeight     = 0f;
        var greatestWeight  = 0f;
        var currentInputs   = 0;

        for (var i = 0; i < inputCount; i++)
        {
            var inputWeight   = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<TextSwitcherBehaviour>)playable.GetInput(i);
            var input         = inputPlayable.GetBehaviour();

            blendedColor    += input.color * inputWeight;
            blendedFontSize += input.fontSize * inputWeight;
            totalWeight     += inputWeight;

            if (inputWeight > greatestWeight)
            {
                this.m_TrackBinding.text = input.text;
                greatestWeight           = inputWeight;
            }

            if (!Mathf.Approximately(inputWeight, 0f)) currentInputs++;
        }

        this.m_TrackBinding.color    = blendedColor + this.m_DefaultColor * (1f - totalWeight);
        this.m_TrackBinding.fontSize = Mathf.RoundToInt(blendedFontSize + this.m_DefaultFontSize * (1f - totalWeight));
        if (currentInputs != 1 && 1f - totalWeight > greatestWeight) this.m_TrackBinding.text = this.m_DefaultText;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        this.m_FirstFrameHappened = false;

        if (this.m_TrackBinding == null) return;

        this.m_TrackBinding.color    = this.m_DefaultColor;
        this.m_TrackBinding.fontSize = this.m_DefaultFontSize;
        this.m_TrackBinding.text     = this.m_DefaultText;
    }
}