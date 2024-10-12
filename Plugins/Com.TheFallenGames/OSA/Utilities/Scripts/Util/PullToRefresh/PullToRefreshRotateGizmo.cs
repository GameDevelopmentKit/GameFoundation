using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.Util.PullToRefresh
{
    /// <summary>
    /// <para> Implementation of <see cref="PullToRefreshGizmo"/> that uses a rotating image to show the pull progress. </para>
    /// <para>The image is rotated by the amount of distance traveled by the click/finger.</para>
    /// <para>When enough pulling distance is covered the gizmo enters the "ready to refresh" state,</para>
    /// <para>the rotation amount applied is damped by <see cref="_ExcessPullRotationDamping"/> (i.e. a value of 1f won't apply any furter rotation, </para>
    /// <para>while a value of 0f will apply the same amount of rotation per distance traveled by the click/finger as before the "ready to refresh" state).</para>
    /// <para>When <see cref="OnRefreshed(bool)"/> is called with true, the gizmo will disappear; if it'll be called with false, </para>
    /// <para>it'll start auto-rotating with a speed of <see cref="_AutoRotationDegreesPerSec"/> degrees per second, until <see cref="IsShown"/> is set to false.</para>
    /// <para>This last use-case is very common for when the refresh event actually takes time (i.e. retrieving items from a server).</para>
    /// </summary>
    public class PullToRefreshRotateGizmo : PullToRefreshGizmo
    {
#pragma warning disable 0649
        [SerializeField] [FormerlySerializedAs("_StartingPoint")] private RectTransform _PullFromStartInitial = null;
        [SerializeField] [FormerlySerializedAs("_EndingPoint")]   private RectTransform _PullFromStartTarget  = null;

        [SerializeField] private RectTransform _PullFromEndInitial = null;
        [SerializeField] private RectTransform _PullFromEndTarget  = null;
#pragma warning restore 0649

        //[Tooltip("When pulling is done from the end, this gizmo will also appear at the end, not at the start. " +
        //	"\nThe gizmo's position in this case is inferred using the parent's size and _StartingPoint & _EndingPoint ")]
        //[SerializeField]
        //bool _AllowAppearingFromEnd = true;

        [SerializeField] [Range(0f, 1f)] private float _ExcessPullRotationDamping = .95f;

        [SerializeField] private float _AutoRotationDegreesPerSec = 200;

        [Tooltip("Will also interpolate its own scale between the Initial's and Target's scale")] [SerializeField] private bool _ScaleWithTarget = true;

        [Tooltip("If true, it won't be affected by Time.timeScale")] [SerializeField] private bool _UseUnscaledTime = true;

        private bool _WaitingForManualHide;

        /// <summary>Calls base implementation + resets the rotation to default each time is assigned, regardless if true or false</summary>
        public override bool IsShown
        {
            get => base.IsShown;
            set
            {
                base.IsShown = value;

                // Reset to default rotation
                this.transform.localRotation = Quaternion.Euler(this._InitialLocalRotation);

                if (!value) this._WaitingForManualHide = false;
            }
        }

        private Vector3   _InitialLocalRotation, _InitialLocalScale;
        private Transform _TR;

        public override void Awake()
        {
            base.Awake();
            this._TR = this.transform;

            this._InitialLocalRotation = this._TR.localRotation.eulerAngles;
            this._InitialLocalScale    = this._TR.localScale;
        }

        private void Update()
        {
            if (this._WaitingForManualHide) this.SetLocalZRotation((this._TR.localEulerAngles.z - (this._UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * this._AutoRotationDegreesPerSec) % 360);
        }

        public override void OnPull(double power)
        {
            base.OnPull(power);

            var powerAbs          = Math.Abs(power);
            var powerSign         = Math.Sign(power);
            var powerAbsClamped01 = Mathf.Clamp01((float)powerAbs);
            var excess            = Mathf.Max(0f, (float)powerAbs - 1f);

            var dampedExcess = excess * (1f - this._ExcessPullRotationDamping);

            this.SetLocalZRotation((this._InitialLocalRotation.z - 360 * (powerAbsClamped01 + dampedExcess)) % 360);

            //_TR.position = LerpUnclamped(_StartingPoint.position, _EndingPoint.position, power <= 1f ? (power - (1f - power/2)*(1f-power/2)) : (1 - 1/(1 + excess) ));
            Vector3 start,      end;
            Vector3 scaleStart, scaleEnd;
            if (powerSign < 0 && this._PullFromEndInitial && this._PullFromEndTarget)
            {
                start      = this._PullFromEndInitial.position;
                end        = this._PullFromEndTarget.position;
                scaleStart = this._PullFromEndInitial.localScale;
                scaleEnd   = this._PullFromEndTarget.localScale;
            }
            else
            {
                start      = this._PullFromStartInitial.position;
                end        = this._PullFromStartTarget.position;
                scaleStart = this._PullFromStartInitial.localScale;
                scaleEnd   = this._PullFromStartTarget.localScale;
            }

            var t01Unclamped = 2 - 2 / (1 + powerAbsClamped01);
            this._TR.position = this.LerpUnclamped(start, end, t01Unclamped);
            if (this._ScaleWithTarget)
                this._TR.localScale = this.LerpUnclamped(scaleStart, scaleEnd, t01Unclamped);
            else
                this._TR.localScale = this._InitialLocalScale;
        }

        public override void OnRefreshCancelled()
        {
            base.OnRefreshCancelled();

            this._WaitingForManualHide = false;
        }

        public override void OnRefreshed(bool autoHide)
        {
            base.OnRefreshed(autoHide);

            this._WaitingForManualHide = !autoHide;
        }

        private Vector3 LerpUnclamped(Vector3 from, Vector3 to, float t)
        {
            return (1f - t) * from + t * to;
        }

        private void SetLocalZRotation(float zRotation)
        {
            var rotE = this._TR.localRotation.eulerAngles;
            rotE.z                 = zRotation;
            this._TR.localRotation = Quaternion.Euler(rotE);
        }
    }
}