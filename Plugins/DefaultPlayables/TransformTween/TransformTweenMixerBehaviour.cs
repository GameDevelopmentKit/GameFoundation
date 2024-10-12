using System;
using UnityEngine;
using UnityEngine.Playables;

public class TransformTweenMixerBehaviour : PlayableBehaviour
{
    private bool m_FirstFrameHappened;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var trackBinding = playerData as Transform;

        if (trackBinding == null) return;

        var defaultPosition = trackBinding.position;
        var defaultRotation = trackBinding.rotation;

        var inputCount = playable.GetInputCount();

        var positionTotalWeight = 0f;
        var rotationTotalWeight = 0f;

        var blendedPosition = Vector3.zero;
        var blendedRotation = new Quaternion(0f, 0f, 0f, 0f);

        for (var i = 0; i < inputCount; i++)
        {
            var playableInput = (ScriptPlayable<TransformTweenBehaviour>)playable.GetInput(i);
            var input         = playableInput.GetBehaviour();

            if (input.endLocation == null) continue;

            var inputWeight = playable.GetInputWeight(i);

            if (!this.m_FirstFrameHappened && !input.startLocation)
            {
                input.startingPosition = defaultPosition;
                input.startingRotation = defaultRotation;
            }

            var normalisedTime = (float)(playableInput.GetTime() / playableInput.GetDuration());
            var tweenProgress  = input.EvaluateCurrentCurve(normalisedTime);

            if (input.tweenPosition)
            {
                positionTotalWeight += inputWeight;

                blendedPosition += Vector3.Lerp(input.startingPosition, input.endLocation.position, tweenProgress) * inputWeight;
            }

            if (input.tweenRotation)
            {
                rotationTotalWeight += inputWeight;

                var desiredRotation = Quaternion.Lerp(input.startingRotation, input.endLocation.rotation, tweenProgress);
                desiredRotation = NormalizeQuaternion(desiredRotation);

                if (Quaternion.Dot(blendedRotation, desiredRotation) < 0f) desiredRotation = ScaleQuaternion(desiredRotation, -1f);

                desiredRotation = ScaleQuaternion(desiredRotation, inputWeight);

                blendedRotation = AddQuaternions(blendedRotation, desiredRotation);
            }
        }

        blendedPosition += defaultPosition * (1f - positionTotalWeight);
        var weightedDefaultRotation = ScaleQuaternion(defaultRotation, 1f - rotationTotalWeight);
        blendedRotation = AddQuaternions(blendedRotation, weightedDefaultRotation);

        trackBinding.position = blendedPosition;
        trackBinding.rotation = blendedRotation;

        this.m_FirstFrameHappened = true;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        this.m_FirstFrameHappened = false;
    }

    private static Quaternion AddQuaternions(Quaternion first, Quaternion second)
    {
        first.w += second.w;
        first.x += second.x;
        first.y += second.y;
        first.z += second.z;
        return first;
    }

    private static Quaternion ScaleQuaternion(Quaternion rotation, float multiplier)
    {
        rotation.w *= multiplier;
        rotation.x *= multiplier;
        rotation.y *= multiplier;
        rotation.z *= multiplier;
        return rotation;
    }

    private static float QuaternionMagnitude(Quaternion rotation)
    {
        return Mathf.Sqrt(Quaternion.Dot(rotation, rotation));
    }

    private static Quaternion NormalizeQuaternion(Quaternion rotation)
    {
        var magnitude = QuaternionMagnitude(rotation);

        if (magnitude > 0f) return ScaleQuaternion(rotation, 1f / magnitude);

        Debug.LogWarning("Cannot normalize a quaternion with zero magnitude.");
        return Quaternion.identity;
    }
}