using System.Collections.Generic;
using UnityEngine;

/*! \cond PRIVATE */

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    public static class AmbientUtil {
        public const string FollowerHolderName = "_Followers";
        public const string ListenerFollowerName = "~ListenerFollower~";
        public const float ListenerFollowerTrigRadius = .01f;
        public const int IgnoreRaycastLayerNumber = 2;

        private static Transform _followerHolder;
        private static ListenerFollower _listenerFollower;
#if PHY3D_ENABLED
        private static Rigidbody _listenerFollowerRB;
#endif
        private static List<TransformFollower> _transformFollowers = new List<TransformFollower>();

        public static void InitFollowerHolder() {
            var h = FollowerHolder;
            if (h != null) {
                h.DestroyAllChildren();
            }
        }

        public static bool InitListenerFollower() {
            var listener = MasterAudio.ListenerTrans;
            if (listener == null) {
                return false;
            }

#if !PHY3D_ENABLED
            return false; // there is no Ambient Sound script functionality without Physics.
#else
            var follower = ListenerFollower;
            if (follower == null) {
                return false;
            }

            follower.StartFollowing(listener, ListenerFollowerTrigRadius);
            return true;
#endif
        }

        public static void RemoveTransformFollower(TransformFollower follower) {
            _transformFollowers.Remove(follower);
        }

        public static Transform InitAudioSourceFollower(Transform transToFollow, string followerName, string soundGroupName, string variationName, 
            float volume,
            bool willFollowSource, bool willPositionOnClosestColliderPoint,
            bool useTopCollider, bool useChildColliders, 
            MasterAudio.AmbientSoundExitMode exitMode, float exitFadeTime,
            MasterAudio.AmbientSoundReEnterMode reEnterMode, float reEnterFadeTime) {

#if !PHY3D_ENABLED
            return null; // there is no Ambient Sound script functionality without Physics.
#else
            if (ListenerFollower == null || FollowerHolder == null) {
                return null;
            }

            var grp = MasterAudio.GrabGroup(soundGroupName);
            if (grp == null) {
                return null;
            }

            if (grp.groupVariations.Count == 0) {
                return null;
            }

            SoundGroupVariation variation = null;

            if (!string.IsNullOrEmpty(variationName)) {
                for (var i = 0; i < grp.groupVariations.Count; i++)
                {
                    var aVar = grp.groupVariations[i];
                    if (aVar.name == variationName)
                    {
                        variation = aVar;
                        break;
                    }
                }
                
                if (variation == null) {
                    Debug.LogError("Could not find Variation '" + variationName + "' in Sound Group '" + soundGroupName);
                    return null;
                }
            } else {
                variation = grp.groupVariations[0];
            }

            var triggerRadius = variation.VarAudio.maxDistance;

            var follower = new GameObject(followerName);
            var existingDupe = FollowerHolder.GetChildTransform(followerName);
            if (existingDupe != null) {
                GameObject.Destroy(existingDupe.gameObject);
            }

            follower.transform.parent = FollowerHolder;
            follower.gameObject.layer = FollowerHolder.gameObject.layer;
            var followerScript = follower.gameObject.AddComponent<TransformFollower>();

            followerScript.StartFollowing(transToFollow, soundGroupName, variationName, volume, triggerRadius, willFollowSource, willPositionOnClosestColliderPoint, useTopCollider,
                useChildColliders, exitMode, exitFadeTime, reEnterMode, reEnterFadeTime);

            _transformFollowers.Add(followerScript);

            return follower.transform;
#endif
        }

        public static ListenerFollower ListenerFollower {
            get {
                if (_listenerFollower != null) {
                    return _listenerFollower;
                }

                if (FollowerHolder == null) {
                    return null;
                }

                var follower = FollowerHolder.GetChildTransform(ListenerFollowerName);
                if (follower == null) {
                    follower = new GameObject(ListenerFollowerName).transform;
                    follower.parent = FollowerHolder;
                    follower.gameObject.layer = FollowerHolder.gameObject.layer;
                }

                _listenerFollower = follower.GetComponent<ListenerFollower>();
                if (_listenerFollower == null) {
                    _listenerFollower = follower.gameObject.AddComponent<ListenerFollower>();
                }

#if PHY3D_ENABLED
                if (MasterAudio.Instance.listenerFollowerHasRigidBody) {
                    var rb = follower.gameObject.GetComponent<Rigidbody>();
                    if (rb == null) {
                        rb = follower.gameObject.AddComponent<Rigidbody>();
                    }
                    rb.useGravity = false;
                    _listenerFollowerRB = rb;
            }
#endif

                return _listenerFollower;
            }
        }

        public static Transform FollowerHolder {
            get {
                if (!Application.isPlaying || MasterAudio.SafeInstance == null) {
                    return null;
                }

                if (_followerHolder != null) {
                    return _followerHolder;
                }

                var ma = MasterAudio.SafeInstance.Trans;
                _followerHolder = ma.GetChildTransform(FollowerHolderName);

                if (_followerHolder != null) {
                    return _followerHolder;
                }

                _followerHolder = new GameObject(FollowerHolderName).transform;
                _followerHolder.parent = ma;
                _followerHolder.gameObject.layer = ma.gameObject.layer;

                return _followerHolder;
            }
        }

        public static void ManualUpdate() {
            UpdateListenerFollower();

            // manually update Transform Followers
            for (var i = 0; i < _transformFollowers.Count; i++) {
                _transformFollowers[i].ManualUpdate();
            }
        }

        private static void UpdateListenerFollower() {
            if (_listenerFollower != null) {
                _listenerFollower.ManualUpdate();
            }
        }

        public static bool HasListenerFollower {
            get { return _listenerFollower != null; }
        }

        public static int AmbientCount
        {
            get
            {
                return _transformFollowers.Count;
            }
        }

        public static bool HasListenerFolowerRigidBody {
            get {
#if PHY3D_ENABLED
                return _listenerFollowerRB != null;
#else
                return false;
#endif
            }
        }
    }
}

/*! \endcond */
