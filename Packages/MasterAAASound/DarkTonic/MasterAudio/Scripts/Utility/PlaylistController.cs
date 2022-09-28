using UnityEngine;
using System;
// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;
using UnityEngine.Audio;
#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
#endif

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class is used to host and play Playlists. Contains cross-fading, ducking and more!
    /// </summary>
    [AudioScriptOrder(-80)]
    [RequireComponent(typeof(AudioSource))]
    // ReSharper disable once CheckNamespace
    public class PlaylistController : MonoBehaviour {
        /*! \cond PRIVATE */
        public const float ScheduledSongMinBadOffset = .5f;
        public const int FramesEarlyToTrigger = 2;
        public const int FramesEarlyToBeSyncable = 10;

        private const int NextScheduleTimeRecalcConsecutiveFrameCount = 5;
        private const string NotReadyMessage =
            "Playlist Controller is not initialized yet. It must call its own Awake & Start method before any other methods are called. If you have a script with an Awake or Start event that needs to call it, make sure PlaylistController.cs is set to execute first (Script Execution Order window in Unity). Awake event is still not guaranteed to work, so use Start where possible.";
        private const float MinSongLength = .5f;
        private const float SlowestFrameTimeForCalc = .3f;

        // ReSharper disable InconsistentNaming
        public bool startPlaylistOnAwake = true;
        public bool isShuffle = false;
        public bool isAutoAdvance = true;
        public bool loopPlaylist = true;
        public float _playlistVolume = 1f;
        public bool isMuted;
        public string startPlaylistName = string.Empty;
        public int syncGroupNum = -1;

        public AudioMixerGroup mixerChannel;
        public MasterAudio.ItemSpatialBlendType spatialBlendType = MasterAudio.ItemSpatialBlendType.ForceTo2D;
        public float spatialBlend = MasterAudio.SpatialBlend_2DValue;

        public bool initializedEventExpanded = false;
        public string initializedCustomEvent = string.Empty;
        public bool crossfadeStartedExpanded = false;
        public string crossfadeStartedCustomEvent = string.Empty;
        public bool songChangedEventExpanded = false;
        public string songChangedCustomEvent = string.Empty;
        public bool songEndedEventExpanded = false;
        public string songEndedCustomEvent = string.Empty;
        public bool songLoopedEventExpanded = false;
        public string songLoopedCustomEvent = string.Empty;
        public bool playlistStartedEventExpanded = false;
        public string playlistStartedCustomEvent = string.Empty;
        public bool playlistEndedEventExpanded = false;
        public string playlistEndedCustomEvent = string.Empty;
        // ReSharper restore InconsistentNaming

        private AudioSource _activeAudio;
        private AudioSource _transitioningAudio;
        private float _activeAudioEndVolume;
        private float _transitioningAudioStartVolume;
        private float _crossFadeStartTime;
        private readonly List<int> _clipsRemaining = new List<int>(10);
        private int _currentSequentialClipIndex;
        private AudioDuckingMode _duckingMode;
        private float _timeToStartUnducking;
        private float _timeToFinishUnducking;
        private float _originalMusicVolume;
        private float _initialDuckVolume;
        private float _duckRange;
        private SoundGroupVariationUpdater _actorUpdater;
        private float _unduckTime;

        private MusicSetting _currentSong;
        private GameObject _go;
        private string _name;
        private FadeMode _curFadeMode = FadeMode.None;
        private float _slowFadeStartTime;
        private float _slowFadeCompletionTime;
        private float _slowFadeStartVolume;
        private float _slowFadeTargetVolume;
        private MasterAudio.Playlist _currentPlaylist;
        private float _lastTimeMissingPlaylistLogged = -5f;
        // ReSharper disable once RedundantNameQualifier
        private System.Action _fadeCompleteCallback;
        private readonly List<MusicSetting> _queuedSongs = new List<MusicSetting>(5);
        private bool _lostFocus;
        private bool _autoStartedPlaylist;

        private AudioSource _audioClip;
        private AudioSource _transClip;
        private MusicSetting _newSongSetting;
        private bool _nextSongRequested;
        private bool _nextSongScheduled;
        private int _lastRandomClipIndex = -1;
        private float _lastTimeSongRequested = -1f;
        private float _currentDuckVolCut;
        private int? _lastSongPosition;
        private double? _currentSchedSongDspStartTime;
        private double? _currentSchedSongDspEndTime;
        private int _lastFrameSongPosition = -1;

        private int _nextScheduleTimeRecalcDifferentFirstFrameNum;
        private double? _nextScheduledTimeRecalcStart;

        private readonly Dictionary<AudioSource, double> _scheduledSongOffsetByAudioSource = new Dictionary<AudioSource, double>(2);
#if ADDRESSABLES_ENABLED 
        private readonly Dictionary<AudioSource, AssetReference> _loadedAddressablesByAudioSource = new Dictionary<AudioSource, AssetReference>(2);
#endif
        public int _frames;

        private static List<PlaylistController> _instances;
        private Coroutine _resourceCoroutine;
#if ADDRESSABLES_ENABLED 
        private Coroutine _addressableCoroutine;
#endif

        private int _songsPlayedFromPlaylist;
        private AudioSource _audio1;
        private AudioSource _audio2;
        private string _activeSongAlias;

        private Transform _trans;
        private bool _willPersist;
        private double? _songPauseTime;
        private int framesOfSongPlayed = 0;        

        public enum AudioPlayType {
            PlayNow,
            Schedule,
            AlreadyScheduled
        }

        public enum PlaylistStates {
            NotInScene,
            Stopped,
            Playing,
            Paused,
            Crossfading
        }

        public enum FadeMode {
            None,
            GradualFade
        }

        public enum AudioDuckingMode {
            NotDucking,
            SetToDuck,
            Ducked,
            Unducking
        }
        /*! \endcond */

        /// <summary>
        /// Used for the SongChanged event you can subscribe to
        /// </summary>
        public delegate void SongChangedEventHandler(string newSongName, MusicSetting song);

        /// <summary>
        /// Used for the SongEnded event you can subscribe to
        /// </summary>
        public delegate void SongEndedEventHandler(string songName);

        /// <summary>
        /// Used for the SongLooped event you can subscribe to
        /// </summary>
        public delegate void SongLoopedEventHandler(string songName);

        /// <summary>
        /// Used for the PlaylistEnded event you can subscribe to
        /// </summary>
        public delegate void PlaylistEndedEventHandler();

        /// <summary>
        /// This event will notify you when the Playlist song changes.
        /// </summary>
        public event SongChangedEventHandler SongChanged;

        /// <summary>
        /// This event will notify you when the Playlist song ends.
        /// </summary>
        public event SongEndedEventHandler SongEnded;

        /// <summary>
        /// This event will notify you when a song loops (ends a loop and starts the same clip again via looping).
        /// </summary>
        public event SongLoopedEventHandler SongLooped;

        /// <summary>
        /// This event will notify you when a Playlist stops after the last song plays.
        /// </summary>
        public event PlaylistEndedEventHandler PlaylistEnded;

        #region Monobehavior events

        // ReSharper disable once UnusedMember.Local
        private void Awake() {
            useGUILayout = false;

            if (ControllerIsReady) {
                // already called by lazy load.
                return;
            }

            ControllerIsReady = false;

            // check for "extra" Playlist Controllers of the same name.
            // ReSharper disable once ArrangeStaticMemberQualifier
            var controllers = (PlaylistController[])GameObject.FindObjectsOfType(typeof(PlaylistController));
            var sameNameCount = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < controllers.Length; i++) {
                if (controllers[i].ControllerName == ControllerName) {
                    sameNameCount++;
                }
            }

            if (sameNameCount > 1) {
                DestroyImmediate(gameObject);

                var mas = FindObjectsOfType(typeof(MasterAudio));
                bool shouldLog = false;
                for (var i = 0; i < mas.Length; i++) {
                    MasterAudio ama = mas[i] as MasterAudio;
                    if (!ama.persistBetweenScenes) {
                        continue;
                    }

                    if (ama.shouldLogDestroys) {
                        shouldLog = true;
                        break;
                    }
                }

                if (shouldLog) {
                    Debug.Log("More than one Playlist Controller prefab exists in this Scene with the same name. Destroying the one called '" + ControllerName + "'. You may wish to set up a Bootstrapper Scene so this does not occur.");
                }

                return;
            }
            // end check

            _autoStartedPlaylist = false;
            _duckingMode = AudioDuckingMode.NotDucking;
            _currentSong = null;
            _songsPlayedFromPlaylist = 0;

            var audios = GetComponents<AudioSource>();
            if (audios.Length < 2) {
                Debug.LogError("This prefab should have exactly two Audio Source components. Please revert it.");
                return;
            }

            var ma = MasterAudio.SafeInstance;
            _willPersist = ma != null && ma.persistBetweenScenes;

            _audio1 = audios[0];
            _audio2 = audios[1];

            _audio1.clip = null;
            _audio2.clip = null;

            if (_audio1.playOnAwake || _audio2.playOnAwake) {
                Debug.LogWarning("One or more 'Play on Awake' checkboxes in the Audio Sources on Playlist Controller '" + name + "' are checked. These are not used in Master Audio. Make sure to uncheck them before hitting Play next time. For Playlist Controllers, use the similarly named checkbox 'Start Playlist on Awake' in the Playlist Controller's Inspector.");
            }

            _activeAudio = _audio1;
            _transitioningAudio = _audio2;

            _audio1.outputAudioMixerGroup = mixerChannel;
            _audio2.outputAudioMixerGroup = mixerChannel;

            SetSpatialBlend();

            SpatializerHelper.TurnOnSpatializerIfEnabled(_audio1);
            SpatializerHelper.TurnOnSpatializerIfEnabled(_audio2);

            _curFadeMode = FadeMode.None;
            _fadeCompleteCallback = null;
            _lostFocus = false;
        }

        /*! \cond PRIVATE */
        public void SetSpatialBlend() {
            if (MasterAudio.SafeInstance == null) {
                return;
            }

#if DISABLE_3D_SOUND
            SetAudioSpatialBlend(MasterAudio.SpatialBlend_2DValue);
#else
            switch (MasterAudio.Instance.musicSpatialBlendType) {
                case MasterAudio.AllMusicSpatialBlendType.ForceAllTo2D:
                    SetAudioSpatialBlend(MasterAudio.SpatialBlend_2DValue);
                    break;
                case MasterAudio.AllMusicSpatialBlendType.ForceAllTo3D:
                    SetAudioSpatialBlend(MasterAudio.SpatialBlend_3DValue);
                    break;
                case MasterAudio.AllMusicSpatialBlendType.ForceAllToCustom:
                    SetAudioSpatialBlend(MasterAudio.Instance.musicSpatialBlend);
                    break;
                case MasterAudio.AllMusicSpatialBlendType.AllowDifferentPerController:
                    switch (spatialBlendType) {
                        case MasterAudio.ItemSpatialBlendType.ForceTo2D:
                            SetAudioSpatialBlend(MasterAudio.SpatialBlend_2DValue);
                            break;
                        case MasterAudio.ItemSpatialBlendType.ForceTo3D:
                            SetAudioSpatialBlend(MasterAudio.SpatialBlend_3DValue);
                            break;
                        case MasterAudio.ItemSpatialBlendType.ForceToCustom:
                            SetAudioSpatialBlend(spatialBlend);
                            break;
                        case MasterAudio.ItemSpatialBlendType.UseCurveFromAudioSource:
                            // do nothing! Keep settings on Audio Source
                            break;
                    }

                    break;
            }
#endif
        }
        /*! \endcond */

        private void DetectAndRescheduleNextGaplessSongIfOff()
        {
            var ma = MasterAudio.SafeInstance;
            if (ma == null || !ma.useGaplessAutoReschedule)
            {
                return;
            }

            // detect if  we need to re-schedule the next gapless song because it's "off" due to changed pitch or time jumping via code after it started.
            if (!CanSchedule 
                || CurrentPlaylistSource.loop 
                || !_currentSchedSongDspStartTime.HasValue 
                || _scheduledSongOffsetByAudioSource.Count == 0) {

                _nextScheduledTimeRecalcStart = null;
                _nextScheduleTimeRecalcDifferentFirstFrameNum = 0;
                return;
            }

            var recalcTimeToPlayNextSong = AudioSettings.dspTime + ((double)_activeAudio.clip.samples / _activeAudio.clip.frequency) - ((double)_activeAudio.timeSamples / _activeAudio.clip.frequency);
            var roundedRecalcTime = Math.Round(recalcTimeToPlayNextSong, 7);
            var roundedCurrentTime = Math.Round(_currentSchedSongDspStartTime.Value, 7);
            if (roundedRecalcTime == roundedCurrentTime)
            {
                _nextScheduledTimeRecalcStart = null;
                _nextScheduleTimeRecalcDifferentFirstFrameNum = 0;
                return;
            }

            if (!_nextScheduledTimeRecalcStart.HasValue)
            {
                _nextScheduledTimeRecalcStart = recalcTimeToPlayNextSong;
                _nextScheduleTimeRecalcDifferentFirstFrameNum = Time.frameCount;
            }

            var roundedSchedTime = Math.Round(_nextScheduledTimeRecalcStart.Value, 7);
            var isCloseEnoughToBeCalledSame = roundedSchedTime == roundedRecalcTime;

            if (!isCloseEnoughToBeCalledSame)
            {
                _nextScheduledTimeRecalcStart = null;
                _nextScheduleTimeRecalcDifferentFirstFrameNum = 0;
                return;
            }

            if (_nextScheduleTimeRecalcDifferentFirstFrameNum + NextScheduleTimeRecalcConsecutiveFrameCount <= Time.frameCount)
            {
                _audioClip.Stop();  // stop the previous scheduled play

                ScheduleClipPlay(recalcTimeToPlayNextSong, _transitioningAudio, false, false);
            }
        }

        private MusicSetting FindSongByAliasOrName(string clipName) {
            var setting = _currentPlaylist.MusicSettings.Find(delegate (MusicSetting obj) {
                return obj.alias == clipName;
            });

            if (setting == null) {
                setting = _currentPlaylist.MusicSettings.Find(delegate (MusicSetting obj) {
                    if (obj.audLocation == MasterAudio.AudioLocation.Clip) {
                        return obj.clip != null && obj.clip.name == clipName;
                    } 
                    if (obj.audLocation == MasterAudio.AudioLocation.ResourceFile) {
                        return obj.resourceFileName == clipName;
                    } 
#if ADDRESSABLES_ENABLED
                    return false; // must use alias
#else
                    return false;
#endif
                    
                });
            }

            return setting;
        }

        private void SetAudiosIfEmpty() {
            var audios = GetComponents<AudioSource>();

            _audio1 = audios[0];
            _audio2 = audios[1];
        }

        private void SetAudioSpatialBlend(float blend) {
            if (_audio1 == null) {
                SetAudiosIfEmpty();
            }

            // ReSharper disable once PossibleNullReferenceException
            _audio1.spatialBlend = blend;
            _audio2.spatialBlend = blend;
        }

        // Use this for initialization 
        // ReSharper disable once UnusedMember.Local
        private void Start() {
            if (ControllerIsReady) {
                // already called by lazy load.
                return;
            }

            if (MasterAudio.SafeInstance == null) {
                // abort, there's no MA game object.
                Debug.LogError("No Master Audio game object exists in the Hierarchy. Aborting Playlist Controller setup code.");
                return;
            }

            if (!string.IsNullOrEmpty(startPlaylistName) && _currentPlaylist == null) {
                // fill up randomizer

                InitializePlaylist();
            }

            ControllerIsReady = true;

            if (initializedEventExpanded && initializedCustomEvent != string.Empty && initializedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(initializedCustomEvent, Trans, false);
            }

            AutoStartPlaylist();

            if (IsMuted) {
                MutePlaylist();
            }
        }

        private void AutoStartPlaylist() {
            if (_currentPlaylist == null || !startPlaylistOnAwake || !IsFrameFastEnough || _autoStartedPlaylist) {
                return;
            }

            // don't do if the frame was huge because the sync to next song will be off.
            PlayNextOrRandom(AudioPlayType.PlayNow);
            _autoStartedPlaylist = true;
        }

        private void CoUpdate() {
            if (MasterAudio.SafeInstance == null) {
                // abort, there's no MA game object.
                return;
            }

            // gradual fade code
            if (_curFadeMode != FadeMode.GradualFade) {
                return;
            }

            if (_activeAudio == null) {
                return; // paused or error in setup
            }

            var timeFractionElapsed = 1f - ((_slowFadeCompletionTime - AudioUtil.Time) / (_slowFadeCompletionTime - _slowFadeStartTime));

            timeFractionElapsed = Math.Min(timeFractionElapsed, 1f);
            timeFractionElapsed = Math.Max(timeFractionElapsed, 0f);

            var newVolume = _slowFadeStartVolume + ((_slowFadeTargetVolume - _slowFadeStartVolume) * timeFractionElapsed);

            if (_slowFadeTargetVolume > _slowFadeStartVolume) {
                newVolume = Math.Min(newVolume, _slowFadeTargetVolume);
            } else {
                newVolume = Math.Max(newVolume, _slowFadeTargetVolume);
            }

            _playlistVolume = newVolume;

            UpdateMasterVolume();

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (AudioUtil.Time < _slowFadeCompletionTime) {
                return;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (MasterAudio.Instance.stopZeroVolumePlaylists && _slowFadeTargetVolume == 0f) {
                StopPlaylist();
            }

            if (_fadeCompleteCallback != null) {
                _fadeCompleteCallback();
                _fadeCompleteCallback = null;
            }
            _curFadeMode = FadeMode.None;
            // ReSharper disable once FunctionNeverReturns
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEnable() {
            _instances = null; // in case you have a new Controller in the next Scene, we need to uncache the list.

            MasterAudio.TrackRuntimeAudioSources(new List<AudioSource> { _audio1, _audio2 });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable() {
            _instances = null; // in case you have a new Controller in the next Scene, we need to uncache the list.

            if (MasterAudio.SafeInstance == null || MasterAudio.AppIsShuttingDown) {
                return;
            }

            if (ActiveAudioSource != null && ActiveAudioSource.clip != null && !_willPersist) {
                StopPlaylist();
            }

            MasterAudio.StopTrackingRuntimeAudioSources(new List<AudioSource> { _audio1, _audio2 });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnApplicationPause(bool pauseStatus) {
            _lostFocus = pauseStatus;
        }

        // ReSharper disable once UnusedMember.Local
        private void Update() {
            _frames++;

            CoUpdate();

            if (_lostFocus || !ControllerIsReady) {
                return; // don't accidentally stop the song below if we just lost focus.
            }

            AutoStartPlaylist(); // in case it didn't happen in Start due to slowness.

            if (_activeAudio.isPlaying) {
                framesOfSongPlayed++;
            }

            if (IsCrossFading) {
                // cross-fade code
                if (_activeAudio.volume >= _activeAudioEndVolume) {
                    CeaseAudioSource(_transitioningAudio);
                    IsCrossFading = false;
                    if (CanSchedule && !_nextSongScheduled) {
                        // this needs to run if using crossfading > 0 seconds, because it will not schedule during cross fading (it would kill the crossfade).
                        PlayNextOrRandom(AudioPlayType.Schedule);
                    }
                    SetDuckProperties(); // they now should read from a new audio source
                }

                var workingCrossFade = Math.Max(CrossFadeTime, .001f);
                var ratioPassed = Mathf.Clamp01((Time.realtimeSinceStartup - _crossFadeStartTime) / workingCrossFade);

                _activeAudio.volume = ratioPassed * _activeAudioEndVolume;
                _transitioningAudio.volume = _transitioningAudioStartVolume * (1 - ratioPassed);
                // end cross-fading code
            }

            if (!_activeAudio.loop && _activeAudio.clip != null) {
                if (AudioUtil.IsClipPaused(_activeAudio)) { // 7/12/2017, changed this if before the next one (.isPlaying) because paused tracks were stopping.
                                                            // do not auto-advance if the audio is paused.
                    goto AfterAutoAdvance;
                }

                if (!_activeAudio.isPlaying) {
                    if (!IsAutoAdvance) {
                        FirePlaylistEndedEventIfAny();
                        CeaseAudioSource(_activeAudio); // this will release the resources if not auto-advance
                        return;
                    }

                    // is auto-advance. 
                    var hasNoNextSong = false;
                    if (isShuffle) {
                        hasNoNextSong = _clipsRemaining.Count == 0;
                    } else {
                        hasNoNextSong = _currentSequentialClipIndex >= _currentPlaylist.MusicSettings.Count;
                    }

                    if (hasNoNextSong && !_activeAudio.isPlaying) { // 8/21/2017, the last song would not unload because it was never detected.
                        FirePlaylistEndedEventIfAny();
                        CeaseAudioSource(_activeAudio); // this will release the resources
                        return;
                    }
                }

                var shouldAdvance = false;

                if (ShouldNotSwitchEarly) { // keep default of shouldAdvance = false. Special logic, do not try to fade in early!
                    //can only be set to true if scheduled song has started
                    if (_currentSchedSongDspStartTime.HasValue && AudioSettings.dspTime > _currentSchedSongDspStartTime.Value) {
                        shouldAdvance = true;
                    }
                } else if (PlaylistState == PlaylistStates.Stopped) {
                    shouldAdvance = true;
                    // this will advance even if the code below didn't and the clip stopped due to excessive lag.
                } else if (IsFrameFastEnough) { // if slow, do not bother with super slow frames because it will try to trigger next song at the wrong time.
                    var currentClipTime = _activeAudio.clip.length - _activeAudio.time - AudioUtil.AdjustEndLeadTimeForPitch(CrossFadeTime, _activeAudio);
                    var clipFadeStartTime = AudioUtil.AdjustEndLeadTimeForPitch(AudioUtil.FrameTime * FramesEarlyToTrigger, _activeAudio);
                    shouldAdvance = currentClipTime <= clipFadeStartTime;
                }

                if (shouldAdvance) {
                    // time to cross fade or fade out
                    if (_currentPlaylist.fadeOutLastSong) {
                        if (isShuffle) {
                            if (_clipsRemaining.Count == 0 || !IsAutoAdvance) {
                                FadeOutPlaylist();
                                return;
                            }
                        } else {
                            if (_currentSequentialClipIndex >= _currentPlaylist.MusicSettings.Count ||
                                _currentPlaylist.MusicSettings.Count == 1 || !IsAutoAdvance) {
                                FadeOutPlaylist();
                                return;
                            }
                        }
                    }

                    if (IsAutoAdvance) {
                        if (!_nextSongRequested && (_lastTimeSongRequested + MinSongLength <= AudioUtil.Time)) {
                            _lastTimeSongRequested = AudioUtil.Time;
                            if (CanSchedule) {
                                _lastSongPosition = null;
                                FadeInScheduledSong();
                            } else {
                                _lastSongPosition = 0;
                                PlayNextOrRandom(AudioPlayType.PlayNow);
                            }
                        }
                    }
                } else {
                    DetectAndRescheduleNextGaplessSongIfOff();
                }
            }

            if (_activeAudio.loop && _activeAudio.clip != null) {
                if (_activeAudio.timeSamples < _lastFrameSongPosition) {
                    var songName = _activeAudio.clip.name;
                    if (SongLooped != null && !string.IsNullOrEmpty(songName)) {
                        SongLooped(songName);
                    }

                    if (songLoopedEventExpanded && songLoopedCustomEvent != string.Empty && songLoopedCustomEvent != MasterAudio.NoGroupName) {
                        MasterAudio.FireCustomEvent(songLoopedCustomEvent, Trans, false);
                    }
                }

                _lastFrameSongPosition = _activeAudio.timeSamples;
            }

            AfterAutoAdvance:

            if (IsCrossFading) {
                return;
            }

            AudioDucking();
        }

#endregion

#region public methods

        /// <summary>
        /// This method returns a reference to the PlaylistController whose name you specify. This is necessary when you have more than one.
        /// </summary>
        /// <param name="playlistControllerName">Name of Playlist Controller</param>
        /// <param name="errorIfNotFound">Defaults to true. Pass false if you don't want an error in the console when not found.</param>
        /// <returns></returns>
        public static PlaylistController InstanceByName(string playlistControllerName, bool errorIfNotFound = true) {
            var match = Instances.Find(delegate (PlaylistController obj) {
                return obj != null &&
                       obj.ControllerName ==
                       playlistControllerName;
            });

            if (match != null) {
                return match;
            }

            if (errorIfNotFound) {
                Debug.LogError("Could not find Playlist Controller '" + playlistControllerName + "'.");
            }
            return null;
        }

        /// <summary>
        /// This method will tell you if the song you specify by name is playing or not. If it's the current song and paused, this will still return true.
        /// </summary>
        /// <returns><c>true</c> if this instance is song playing the specified songName; otherwise, <c>false</c>.</returns>
        /// <param name="songName">Song name or alias. Here you pass the name of the clip or its alias to check.</param>
        public bool IsSongPlaying(string songName) {
            if (!HasPlaylist) {
                return false;
            }

            if (ActiveAudioSource == null || ActiveAudioSource.clip == null) {
                return false;
            }

            if (ActiveAudioSource.clip.name == songName) {
                return true;
            }

            return _activeSongAlias == songName;
        }

        /// <summary>
        /// Call this method to clear all songs out of the queued songs list.
        /// </summary>
        public void ClearQueue() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            _queuedSongs.Clear();
        }

        /// <summary>
        /// This method mutes the Playlist if it's not muted, and vice versa.
        /// </summary>
        public void ToggleMutePlaylist() {
            if (Application.isPlaying && !ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (IsMuted) {
                UnmutePlaylist();
            } else {
                MutePlaylist();
            }
        }

        /// <summary>
        /// This method mutes the Playlist.
        /// </summary>
        public void MutePlaylist() {
            PlaylistIsMuted = true;
        }

        /// <summary>
        /// This method unmutes the Playlist.
        /// </summary>
        public void UnmutePlaylist() {
            PlaylistIsMuted = false;
        }

        /// <summary>
        /// This method will pause the Playlist.
        /// </summary>
        public void PausePlaylist() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_activeAudio == null || _transitioningAudio == null) {
                return;
            }

            if (_activeAudio.clip != null) {
                _activeAudio.Pause();
            }

            if (!_songPauseTime.HasValue) { // in case you call pause twice before unpausing. 
                _songPauseTime = AudioSettings.dspTime;
            }

            if (_transitioningAudio.clip != null) {
                _transitioningAudio.Pause(); // previously scheduled gapless next song should not start during pause.
            }
        }

        /// <summary>
        /// This method will unpause the Playlist.
        /// </summary>
        public bool UnpausePlaylist() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                _songPauseTime = null;
                return false;
            }

            // if the playlist is currently playing or stopped, we are good, exit out. Don't want to play a stopped one.
            if (PlaylistState == PlaylistStates.Playing || PlaylistState == PlaylistStates.Crossfading || PlaylistState == PlaylistStates.Stopped) {
                _songPauseTime = null;
                return false;
            }

            if (_activeAudio == null || _transitioningAudio == null) {
                _songPauseTime = null;
                return false;
            }

            if (_activeAudio.clip == null && _currentPlaylist != null) {
                // if we have a playlist defined (through the MA prefab), but have not started actually playing a track from this list
                // the _activeAudio.clip is null. This causes unpause to fail
                // if we have a playlist defined, and tell this to resume playing, we should start from the first track.
                FinishPlaylistInit();
                _songPauseTime = null;
                return true;
            }

            if (_activeAudio.clip == null) {
                _songPauseTime = null;
                return false;
            }

            if (!_scheduledSongOffsetByAudioSource.ContainsKey(_activeAudio)) {
                _activeAudio.Play();
                framesOfSongPlayed = 0;
                AudioUtil.ClipPlayed(_activeAudio.clip, GameObj);
            } else if (_songPauseTime.HasValue && _currentSchedSongDspStartTime.HasValue) {
                var unpauseTime = AudioSettings.dspTime;
                var timePaused = unpauseTime - _songPauseTime.Value;

                // schedule ahead by that much.
                var newSongStartOffset = _currentSchedSongDspStartTime.Value - AudioSettings.dspTime + timePaused;

                _songPauseTime = null;

                _activeAudio.Stop(); // stop the previous scheduled play

                ScheduleClipPlay(newSongStartOffset, _activeAudio, true);
            }

            if (!_scheduledSongOffsetByAudioSource.ContainsKey(_transitioningAudio)) {
                _transitioningAudio.Play();
                AudioUtil.ClipPlayed(_transitioningAudio.clip, GameObj);
            } else if (_songPauseTime.HasValue && _currentSchedSongDspStartTime.HasValue) {
                var unpauseTime = AudioSettings.dspTime;
                var timePaused = unpauseTime - _songPauseTime.Value;

                // schedule ahead by that much.
                var newSongStartOffset = _currentSchedSongDspStartTime.Value - AudioSettings.dspTime + timePaused;

                _songPauseTime = null;

                _transitioningAudio.Stop(); // stop the previous scheduled play

                ScheduleClipPlay(newSongStartOffset, _transitioningAudio, true);
            }

            return true;
        }

        /// <summary>
        /// This method will Stop the Playlist. 
        /// </summary>
        public void StopPlaylist(bool onlyFadingClip = false) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (!Application.isPlaying) {
                return;
            }

            _currentSchedSongDspStartTime = null;
            _currentSchedSongDspEndTime = null;
            _currentSong = null;

            switch (PlaylistState) {
                case PlaylistStates.NotInScene:
                case PlaylistStates.Stopped:
                    return; // no need to stop anything.
            }

            if (!onlyFadingClip) {
                CeaseAudioSource(_activeAudio);
            }

            CeaseAudioSource(_transitioningAudio);

            if (!onlyFadingClip && _clipsRemaining.Count == 0 && PlaylistEnded != null) {
                PlaylistEnded();
            }
        }

        /// <summary>
        /// This method allows you to fade the Playlist to a specified volume over X seconds.
        /// </summary>
        /// <param name="targetVolume">The volume to fade to.</param>
        /// <param name="fadeTime">The amount of time to fully fade to the target volume.</param>
        /// <param name="callback">Optional callback method</param>
        // ReSharper disable once RedundantNameQualifier
        public void FadeToVolume(float targetVolume, float fadeTime, System.Action callback = null) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (fadeTime <= MasterAudio.InnerLoopCheckInterval) {
                _playlistVolume = targetVolume;
                UpdateMasterVolume();
                _curFadeMode = FadeMode.None; // in case another fade is happening, stop it!
                return;
            }

            _curFadeMode = FadeMode.GradualFade;

            if (_duckingMode == AudioDuckingMode.NotDucking) {
                _slowFadeStartVolume = _playlistVolume;
            } else {
                _slowFadeStartVolume = _activeAudio.volume;
            }

            _slowFadeTargetVolume = targetVolume;
            _slowFadeStartTime = AudioUtil.Time;
            _slowFadeCompletionTime = AudioUtil.Time + fadeTime;

            _fadeCompleteCallback = callback;
        }

        /// <summary>
        /// This method will play a random song in the current Playlist.
        /// </summary>
        public void PlayRandomSong() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            PlayARandomSong(AudioPlayType.PlayNow);
        }

        /*! \cond PRIVATE */
        public void PlayARandomSong(AudioPlayType playType) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_clipsRemaining.Count == 0) {
                Debug.LogWarning(
                    "There are no clips left in this Playlist. Turn on Loop Playlist if you want to loop the entire song selection.");
                return;
            }

            if (IsCrossFading && playType == AudioPlayType.Schedule) {
                return; // this will kill the crossfade, so abort
            }

            var isMidsong = framesOfSongPlayed > 0;

            if (isMidsong) {
                _nextSongScheduled = false;
            }

            var randIndex = UnityEngine.Random.Range(0, _clipsRemaining.Count);
            var clipIndex = _clipsRemaining[randIndex];

            switch (playType) {
                case AudioPlayType.PlayNow:
                    RemoveRandomClip(randIndex);
                    break;
                case AudioPlayType.Schedule:
                    _lastRandomClipIndex = randIndex;
                    break;
                case AudioPlayType.AlreadyScheduled:
                    if (_lastRandomClipIndex >= 0) {
                        RemoveRandomClip(_lastRandomClipIndex);
                    }
                    break;
            }

            PlaySong(_currentPlaylist.MusicSettings[clipIndex], playType);
        }
        /*! \endcond */

        private void RemoveRandomClip(int randIndex) {
            _clipsRemaining.RemoveAt(randIndex);
            if (loopPlaylist && _clipsRemaining.Count == 0) {
                FillClips();
            }
        }

        private void PlayFirstQueuedSong(AudioPlayType playType) {
            if (_queuedSongs.Count == 0) {
                Debug.LogWarning("There are zero queued songs in PlaylistController '" + ControllerName +
                                 "'. Cannot play first queued song.");
                return;
            }

            var oldestQueued = _queuedSongs[0];
            _queuedSongs.RemoveAt(0); // remove before playing so the queued song can loop.

            _currentSequentialClipIndex = oldestQueued.songIndex;
            // keep track of which song we're playing so we don't loop playlist if it's not supposed to.
            PlaySong(oldestQueued, playType);
        }

        /// <summary>
        /// This method will play the next song in the current Playlist.
        /// </summary>
        public void PlayNextSong() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            PlayTheNextSong(AudioPlayType.PlayNow);
        }

        /*! \cond PRIVATE */
        public void PlayTheNextSong(AudioPlayType playType) {
            if (_currentPlaylist == null) {
                return;
            }

            if (IsCrossFading && playType == AudioPlayType.Schedule) {
                return; // this will kill the crossfade, so abort
            }

            if (playType != AudioPlayType.AlreadyScheduled && _songsPlayedFromPlaylist > 0 && !_nextSongScheduled) {
                AdvanceSongCounter();
            }

            if (_currentSequentialClipIndex >= _currentPlaylist.MusicSettings.Count) {
                Debug.LogWarning("There are no clips left in this Playlist. Turn on Loop Playlist if you want to loop the entire song selection.");
                return;
            }

            var isMidsong = framesOfSongPlayed > 0;

            if (isMidsong) {
                _nextSongScheduled = false;
                _lastSongPosition = ActiveAudioSource.timeSamples;
            }

            PlaySong(_currentPlaylist.MusicSettings[_currentSequentialClipIndex], playType);
        }
        /*! \endcond */

        private void AdvanceSongCounter() {
            _currentSequentialClipIndex++;

            if (_currentSequentialClipIndex < _currentPlaylist.MusicSettings.Count) {
                return;
            }
            if (loopPlaylist) {
                _currentSequentialClipIndex = 0;
            }
        }

        /// <summary>
        /// This method will end the looping of the current song and cancel any scheduled / queued songs. Also turns off auto-advance if it's on to accomplish this.
        /// </summary>
        public void StopPlaylistAfterCurrentSong() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_currentPlaylist == null) {
                MasterAudio.LogNoPlaylist(ControllerName, "StopPlaylistAfterCurrentSong");
                return;
            }

            if (!_activeAudio.isPlaying) {
                Debug.Log("No song is currently playing.");
                return;
            }

            // turn off loop if it's on.
            _activeAudio.loop = false;

            _queuedSongs.Clear();

            isAutoAdvance = false;

            if (_scheduledSongOffsetByAudioSource.ContainsKey(_activeAudio)) {
                CeaseAudioSource(_activeAudio);
            }

            if (_scheduledSongOffsetByAudioSource.ContainsKey(_transitioningAudio)) {
                CeaseAudioSource(_transitioningAudio);
            }
        }

        /// <summary>
        /// This method will end the looping of the current song so the next can play when it's done.
        /// </summary>
        public void StopLoopingCurrentSong() {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_currentPlaylist == null) {
                MasterAudio.LogNoPlaylist(ControllerName, "StopLoopingCurrentSong");
                return;
            }

            if (!_activeAudio.isPlaying) {
                Debug.Log("No song is currently playing.");
                return;
            }

            // turn off loop if it's on.
            _activeAudio.loop = false;

            if (CanSchedule && _queuedSongs.Count == 0) {
                ScheduleNextSong();
            }
        }

        /// <summary>
        /// This method will play the song in the current Playlist whose name you specify as soon as the currently playing song is done. The current song, if looping, will have loop turned off by this call. This requires auto-advance to work.
        /// </summary>
        /// <param name="clipName">The name or alias of the song to play.</param>
        /// <param name="scheduleNow">Whether the song should immediately be scheduled to play (when Gapless song transitions is enabled)</param>
        public void QueuePlaylistClip(string clipName, bool scheduleNow = true) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_currentPlaylist == null) {
                MasterAudio.LogNoPlaylist(ControllerName, "QueuePlaylistClip");
                return;
            }

            if (!_activeAudio.isPlaying) {
                TriggerPlaylistClip(clipName);
                return;
            }

            var setting = FindSongByAliasOrName(clipName);

            if (setting == null) {
                Debug.LogWarning("Could not find clip '" + clipName + "' in current Playlist in '" + ControllerName +
                                 "'. If you are using Addressables, try assigning an alias to the song and use the alias when specifying the song to play.");
                return;
            }

            // turn off loop if it's on.
            _activeAudio.loop = false;
            // add to queue.
            _queuedSongs.Add(setting);

            if (CanSchedule && scheduleNow) {
                PlayNextOrRandom(AudioPlayType.Schedule);
            }
        }

        /// <summary>
        /// This method will play the song in the current Playlist whose name you specify.
        /// </summary>
        /// <param name="clipName">The name of the song to play.</param>
        /// <returns>bool - whether the song was played or not</returns>
        public bool TriggerPlaylistClip(string clipName) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return false;
            }

            if (_currentPlaylist == null) {
                MasterAudio.LogNoPlaylist(ControllerName, "TriggerPlaylistClip");
                return false;
            }

            // search by alias first!
            var setting = FindSongByAliasOrName(clipName);

            if (setting == null) {
                Debug.LogWarning("Could not find clip '" + clipName + "' in current Playlist in '" + ControllerName +
                                 "'.");
                return false;
            }

            _nextSongScheduled = false; // this should make sure that we don't play the same song twice in a row by failing to increment the song number.
            _currentSequentialClipIndex = setting.songIndex;

            // removed AdvanceSongCounter call here as it caused an error.
            PlaySong(setting, AudioPlayType.PlayNow);

            return true;
        }

        /*! \cond PRIVATE */
        public void EndDucking(SoundGroupVariationUpdater actorUpdater)
        {
            if (_actorUpdater != actorUpdater)
            {
                return;
            }

            if (_duckingMode != AudioDuckingMode.Ducked)
            {
                return;
            }

            // begin unduck
            _timeToStartUnducking = AudioUtil.Time;

            var duckFinishTime = _timeToStartUnducking + _unduckTime;
             
            _timeToFinishUnducking = duckFinishTime;
        }

        public void DuckMusicForTime(SoundGroupVariationUpdater actorUpdater, float duckLength, float unduckTime, float pitch, float duckedTimePercentage, float duckedVolCut) {
            if (MasterAudio.IsWarming) {
                return;
            }

            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (IsCrossFading) {
                return; // no ducking during cross-fading, it screws up calculations.
            }

            _actorUpdater = actorUpdater;
            var rangedDuck = AudioUtil.AdjustAudioClipDurationForPitch(duckLength, pitch);
            _unduckTime = unduckTime;

            _currentDuckVolCut = duckedVolCut; // store for later usage

            var origDb = AudioUtil.GetDbFromFloatVolume(_originalMusicVolume);
            var targetVolumeDb = origDb + duckedVolCut;
            var targetVolumeNormal = AudioUtil.GetFloatVolumeFromDb(targetVolumeDb);

            _initialDuckVolume = targetVolumeNormal;
            _duckRange = _originalMusicVolume - targetVolumeNormal;

            _duckingMode = AudioDuckingMode.SetToDuck;
            _timeToStartUnducking = AudioUtil.Time + (rangedDuck * duckedTimePercentage);

            var duckFinishTime = _timeToStartUnducking + unduckTime;
            if (duckFinishTime > AudioUtil.Time + rangedDuck) {
                duckFinishTime = AudioUtil.Time + rangedDuck;
            }

            _timeToFinishUnducking = duckFinishTime;
        }
        /*! \endcond */

        private void InitControllerIfNot() {
            if (ControllerIsReady) {
                return;
            }

            Awake();
            Start();
        }

        /// <summary>
        /// This method is used to update state based on the Playlist Master Volume.
        /// </summary>
        public void UpdateMasterVolume() {
            if (!Application.isPlaying) {
                return;
            }

            InitControllerIfNot();

            if (_currentSong != null) {
                var newVolume = _currentSong.volume * PlaylistVolume;

                if (!IsCrossFading) {
                    if (_activeAudio != null) {
                        _activeAudio.volume = newVolume;
                    }

                    if (_transitioningAudio != null) {
                        _transitioningAudio.volume = newVolume;
                    }
                }

                _activeAudioEndVolume = newVolume;
            }

            SetDuckProperties();
        }

        /// <summary>
        /// This method is used to start a Playlist whether it's already loaded and playing or not.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to start</param>
        /// <param name="clipName"><b>Optional</b> - The name of the specific clip to play.</param>

        public void StartPlaylist(string playlistName, string clipName = null) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_currentPlaylist != null && _currentPlaylist.playlistName == playlistName) {
                RestartPlaylist(clipName);
            } else {
                ChangePlaylist(playlistName, true, clipName);
            }
        }

        /// <summary>
        /// This method is used to change the current Playlist to a new one, and optionally start it playing.
        /// </summary>
        /// <param name="playlistName">The name of the Playlist to start</param>
        /// <param name="playFirstClip">Defaults to true. Whether to start the first song or not.</param>
        /// <param name="clipName"><b>Optional</b> - Name of the specific clip to play.</param>
        public void ChangePlaylist(string playlistName, bool playFirstClip = true, string clipName = null) {
            InitControllerIfNot();

            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            if (_currentPlaylist != null && _currentPlaylist.playlistName == playlistName) {
                Debug.LogWarning("The Playlist '" + playlistName +
                                 "' is already loaded. Ignoring Change Playlist request.");
                return;
            }

            startPlaylistName = playlistName;
            FinishPlaylistInit(playFirstClip, clipName);
        }

        private void FinishPlaylistInit(bool playFirstClip = true, string clipName = null) {
            if (IsCrossFading) {
                StopPlaylist(true);
            }

            InitializePlaylist();

            if (!Application.isPlaying) {
                return;
            }

            _queuedSongs.Clear();
            
            if (!string.IsNullOrEmpty(clipName)) {
                var setting = FindSongByAliasOrName(clipName);
                if (setting != null) {
                    _queuedSongs.Add(setting);
                }
            }
            
            if (playFirstClip) {
                PlayNextOrRandom(AudioPlayType.PlayNow);
            }
        }

        /// <summary>
        /// This method can be called to restart the current Playlist
        /// </summary>
        /// <param name="clipName"><b>Optional</b> - The clip name to play</param>
        public void RestartPlaylist(string clipName = null) {
            if (!ControllerIsReady) {
                Debug.LogError(NotReadyMessage);
                return;
            }

            FinishPlaylistInit(true, clipName);
        }

#endregion

#region Helper methods

        private void CheckIfPlaylistStarted() {
            if (_songsPlayedFromPlaylist > 0) {
                return;
            }

            if (playlistStartedEventExpanded && playlistStartedCustomEvent != string.Empty && playlistStartedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(playlistStartedCustomEvent, Trans);
            }
        }

        private PlaylistController FindOtherControllerInSameSyncGroup() {
            if (syncGroupNum <= 0 || _currentPlaylist.songTransitionType != MasterAudio.SongFadeInPosition.SynchronizeClips) {
                return null;
            }

            var firstMatchingGroupController = Instances.Find(delegate (PlaylistController obj) {
                return obj != this &&
                       obj.syncGroupNum == syncGroupNum &&
                       obj.ActiveAudioSource != null &&
                       obj.ActiveAudioSource.isPlaying;
            });

            return firstMatchingGroupController;
        }

        private void FadeOutPlaylist() {
            if (_curFadeMode == FadeMode.GradualFade) {
                return;
            }

            var volumeBeforeFade = _playlistVolume;

            FadeToVolume(0f, CrossFadeTime, delegate {
                StopPlaylist();
                _playlistVolume = volumeBeforeFade;
            });
        }

        private void InitializePlaylist() {
            FillClips();
            _songsPlayedFromPlaylist = 0;
            _currentSequentialClipIndex = 0;
            _nextSongScheduled = false;
            _lastRandomClipIndex = -1;
        }

        private void PlayNextOrRandom(AudioPlayType playType) {
            _nextSongRequested = true;

            if (_queuedSongs.Count > 0) {
                PlayFirstQueuedSong(playType);
            } else if (!isShuffle) {
                PlayTheNextSong(playType);
            } else {
                PlayARandomSong(playType);
            }
        }

        private void FirePlaylistEndedEventIfAny() {
            if (playlistEndedEventExpanded && playlistEndedCustomEvent != string.Empty && playlistStartedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(playlistEndedCustomEvent, Trans);
            }
        }

        private void FillClips() {
            _clipsRemaining.Clear();

            // add clips from named playlist.
            if (startPlaylistName == MasterAudio.NoPlaylistName) {
                return;
            }

            _currentPlaylist = MasterAudio.GrabPlaylist(startPlaylistName);

            if (_currentPlaylist == null) {
                return;
            }

            for (var i = 0; i < _currentPlaylist.MusicSettings.Count; i++) {
                var aSong = _currentPlaylist.MusicSettings[i];
                aSong.songIndex = i;

                switch (aSong.audLocation) {
                    case MasterAudio.AudioLocation.Clip:
                        if (aSong.clip == null) {
                            continue;
                        }
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        if (string.IsNullOrEmpty(aSong.resourceFileName)) {
                            continue;
                        }
                        break;
#if ADDRESSABLES_ENABLED
                    case MasterAudio.AudioLocation.Addressable:
                        if (!AudioAddressableOptimizer.IsAddressableValid(aSong.audioClipAddressable)) {
                            continue;
                        }
                        break;
#endif
                    default:
                        break;
                }

                _clipsRemaining.Add(i);
            }
        }

        private void PlaySong(MusicSetting setting, AudioPlayType playType) {
            _newSongSetting = setting;

            if (_activeAudio == null) {
                Debug.LogError("PlaylistController prefab is not in your scene. Cannot play a song.");
                return;
            }

            AudioClip clipToPlay = null;

            var clipWillBeAudibleNow = playType == AudioPlayType.PlayNow || playType == AudioPlayType.AlreadyScheduled;

            if (clipWillBeAudibleNow) {
                _lastFrameSongPosition = -1;
            }

            if (clipWillBeAudibleNow && _currentSong != null && !CanSchedule) {
                if (_currentSong.songChangedEventExpanded && _currentSong.songChangedCustomEvent != string.Empty &&
                    _currentSong.songChangedCustomEvent != MasterAudio.NoGroupName) {
                    MasterAudio.FireCustomEvent(_currentSong.songChangedCustomEvent, Trans);
                }
            }

            if (playType != AudioPlayType.AlreadyScheduled) {
                if (_activeAudio.clip != null) {
                    var newSongName = string.Empty;
                    switch (setting.audLocation) {
                        case MasterAudio.AudioLocation.Clip:
                            if (setting.clip != null) {
                                newSongName = setting.clip.name;
                            }
                            break;
                        case MasterAudio.AudioLocation.ResourceFile:
                            newSongName = setting.resourceFileName;
                            break;
#if ADDRESSABLES_ENABLED
                        case MasterAudio.AudioLocation.Addressable:
                            if (AudioAddressableOptimizer.IsAddressableValid(setting.audioClipAddressable)) {
                                newSongName = string.IsNullOrEmpty(setting.alias) ? "~Empty Alias Song~" : setting.alias;
                            }
                            break;
#endif
                    }

                    if (string.IsNullOrEmpty(newSongName)) {
                        Debug.LogWarning("The next song has no clip or Resource file assigned. Please fix. Ignoring song change request.");
                        return;
                    }
                }

                if (_activeAudio.clip == null) {
                    _audioClip = _activeAudio;
                    _transClip = _transitioningAudio;
                } else if (_transitioningAudio.clip == null) {
                    _audioClip = _transitioningAudio;
                    _transClip = _activeAudio;
                } else {
                    // both are busy!
                    _audioClip = _transitioningAudio;
                    _transClip = _activeAudio;
                }

                _audioClip.loop = SongShouldLoop(setting);

                switch (setting.audLocation) {
                    case MasterAudio.AudioLocation.Clip:
                        if (setting.clip == null) {
                            MasterAudio.LogWarning("MasterAudio will not play empty Playlist clip for PlaylistController '" + ControllerName + "'.");
                            return;
                        }

                        clipToPlay = setting.clip;
                        break;
                    case MasterAudio.AudioLocation.ResourceFile:
                        if (_resourceCoroutine != null) {
                            StopCoroutine(_resourceCoroutine);
                        }

                        _resourceCoroutine = StartCoroutine(AudioResourceOptimizer.PopulateResourceSongToPlaylistControllerAsync(setting, 
                            setting.resourceFileName, CurrentPlaylist.playlistName, this, playType));
                        break;
#if ADDRESSABLES_ENABLED
                    case MasterAudio.AudioLocation.Addressable:
                        if (_addressableCoroutine != null) {
                            StopCoroutine(_addressableCoroutine);
                        }

                        _addressableCoroutine = StartCoroutine(AudioAddressableOptimizer.PopulateAddressableSongToPlaylistControllerAsync(setting, 
                            setting.audioClipAddressable, this, playType));
                        break;
#endif
                }
            } else {
                FinishLoadingNewSong(setting, null, AudioPlayType.AlreadyScheduled);
            }

            if (clipToPlay != null) {
                FinishLoadingNewSong(setting, clipToPlay, playType);
            }
        }

        /*! \cond PRIVATE */
        public double? ScheduledGaplessNextSongStartTime() {
            if (!_scheduledSongOffsetByAudioSource.ContainsKey(_audioClip)) {
                return null;
            }

            return _scheduledSongOffsetByAudioSource[_audioClip];
        }

        // ReSharper disable once FunctionComplexityOverflow
        public void FinishLoadingNewSong(MusicSetting songSetting, AudioClip clipToPlay, AudioPlayType playType) {
            _nextSongRequested = false;

            var isScheduledPlay = playType == AudioPlayType.Schedule;
            var shouldPopulateClip = playType == AudioPlayType.PlayNow || isScheduledPlay;
            var clipWillBeAudibleNow = playType == AudioPlayType.PlayNow || playType == AudioPlayType.AlreadyScheduled;

            double? nextClipStartTime = null;

            if (shouldPopulateClip) {
                if (isScheduledPlay && CanSchedule) { // only needed for gapless, looping clip. We can't check if it's looping?
                    nextClipStartTime = CalculateNextTrackStartTimeOffset();
                }

                _audioClip.clip = clipToPlay;
                _audioClip.pitch = _newSongSetting.pitch;

#if ADDRESSABLES_ENABLED
                switch (songSetting.audLocation) {
                    case MasterAudio.AudioLocation.Addressable:
                        _loadedAddressablesByAudioSource[_audioClip] = songSetting.audioClipAddressable;
                        AudioAddressableOptimizer.AddAddressablePlayingClip(songSetting.audioClipAddressable, _audioClip);
                        break;
                }
#endif
 
                _activeSongAlias = songSetting.alias;
            }

            // set last known time for current song.
            if (_currentSong != null) {
                _currentSong.lastKnownTimePoint = _activeAudio.timeSamples;
                _currentSong.wasLastKnownTimePointSet = true;
            }

            if (clipWillBeAudibleNow) {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (CrossFadeTime == 0 || _transClip.clip == null) {
                    CeaseAudioSource(_transClip);

                    _audioClip.volume = _newSongSetting.volume * PlaylistVolume;

                    if (!ActiveAudioSource.isPlaying && _currentPlaylist != null && (_currentPlaylist.fadeInFirstSong && CrossFadeTime > 0f)) {
                        CrossFadeNow(_audioClip);
                    }
                } else {
                    CrossFadeNow(_audioClip);
                }

                SetDuckProperties();
            }

            var songTimeChanged = false;

            switch (playType) {
                case AudioPlayType.AlreadyScheduled:
                    // start crossfading now	
                    _nextSongScheduled = false;
                    RemoveScheduledClip();
                    break;
                case AudioPlayType.PlayNow:
                    if (_audioClip.clip != null && _audioClip.timeSamples >= _audioClip.clip.samples - 1) { // prevent the "index too far" error if you position past the end of clip.
                        _audioClip.timeSamples = 0;
                    }

                    _audioClip.Play(); // need to play before setting time or it sometimes resets back to zero.
                    framesOfSongPlayed = 0;
                    AudioUtil.ClipPlayed(_activeAudio.clip, GameObj);

                    CheckIfPlaylistStarted();
                    _songsPlayedFromPlaylist++;

                    // this sets the time to match for "synchronized"
                    var firstMatchingGroupController = FindOtherControllerInSameSyncGroup();

                    if (firstMatchingGroupController != null) {
                        var matchingTimeSamples = firstMatchingGroupController._activeAudio.timeSamples;
                        var matchingTime = firstMatchingGroupController._audioClip.time;
                        var hasEnoughTimeToMatchPosition = Math.Abs(_audioClip.clip.length - matchingTime) >= AudioUtil.FrameTime * FramesEarlyToBeSyncable; // 10 frames time (to catch any weirdness).

                        if (_audioClip.clip != null && matchingTimeSamples < _audioClip.clip.samples && hasEnoughTimeToMatchPosition) {
                            // align song starting to the same time as other song already playing in the same Sync Group
                            _audioClip.timeSamples = matchingTimeSamples;
                            songTimeChanged = true;
                        }
                    }
                    // end set time code
                    break;
                case AudioPlayType.Schedule:
                    // need to calculate for old, previously looping clip
                    if (!nextClipStartTime.HasValue) {
                        nextClipStartTime = CalculateNextTrackStartTimeOffset();
                    }

                    ScheduleClipPlay(nextClipStartTime.Value, _audioClip, false);

                    _nextSongScheduled = true;
                    CheckIfPlaylistStarted();
                    _songsPlayedFromPlaylist++;

                    break;
            }

            // this code will adjust the starting position of a song, but shouldn't do so when you first change Playlists.
            if (_currentPlaylist != null) {
                if (_songsPlayedFromPlaylist <= 1 && !songTimeChanged) {
                    _audioClip.timeSamples = 0;
                    // reset pointer so a new Playlist always starts at the beginning, but don't do it for synchronized! We need that first song to use the sync group.
                } else {
                    switch (_currentPlaylist.songTransitionType) {
                        case MasterAudio.SongFadeInPosition.SynchronizeClips:
                            if (!songTimeChanged) { // only do this for single Controllers. Otherwise the sync group code above will get defeated.
                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                if (playType == AudioPlayType.PlayNow) {
                                    // ReSharper disable once MergeConditionalExpression
                                    var newTimeSamples = _lastSongPosition.HasValue ? _lastSongPosition.Value : _activeAudio.timeSamples;
                                    if (_transitioningAudio.clip != null && newTimeSamples >= _transitioningAudio.clip.samples - 1) { // prevent the "index too far" error if you position past the end of clip.
                                        newTimeSamples = 0;
                                    }

                                    _lastSongPosition = null;

                                    _transitioningAudio.timeSamples = newTimeSamples;
                                } else {
                                    if (ShouldNotSwitchEarly) {
                                        // DO NOT set _transitioningAudio.timeSamples = 0, because it will "skip"
                                    } else {
                                        _transitioningAudio.timeSamples = 0;
                                    }
                                }
                            }
                            break;
                        case MasterAudio.SongFadeInPosition.NewClipFromLastKnownPosition:
                            var thisSongInPlaylist = _currentPlaylist.MusicSettings.Find(delegate (MusicSetting obj) {
                                return obj == _newSongSetting;
                            });

                            if (thisSongInPlaylist != null) {
                                var lastKnownTimePoint = thisSongInPlaylist.lastKnownTimePoint;
                                if (_transitioningAudio.clip != null && lastKnownTimePoint >= _transitioningAudio.clip.samples - 1) { // prevent the "index too far" error if you position past the end of clip.
                                    lastKnownTimePoint = 0;
                                }

                                _transitioningAudio.timeSamples = lastKnownTimePoint;
                            }
                            break;
                        case MasterAudio.SongFadeInPosition.NewClipFromBeginning:
                            if (ShouldNotSwitchEarly) {
                                // DO NOT set _audioClip.timeSamples = 0, because it will "skip"
                            } else {
                                _audioClip.timeSamples = 0; // new song will start at beginning
                            }
                            break;
                    }
                }

                // only use Custom Start Time for "From Last Known Position" if the song hasn't played before and doesn't have a last known position.
                var isFromLastKnown = _currentPlaylist.songTransitionType == MasterAudio.SongFadeInPosition.NewClipFromLastKnownPosition && !_newSongSetting.wasLastKnownTimePointSet;

                // account for custom start time.
                if (_currentPlaylist.songTransitionType == MasterAudio.SongFadeInPosition.NewClipFromBeginning || isFromLastKnown) {
                    var customStartTime = _newSongSetting.SongStartTime;
                    if (customStartTime > 0f) {
                        // ReSharper disable once PossibleNullReferenceException
                        _audioClip.timeSamples = (int)(customStartTime * _audioClip.clip.frequency);
                    }
                }
            }

            if (isScheduledPlay) {
                UpdateMasterVolume(); // fix the volume of other Audio Source for scheduled song, to avoid "pop" when it starts.
            }

            if (clipWillBeAudibleNow) {
                _activeAudio = _audioClip;
                _transitioningAudio = _transClip;

                // song changed
                if (songChangedCustomEvent != string.Empty && songChangedEventExpanded && songChangedCustomEvent != MasterAudio.NoGroupName) {
                    MasterAudio.FireCustomEvent(songChangedCustomEvent, Trans);
                }

                if (SongChanged != null) {
                    var clipName = String.Empty;
                    if (_audioClip != null) {
                        clipName = _audioClip.clip.name;
                    }
                    SongChanged(clipName, _newSongSetting);
                }
                // song changed end
            }

            _activeAudioEndVolume = _newSongSetting.volume * PlaylistVolume;
            var transStartVol = _transitioningAudio.volume;
            if (_currentSong != null) {
                transStartVol = _currentSong.volume;
            }

            _transitioningAudioStartVolume = transStartVol * PlaylistVolume;
            _currentSong = _newSongSetting;

            if (clipWillBeAudibleNow && _currentSong.songStartedEventExpanded && _currentSong.songStartedCustomEvent != string.Empty && _currentSong.songStartedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(_currentSong.songStartedCustomEvent, Trans);
            }

            if (CanSchedule && playType != AudioPlayType.Schedule && !_currentSong.isLoop) {
                ScheduleNextSong();
            }
        }
        /*! \endcond */

        private void RemoveScheduledClip() {
            if (_audioClip != null) {
                _scheduledSongOffsetByAudioSource.Remove(_audioClip);
            }
        }

        private void ScheduleNextSong() {
            PlayNextOrRandom(AudioPlayType.Schedule);
        }

        private void FadeInScheduledSong() {
            PlayNextOrRandom(AudioPlayType.AlreadyScheduled);
        }

        private double CalculateNextTrackStartTimeOffset() {
            var matchingController = FindOtherControllerInSameSyncGroup();

            if (matchingController != null) {
                var otherScheduled = matchingController.ScheduledGaplessNextSongStartTime();

                if (otherScheduled.HasValue) {
                    return otherScheduled.Value;
                }
            }

            return GetClipDuration(_activeAudio);
        }

        private double GetClipDuration(AudioSource src) {
            return AudioUtil.AdjustAudioClipDurationForPitch(src.clip.length - src.time, src) - CrossFadeTime;
        }

        private void ScheduleClipPlay(double scheduledPlayTimeOffset, AudioSource source, bool calledAfterPause, bool addDspTime = true) {
            double schedTime = addDspTime ? AudioSettings.dspTime + scheduledPlayTimeOffset : scheduledPlayTimeOffset;

            if (ShouldNotSwitchEarly && _currentSchedSongDspEndTime.HasValue) { // this is being calculated AFTER the scheduled song starts.
                if (calledAfterPause) {
                    // do not modify
                } else {
                    schedTime = _currentSchedSongDspEndTime.Value;
                    scheduledPlayTimeOffset = schedTime - AudioSettings.dspTime;
                }
            }

            source.PlayScheduled(schedTime);
            
            _currentSchedSongDspStartTime = schedTime;
            _currentSchedSongDspEndTime = schedTime + GetClipDuration(source);

            RemoveScheduledClip();

            _scheduledSongOffsetByAudioSource.Add(source, scheduledPlayTimeOffset);

            _nextScheduledTimeRecalcStart = null;
            _nextScheduleTimeRecalcDifferentFirstFrameNum = 0;
        }

        private void CrossFadeNow(AudioSource audioClip) {
            audioClip.volume = 0f;
            IsCrossFading = true;
            ResetDuckingState();
            _crossFadeStartTime = AudioUtil.Time;

            if (crossfadeStartedExpanded && crossfadeStartedCustomEvent != string.Empty && crossfadeStartedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(crossfadeStartedCustomEvent, Trans, false);
            }
        }

        private void CeaseAudioSource(AudioSource source) {
            if (source == null) {
                return;
            }

            if (source == _activeAudio) {
                framesOfSongPlayed = 0;
                _activeSongAlias = null;
            }

            var isValidClip = source.clip != null;
            var songName = source.clip == null ? string.Empty : source.clip.name;
            source.Stop();
            source.timeSamples = 0; // so it doesn't reset to last start time automatically.
            
            if (_transClip == null || _transClip.clip != source.clip) // don't unload audio if the same song is playing for crossfade
            {
                AudioUtil.UnloadNonPreloadedAudioData(source.clip, GameObj);
            }

            AudioResourceOptimizer.UnloadPlaylistSongIfUnused(ControllerName, source.clip);
            source.clip = null;

#if ADDRESSABLES_ENABLED
            if (_loadedAddressablesByAudioSource.ContainsKey(source)) {
                var addressable = _loadedAddressablesByAudioSource[source];
                _loadedAddressablesByAudioSource.Remove(source);
                AudioAddressableOptimizer.RemoveAddressablePlayingClip(addressable, source); 
            }
#endif
            RemoveScheduledClip();

            // only trigger the song ended event if we have actually ceased a valid audio clip
            // otherwise we trigger this event incorrectly the first time we start playing a playlist controller.
            if (!isValidClip) {
                return;
            }

            // song ended start
            if (!string.IsNullOrEmpty(songName) && songEndedEventExpanded && songEndedCustomEvent != string.Empty && songEndedCustomEvent != MasterAudio.NoGroupName) {
                MasterAudio.FireCustomEvent(songEndedCustomEvent, Trans, false);
            }

            if (SongEnded != null && !string.IsNullOrEmpty(songName)) {
                SongEnded(songName);
            }
            // song ended end
        }

        private void SetDuckProperties() {
            _originalMusicVolume = _activeAudio == null ? 1 : _activeAudio.volume;

            if (_currentSong != null) {
                _originalMusicVolume = _currentSong.volume * PlaylistVolume;
            }

            var origDb = AudioUtil.GetDbFromFloatVolume(_originalMusicVolume);
            var targetVolumeDb = origDb - _currentDuckVolCut;
            var targetVolumeNormal = AudioUtil.GetFloatVolumeFromDb(targetVolumeDb);

            _duckRange = _originalMusicVolume - targetVolumeNormal;
            _initialDuckVolume = targetVolumeNormal;

            ResetDuckingState(); // cancel any ducking
        }

        private void AudioDucking() {
            switch (_duckingMode) {
                case AudioDuckingMode.NotDucking:
                    break;
                case AudioDuckingMode.SetToDuck:
                    _activeAudio.volume = _initialDuckVolume;
                    _duckingMode = AudioDuckingMode.Ducked;
                    break;
                case AudioDuckingMode.Ducked:
                    if (Time.realtimeSinceStartup >= _timeToStartUnducking)
                    {
                        _duckingMode = AudioDuckingMode.Unducking;
                        break;
                    } 

                    if (Time.realtimeSinceStartup >= _timeToFinishUnducking) {
                        _activeAudio.volume = _originalMusicVolume;
                        ResetDuckingState();
                    } 
                    break;
                case AudioDuckingMode.Unducking:
                    _activeAudio.volume = _initialDuckVolume +
                                          (Time.realtimeSinceStartup - _timeToStartUnducking) /
                                          (_timeToFinishUnducking - _timeToStartUnducking) * _duckRange;
                    if (Time.realtimeSinceStartup >= _timeToFinishUnducking)
                    {
                        _activeAudio.volume = _originalMusicVolume;
                        ResetDuckingState();
                    }
                    break;
            }
        }

        private void ResetDuckingState()
        {
            _duckingMode = AudioDuckingMode.NotDucking;
            _actorUpdater = null;
        }

        private bool SongIsNonAdvancible {
            get {
                return CurrentPlaylist != null
                        && CurrentPlaylist.songTransitionType == MasterAudio.SongFadeInPosition.SynchronizeClips
                        && CrossFadeTime > 0;
            }
        }

        private bool SongShouldLoop(MusicSetting setting) {
            if (_queuedSongs.Count > 0) {
                return false;
            }

            if (SongIsNonAdvancible) {
                return true;
            }

            return setting.isLoop;
        }

#endregion

#region Properties

        /// <summary>
        /// This property returns true if the Playlist Controller has already run its Awake method. You should not call any PlaylistController method until it has done so.
        /// </summary>
        public bool ControllerIsReady { get; private set; }

        /// <summary>
        /// This property returns the current state of the Playlist. Choices are: NotInScene, Stopped, Playing, Paused, Crossfading
        /// </summary>
        public PlaylistStates PlaylistState {
            get {
                if (_activeAudio == null || _transitioningAudio == null) {
                    return PlaylistStates.NotInScene;
                }

                if (!ActiveAudioSource.isPlaying) {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (ActiveAudioSource.time != 0f) {
                        return PlaylistStates.Paused;
                    }

                    return PlaylistStates.Stopped;
                }

                if (IsCrossFading) {
                    return PlaylistStates.Crossfading;
                }

                return PlaylistStates.Playing;
            }
        }

        /// <summary>
        /// This property returns the active audio source for the PlaylistControllers in the Scene. During cross-fading, the one fading in is returned, not the one fading out.
        /// </summary>
        public AudioSource ActiveAudioSource {
            get {
                if (_activeAudio != null && _activeAudio.clip == null) {
                    return _transitioningAudio;
                }

                return _activeAudio;
            }
        }

        /// <summary>
        /// This property returns all the PlaylistControllers in the Scene.
        /// </summary>
        public static List<PlaylistController> Instances {
            get {
                if (_instances != null) {
                    return _instances;
                }
                _instances = new List<PlaylistController>();

                var controllers = FindObjectsOfType(typeof(PlaylistController));
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < controllers.Length; i++) {
                    _instances.Add(controllers[i] as PlaylistController);
                }

                return _instances;
            }
            set {
                // only for non-caching.
                _instances = value;
            }
        }

        /// <summary>
        /// This property returns the GameObject for the PlaylistController's GameObject.
        /// </summary>
        public GameObject PlaylistControllerGameObject {
            get { return _go; }
        }

        /// <summary>
        ///  This property returns the current Audio Source for the current Playlist song that is playing.
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public AudioSource CurrentPlaylistSource {
            get { return _activeAudio; }
        }

        /// <summary>
        ///  This property returns the current Audio Clip for the current Playlist song that is playing.
        /// </summary>
        public AudioClip CurrentPlaylistClip {
            get {
                if (_activeAudio == null) {
                    return null;
                }

                return _activeAudio.clip;
            }
        }

        /// <summary>
        /// This property returns the currently fading out Audio Clip for the Playlist (null if not during cross-fading).
        /// </summary>
        public AudioClip FadingPlaylistClip {
            get {
                if (!IsCrossFading) {
                    return null;
                }

                if (_transitioningAudio == null) {
                    return null;
                }

                return _transitioningAudio.clip;
            }
        }

        /// <summary>
        /// This property returns the currently fading out Audio Source for the Playlist (null if not during cross-fading).
        /// </summary>
        public AudioSource FadingSource {
            get {
                if (!IsCrossFading) {
                    return null;
                }

                return _transitioningAudio;
            }
        }

        /// <summary>
        /// This property returns whether or not the Playlist is currently cross-fading.
        /// </summary>
        public bool IsCrossFading { get; private set; }

        /// <summary>
        /// This property returns whether or not the Playlist is currently cross-fading or doing another fade.
        /// </summary>
        public bool IsFading {
            get { return IsCrossFading || _curFadeMode != FadeMode.None; }
        }

        /// <summary>
        /// This property gets and sets the volume of the Playlist Controller with Master Playlist Volume taken into account.
        /// </summary>
        public float PlaylistVolume {
            get { return MasterAudio.PlaylistMasterVolume * _playlistVolume; }
            set {
                _playlistVolume = value;
                UpdateMasterVolume();
            }
        }

        /*! \cond PRIVATE */
        public void RouteToMixerChannel(AudioMixerGroup group) {
            _activeAudio.outputAudioMixerGroup = group;
            _transitioningAudio.outputAudioMixerGroup = group;
        }
        /*! \endcond */

        /// <summary>
        /// This property returns the current Playlist
        /// </summary>
        public MasterAudio.Playlist CurrentPlaylist {
            get {
                if (_currentPlaylist != null || !(Time.realtimeSinceStartup - _lastTimeMissingPlaylistLogged > 2f)) {
                    return _currentPlaylist;
                }

                Debug.LogWarning("Current Playlist is NULL. Subsequent calls will fail.");
                _lastTimeMissingPlaylistLogged = AudioUtil.Time;
                return _currentPlaylist;
            }
        }

        /// <summary>
        /// This property returns whether you have a Playlist assigned to this controller or not.
        /// </summary>
        public bool HasPlaylist {
            get { return _currentPlaylist != null; }
        }

        /// <summary>
        /// This property returns the name of the current Playlist
        /// </summary>
        public string PlaylistName {
            get {
                if (CurrentPlaylist == null) {
                    return string.Empty;
                }

                return CurrentPlaylist.playlistName;
            }
        }

        /// <summary>
        /// This returns the currently playing song. Do not set any fields on the MusicSetting returned. Consider them read-only.
        /// </summary>
        public MusicSetting CurrentSong {
            get {
                return _currentSong;
            }
        }

        /*! \cond PRIVATE */
        private bool IsMuted {
            get { return isMuted; }
        }

        /// <summary>
        /// This property returns whether the current Playlist is muted or not
        /// </summary>
        private bool PlaylistIsMuted {
            set {
                isMuted = value;

                if (Application.isPlaying) {
                    if (_activeAudio != null) {
                        _activeAudio.mute = value;
                    }

                    if (_transitioningAudio != null) {
                        _transitioningAudio.mute = value;
                    }
                } else {
                    var audios = GetComponents<AudioSource>();
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < audios.Length; i++) {
                        audios[i].mute = value;
                    }
                }
            }
        }

        private float CrossFadeTime {
            get {
                if (_currentPlaylist != null) {
                    return _currentPlaylist.crossfadeMode == MasterAudio.Playlist.CrossfadeTimeMode.UseMasterSetting
                        ? MasterAudio.Instance.MasterCrossFadeTime
                        : _currentPlaylist.crossFadeTime;
                }

                return MasterAudio.Instance.MasterCrossFadeTime;
            }
        }

        private bool IsAutoAdvance {
            get {
                if (SongIsNonAdvancible) {
                    return false;
                }

                return isAutoAdvance;
            }
        }

        public GameObject GameObj {
            get {
                if (_go != null) {
                    return _go;
                }

                _go = gameObject;

                return _go;
            }
        }

        public string ControllerName {
            get {
                if (_name != null) {
                    return _name;
                }
                _name = GameObj.name;

                return _name;
            }
        }

        public bool CanSchedule {
            get {
                return MasterAudio.Instance.useGaplessPlaylists && IsAutoAdvance;
            }
        }

        private bool IsFrameFastEnough {
            get { return AudioUtil.FrameTime < SlowestFrameTimeForCalc; }
        }

        private bool ShouldNotSwitchEarly {
            get { return CrossFadeTime <= 0 && CanSchedule; }
        }

        private Transform Trans {
            get {
                if (_trans != null) {
                    return _trans;
                }
                _trans = transform;

                return _trans;
            }
        }
        /*! \endcond */

        /// <summary>
        /// This tells you how many clips still haven't been played in the Playlist.
        /// </summary>
        public int ClipsRemainingInCurrentPlaylist {
            get { return _clipsRemaining.Count; }
        }

#endregion
    }
}