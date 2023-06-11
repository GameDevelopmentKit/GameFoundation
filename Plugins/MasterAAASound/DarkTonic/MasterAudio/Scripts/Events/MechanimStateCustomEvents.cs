using UnityEngine;
#if MULTIPLAYER_ENABLED
    using DarkTonic.MasterAudio.Multiplayer;
#endif

/*! \cond PRIVATE */
// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class MechanimStateCustomEvents : StateMachineBehaviour {
        [Tooltip("Select for event to re-fire each time animation loops without exiting state")]
        [Header("Retrigger Events Each Time Anim Loops w/o Exiting State")]
        public bool RetriggerWhenStateLoops = false;

#if MULTIPLAYER_ENABLED
        [Header("Select For Events To Be Received By All Connected Players")]
        public bool MultiplayerBroadcast = false;
#endif

        [Tooltip("Fire A Custom Event When State Is Entered")]
        [Header("Enter Custom Event")]
        public bool fireEnterEvent = false;
        [MasterCustomEvent]
        public string enterCustomEvent = MasterAudio.NoGroupName;

        [Tooltip("Fire a Custom Event when state is Exited")]
        [Header("Exit Custom Event")]
        public bool fireExitEvent = false;
        [MasterCustomEvent]
        public string exitCustomEvent = MasterAudio.NoGroupName;

        [Tooltip("Fire a Custom Event timed to the animation state's normalized time.  " +
            "Normalized time is simply the length in time of the animation.  " +
            "Time is represented as a float from 0f - 1f.  0f is the beginning, .5f is the middle, 1f is the end...etc.etc.  " +
            "Select a Start time from 0 - 1.")]
        [Header("Fire Custom EventTimed to Animation")]
        public bool fireAnimTimeEvent = false; //Fire a Custom Event at a speccific time in your animation

        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToFireEvent; //Based upon normalizedTime
        [MasterCustomEvent]
        public string timedCustomEvent = MasterAudio.NoGroupName;

        [Tooltip("Fire a Custom Event with timed to the animation.  This allows you to " +
            "time your Custom Events to the actions in you animation. Select the number of Custom Events to be fired, up to 4. " +
            "Then set the time you want each Custom Event to fire with each subsequent time greater than the previous time.")]

        [Header("Fire Multiple Custom Events Timed to Anim")]
        public bool fireMultiAnimTimeEvent = false;

        [Range(0, 4)]
        public int numOfMultiEventsToFire;
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToFireMultiEvent1;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToFireMultiEvent2;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToFireMultiEvent3;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToFireMultiEvent4;           //Based upon normalizedTime
        [MasterCustomEvent]
        public string MultiTimedEvent = MasterAudio.NoGroupName;

        private bool _playMultiEvent1 = true;
        private bool _playMultiEvent2 = true;
        private bool _playMultiEvent3 = true;
        private bool _playMultiEvent4 = true;
        private bool _fireTimedEvent = true;
        private Transform _actorTrans;
        private int _lastRepetition = -1;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            _lastRepetition = 0;

            _actorTrans = ActorTrans(animator);

            if (!fireEnterEvent) {
                return;
            }

            if (enterCustomEvent == MasterAudio.NoGroupName || string.IsNullOrEmpty(enterCustomEvent)) {
                return;
            }

#if MULTIPLAYER_ENABLED
            if (CanTransmitToOtherPlayers) {
                MasterAudioMultiplayerAdapter.FireCustomEvent(enterCustomEvent, _actorTrans, true);
            } else {
                MasterAudio.FireCustomEvent(enterCustomEvent, _actorTrans);
            }
#else
            MasterAudio.FireCustomEvent(enterCustomEvent, _actorTrans);
#endif
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            var animRepetition = (int)stateInfo.normalizedTime;
            var animTime = stateInfo.normalizedTime - animRepetition;

            if (!fireAnimTimeEvent) {
                goto multievent;
            }

#region Timed to Anim
            if (!_fireTimedEvent && RetriggerWhenStateLoops) {
                // change back to true if "re-trigger" checked and anim has looped.

                if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                    _fireTimedEvent = true;
                }
            }

            if (_fireTimedEvent) {
                if (animTime > whenToFireEvent) {
                    _fireTimedEvent = false;

#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.FireCustomEvent(timedCustomEvent, _actorTrans, true);
                    } else {
                        MasterAudio.FireCustomEvent(timedCustomEvent, _actorTrans);
                    }
#else
                    MasterAudio.FireCustomEvent(timedCustomEvent, _actorTrans);
#endif
                }
            }

#endregion

            multievent:

            if (!fireMultiAnimTimeEvent) {
                goto afterMulti;
            }

#region Fire Multiple Events Timed To Anim

            if (RetriggerWhenStateLoops) {
                if (!_playMultiEvent1) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        _playMultiEvent1 = true;
                    }
                }
                if (!_playMultiEvent2) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        _playMultiEvent2 = true;
                    }
                }
                if (!_playMultiEvent3) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        _playMultiEvent3 = true;
                    }
                }
                if (!_playMultiEvent4) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        _playMultiEvent4 = true;
                    }
                }
            }

            if (!_playMultiEvent1) {
                goto decideMulti2;
            }
            if (animTime < whenToFireMultiEvent1 || numOfMultiEventsToFire < 1) {
                goto decideMulti2;
            }

            _playMultiEvent1 = false;
#if MULTIPLAYER_ENABLED
            if (CanTransmitToOtherPlayers) {
                MasterAudioMultiplayerAdapter.FireCustomEvent(MultiTimedEvent, _actorTrans, true);
            } else {
                MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
            }
#else
            MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
#endif

            decideMulti2:

            if (!_playMultiEvent2) {
                goto decideMulti3;
            }

            if (animTime < whenToFireMultiEvent2 || numOfMultiEventsToFire < 2) {
                goto decideMulti3;
            }

            _playMultiEvent2 = false;
#if MULTIPLAYER_ENABLED
            if (CanTransmitToOtherPlayers) {
                MasterAudioMultiplayerAdapter.FireCustomEvent(MultiTimedEvent, _actorTrans, true);
            } else {
                MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
            }
#else
            MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
#endif

            decideMulti3:

            if (!_playMultiEvent3) {
                goto decideMulti4;
            }

            if (animTime < whenToFireMultiEvent3 || numOfMultiEventsToFire < 3) {
                goto decideMulti4;
            }

            _playMultiEvent3 = false;
#if MULTIPLAYER_ENABLED
            if (CanTransmitToOtherPlayers) {
                MasterAudioMultiplayerAdapter.FireCustomEvent(MultiTimedEvent, _actorTrans, true);
            } else {
                MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
            }
#else
            MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
#endif

            decideMulti4:

            if (!_playMultiEvent4) {
                goto afterMulti;
            }

            if (animTime < whenToFireMultiEvent4 || numOfMultiEventsToFire < 4) {
                goto afterMulti;
            }

            _playMultiEvent4 = false;
#if MULTIPLAYER_ENABLED
            if (CanTransmitToOtherPlayers) {
                MasterAudioMultiplayerAdapter.FireCustomEvent(MultiTimedEvent, _actorTrans, true);
            } else {
                MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
            }
#else
            MasterAudio.FireCustomEvent(MultiTimedEvent, _actorTrans);
#endif

            #endregion

            afterMulti:

            _lastRepetition = animRepetition;
        }

        // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (fireExitEvent && exitCustomEvent != MasterAudio.NoGroupName && !string.IsNullOrEmpty(exitCustomEvent)) {
#if MULTIPLAYER_ENABLED
                if (CanTransmitToOtherPlayers) {
                    MasterAudioMultiplayerAdapter.FireCustomEvent(exitCustomEvent, _actorTrans, true);
                } else {
                    MasterAudio.FireCustomEvent(exitCustomEvent, _actorTrans);
                }
#else
                MasterAudio.FireCustomEvent(exitCustomEvent, _actorTrans);
#endif
            }

            if (fireMultiAnimTimeEvent) {
                _playMultiEvent1 = true;
                _playMultiEvent2 = true;
                _playMultiEvent3 = true;
                _playMultiEvent4 = true;
            }

            if (fireAnimTimeEvent) {
                _fireTimedEvent = true;
            }
        }

        // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
        //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //
        //}

        // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
        //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        //
        //}

        private Transform ActorTrans(Animator anim) {
            if (_actorTrans != null) {
                return _actorTrans;
            }

            _actorTrans = anim.transform;

            return _actorTrans;
        }

#if MULTIPLAYER_ENABLED
        private bool CanTransmitToOtherPlayers {
            get { return MultiplayerBroadcast && MasterAudioMultiplayerAdapter.CanSendRPCs; }
        }
#endif
    }
}
/*! \endcond */
