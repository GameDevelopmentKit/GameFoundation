using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class is used to configure and create temporary per-Scene Sound Groups and Buses
    /// </summary>
    [AudioScriptOrder(-35)]
    // ReSharper disable once CheckNamespace
    public class DynamicSoundGroupCreator : MonoBehaviour {
        /*! \cond PRIVATE */
        public const int ExtraHardCodedBusOptions = 1;

        // ReSharper disable InconsistentNaming
        public SystemLanguage previewLanguage = SystemLanguage.English;
        public MasterAudio.DragGroupMode curDragGroupMode = MasterAudio.DragGroupMode.OneGroupPerClip;
        public GameObject groupTemplate;
        public GameObject variationTemplate;
        public bool errorOnDuplicates = false;
        public bool createOnAwake = true;
        public bool soundGroupsAreExpanded = true;
        public bool removeGroupsOnSceneChange = true;
        public CreateItemsWhen reUseMode = CreateItemsWhen.FirstEnableOnly;
        public bool showCustomEvents = true;
        public MasterAudio.AudioLocation bulkVariationMode = MasterAudio.AudioLocation.Clip;
        public List<CustomEvent> customEventsToCreate = new List<CustomEvent>();
		public List<CustomEventCategory> customEventCategories = new List<CustomEventCategory> {
			new CustomEventCategory()
		};
		public string newEventName = "my event";
		public string newCustomEventCategoryName = "New Category";
		public string addToCustomEventCategoryName = "New Category";
		public bool showMusicDucking = true;
        public List<DuckGroupInfo> musicDuckingSounds = new List<DuckGroupInfo>();
        public List<GroupBus> groupBuses = new List<GroupBus>();
        public bool playListExpanded = false;
        public bool playlistEditorExp = true;
        public List<MasterAudio.Playlist> musicPlaylists = new List<MasterAudio.Playlist>();
        public List<GameObject> audioSourceTemplates = new List<GameObject>(10);
        public string audioSourceTemplateName = "Max Distance 500";
        public bool groupByBus = false;

        public bool itemsCreatedEventExpanded = false;
        public string itemsCreatedCustomEvent = string.Empty;

        public bool showUnityMixerGroupAssignment = true;
        // ReSharper restore InconsistentNaming

        private bool _hasCreated;
        private readonly List<Transform> _groupsToRemove = new List<Transform>();
        private Transform _trans;
        private int _instanceId = -1;

        public enum CreateItemsWhen {
            FirstEnableOnly,
            EveryEnable
        }
        /*! \endcond */

        private readonly List<DynamicSoundGroup> _groupsToCreate = new List<DynamicSoundGroup>();

        // ReSharper disable once UnusedMember.Local
        private void Awake() {
            _trans = transform;
            _hasCreated = false;
            var aud = GetComponent<AudioSource>();
            if (aud != null) {
                Destroy(aud);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEnable() {
            CreateItemsIfReady(); // create in Enable event if it's all ready
        }

        // ReSharper disable once UnusedMember.Local
        private void Start() {
            CreateItemsIfReady(); // if it wasn't ready in Enable, create everything in Start
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable() {
            if (MasterAudio.AppIsShuttingDown) {
                return;
            }

            // scene changing
            if (!removeGroupsOnSceneChange) {
                // nothing to do.
                return;
            }

            if (MasterAudio.SafeInstance != null) {
                RemoveItems();
            }
        }

        private void CreateItemsIfReady() {
            if (MasterAudio.SafeInstance == null) { 
				return;
			}

			if (createOnAwake && MasterAudio.SoundsReady && !_hasCreated) {
                CreateItems();
            }
        }

        /// <summary>
        /// This method will remove the Sound Groups, Variations, buses, ducking triggers and Playlist objects specified in the Dynamic Sound Group Creator's Inspector. It is called automatically if you check the "Auto-remove Items" checkbox, otherwise you will need to call this method manually.
        /// </summary>
        public void RemoveItems() {
            // delete any buses we created too
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < groupBuses.Count; i++) {
                var aBus = groupBuses[i];

                if (aBus.isExisting) {
                    continue; // don't delete!
                }

                var existingBus = MasterAudio.GrabBusByName(aBus.busName);
                if (existingBus != null && !existingBus.isTemporary)
                {
                    continue; // don't delete, it was an existing bus you used because it already existed and you couldn't create it.
                }

                if (existingBus != null)
                {
                    existingBus.RemoveActorInstanceId(InstanceId);
                    if (existingBus.HasLiveActors)
                    {
                        continue;
                    }
                }

                MasterAudio.DeleteBusByName(aBus.busName);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _groupsToRemove.Count; i++) {
                var groupName = _groupsToRemove[i].name;

                var grp = MasterAudio.GrabGroup(groupName, false);
                if (grp == null)
                {
                    continue;
                }

                grp.RemoveActorInstanceId(InstanceId);
                if (grp.HasLiveActors)
                {
                    continue;
                }

                MasterAudio.RemoveSoundGroupFromDuckList(groupName);
                MasterAudio.DeleteSoundGroup(groupName);
            }
            _groupsToRemove.Clear();


            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < customEventsToCreate.Count; i++) {
                var anEvent = customEventsToCreate[i];

                var matchingEvent = MasterAudio.Instance.customEvents.Find(delegate(CustomEvent cEvent)
                {
                    return cEvent.EventName == anEvent.EventName && cEvent.isTemporary;
                });

                if (matchingEvent == null)
                {
                    continue;
                }

                matchingEvent.RemoveActorInstanceId(InstanceId);

                if (matchingEvent.HasLiveActors)
                {
                    continue;
                }

                MasterAudio.DeleteCustomEvent(anEvent.EventName);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < customEventCategories.Count; i++) {
				var aCat = customEventCategories[i];

                var matchingCat = MasterAudio.Instance.customEventCategories.Find(delegate(CustomEventCategory category)
                {
                    return category.CatName == aCat.CatName && category.IsTemporary;
                });

                if (matchingCat == null)
                {
                    continue;
                }

                matchingCat.RemoveActorInstanceId(InstanceId);

                if (matchingCat.HasLiveActors)
                {
                    continue;
                }

                MasterAudio.Instance.customEventCategories.Remove(matchingCat);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < musicPlaylists.Count; i++) {
                var aPlaylist = musicPlaylists[i];

                var playlist = MasterAudio.GrabPlaylist(aPlaylist.playlistName);
                if (playlist == null)
                {
                    continue;
                }

                playlist.RemoveActorInstanceId(InstanceId);

                if (playlist.HasLiveActors)
                {
                    continue;
                }

                MasterAudio.DeletePlaylist(aPlaylist.playlistName);
            }

            if (reUseMode == CreateItemsWhen.EveryEnable) {
                _hasCreated = false;
            }

            MasterAudio.SilenceOrUnsilenceGroupsFromSoloChange();
        }

        /// <summary>
        /// This method will create the Sound Groups, Variations, buses, ducking triggers and Playlist objects specified in the Dynamic Sound Group Creator's Inspector. It is called automatically if you check the "Auto-create Items" checkbox, otherwise you will need to call this method manually.
        /// </summary>
        public void CreateItems() {
            if (_hasCreated) {
                Debug.LogWarning("DynamicSoundGroupCreator '" + transform.name +
                                 "' has already created its items. Cannot create again.");
                return;
            }

            var ma = MasterAudio.Instance;
            if (ma == null) {
                return;
            }

            PopulateGroupData();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < groupBuses.Count; i++) {
                var aBus = groupBuses[i];

                if (aBus.isExisting) {
                    var confirmBus = MasterAudio.GrabBusByName(aBus.busName);
                    if (confirmBus == null) {
                        MasterAudio.LogWarning("Existing bus '" + aBus.busName +
                                               "' was not found, specified in prefab '" + name + "'.");
                    }
                    continue; // already exists.
                }

                var createdBus = MasterAudio.GrabBusByName(aBus.busName);

                if (createdBus == null)
                {
                    if (MasterAudio.CreateBus(aBus.busName, InstanceId, errorOnDuplicates, true))
                    {
                        createdBus = MasterAudio.GrabBusByName(aBus.busName);
                    }
                } else {
                    createdBus.AddActorInstanceId(InstanceId);
                }

                if (createdBus == null) {
                    continue;
                }

                var busVol = PersistentAudioSettings.GetBusVolume(aBus.busName);
                if (!busVol.HasValue) {
                    createdBus.volume = aBus.volume;
                    createdBus.OriginalVolume = createdBus.volume;
                }
                createdBus.voiceLimit = aBus.voiceLimit;
                createdBus.busVoiceLimitExceededMode = aBus.busVoiceLimitExceededMode;
                createdBus.forceTo2D = aBus.forceTo2D;
                createdBus.mixerChannel = aBus.mixerChannel;
                createdBus.busColor = aBus.busColor;
                createdBus.isUsingOcclusion = aBus.isUsingOcclusion;
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _groupsToCreate.Count; i++) {
                var aGroup = _groupsToCreate[i];

                var busName = string.Empty;
                var selectedBusIndex = aGroup.busIndex == -1 ? 0 : aGroup.busIndex;
                if (selectedBusIndex >= HardCodedBusOptions) {
                    var selectedBus = groupBuses[selectedBusIndex - HardCodedBusOptions];
                    busName = selectedBus.busName;
                }
                aGroup.busName = busName;

                Transform groupTrans;
                var existingGroup = MasterAudio.GrabGroup(aGroup.name, false);
                if (existingGroup != null)
                {
                    existingGroup.AddActorInstanceId(InstanceId);
                    groupTrans = existingGroup.transform;
                } else {
                    groupTrans = MasterAudio.CreateSoundGroup(aGroup, InstanceId, errorOnDuplicates);
                }

                // remove fx components
                // ReSharper disable ForCanBeConvertedToForeach
                for (var v = 0; v < aGroup.groupVariations.Count; v++) {
                    // ReSharper restore ForCanBeConvertedToForeach
                    var aVar = aGroup.groupVariations[v];
                    if (aVar.LowPassFilter != null) {
                        Destroy(aVar.LowPassFilter);
                    }
                    if (aVar.HighPassFilter != null) {
                        Destroy(aVar.HighPassFilter);
                    }
                    if (aVar.DistortionFilter != null) {
                        Destroy(aVar.DistortionFilter);
                    }
                    if (aVar.ChorusFilter != null) {
                        Destroy(aVar.ChorusFilter);
                    }
                    if (aVar.EchoFilter != null) {
                        Destroy(aVar.EchoFilter);
                    }
                    if (aVar.ReverbFilter != null) {
                        Destroy(aVar.ReverbFilter);
                    }
                }

                if (groupTrans == null) {
                    continue;
                }

                _groupsToRemove.Add(groupTrans);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < musicDuckingSounds.Count; i++) {
                var aDuck = musicDuckingSounds[i];
                if (aDuck.soundType == MasterAudio.NoGroupName) {
                    continue;
                }

                MasterAudio.AddSoundGroupToDuckList(aDuck.soundType, aDuck.riseVolStart, aDuck.duckedVolumeCut, aDuck.unduckTime, true);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < customEventCategories.Count; i++) {
				var aCat = customEventCategories[i];
				MasterAudio.CreateCustomEventCategoryIfNotThere(aCat.CatName, InstanceId, errorOnDuplicates, true);
			}

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < customEventsToCreate.Count; i++) {
                var anEvent = customEventsToCreate[i];
				MasterAudio.CreateCustomEvent(anEvent.EventName, anEvent.eventReceiveMode, anEvent.distanceThreshold, anEvent.eventRcvFilterMode, anEvent.filterModeQty, InstanceId, anEvent.categoryName, true, errorOnDuplicates);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < musicPlaylists.Count; i++) {
                var aPlaylist = musicPlaylists[i];
				aPlaylist.isTemporary = true;

                var existingPlaylist = MasterAudio.Instance.musicPlaylists.Find(delegate(MasterAudio.Playlist playlist)
                {
                    return playlist.playlistName == aPlaylist.playlistName && aPlaylist.isTemporary;
                });

                if (existingPlaylist != null)
                {
                    existingPlaylist.AddActorInstanceId(InstanceId);
                    continue;
                }

				MasterAudio.CreatePlaylist(aPlaylist, errorOnDuplicates);
                aPlaylist.AddActorInstanceId(InstanceId);
            }

            MasterAudio.SilenceOrUnsilenceGroupsFromSoloChange(); // to make sure non-soloed things get muted

            _hasCreated = true;

            if (itemsCreatedEventExpanded) {
				FireEvents();
            }
        }

		private void FireEvents() {
            MasterAudio.FireCustomEventNextFrame(itemsCreatedCustomEvent, _trans);
		}

        /*! \cond PRIVATE */
        public void PopulateGroupData() {
            if (_trans == null) {
                _trans = transform;
            }
            _groupsToCreate.Clear();

            for (var i = 0; i < _trans.childCount; i++) {
                var aGroup = _trans.GetChild(i).GetComponent<DynamicSoundGroup>();
                if (aGroup == null) {
                    continue;
                }

                aGroup.groupVariations.Clear();

                for (var c = 0; c < aGroup.transform.childCount; c++) {
                    var aVar = aGroup.transform.GetChild(c).GetComponent<DynamicGroupVariation>();
                    if (aVar == null) {
                        continue;
                    }

                    aGroup.groupVariations.Add(aVar);
                }

                _groupsToCreate.Add(aGroup);
            }
        }

        public static int HardCodedBusOptions {
            get { return MasterAudio.HardCodedBusOptions + ExtraHardCodedBusOptions; }
        }
        /*! \endcond */

        /// <summary>
        /// This property can be used to read and write the Dynamic Sound Groups.
        /// </summary>	
        public List<DynamicSoundGroup> GroupsToCreate {
            get { return _groupsToCreate; }
        }

		/*! \cond PRIVATE */
		public int InstanceId {
            get {
                if (_instanceId < 0)
                {
                    _instanceId = GetInstanceID();
                }

                return _instanceId;
            }
        }

        public bool ShouldShowUnityAudioMixerGroupAssignments {
            get {
                return showUnityMixerGroupAssignment;
            }
        }
        /*! \endcond */
    }
}