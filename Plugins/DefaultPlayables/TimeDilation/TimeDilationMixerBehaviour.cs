using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeDilationMixerBehaviour : PlayableBehaviour
{
    private readonly float defaultTimeScale = 1f;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var inputCount = playable.GetInputCount();

        var mixedTimeScale    = 0f;
        var totalWeight       = 0f;
        var currentInputCount = 0;

        for (var i = 0; i < inputCount; i++)
        {
            var inputWeight = playable.GetInputWeight(i);

            if (inputWeight > 0f) currentInputCount++;

            totalWeight += inputWeight;

            var playableInput = (ScriptPlayable<TimeDilationBehaviour>)playable.GetInput(i);
            var input         = playableInput.GetBehaviour();

            mixedTimeScale += inputWeight * input.timeScale;
        }

        Time.timeScale = mixedTimeScale + this.defaultTimeScale * (1f - totalWeight);

        if (currentInputCount == 0) Time.timeScale = this.defaultTimeScale;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        Time.timeScale = this.defaultTimeScale;
    }

    public override void OnGraphStop(Playable playable)
    {
        Time.timeScale = this.defaultTimeScale;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        Time.timeScale = this.defaultTimeScale;
    }
}