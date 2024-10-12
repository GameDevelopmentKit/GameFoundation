using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TransformTweenBehaviour : PlayableBehaviour
{
    public enum TweenType
    {
        Linear,
        Deceleration,
        Harmonic,
        Custom,
    }

    public Transform      startLocation;
    public Transform      endLocation;
    public bool           tweenPosition = true;
    public bool           tweenRotation = true;
    public TweenType      tweenType;
    public AnimationCurve customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public Vector3    startingPosition;
    public Quaternion startingRotation = Quaternion.identity;

    private AnimationCurve m_LinearCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private AnimationCurve m_DecelerationCurve = new(
        new Keyframe(0f, 0f, -k_RightAngleInRads, k_RightAngleInRads),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    private AnimationCurve m_HarmonicCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private const float k_RightAngleInRads = Mathf.PI * 0.5f;

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (this.startLocation)
        {
            this.startingPosition = this.startLocation.position;
            this.startingRotation = this.startLocation.rotation;
        }
    }

    public float EvaluateCurrentCurve(float time)
    {
        if (this.tweenType == TweenType.Custom && !this.IsCustomCurveNormalised())
        {
            Debug.LogError("Custom Curve is not normalised.  Curve must start at 0,0 and end at 1,1.");
            return 0f;
        }

        switch (this.tweenType)
        {
            case TweenType.Linear:       return this.m_LinearCurve.Evaluate(time);
            case TweenType.Deceleration: return this.m_DecelerationCurve.Evaluate(time);
            case TweenType.Harmonic:     return this.m_HarmonicCurve.Evaluate(time);
            default:                     return this.customCurve.Evaluate(time);
        }
    }

    private bool IsCustomCurveNormalised()
    {
        if (!Mathf.Approximately(this.customCurve[0].time, 0f)) return false;

        if (!Mathf.Approximately(this.customCurve[0].value, 0f)) return false;

        if (!Mathf.Approximately(this.customCurve[this.customCurve.length - 1].time, 1f)) return false;

        return Mathf.Approximately(this.customCurve[this.customCurve.length - 1].value, 1f);
    }
}