/*! \cond PRIVATE */
using DarkTonic.MasterAudio;
using UnityEngine;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
public class TransformFollower : MonoBehaviour {
    [Tooltip("This is for diagnostic purposes only. Do not change or assign this field.")]
    public Transform RuntimeFollowingTransform;

    private GameObject _goToFollow;
    private Transform _trans;
    private GameObject _go;
#if PHY3D_ENABLED
    private SphereCollider _collider;
#endif
    private string _soundType;
    private string _variationName;
    private bool _willFollowSource;
    private bool _isInsideTrigger;
    private bool _hasPlayedSound;
    private float _playVolume;
    private bool _positionAtClosestColliderPoint = false;
    private MasterAudio.AmbientSoundExitMode _exitMode;
    private float _exitFadeTime;
    private MasterAudio.AmbientSoundReEnterMode _reEnterMode;
    private float _reEnterFadeTime;
#if PHY3D_ENABLED
    private readonly List<Collider> _actorColliders = new List<Collider>();
#endif
#if PHY2D_ENABLED
    private readonly List<Collider2D> _actorColliders2D = new List<Collider2D>();
#endif
    private Vector3 _lastListenerPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
#if PHY3D_ENABLED
    private readonly Dictionary<Collider, Vector3> _lastPositionByCollider = new Dictionary<Collider, Vector3>();
#endif
#if PHY2D_ENABLED
    private readonly Dictionary<Collider2D, Vector3> _lastPositionByCollider2D = new Dictionary<Collider2D, Vector3>();
#endif
    private PlaySoundResult playingVariation;
    private PlaySoundResult fadingVariation;

    // ReSharper disable once UnusedMember.Local
    void Awake() {
#if PHY3D_ENABLED
        var trig = Trigger;
        if (trig == null || _actorColliders.Count == 0) { } // get rid of warning
#endif
        if (_lastListenerPos == Vector3.zero || playingVariation != null || _positionAtClosestColliderPoint) { } // get rid of warning

#if PHY2D_ENABLED
        if (_actorColliders2D.Count == 0) { } // get rid of warning
#endif
    }

    void OnDisable() {
        AmbientUtil.RemoveTransformFollower(this);
        PerformTriggerExit(); // trigger exit doesn't seem to fire on same frame for pooling, when just did trigger enter same frame.
    }

    /// <summary>
    /// This gets called by SoundGroupUpdater when Chained Loop updates with a new Variation.
    /// </summary>
    /// <param name="newVariation"></param>
    public void UpdateAudioVariation(SoundGroupVariation newVariation)
    {
        playingVariation.ActingVariation = newVariation;
    }

    public void StartFollowing(Transform transToFollow, string soundType, string variationName, float volume, float trigRadius,
        bool willFollowSource, bool positionAtClosestColliderPoint,
                               bool useTopCollider, bool useChildColliders,
                               MasterAudio.AmbientSoundExitMode exitMode, float exitFadeTime,
                               MasterAudio.AmbientSoundReEnterMode reEnterMode, float reEnterFadeTime) {

        RuntimeFollowingTransform = transToFollow;
        _goToFollow = transToFollow.gameObject;
#if PHY3D_ENABLED
        Trigger.radius = trigRadius;
#endif
        _soundType = soundType;
        _variationName = variationName;
        _playVolume = volume;
        _willFollowSource = willFollowSource;
        _exitMode = exitMode;
        _exitFadeTime = exitFadeTime;
        _reEnterMode = reEnterMode;
        _reEnterFadeTime = reEnterFadeTime;
#if PHY3D_ENABLED
        _lastPositionByCollider.Clear();
#endif
#if PHY2D_ENABLED
        _lastPositionByCollider2D.Clear();
#endif

        if (useTopCollider) {
#if PHY3D_ENABLED
            Collider col3D = transToFollow.GetComponent<Collider>();
#else 
            Component col3D = null;
#endif
            if (col3D != null) {
#if PHY3D_ENABLED
                _actorColliders.Add(col3D);
                _lastPositionByCollider.Add(col3D, transToFollow.position);
#endif
            } else {
#if PHY2D_ENABLED
                Collider2D col2D = transToFollow.GetComponent<Collider2D>();
                if (col2D != null) {
                    _actorColliders2D.Add(col2D);
                    _lastPositionByCollider2D.Add(col2D, transToFollow.position);
                }
#endif
            }
        }

        if (useChildColliders && transToFollow != null) {
            for (var i = 0; i < transToFollow.childCount; i++) {
#if PHY3D_ENABLED || PHY2D_ENABLED
                var child = transToFollow.GetChild(i);
#endif

#if PHY3D_ENABLED
                Collider col3D = child.GetComponent<Collider>();
                if (col3D != null) {
                    _actorColliders.Add(col3D);
                    _lastPositionByCollider.Add(col3D, transToFollow.position);
                    continue;
                }
#endif

#if PHY2D_ENABLED
                Collider2D col2D = child.GetComponent<Collider2D>();
                if (col2D != null) {
                    _actorColliders2D.Add(col2D);
                    _lastPositionByCollider2D.Add(col2D, transToFollow.position);
                }
#endif
            }
        }

        _lastListenerPos = MasterAudio.ListenerTrans.position;

        var col3DCount = 0;
        var col2DCount = 0;

#if PHY3D_ENABLED
        col3DCount = _actorColliders.Count;
#endif
#if PHY2D_ENABLED
        col2DCount = _actorColliders2D.Count;
#endif

        if (col3DCount == 0 && col2DCount == 0 && positionAtClosestColliderPoint) {
			Debug.Log("Can't follow collider of '" + transToFollow.name + "' because it doesn't have any colliders.");
		} else {
			_positionAtClosestColliderPoint = positionAtClosestColliderPoint;
			if (_positionAtClosestColliderPoint) {
                RecalcClosestColliderPosition(true);
                MasterAudio.QueueTransformFollowerForColliderPositionRecalc(this);
			}
		}		
    }

    private void StopFollowing() {
        RuntimeFollowingTransform = null;
        GameObject.Destroy(GameObj);
    }

    private void PlaySound() {
        var hasSpecificVariation = !string.IsNullOrEmpty(_variationName);

        var needsResult = _positionAtClosestColliderPoint || _exitMode == MasterAudio.AmbientSoundExitMode.FadeSound;

        var shouldFollowSource = _willFollowSource && !_positionAtClosestColliderPoint;

        if (fadingVariation != null && fadingVariation.ActingVariation != null) {
            var reEnterModeToUse = _reEnterMode;

            if (!fadingVariation.ActingVariation.IsPlaying) {
                reEnterModeToUse = MasterAudio.AmbientSoundReEnterMode.StopExistingSound; // it cannot fade it back in if it already stopped.
            }

            switch (reEnterModeToUse) {
                case MasterAudio.AmbientSoundReEnterMode.FadeInSameSound:
                    fadingVariation.ActingVariation.FadeToVolume(_playVolume, _reEnterFadeTime);
                    playingVariation = fadingVariation;

                    fadingVariation = null;
                    _hasPlayedSound = true;
                    return;
                case MasterAudio.AmbientSoundReEnterMode.StopExistingSound:
                    fadingVariation.ActingVariation.Stop();
                    break;
            }
        }

        if (shouldFollowSource) { // no point following when we're going to set the position every closest collider position recalc.
            if (needsResult) {
                if (hasSpecificVariation) {
                    playingVariation = MasterAudio.PlaySound3DFollowTransform(_soundType, RuntimeFollowingTransform, _playVolume, 1f, 0f, _variationName);
                } else {
                    playingVariation = MasterAudio.PlaySound3DFollowTransform(_soundType, RuntimeFollowingTransform, _playVolume);
                }
            } else {
                if (hasSpecificVariation) {
                    MasterAudio.PlaySound3DFollowTransformAndForget(_soundType, RuntimeFollowingTransform, _playVolume, 1f, 0f, _variationName);
                } else {
                    MasterAudio.PlaySound3DFollowTransformAndForget(_soundType, RuntimeFollowingTransform, _playVolume);
                }
            }
        } else {
            if (needsResult) {
                if (hasSpecificVariation) {
                    playingVariation = MasterAudio.PlaySound3DAtTransform(_soundType, RuntimeFollowingTransform, _playVolume, 1f, 0f, _variationName);
                } else {
                    playingVariation = MasterAudio.PlaySound3DAtTransform(_soundType, RuntimeFollowingTransform, _playVolume);
                }
            } else {
                if (hasSpecificVariation) {
                    MasterAudio.PlaySound3DAtTransformAndForget(_soundType, RuntimeFollowingTransform, _playVolume, 1f, 0f, _variationName);
                } else {
                    MasterAudio.PlaySound3DAtTransformAndForget(_soundType, RuntimeFollowingTransform, _playVolume);
                }
            }
        }

        fadingVariation = null;
        _hasPlayedSound = true;
    }

    // ReSharper disable once UnusedMember.Local
    public void ManualUpdate() {
        if (RuntimeFollowingTransform == null || !DTMonoHelper.IsActive(_goToFollow)) {
            StopFollowing();
            return;
        }

        if (!_positionAtClosestColliderPoint) {
            Trans.position = RuntimeFollowingTransform.position;
        }

        if (!_isInsideTrigger || _hasPlayedSound) {
            return;
        }

        PlaySound();
    }

	/// <summary>
	/// Called in a queue from MasterAudio to limit the number of times this calculation occurs per frame.
	/// </summary>
	/// <returns>true if is calculated "closest position on collider"</returns>
	public bool RecalcClosestColliderPosition(bool forceRecalc = false) {
		// follow at closest point
		var listenerPos = MasterAudio.ListenerTrans.position;
		var hasListenerMoved = _lastListenerPos != listenerPos;
		var closestPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		var hasPointMoved = false;

		if (hasListenerMoved) {
			// remove warning
		}

#if PHY3D_ENABLED
        var col3dColliders = _actorColliders.Count;
#else
        var col3dColliders = 0;
#endif

#if PHY2D_ENABLED
        var col2dColliders = _actorColliders2D.Count;
#else
        var col2dColliders = 0;
#endif

        if (col3dColliders > 0) {
#if PHY3D_ENABLED
    		var minDist = float.MaxValue;
			if (col3dColliders == 1) {
				var colZero = _actorColliders[0];
                if (colZero == null) {
                    return false;
                }
                
                var colPos = colZero.transform.position;
				
				if (!forceRecalc && _lastPositionByCollider[colZero] == colPos && !hasListenerMoved) {
                    // same positions, no reason to calculate new position
                    return false;
				}

				hasPointMoved = true;
				closestPoint = colZero.ClosestPoint(listenerPos);
				
				_lastPositionByCollider[colZero] = colPos;
			} else {
				// ReSharper disable once ForCanBeConvertedToForeach
				for (var i = 0; i < _actorColliders.Count; i++) {
					var col = _actorColliders[i];
					var colPos = col.transform.position;
					
					if (!forceRecalc && _lastPositionByCollider[col] == colPos && !hasListenerMoved) {
						continue; // has not moved, continue loop
					}
					
					hasPointMoved = true;
					
					var closestPointOnCollider = col.ClosestPoint(listenerPos);
					var dist = (listenerPos - closestPointOnCollider).sqrMagnitude;
					if (dist < minDist) {
						closestPoint = closestPointOnCollider;
						minDist = dist;
					}
					
					_lastPositionByCollider[col] = colPos;
				}
			}
#endif
        } else if (col2dColliders > 0) {
#if PHY2D_ENABLED
    		var minDist = float.MaxValue;
			if (_actorColliders2D.Count == 1) {
				var colZero = _actorColliders2D[0];
				var colPos = colZero.transform.position;
				
				if (!forceRecalc && _lastPositionByCollider2D[colZero] == colPos && !hasListenerMoved) {
					// same positions, no reason to calculate new position
					return false;
				}
				
				hasPointMoved = true;
				closestPoint = colZero.bounds.ClosestPoint(listenerPos);
				
				_lastPositionByCollider2D[colZero] = colPos;
			} else {
				// ReSharper disable once ForCanBeConvertedToForeach
				for (var i = 0; i < _actorColliders2D.Count; i++) {
					var col = _actorColliders2D[i];
					var colPos = col.transform.position;
					
					if (!forceRecalc && _lastPositionByCollider2D[col] == colPos && !hasListenerMoved) {
						continue; // has not moved, continue loop
					}
					
					hasPointMoved = true;
					
					var closestPointOn2DCollider = col.bounds.ClosestPoint(listenerPos);
					var dist = (listenerPos - closestPointOn2DCollider).sqrMagnitude;
					if (dist < minDist) {
						closestPoint = closestPointOn2DCollider;
						minDist = dist;
					}
					
					_lastPositionByCollider2D[col] = colPos;
				}
			}
#endif
        } else {
			return false; // no colliders. Exit
		}
		
		if (!hasPointMoved) {
			return false; // nothing changed, exit.
		}
		
		Trans.position = closestPoint;
		Trans.LookAt(MasterAudio.ListenerTrans);
		if (playingVariation != null && playingVariation.ActingVariation != null) {
            playingVariation.ActingVariation.MoveToAmbientColliderPosition(closestPoint, this);
		}
		
		_lastListenerPos = listenerPos;
		
		return true;
	}

#if PHY3D_ENABLED
    // ReSharper disable once UnusedMember.Local
    private void OnTriggerEnter(Collider other) {
        if (RuntimeFollowingTransform == null) {
            return;
        }

        if (other == null || name == AmbientUtil.ListenerFollowerName || other.name != AmbientUtil.ListenerFollowerName) {
            return; // abort if this is the Listener or if not colliding with Listener.
        }

        _isInsideTrigger = true;

        PlaySound();
    }

    // ReSharper disable once UnusedMember.Local
    private void OnTriggerExit(Collider other) {
        if (RuntimeFollowingTransform == null) {
            return;
        }

        if (other == null || other.name != AmbientUtil.ListenerFollowerName) {
            return; // abort if not colliding with Listener.
        }

        PerformTriggerExit();
    }
#endif

    private void PerformTriggerExit() {
        _isInsideTrigger = false;
        _hasPlayedSound = false;

        var grp = MasterAudio.GrabGroup(_soundType, false);
        if (grp == null) { // might be destroyed! Proceeding will log spam.
            return;
        }

        switch (_exitMode) {
            case MasterAudio.AmbientSoundExitMode.StopSound:
                MasterAudio.StopSoundGroupOfTransform(RuntimeFollowingTransform, _soundType);
                break;
            case MasterAudio.AmbientSoundExitMode.FadeSound:
                MasterAudio.FadeOutSoundGroupOfTransform(RuntimeFollowingTransform, _soundType, _exitFadeTime);
                break;
        }

        fadingVariation = playingVariation;
        playingVariation = null;

        //StopFollowing();
    }

#if PHY3D_ENABLED
    public SphereCollider Trigger {
        get {
            if (_collider != null) {
                return _collider;
            }

            _collider = GameObj.AddComponent<SphereCollider>();
            _collider.isTrigger = true;

            return _collider;
        }
    }
#endif

    public GameObject GameObj {
        get {
            if (_go != null) {
                return _go;
            }

            _go = gameObject;
            return _go;
        }
    }

    public Transform Trans {
        get {
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (_trans == null) {
                _trans = transform;
            }

            return _trans;
        }
    }
}
/*! \endcond */
