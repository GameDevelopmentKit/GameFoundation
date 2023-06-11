/*! \cond PRIVATE */
using DarkTonic.MasterAudio;
using UnityEngine;

// ReSharper disable once CheckNamespace
[AudioScriptOrder(-10)]
public class ListenerFollower : MonoBehaviour {
    private Transform _transToFollow;
    private GameObject _goToFollow;
    private Transform _trans;
    private GameObject _go;
#if PHY3D_ENABLED
    private SphereCollider _collider;
#endif

    // ReSharper disable once UnusedMember.Local
    void Awake() {
#if PHY3D_ENABLED
        var trig = Trigger;
        if (trig == null) { } // get rid of warning
#endif
    }

    public void StartFollowing(Transform transToFollow, float trigRadius) {
        _transToFollow = transToFollow;
        _goToFollow = transToFollow.gameObject;
#if PHY3D_ENABLED
        Trigger.radius = trigRadius;
#endif
    }

    // ReSharper disable once UnusedMember.Local
    public void ManualUpdate() {
        BatchOcclusionRaycasts();

        if (_transToFollow == null || !DTMonoHelper.IsActive(_goToFollow)) {
            // wait for new AudioListener to show up.
            return;
        }

        Trans.position = _transToFollow.position;
    }

    // ReSharper disable once MemberCanBeMadeStatic.Local
    private void BatchOcclusionRaycasts() {
        if (!MasterAudio.Instance.useOcclusion) {
            return; // save them for later when it's turned back on.
        }

        for (var i = 0; i < MasterAudio.Instance.occlusionMaxRayCastsPerFrame;) {
            if (!MasterAudio.HasQueuedOcclusionRays()) {
                break; // no more waiting there. Abort
            }

            var updater = MasterAudio.OldestQueuedOcclusionRay();
            if (updater == null || !updater.enabled) { // Updater was destroyed while waiting, or sound is done playing and Updater disabled.
                continue;
            }

            var wasCast = updater.RayCastForOcclusion();
            if (wasCast) {
                // ray was cast. Increment counter
                i++;
            }
        }
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