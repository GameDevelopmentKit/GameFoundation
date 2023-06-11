/*! \cond PRIVATE */
using UnityEngine;
using System.Collections.Generic;
using System;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    [AddComponentMenu("Dark Tonic/Master Audio/Ambient Sound")]
    // ReSharper disable once CheckNamespace
    [AudioScriptOrder(-20)]
    public class AmbientSound : MonoBehaviour {
        [SoundGroup] public string AmbientSoundGroup = MasterAudio.NoGroupName;
        public EventSounds.VariationType variationType = EventSounds.VariationType.PlayRandom;
        public string variationName = string.Empty;
        public float playVolume = 1f;
        public MasterAudio.AmbientSoundExitMode exitMode = MasterAudio.AmbientSoundExitMode.StopSound;
        public float exitFadeTime = .5f;
        public MasterAudio.AmbientSoundReEnterMode reEnterMode = MasterAudio.AmbientSoundReEnterMode.StopExistingSound;
        public float reEnterFadeTime = .5f;

        [Tooltip("This option is useful if your caller ever moves, as it will make the Audio Source follow to the location of the caller every frame.")]
		public bool FollowCaller;

        [Tooltip("Using this option, the Audio Source will be updated every frame to the closest position on the caller's collider, if any. This will override the Follow Caller option above and happen instead.")]
		public bool UseClosestColliderPosition;

        public bool UseTopCollider = true;
        public bool IncludeChildColliders = false;

        [Tooltip("This is for diagnostic purposes only. Do not change or assign this field.")]
        public Transform RuntimeFollower;

        private Transform _trans;
		public float colliderMaxDistance;
        public long lastTimeMaxDistanceCalced = 0;

        // ReSharper disable once UnusedMember.Local
        void OnEnable() {
            MasterAudio.SetupAmbientNextFrame(this);
        }

        // ReSharper disable once UnusedMember.Local
        void OnDisable() {
            if (MasterAudio.AppIsShuttingDown) {
                return; // do nothing
            }

            if (!IsValidSoundGroup) {
                return;
            }

            if (MasterAudio.SafeInstance == null) {
                return;
            }

            MasterAudio.RemoveDelayedAmbient(this); // make sure it doesn't start playing or have trackers if it hasn't yet (< 1 frame since enabling).
            StopTrackers();
        }

        private void StopTrackers() {
            var grp = MasterAudio.GrabGroup(AmbientSoundGroup, false); // script execution order thing with DGSC. Need to check so warnings don't get logged.
            if (grp != null) {
                switch (exitMode) {
                    case MasterAudio.AmbientSoundExitMode.StopSound:
                        MasterAudio.StopSoundGroupOfTransform(Trans, AmbientSoundGroup);
                        break;
                    case MasterAudio.AmbientSoundExitMode.FadeSound:
                        MasterAudio.FadeOutSoundGroupOfTransform(Trans, AmbientSoundGroup, exitFadeTime);
                        break;
                }
            }

            RuntimeFollower = null;
        }

        /*! \cond PRIVATE */
        public void CalculateRadius() {
            var aud = GetNamedOrFirstAudioSource();

            if (aud == null) {
                colliderMaxDistance = 0f;
                return;
            }

            colliderMaxDistance = aud.maxDistance;
            lastTimeMaxDistanceCalced = DateTime.Now.Ticks;
        }

        public AudioSource GetNamedOrFirstAudioSource() {
            if (string.IsNullOrEmpty(AmbientSoundGroup)) {
                colliderMaxDistance = 0;
                return null;
            }

            if (MasterAudio.SafeInstance == null) {
                colliderMaxDistance = 0;
                return null;
            }

            var grp = MasterAudio.Instance.transform.Find(AmbientSoundGroup);
            if (grp == null) {
                colliderMaxDistance = 0;
                return null;
            }

            Transform transVar = null;

            switch (variationType) {
                case EventSounds.VariationType.PlayRandom:
                    transVar = grp.GetChild(0);
                    break;
                case EventSounds.VariationType.PlaySpecific:
                    transVar = grp.transform.Find(variationName);
                    break;
            }

            if (transVar == null) {
                colliderMaxDistance = 0;
                return null;
            }

            return transVar.GetComponent<AudioSource>();
        }

        public List<AudioSource> GetAllVariationAudioSources() {
            if (string.IsNullOrEmpty(AmbientSoundGroup)) {
                colliderMaxDistance = 0;
                return null;
            }

            if (MasterAudio.SafeInstance == null) {
                colliderMaxDistance = 0;
                return null;
            }

            var grp = MasterAudio.Instance.transform.Find(AmbientSoundGroup);
            if (grp == null) {
                colliderMaxDistance = 0;
                return null;
            }

            var audioSources = new List<AudioSource>(grp.childCount);

            for (var i = 0; i < grp.childCount; i++) {
                var a = grp.GetChild(i).GetComponent<AudioSource>();
                audioSources.Add(a);
            }

            return audioSources;
        }

        /*! \endcond */

        void OnDrawGizmos() {
			if (MasterAudio.SafeInstance == null || !MasterAudio.Instance.showRangeSoundGizmos) {
				return;
			}

            if (lastTimeMaxDistanceCalced < DateTime.Now.AddHours(-1).Ticks) {
                lastTimeMaxDistanceCalced = DateTime.Now.Ticks;
                CalculateRadius();
            }

			if (colliderMaxDistance == 0f) {
				return;
			}

			var gizmoColor = Color.green;
			if (MasterAudio.SafeInstance != null) {
				gizmoColor = MasterAudio.Instance.rangeGizmoColor;
			}

			Gizmos.color = gizmoColor; 
			Gizmos.DrawWireSphere(transform.position, colliderMaxDistance);
		}

        void OnDrawGizmosSelected() {
            if (MasterAudio.SafeInstance == null || !MasterAudio.Instance.showSelectedRangeSoundGizmos) {
                return;
            }

            if (lastTimeMaxDistanceCalced < DateTime.Now.AddHours(-1).Ticks) {
                lastTimeMaxDistanceCalced = DateTime.Now.Ticks;
                CalculateRadius();
            }

            if (colliderMaxDistance == 0f) {
                return;
            }
             
            var gizmoColor = Color.green;
            if (MasterAudio.SafeInstance != null) {
                gizmoColor = MasterAudio.Instance.selectedRangeGizmoColor;
            }
             
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, colliderMaxDistance);
        }

        public void StartTrackers() {
            if (!IsValidSoundGroup) {
                return;
            }

#if !PHY3D_ENABLED
            MasterAudio.LogWarningIfNeverLogged("Ambient Sounds script will not function because you do not have Physics package installed.", MasterAudio.PHYSICS_DISABLED);
#else
            var shouldIgnoreCollisions = Physics.GetIgnoreLayerCollision(AmbientUtil.IgnoreRaycastLayerNumber, AmbientUtil.IgnoreRaycastLayerNumber);
            if (shouldIgnoreCollisions) {
                MasterAudio.LogWarningIfNeverLogged("You have disabled collisions between Ignore Raycast layer and itself on the Physics Layer Collision matrix. This must be turned back on or Ambient Sounds script will not function.", MasterAudio.ERROR_MA_LAYER_COLLISIONS_DISABLED);
                return;
            }
#endif

            var isListenerFollowerAvailable = AmbientUtil.InitListenerFollower();
            if (!isListenerFollowerAvailable) {
                MasterAudio.LogWarning("Your Ambient Sound script on Game Object '" + name + "' will not function because you have no Audio Listener component in any active Game Object in the Scene.");
                return; // don't bother creating the follower because there's no Listener to collide with.
            }

            if (!AmbientUtil.HasListenerFolowerRigidBody) {
                MasterAudio.LogWarning("Your Ambient Sound script on Game Object '" + name + "' will not function because you have turned off the Listener Follower RigidBody in Advanced Settings.");
            }

			var followerName = name + "_" + AmbientSoundGroup + "_Follower" + "_" + Guid.NewGuid();
            RuntimeFollower = AmbientUtil.InitAudioSourceFollower(Trans, followerName, AmbientSoundGroup, variationName, playVolume, FollowCaller, UseClosestColliderPosition, UseTopCollider, IncludeChildColliders, exitMode, exitFadeTime, reEnterMode, reEnterFadeTime);
        }

        public bool IsValidSoundGroup {
            get {
                return !MasterAudio.SoundGroupHardCodedNames.Contains(AmbientSoundGroup);
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
}
/*! \endcond */
