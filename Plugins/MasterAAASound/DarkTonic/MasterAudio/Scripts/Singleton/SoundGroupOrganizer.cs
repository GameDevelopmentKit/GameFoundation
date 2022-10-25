/*! \cond PRIVATE */
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    public class SoundGroupOrganizer : MonoBehaviour {
        // ReSharper disable InconsistentNaming
        public GameObject dynGroupTemplate;
        public GameObject dynVariationTemplate;
        public GameObject maGroupTemplate;
        public GameObject maVariationTemplate;

        public MasterAudio.DragGroupMode curDragGroupMode = MasterAudio.DragGroupMode.OneGroupPerClip;
        public MasterAudio.AudioLocation bulkVariationMode = MasterAudio.AudioLocation.Clip;
        public SystemLanguage previewLanguage = SystemLanguage.English;
        public bool useTextGroupFilter = false;
        public string textGroupFilter = string.Empty;
        public TransferMode transMode = TransferMode.None;
        public GameObject sourceObject = null;
        public List<SoundGroupSelection> selectedSourceSoundGroups = new List<SoundGroupSelection>();
        public GameObject destObject = null;
        public List<SoundGroupSelection> selectedDestSoundGroups = new List<SoundGroupSelection>();
        public MAItemType itemType = MAItemType.SoundGroups;
        public List<CustomEventSelection> selectedSourceCustomEvents = new List<CustomEventSelection>();
        public List<CustomEventSelection> selectedDestCustomEvents = new List<CustomEventSelection>();
        public List<CustomEvent> customEvents = new List<CustomEvent>();
        public List<CustomEventCategory> customEventCategories = new List<CustomEventCategory> {
            new CustomEventCategory()
        };
        public string newEventName = "my event";
        public string newCustomEventCategoryName = "New Category";
        public string addToCustomEventCategoryName = "New Category";
        // ReSharper restore InconsistentNaming

        public class CustomEventSelection {
            public CustomEvent Event;
            public bool IsSelected;

            public CustomEventSelection(CustomEvent cEvent, bool isSelected) {
                Event = cEvent;
                IsSelected = isSelected;
            }
        }

        public class SoundGroupSelection {
            public GameObject Go;
            public bool IsSelected;

            public SoundGroupSelection(GameObject go, bool isSelected) {
                Go = go;
                IsSelected = isSelected;
            }
        }

        public enum MAItemType {
            SoundGroups,
            CustomEvents
        }

        public enum TransferMode {
            None,
            Import,
            Export
        }

        // ReSharper disable once UnusedMember.Local
        private void Awake() {
            Debug.LogError(
                "You have a Sound Group Organizer prefab in this Scene. You should never play a Scene with that type of prefab as it could take up tremendous amounts of audio memory. Please use a Sandbox Scene for that, which is only used to make changes to that prefab and apply them. This Sandbox Scene should never be a Scene that is played in the game.");
        }
    }
}
/*! \endcond */