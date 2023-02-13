/*! \cond PRIVATE */

using UnityEngine;
using Random = UnityEngine.Random;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class is only activated when you need code to execute in an Update method, such as "follow" code.
    /// </summary>
    // ReSharper disable once CheckNamespace
    [AudioScriptOrder(-15)]
    public class SoundGroupVariationUpdater : MonoBehaviour {
        private const float TimeEarlyToScheduleNextClip = .1f;
        private const float FakeNegativeFloatValue = -10f;

        private Transform _objectToFollow;
        private GameObject _objectToFollowGo;
        private bool _isFollowing;
        private SoundGroupVariation _variation;
        private float _priorityLastUpdated = FakeNegativeFloatValue;
        private bool _useClipAgePriority;
        private WaitForSoundFinishMode _waitMode = WaitForSoundFinishMode.None;
        private AudioSource _varAudio;
        private MasterAudioGroup _parentGrp;
        private Transform _trans;
        private int _frameNum = -1;
        private bool _inited = false;

        // fade in out vars
        private float _fadeOutStartTime = -5;
        private bool _fadeInOutWillFadeOut;
        private bool _hasFadeInOutSetMaxVolume;
        private float _fadeInOutInFactor;
        private float _fadeInOutOutFactor;

        // fade out early vars
        private System.Action _fadeOutEarlyCompletionCallback;
        private int _fadeOutEarlyTotalFrames;
        private float _fadeOutEarlyFrameVolChange;
        private int _fadeOutEarlyFrameNumber;
        private float _fadeOutEarlyOrigVol;

        // gradual fade vars
        private float _fadeToTargetFrameVolChange;
        private int _fadeToTargetFrameNumber;
        private float _fadeToTargetOrigVol;
        private System.Action _fadeToTargetCompletionCallback;
        private int _fadeToTargetTotalFrames;
        private float _fadeToTargetVolume;
        private bool _fadeOutStarted;
        private float _lastFrameClipTime = -1f;
        private bool _isPlayingBackward;

        private int _pitchGlideToTargetTotalFrames;
        private float _pitchGlideToTargetFramePitchChange;
        private int _pitchGlideToTargetFrameNumber;
        private float _glideToTargetPitch;
        private float _glideToTargetOrigPitch;
        private System.Action _glideToPitchCompletionCallback;

        private bool _hasStartedNextInChain;

        private bool _isWaitingForQueuedOcclusionRay;
        private int _framesPlayed = 0;
        private float? _clipStartPosition;
        private float? _clipEndPosition;
        private double? _clipSchedEndTime;
        private bool _hasScheduledNextClip;
        private bool _hasScheduledEndLinkedGroups;
        private int _lastFrameClipPosition = -1;
        private int _timesLooped = 0;
        private bool _isPaused;
        private double _pauseTime;

        private static int _maCachedFromFrame = -1;
        private static MasterAudio _maThisFrame;
        private static Transform _listenerThisFrame;

        private enum WaitForSoundFinishMode {
            None,
            Play,
            WaitForEnd,
            StopOrRepeat
        }

        #region Public methods

        public void GlidePitch(float targetPitch, float glideTime, System.Action completionCallback = null) {
            GrpVariation.curPitchMode = SoundGroupVariation.PitchMode.Gliding;

            var pitchDiff = targetPitch - VarAudio.pitch;

            _pitchGlideToTargetTotalFrames = (int)(glideTime / AudioUtil.FrameTime);
            _pitchGlideToTargetFramePitchChange = pitchDiff / _pitchGlideToTargetTotalFrames;
            _pitchGlideToTargetFrameNumber = 0;
            _glideToTargetPitch = targetPitch;
            _glideToTargetOrigPitch = VarAudio.pitch;
            _glideToPitchCompletionCallback = completionCallback;
        }

        public void FadeOverTimeToVolume(float targetVolume, float fadeTime, System.Action completionCallback = null) {
            GrpVariation.curFadeMode = SoundGroupVariation.FadeMode.GradualFade;

            var volDiff = targetVolume - VarAudio.volume;

            var currentClipTime = VarAudio.time;
            var currentClipLength = ClipEndPosition;

            if (!VarAudio.loop && VarAudio.clip != null && fadeTime + currentClipTime > currentClipLength) {
                // if too long, fade out faster
                fadeTime = currentClipLength - currentClipTime;
            }

            _fadeToTargetTotalFrames = (int)(fadeTime / AudioUtil.FrameTime);
            _fadeToTargetFrameVolChange = volDiff / _fadeToTargetTotalFrames;
            _fadeToTargetFrameNumber = 0;
            _fadeToTargetOrigVol = VarAudio.volume;
            _fadeToTargetVolume = targetVolume;
            _fadeToTargetCompletionCallback = completionCallback;
        }

        public void FadeOutEarly(float fadeTime, System.Action completionCallback = null) {
            GrpVariation.curFadeMode = SoundGroupVariation.FadeMode.FadeOutEarly;
            // cancel the FadeInOut loop, if it's going.

            if (!VarAudio.loop && VarAudio.clip != null && VarAudio.time + fadeTime > ClipEndPosition) {
                // if too long, fade out faster
                fadeTime = ClipEndPosition - VarAudio.time;
            }

            var frameTime = AudioUtil.FrameTime;
            if (frameTime == 0) {
                frameTime = AudioUtil.FixedDeltaTime;
            }

            _fadeOutEarlyCompletionCallback = completionCallback;
            _fadeOutEarlyTotalFrames = (int)(fadeTime / frameTime);
            _fadeOutEarlyFrameVolChange = -VarAudio.volume / _fadeOutEarlyTotalFrames;
            _fadeOutEarlyFrameNumber = 0;
            _fadeOutEarlyOrigVol = VarAudio.volume;
        }

        // Called by Master Audio, do not call.
        public void Initialize() {
            if (_inited) {
                return;
            }

            _lastFrameClipPosition = -1;
            _timesLooped = 0;
            _isPaused = false;
            _pauseTime = -1;
            _clipStartPosition = null;
            _clipEndPosition = null;
            _clipSchedEndTime = null;
            _hasScheduledNextClip = false;
            _hasScheduledEndLinkedGroups = false;
            _inited = true;
        }

        public void FadeInOut() {
            GrpVariation.curFadeMode = SoundGroupVariation.FadeMode.FadeInOut;
            // wait to set this so it stops the previous one if it's still going.
            _fadeOutStartTime = ClipEndPosition - GrpVariation.fadeOutTime;

            if (GrpVariation.fadeInTime > 0f) {
                VarAudio.volume = 0f; // start at zero volume
                _fadeInOutInFactor = GrpVariation.fadeMaxVolume / GrpVariation.fadeInTime;
            } else {
                _fadeInOutInFactor = 0f;
            }

            _fadeInOutWillFadeOut = GrpVariation.fadeOutTime > 0f && !VarAudio.loop;

            if (_fadeInOutWillFadeOut) {
                _fadeInOutOutFactor = GrpVariation.fadeMaxVolume / (ClipEndPosition - _fadeOutStartTime);
            } else {
                _fadeInOutOutFactor = 0f;
            }
        }

        public void FollowObject(bool follow, Transform objToFollow, bool clipAgePriority) {
            _isFollowing = follow;

            if (objToFollow != null) {
                _objectToFollow = objToFollow;
                _objectToFollowGo = objToFollow.gameObject;
            }
            _useClipAgePriority = clipAgePriority;

            UpdateCachedObjects();
            UpdateAudioLocationAndPriority(false); // in case we're not following, it should get one update.
        }

        public void WaitForSoundFinish() {
            if (MasterAudio.IsWarming) {
                PlaySoundAndWait();
                return;
            }

            _waitMode = WaitForSoundFinishMode.Play;
        }

        public void StopPitchGliding() {
            GrpVariation.curPitchMode = SoundGroupVariation.PitchMode.None;

            if (_glideToPitchCompletionCallback != null) {
                _glideToPitchCompletionCallback();
                _glideToPitchCompletionCallback = null;
            }

            DisableIfFinished();
        }

        public void StopFading() {
            GrpVariation.curFadeMode = SoundGroupVariation.FadeMode.None;

            DisableIfFinished();
        }

        public void StopWaitingForFinish() {
            _waitMode = WaitForSoundFinishMode.None;
            GrpVariation.curDetectEndMode = SoundGroupVariation.DetectEndMode.None;

            DisableIfFinished();
        }

        public void StopFollowing() {
            _isFollowing = false;
            _useClipAgePriority = false;
            _objectToFollow = null;
            _objectToFollowGo = null;

            DisableIfFinished();
        }

        #endregion

        #region Helper methods

        private void DisableIfFinished() {
            if (_isFollowing
                || GrpVariation.curDetectEndMode == SoundGroupVariation.DetectEndMode.DetectEnd
                || GrpVariation.curFadeMode != SoundGroupVariation.FadeMode.None) {

                return;
            }

            if (GrpVariation.curPitchMode != SoundGroupVariation.PitchMode.None) {
                return;
            }

            enabled = false;
        }

        private void UpdateAudioLocationAndPriority(bool rePrioritize) {
            // update location, only if following.
            if (_isFollowing && _objectToFollow != null) {
                Trans.position = _objectToFollow.position;
            }

            // re-set priority, still used by non-following (audio clip age priority)
            if (!_maThisFrame.prioritizeOnDistance || !rePrioritize || ParentGroup.alwaysHighestPriority) {
                return;
            }

            if (Time.realtimeSinceStartup - _priorityLastUpdated <= MasterAudio.ReprioritizeTime) {
                return;
            }

            AudioPrioritizer.Set3DPriority(GrpVariation, _useClipAgePriority);
            _priorityLastUpdated = AudioUtil.Time;
        }

        private void ResetToNonOcclusionSetting() {
            var lp = GrpVariation.LowPassFilter;
            if (lp != null) {
                lp.cutoffFrequency = AudioUtil.DefaultMinOcclusionCutoffFrequency;
            }
        }

        private void UpdateOcclusion() {
            var hasOcclusionOn = GrpVariation.UsesOcclusion;
            if (!hasOcclusionOn) {
                MasterAudio.StopTrackingOcclusionForSource(GrpVariation.GameObj);
                ResetToNonOcclusionSetting();

                return;
            }

            if (_listenerThisFrame == null) {
                // cannot occlude without something to raycast at.
                return;
            }

            if (IsOcclusionMeasuringPaused) {
                return; // wait until processed
            }

            MasterAudio.AddToQueuedOcclusionRays(this);
            _isWaitingForQueuedOcclusionRay = true;
        }

        private void DoneWithOcclusion() {
            _isWaitingForQueuedOcclusionRay = false;
            MasterAudio.RemoveFromOcclusionFrequencyTransitioning(GrpVariation);
        }

        /// <summary>
        /// This method is called in a batch from ListenerFollower
        /// </summary>
        /// <returns></returns>
        public bool RayCastForOcclusion() {
            DoneWithOcclusion();

            var raycastOrigin = Trans.position;

            var offset = RayCastOriginOffset;
            if (offset > 0) {
                raycastOrigin = Vector3.MoveTowards(raycastOrigin, _listenerThisFrame.position, offset);
            }

            var direction = _listenerThisFrame.position - raycastOrigin;
            var distanceToListener = direction.magnitude;

            if (distanceToListener > VarAudio.maxDistance) {
                // out of hearing range, no reason to calculate occlusion.
                MasterAudio.AddToOcclusionOutOfRangeSources(GrpVariation.GameObj);
                ResetToNonOcclusionSetting();
                return false;
            }

            MasterAudio.AddToOcclusionInRangeSources(GrpVariation.GameObj);
            var is2DRaycast = _maThisFrame.occlusionRaycastMode == MasterAudio.RaycastMode.Physics2D;

            if (GrpVariation.LowPassFilter == null) {
                // in case Occlusion got turned on during runtime.
                var newFilter = GrpVariation.gameObject.AddComponent<AudioLowPassFilter>();
                GrpVariation.LowPassFilter = newFilter;
            }

#if PHY2D_ENABLED
            var oldQueriesStart = Physics2D.queriesStartInColliders;
            if (is2DRaycast) {
                Physics2D.queriesStartInColliders = _maThisFrame.occlusionIncludeStartRaycast2DCollider;
            }
#endif

#if PHY2D_ENABLED || PHY3D_ENABLED
            var oldRaycastsHitTriggers = true;
#endif

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (is2DRaycast) {
#if PHY2D_ENABLED
                oldRaycastsHitTriggers = Physics2D.queriesHitTriggers;
                Physics2D.queriesHitTriggers = _maThisFrame.occlusionRaycastsHitTriggers;
#endif
            } else {
#if PHY3D_ENABLED
                oldRaycastsHitTriggers = Physics.queriesHitTriggers;
                Physics.queriesHitTriggers = _maThisFrame.occlusionRaycastsHitTriggers;
#endif
            }

            var hitPoint = Vector3.zero;
            float? hitDistance = null;
            var isHit = false;

            if (_maThisFrame.occlusionUseLayerMask) {
                switch (_maThisFrame.occlusionRaycastMode) {
                    case MasterAudio.RaycastMode.Physics3D:
#if PHY3D_ENABLED
                        RaycastHit hitObject;
                        if (Physics.Raycast(raycastOrigin, direction, out hitObject, distanceToListener, _maThisFrame.occlusionLayerMask.value)) {
                            isHit = true;
                            hitPoint = hitObject.point;
                            hitDistance = hitObject.distance;
                        }
#endif
                        break;
                    case MasterAudio.RaycastMode.Physics2D:
#if PHY2D_ENABLED
                        var castHit2D = Physics2D.Raycast(raycastOrigin, direction, distanceToListener, _maThisFrame.occlusionLayerMask.value);
                        if (castHit2D.transform != null) {
                            isHit = true;
                            hitPoint = castHit2D.point;
                            hitDistance = castHit2D.distance;
                        }
#endif
                        break;
                }
            } else {
                switch (_maThisFrame.occlusionRaycastMode) {
                    case MasterAudio.RaycastMode.Physics3D:
#if PHY3D_ENABLED
                        RaycastHit hitObject;
                        if (Physics.Raycast(raycastOrigin, direction, out hitObject, distanceToListener)) {
                            isHit = true;
                            hitPoint = hitObject.point;
                            hitDistance = hitObject.distance;
                        }
#endif
                        break;
                    case MasterAudio.RaycastMode.Physics2D:
#if PHY2D_ENABLED
                        var castHit2D = Physics2D.Raycast(raycastOrigin, direction, distanceToListener);
                        if (castHit2D.transform != null) {
                            isHit = true;
                            hitPoint = castHit2D.point;
                            hitDistance = castHit2D.distance;
                        }
#endif
                        break;
                }
            }

            if (is2DRaycast) {
#if PHY2D_ENABLED
                Physics2D.queriesStartInColliders = oldQueriesStart;
                Physics2D.queriesHitTriggers = oldRaycastsHitTriggers;
#endif
            } else {
#if PHY3D_ENABLED
                Physics.queriesHitTriggers = oldRaycastsHitTriggers;
#endif
            }

            if (_maThisFrame.occlusionShowRaycasts) {
                var endPoint = isHit ? hitPoint : _listenerThisFrame.position;
                var lineColor = isHit ? Color.red : Color.green;
                Debug.DrawLine(raycastOrigin, endPoint, lineColor, .1f);
            }

            if (!isHit) {
                // ReSharper disable once PossibleNullReferenceException
                MasterAudio.RemoveFromBlockedOcclusionSources(GrpVariation.GameObj);
                ResetToNonOcclusionSetting();
                return true;
            }

            MasterAudio.AddToBlockedOcclusionSources(GrpVariation.GameObj);

            var ratioToEdgeOfSound = hitDistance.Value / VarAudio.maxDistance;
            var filterFrequency = AudioUtil.GetOcclusionCutoffFrequencyByDistanceRatio(ratioToEdgeOfSound, this);

            var fadeTime = _maThisFrame.occlusionFreqChangeSeconds;
            if (fadeTime <= MasterAudio.InnerLoopCheckInterval) { // fast, just do it instantly.
                // ReSharper disable once PossibleNullReferenceException
                GrpVariation.LowPassFilter.cutoffFrequency = filterFrequency;
                return true;
            }

            MasterAudio.GradualOcclusionFreqChange(GrpVariation, fadeTime, filterFrequency);

            return true;
        }

        private void PlaySoundAndWait() {
            if (VarAudio.clip == null) { // in case the warming sound is an "internet file"
                return;
            }

            double startTime = AudioSettings.dspTime;

            if (GrpVariation.PlaySoundParm.TimeToSchedulePlay.HasValue) {
                startTime = GrpVariation.PlaySoundParm.TimeToSchedulePlay.Value;
            }

            var delayTime = 0f;

            if (GrpVariation.useIntroSilence && GrpVariation.introSilenceMax > 0f) {
                var rndSilence = Random.Range(GrpVariation.introSilenceMin, GrpVariation.introSilenceMax);
                delayTime += rndSilence;
            }

            delayTime += GrpVariation.PlaySoundParm.DelaySoundTime;

            if (delayTime > 0f) {
                startTime += delayTime;
            }

            VarAudio.PlayScheduled(startTime);

            switch (GrpVariation.audLocation) {
#if ADDRESSABLES_ENABLED
                case MasterAudio.AudioLocation.Addressable:
                    AudioAddressableOptimizer.AddAddressablePlayingClip(GrpVariation.audioClipAddressable, VarAudio);
                    break;
#endif
                default:
                    AudioUtil.ClipPlayed(VarAudio.clip, GrpVariation.GameObj);
                    break;
            }

            if (GrpVariation.useRandomStartTime) {
                VarAudio.time = ClipStartPosition;

                if (!VarAudio.loop) { // don't stop it if it's going to loop.
                    var playableLength = AudioUtil.AdjustAudioClipDurationForPitch(ClipEndPosition - ClipStartPosition, VarAudio);
                    _clipSchedEndTime = startTime + playableLength;
                    VarAudio.SetScheduledEndTime(_clipSchedEndTime.Value);
                }
            }

            GrpVariation.LastTimePlayed = AudioUtil.Time;

            DuckIfNotSilent();

            _isPlayingBackward = GrpVariation.OriginalPitch < 0;
            _lastFrameClipTime = _isPlayingBackward ? ClipEndPosition + 1 : -1f;

            _waitMode = WaitForSoundFinishMode.WaitForEnd;
        }

        private void DuckIfNotSilent() {
            bool isSilent = false;

            if (GrpVariation.PlaySoundParm.VolumePercentage <= 0) {
                isSilent = true;
            } else if (GrpVariation.ParentGroup.groupMasterVolume <= 0) {
                isSilent = true;
            } else if (GrpVariation.VarAudio.mute) { // other group soloed
                isSilent = true;
            } else if (MasterAudio.MixerMuted) { 
                isSilent = true;
            } else if (GrpVariation.ParentGroup.isMuted) {
                isSilent = true;
            } else {
                var bus = GrpVariation.ParentGroup.BusForGroup;
                if (bus != null && bus.isMuted) {
                    isSilent = true;
                }
            }

            // sound play worked! Duck music if a ducking sound and sound is not silent.
            if (!isSilent) {
                MasterAudio.DuckSoundGroup(ParentGroup.GameObjectName, VarAudio, this);
            }
        }

        private void StopOrChain() {
            var playSnd = GrpVariation.PlaySoundParm;

            var wasPlaying = playSnd.IsPlaying;
            var usingChainLoop = wasPlaying && playSnd.IsChainLoop;

            if (!VarAudio.loop || usingChainLoop) {
                GrpVariation.Stop();
            }

            if (!usingChainLoop) {
                return;
            }
            StopWaitingForFinish();

            MaybeChain();
        }

        public void Pause() {
            _isPaused = true;
            _pauseTime = AudioSettings.dspTime;
            MasterAudio.EndDucking(this);
        }

        public void Unpause() {
            _isPaused = false;

            if (_clipSchedEndTime.HasValue) {
                var timePaused = AudioSettings.dspTime - _pauseTime;
                _clipSchedEndTime += timePaused;
                VarAudio.SetScheduledEndTime(_clipSchedEndTime.Value);
            }
        }

        public void MaybeChain() {
            if (_hasStartedNextInChain) {
                return;
            }

            _hasStartedNextInChain = true;

            var playSnd = GrpVariation.PlaySoundParm;

            var clipsRemaining = MasterAudio.RemainingClipsInGroup(ParentGroup.GameObjectName);
            var totalClips = MasterAudio.VoicesForGroup(ParentGroup.GameObjectName);

            if (clipsRemaining == totalClips) {
                ParentGroup.FireLastVariationFinishedPlay();
            }

            // check if loop count is over.
            if (ParentGroup.chainLoopMode == MasterAudioGroup.ChainedLoopLoopMode.NumberOfLoops && ParentGroup.ChainLoopCount >= ParentGroup.chainLoopNumLoops) {
                // done looping
                return;
            }

            var rndDelay = playSnd.DelaySoundTime;
            if (ParentGroup.chainLoopDelayMin > 0f || ParentGroup.chainLoopDelayMax > 0f) {
                rndDelay = Random.Range(ParentGroup.chainLoopDelayMin, ParentGroup.chainLoopDelayMax);
            }

            // cannot use "AndForget" methods! Chain loop needs to check the status.
            if (playSnd.AttachToSource || playSnd.SourceTrans != null) {
                PlaySoundResult chainedVariation = null;
                
                if (playSnd.AttachToSource) {
                    chainedVariation = MasterAudio.PlaySound3DFollowTransform(playSnd.SoundType, playSnd.SourceTrans,
                        playSnd.VolumePercentage, playSnd.Pitch, rndDelay, null, _clipSchedEndTime, true);
                } else {
                    chainedVariation = MasterAudio.PlaySound3DAtTransform(playSnd.SoundType, playSnd.SourceTrans, playSnd.VolumePercentage,
                        playSnd.Pitch, rndDelay, null, _clipSchedEndTime, true);
                }

                if (chainedVariation.ActingVariation != null) {
                    // must set and inform TransformTracker of the new Variation so it doesn't continue to follow the old.
                    chainedVariation.ActingVariation.UpdateAudioVariation(GrpVariation.AmbientFollower);
                }
            } else {
                MasterAudio.PlaySoundAndForget(playSnd.SoundType, playSnd.VolumePercentage, playSnd.Pitch, rndDelay, null, _clipSchedEndTime, true);
            }
        }

        private void UpdatePitch() {
            switch (GrpVariation.curPitchMode) {
                case SoundGroupVariation.PitchMode.None:
                    return;
                case SoundGroupVariation.PitchMode.Gliding:
                    if (!VarAudio.isPlaying) {
                        break;
                    }

                    _pitchGlideToTargetFrameNumber++;
                    if (_pitchGlideToTargetFrameNumber >= _pitchGlideToTargetTotalFrames) {
                        VarAudio.pitch = _glideToTargetPitch;
                        StopPitchGliding();
                    } else {
                        VarAudio.pitch = (_pitchGlideToTargetFrameNumber * _pitchGlideToTargetFramePitchChange) + _glideToTargetOrigPitch;
                    }

                    break;
            }
        }

        private void PerformFading() {
            switch (GrpVariation.curFadeMode) {
                case SoundGroupVariation.FadeMode.None:
                    break;
                case SoundGroupVariation.FadeMode.FadeInOut:
                    if (!VarAudio.isPlaying) {
                        break;
                    }

                    var clipTime = VarAudio.time;
                    if (GrpVariation.fadeInTime > 0f && clipTime < GrpVariation.fadeInTime) {
                        // fade in!
                        VarAudio.volume = clipTime * _fadeInOutInFactor;
                    } else if (clipTime >= GrpVariation.fadeInTime && !_hasFadeInOutSetMaxVolume) {
                        VarAudio.volume = GrpVariation.fadeMaxVolume;
                        _hasFadeInOutSetMaxVolume = true;
                        if (!_fadeInOutWillFadeOut) {
                            StopFading();
                        }
                    } else if (_fadeInOutWillFadeOut && clipTime >= _fadeOutStartTime) {
                        // fade out!
                        if (GrpVariation.PlaySoundParm.IsChainLoop && !_fadeOutStarted) {
                            MaybeChain();
                            _fadeOutStarted = true;
                        }
                        VarAudio.volume = (ClipEndPosition - clipTime) * _fadeInOutOutFactor;
                    }
                    break;
                case SoundGroupVariation.FadeMode.FadeOutEarly:
                    if (!VarAudio.isPlaying) {
                        break;
                    }

                    _fadeOutEarlyFrameNumber++;

                    VarAudio.volume = (_fadeOutEarlyFrameNumber * _fadeOutEarlyFrameVolChange) + _fadeOutEarlyOrigVol;

                    if (_fadeOutEarlyFrameNumber >= _fadeOutEarlyTotalFrames) {
                        GrpVariation.curFadeMode = SoundGroupVariation.FadeMode.None;
                        GrpVariation.Stop();
                        if (_fadeOutEarlyCompletionCallback != null)
                        {
                            _fadeOutEarlyCompletionCallback();
                        }
                    }

                    break;
                case SoundGroupVariation.FadeMode.GradualFade:
                    if (!VarAudio.isPlaying) {
                        break;
                    }

                    _fadeToTargetFrameNumber++;
                    if (_fadeToTargetFrameNumber >= _fadeToTargetTotalFrames) {
                        var grpInfo = MasterAudio.GetAllVariationsOfGroup(ParentGroup.GameObjectName);
                        for (var i = 0; i < grpInfo.Count; i++) {
                            var aVar = grpInfo[i];
                            if (aVar.Variation != this.GrpVariation)
                            {
                                continue;
                            }

                            aVar.LastPercentageVolume = _fadeToTargetVolume;
                            break;
                        }


                        VarAudio.volume = _fadeToTargetVolume;
                        StopFading();
                        if (_fadeToTargetCompletionCallback != null)
                        {
                            _fadeToTargetCompletionCallback();
                        }
                    } else {
                        VarAudio.volume = (_fadeToTargetFrameNumber * _fadeToTargetFrameVolChange) + _fadeToTargetOrigVol;
                    }
                    break;
            }
        }

#endregion

#region MonoBehavior events

        // ReSharper disable once UnusedMember.Local
        private void OnEnable() {
            _inited = false;

            // values to be reset every time a sound plays.
            _fadeInOutWillFadeOut = false;
            _hasFadeInOutSetMaxVolume = false;
            _fadeOutStarted = false;
            _hasStartedNextInChain = false;
            _framesPlayed = 0;

            _clipStartPosition = null;
            _clipEndPosition = null;

            DoneWithOcclusion();
            MasterAudio.RegisterUpdaterForUpdates(this);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable() {
            if (MasterAudio.AppIsShuttingDown) {
                return; // do nothing
            }

            _framesPlayed = 0;

            DoneWithOcclusion();
            MasterAudio.UnregisterUpdaterForUpdates(this);
        }

        public void UpdateCachedObjects() {
            _frameNum = AudioUtil.FrameCount;

            // ReSharper disable once InvertIf
            if (_maCachedFromFrame >= _frameNum) {
                return; // same frame. Use cached objects
            }

            // new frame. Update cached objects and frame counters;
            _maCachedFromFrame = _frameNum;
            _maThisFrame = MasterAudio.Instance;
            _listenerThisFrame = MasterAudio.ListenerTrans;
        }

        /// <summary>
        /// This method will be called by MasterAudio.cs either during LateUpdate (default) or FixedUpdate, however you've configured it in Advanced Settings.
        /// </summary>
        public void ManualUpdate() {
            UpdateCachedObjects();

            _framesPlayed++;

            if (VarAudio.loop) {
                if (VarAudio.timeSamples < _lastFrameClipPosition) {
                    _timesLooped++;
                    if (VarAudio.loop && GrpVariation.useCustomLooping && _timesLooped >= GrpVariation.MaxLoops) {
                        GrpVariation.Stop();
                    } else {
                        GrpVariation.SoundLoopStarted(_timesLooped);
                    }
                }

                _lastFrameClipPosition = VarAudio.timeSamples;
            }

            if (_isFollowing) { // check for despawned caller and act if so.
                if (ParentGroup.targetDespawnedBehavior != MasterAudioGroup.TargetDespawnedBehavior.None) {
                    if (_objectToFollowGo == null || !DTMonoHelper.IsActive(_objectToFollowGo)) {
                        switch (ParentGroup.targetDespawnedBehavior) {
                            case MasterAudioGroup.TargetDespawnedBehavior.Stop:
                                GrpVariation.Stop();
                                break;
                            case MasterAudioGroup.TargetDespawnedBehavior.FadeOut:
                                GrpVariation.FadeOutNowAndStop(ParentGroup.despawnFadeTime);
                                break;
                        }

                        StopFollowing();
                    }
                }
            }

            // fade in out / out early etc.
            PerformFading();

            // priority
            UpdateAudioLocationAndPriority(true);

            // occlusion
            UpdateOcclusion();

            // pitch
            UpdatePitch();


            switch (_waitMode) {
                case WaitForSoundFinishMode.None:
                    break;
                case WaitForSoundFinishMode.Play:
                    PlaySoundAndWait();
                    break;
                case WaitForSoundFinishMode.WaitForEnd:
                    if (_isPaused) {
                        break;
                    }

                    if (_clipSchedEndTime.HasValue) {
                        if (AudioSettings.dspTime + TimeEarlyToScheduleNextClip >= _clipSchedEndTime.Value) {
                            if (GrpVariation.PlaySoundParm.IsChainLoop && !_hasScheduledNextClip) {
                                MaybeChain();
                                _hasScheduledNextClip = true;
                            }
                            if (HasEndLinkedGroups && !_hasScheduledEndLinkedGroups) {
                                GrpVariation.PlayEndLinkedGroups(_clipSchedEndTime.Value);
                                _hasScheduledEndLinkedGroups = true;
                            }
                        }
                    }

                    var willChangeModes = false;

                    if (_isPlayingBackward) {
                        if (VarAudio.time > _lastFrameClipTime) {
                            willChangeModes = true;
                        }
                    } else {
                        if (VarAudio.time < _lastFrameClipTime) {
                            willChangeModes = true;
                        }
                    }

                    _lastFrameClipTime = VarAudio.time;

                    if (willChangeModes) {
                        _waitMode = WaitForSoundFinishMode.StopOrRepeat;
                    }
                    break;
                case WaitForSoundFinishMode.StopOrRepeat:
                    StopOrChain();
                    break;
            }
        }

#endregion

#region Properties

        public float ClipStartPosition {
            get {
                if (_clipStartPosition.HasValue) {
                    return _clipStartPosition.Value;
                }

                if (GrpVariation.useRandomStartTime) {
                    _clipStartPosition = Random.Range(GrpVariation.randomStartMinPercent, GrpVariation.randomStartMaxPercent) * 0.01f * VarAudio.clip.length;
                } else {
                    _clipStartPosition = 0f;
                }

                return _clipStartPosition.Value;
            }
        }

        public float ClipEndPosition {
            get {
                if (_clipEndPosition.HasValue) {
                    return _clipEndPosition.Value;
                }

                if (GrpVariation.useRandomStartTime) {
                    _clipEndPosition = GrpVariation.randomEndPercent * 0.01f * VarAudio.clip.length;
                } else {
                    _clipEndPosition = VarAudio.clip.length;
                }

                return _clipEndPosition.Value;
            }
        }

        public int FramesPlayed {
            get {
                return _framesPlayed;
            }
        }

        public MasterAudio MAThisFrame {
            get {
                return _maThisFrame;
            }
        }

        public float MaxOcclusionFreq {
            get {
                // ReSharper disable once InvertIf
                if (GrpVariation.UsesOcclusion && ParentGroup.willOcclusionOverrideFrequencies) {
                    return ParentGroup.occlusionMaxCutoffFreq;
                }

                return _maThisFrame.occlusionMaxCutoffFreq;
            }
        }

        public float MinOcclusionFreq {
            get {
                // ReSharper disable once InvertIf
                if (GrpVariation.UsesOcclusion && ParentGroup.willOcclusionOverrideFrequencies) {
                    return ParentGroup.occlusionMinCutoffFreq;
                }

                return _maThisFrame.occlusionMinCutoffFreq;
            }
        }

        private Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }

                _trans = GrpVariation.Trans;

                return _trans;
            }
        }

        private AudioSource VarAudio {
            get {
                if (_varAudio != null) {
                    return _varAudio;
                }

                _varAudio = GrpVariation.VarAudio;

                return _varAudio;
            }
        }

        private MasterAudioGroup ParentGroup {
            get {
                if (_parentGrp != null) {
                    return _parentGrp;
                }

                _parentGrp = GrpVariation.ParentGroup;

                return _parentGrp;
            }
        }

        private SoundGroupVariation GrpVariation {
            get {
                if (_variation != null) {
                    return _variation;
                }

                _variation = GetComponent<SoundGroupVariation>();

                return _variation;
            }
        }

        private float RayCastOriginOffset {
            get {
                // ReSharper disable once InvertIf
                if (GrpVariation.UsesOcclusion && ParentGroup.willOcclusionOverrideRaycastOffset) {
                    return ParentGroup.occlusionRayCastOffset;
                }

                return _maThisFrame.occlusionRayCastOffset;
            }
        }

        private bool IsOcclusionMeasuringPaused {
            get { return _isWaitingForQueuedOcclusionRay || MasterAudio.IsOcclusionFrequencyTransitioning(GrpVariation); }
        }

        private bool HasEndLinkedGroups {
            get {
                if (GrpVariation.ParentGroup.endLinkedGroups.Count > 0) {
                    return true;
                }

                return false;
            }
        }
#endregion
    }
}
/*! \endcond */
