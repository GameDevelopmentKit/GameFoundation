using UnityEngine;
#if MULTIPLAYER_ENABLED
    using DarkTonic.MasterAudio.Multiplayer;
#endif

/*! \cond PRIVATE */
// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class MechanimStateSounds : StateMachineBehaviour {
        [Header("Select For Sounds To Follow Object")]
        public bool SoundFollowsObject = false;

        [Tooltip("Select for sounds to retrigger each time animation loops without exiting state")]
        [Header("Retrigger Sounds Each Time Anim Loops w/o Exiting State")]
        public bool RetriggerWhenStateLoops = false;

#if MULTIPLAYER_ENABLED
        [Header("Select For Sounds To Be Heard By All Connected Players")]
        public bool MultiplayerBroadcast = false;
#endif

        [Tooltip("Play a Sound Group when state is Entered")]
        [Header("Enter Sound Group")]
        public bool playEnterSound = false;
        public bool stopEnterSoundOnExit = false;
        [SoundGroup]
        public string enterSoundGroup = MasterAudio.NoGroupName;
        [Tooltip("Random Variation plays if blank, otherwise name a Variation from the above Sound Group to play.")]
        public string enterVariationName; //User inputs name of variation to play
        private bool wasEnterSoundPlayed;

        [Tooltip("Play a Sound Group when state is Exited")]
        [Header("Exit Sound Group")]
        public bool playExitSound = false;
        [SoundGroup]
        public string exitSoundGroup = MasterAudio.NoGroupName;
        [Tooltip("Random Variation plays if blank, otherwise name a Variation from the above Sound Group to play.")]
        public string exitVariationName; //User inputs name of variation to play

        [Tooltip("Play a Sound Group (Normal or Looped Chain Variation Mode) timed to the animation state's normalized time.  " +
            "Normalized time is simply the length in time of the animation.  " +
            "Time is represented as a float from 0f - 1f.  0f is the beginning, .5f is the middle, 1f is the end...etc.etc.  " +
            "Select a Start time from 0 - 1.  Select a stop time greater than the start time or leave stop time equals to zero and " +
            "select Stop Anim Time Sound On Exit.  This can be used for Looped Chain Sound Groups since you have to define a stop time.")]
        [Header("Play Sound Group Timed to Animation")]
        public bool playAnimTimeSound = false;             //Play a sound at a speccific time in your animation
        public bool stopAnimTimeSoundOnExit = false;       //Stop sound upon state exit instead of using Time
        [Tooltip("If selected, When To Stop Sound (below) will be used. Otherwise the sound will not be stopped unless you have Stop Anim Time Sound On Exit selected above.")]
        public bool useStopTime;

        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStartSound;          //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStopSound;           //Based upon normalizedTime
        [SoundGroup]
        public string TimedSoundGroup = MasterAudio.NoGroupName;
        [Tooltip("Random Variation plays if blank, otherwise name a Variation from the above Sound Group to play.")]
        public string timedVariationName; //User inputs name of variation to play
        private bool playSoundStart = true;
        private bool playSoundStop = true;

        [Tooltip("Play a Sound Group with each variation timed to the animation.  This allows you to " +
            "time your sounds to the actions in you animation.  Example: A sword swing combo where you want swoosh sounds" +
            "or character grunts timed to the acceleration phase of the sword swing.  Select the number of sounds to be played, up to 4.  " +
            "Then set the time you want each sound to start with each subsequent time greater than the previous time.")]

        [Header("Play Multiple Sounds Timed to Anim")]
        public bool playMultiAnimTimeSounds = false;

        public bool StopMultiAnimTimeSoundsOnExit;

        [Range(0, 4)]
        public int numOfMultiSoundsToPlay;
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStartMultiSound1;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStartMultiSound2;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStartMultiSound3;           //Based upon normalizedTime
        [Tooltip("This value will be compared to the normalizedTime of the animation you are playing. NormalizedTime is represented as a float so 0 is the beginning, 1 is the end and .5f would be the middle etc.")]
        [Range(0f, 1f)]
        public float whenToStartMultiSound4;           //Based upon normalizedTime
        [SoundGroup]
        public string MultiSoundsTimedGroup = MasterAudio.NoGroupName;
        [Tooltip("Random Variation plays if blank, otherwise name a Variation from the above Sound Group to play.")]
        public string multiTimedVariationName; //User inputs name of variation to play

        private bool playMultiSound1 = true;
        private bool playMultiSound2 = true;
        private bool playMultiSound3 = true;
        private bool playMultiSound4 = true;
        private Transform _actorTrans;
        private int _lastRepetition = -1;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            _lastRepetition = 0;
            _actorTrans = ActorTrans(animator);

            if (!playEnterSound) {
                return;
            }

            var varName = GetVariationName(enterVariationName);
            
            if (SoundFollowsObject) {
#if MULTIPLAYER_ENABLED
                if (CanTransmitToOtherPlayers) {
                    if (varName == null) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans);
                    } else {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
                } else {
                    if (varName == null) {
                        MasterAudio.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
                }
#else
                if (varName == null) {
                    MasterAudio.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans);
                } else {
                    MasterAudio.PlaySound3DFollowTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                }
#endif
            } else {
#if MULTIPLAYER_ENABLED
                if (CanTransmitToOtherPlayers) {
                    if (varName == null) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans);
                    } else {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
                } else {
                    if (varName == null) {
                        MasterAudio.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
                }
#else
                if (varName == null) {
                    MasterAudio.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans);
                } else {
                    MasterAudio.PlaySound3DAtTransformAndForget(enterSoundGroup, _actorTrans, 1f, null, 0f, varName);
                }
#endif
            }
            wasEnterSoundPlayed = true;
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            var animRepetition = (int)stateInfo.normalizedTime;
            var animTime = stateInfo.normalizedTime - animRepetition;

            if (!playAnimTimeSound) {
                goto multisounds;
            }

#region Timed to Anim

            if (!playSoundStart && RetriggerWhenStateLoops) {
                // change back to true if "re-trigger" checked and anim has looped.
                if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                    playSoundStart = true;
                }
            }

            if (playSoundStart) {
                if (animTime > whenToStartSound) {
                    playSoundStart = false;

                    //If user selects useStopTime and the stop time is less then start time, they will hear no sound
                    if (useStopTime && whenToStopSound < whenToStartSound) {
                        Debug.LogError("Stop time must be greater than start time when Use Stop Time is selected.");
                        goto outside;
                    }

                    var varName = GetVariationName(timedVariationName);
                    if (SoundFollowsObject) {
                        if (varName == null) {
#if MULTIPLAYER_ENABLED
                            if (CanTransmitToOtherPlayers) {
                                MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans);
                            } else {
                                MasterAudio.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans);
                            }
#else
                            MasterAudio.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans);
#endif
                            MasterAudio.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans);
                        } else {
#if MULTIPLAYER_ENABLED
                            if (CanTransmitToOtherPlayers) {
                                MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans, 1f,
                                    null, 0f, varName);
                            } else {
                                MasterAudio.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans, 1f,
                                    null, 0f, varName);
                            }
#else
                            MasterAudio.PlaySound3DFollowTransformAndForget(TimedSoundGroup, _actorTrans, 1f, null, 0f, varName);
#endif
                        }
                    } else {
                        if (varName == null) {
#if MULTIPLAYER_ENABLED
                            if (CanTransmitToOtherPlayers) {
                                MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans);
                            } else {
                                MasterAudio.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans);
                            }
#else
                            MasterAudio.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans);
#endif
                        } else {
#if MULTIPLAYER_ENABLED
                            if (CanTransmitToOtherPlayers) {
                                MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans, 1f, null,
                                    0f, varName);
                            } else {
                                MasterAudio.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans, 1f, null,
                                    0f, varName);
                            }
#else
                            MasterAudio.PlaySound3DAtTransformAndForget(TimedSoundGroup, _actorTrans, 1f, null, 0f, varName);
#endif
                        }
                    }
                }
            }

            outside:

            if (useStopTime) {
                if (playSoundStop) {
                    if (animTime > whenToStartSound) {
                        if (!stopAnimTimeSoundOnExit) {
                            //Sound will stop upon exit instead of relying on animation time
                            if (animTime > whenToStopSound) {
                                playSoundStop = false;
#if MULTIPLAYER_ENABLED
                                if (CanTransmitToOtherPlayers) {
                                    MasterAudioMultiplayerAdapter.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
                                } else {
                                    MasterAudio.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
                                }
#else
                                MasterAudio.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
#endif
                            }
                        }
                    }
                }
            }

#endregion

            multisounds:

            if (!playMultiAnimTimeSounds) {
                goto afterMulti;
            }

#region Play Multiple Sounds Timed To Anim

            if (RetriggerWhenStateLoops) {
                if (!playMultiSound1) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        playMultiSound1 = true;
                    }
                }
                if (!playMultiSound2) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        playMultiSound2 = true;
                    }
                }
                if (!playMultiSound3) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        playMultiSound3 = true;
                    }
                }
                if (!playMultiSound4) {
                    // change back to true if "re-trigger" checked and anim has looped.
                    if (_lastRepetition >= 0 && animRepetition > _lastRepetition) {
                        playMultiSound4 = true;
                    }
                }
            }

            var multiVarName = GetVariationName(multiTimedVariationName);

            if (!playMultiSound1) {
                goto decideMulti2;
            }
            if (animTime < whenToStartMultiSound1 || numOfMultiSoundsToPlay < 1) {
                goto decideMulti2;
            }

            playMultiSound1 = false;
            if (SoundFollowsObject) {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                            _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                            _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                        _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                            _actorTrans, 1f, null, 0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                            _actorTrans, 1f, null, 0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup,
                        _actorTrans, 1f, null, 0f, multiVarName);
#endif
                }
            } else {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                        0f, multiVarName);
#endif
                }
            }

            decideMulti2:

            if (!playMultiSound2) {
                goto decideMulti3;
            }

            if (animTime < whenToStartMultiSound2 || numOfMultiSoundsToPlay < 2) {
                goto decideMulti3;
            }

            playMultiSound2 = false;
            if (SoundFollowsObject) {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(
                            MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                        null, 0f, multiVarName);
#endif
                }
            } else {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                        0f, multiVarName);
#endif
                }
            }

            decideMulti3:

            if (!playMultiSound3) {
                goto decideMulti4;
            }

            if (animTime < whenToStartMultiSound3 || numOfMultiSoundsToPlay < 3) {
                goto decideMulti4;
            }

            playMultiSound3 = false;
            if (SoundFollowsObject) {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                        null, 0f, multiVarName);
#endif
                }
            } else {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                        0f, multiVarName);
#endif
                }
            }

            decideMulti4:

            if (!playMultiSound4) {
                goto afterMulti;
            }

            if (animTime < whenToStartMultiSound4 || numOfMultiSoundsToPlay < 4) {
                goto afterMulti;
            }

            playMultiSound4 = false;
            if (SoundFollowsObject) {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                            null, 0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DFollowTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f,
                        null, 0f, multiVarName);
#endif
                }
            } else {
                if (multiVarName == null) {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans);
#endif
                } else {
#if MULTIPLAYER_ENABLED                    
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                            0f, multiVarName);
                    }
#else
                    MasterAudio.PlaySound3DAtTransformAndForget(MultiSoundsTimedGroup, _actorTrans, 1f, null,
                        0f, multiVarName);
#endif
                }
            }

#endregion

            afterMulti:

            _lastRepetition = animRepetition;
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            if (wasEnterSoundPlayed && stopEnterSoundOnExit) {
#if MULTIPLAYER_ENABLED
                if (CanTransmitToOtherPlayers) {
                    MasterAudioMultiplayerAdapter.StopSoundGroupOfTransform(_actorTrans, enterSoundGroup);
                } else {
                    MasterAudio.StopSoundGroupOfTransform(_actorTrans, enterSoundGroup);
                }
#else
                MasterAudio.StopSoundGroupOfTransform(_actorTrans, enterSoundGroup);
#endif
            }
            wasEnterSoundPlayed = false;

            if (playExitSound && exitSoundGroup != MasterAudio.NoGroupName && !string.IsNullOrEmpty(exitSoundGroup)) {
                var varName = GetVariationName(exitVariationName);

                if (SoundFollowsObject) {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        if (varName == null) {
                            MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(exitSoundGroup,
                                _actorTrans);
                        } else {
                            MasterAudioMultiplayerAdapter.PlaySound3DFollowTransformAndForget(exitSoundGroup,
                                _actorTrans, 1f, null, 0, varName);
                        }
                    } else {
                        if (varName == null) {
                            MasterAudio.PlaySound3DFollowTransformAndForget(exitSoundGroup, _actorTrans);
                        } else {
                            MasterAudio.PlaySound3DFollowTransformAndForget(exitSoundGroup, _actorTrans, 1f, null, 0f, varName);
                        }
                    }
#else
                    if (varName == null) {
                        MasterAudio.PlaySound3DFollowTransformAndForget(exitSoundGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DFollowTransformAndForget(exitSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
#endif
                } else {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        if (varName == null) {
                            MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans);
                        } else {
                            MasterAudioMultiplayerAdapter.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans, 1f, null, 0f, varName);
                        }
                    } else {
                        if (varName == null) {
                            MasterAudio.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans);
                        } else {
                            MasterAudio.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans, 1f, null, 0f, varName);
                        }
                    }
#else
                    if (varName == null) {
                        MasterAudio.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans);
                    } else {
                        MasterAudio.PlaySound3DAtTransformAndForget(exitSoundGroup, _actorTrans, 1f, null, 0f, varName);
                    }
#endif
                }
            }

#region Timed to Anim
            if (playAnimTimeSound) {
                if (stopAnimTimeSoundOnExit) {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
                    } else {
                        MasterAudio.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
                    }
#else
                    MasterAudio.StopSoundGroupOfTransform(_actorTrans, TimedSoundGroup);
#endif
                }

                playSoundStart = true;
                playSoundStop = true;
            }
#endregion

#region Play Multiple Sounds Timed To Anim
            if (playMultiAnimTimeSounds) {
                if (StopMultiAnimTimeSoundsOnExit) {
#if MULTIPLAYER_ENABLED
                    if (CanTransmitToOtherPlayers) {
                        MasterAudioMultiplayerAdapter.StopSoundGroupOfTransform(_actorTrans, MultiSoundsTimedGroup);
                    } else {
                        MasterAudio.StopSoundGroupOfTransform(_actorTrans, MultiSoundsTimedGroup);
                    }
#else
                    MasterAudio.StopSoundGroupOfTransform(_actorTrans, MultiSoundsTimedGroup);
#endif
                }
                playMultiSound1 = true;
                playMultiSound2 = true;
                playMultiSound3 = true;
                playMultiSound4 = true;
            }
#endregion
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

        private static string GetVariationName(string varName) {
            if (string.IsNullOrEmpty(varName)) {
                return null;
            }

            varName = varName.Trim();

            if (string.IsNullOrEmpty(varName)) {
                return null;
            }

            return varName;
        }

#if MULTIPLAYER_ENABLED
        private bool CanTransmitToOtherPlayers {
            get { return MultiplayerBroadcast && MasterAudioMultiplayerAdapter.CanSendRPCs; }
        }
#endif
    
    }
}
/*! \endcond */
