using System.Collections.Generic;
using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
    // ReSharper disable once CheckNamespace
    // ReSharper disable once InconsistentNaming
    public class MA_SampleICustomEventReceiver : MonoBehaviour, ICustomEventReceiver
    {
        private readonly List<string> _eventsSubscribedTo = new List<string>() {
        "PlayerMoved",
        "PlayerStoppedMoving"
    };

        // ReSharper disable once UnusedMember.Local
        void Awake()
        {
        }

        // ReSharper disable once UnusedMember.Local
        void Start()
        {
            CheckForIllegalCustomEvents();
        }

        // Use this for initialization
        // ReSharper disable once UnusedMember.Local
        void OnEnable()
        {
            RegisterReceiver();
        }

        // ReSharper disable once UnusedMember.Local
        void OnDisable()
        {
            if (MasterAudio.SafeInstance == null || MasterAudio.AppIsShuttingDown)
            {
                return;
            }
            UnregisterReceiver();
        }


        #region ICustomEventReceiver methods
        public void CheckForIllegalCustomEvents()
        {
            // this is totally optional, but implementing this method will save you debugging time because you will know right away if your event(s) don't exist if you call this in Start.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _eventsSubscribedTo.Count; i++)
            {
                var eventName = _eventsSubscribedTo[i];
                if (!MasterAudio.CustomEventExists(eventName))
                {
                    Debug.LogError("Custom Event, listened to by '" + name + "', could not be found in MasterAudio.");
                }
            }
        }

        public void ReceiveEvent(string customEventName, Vector3 originPoint)
        {
            switch (customEventName)
            {
                case "PlayerMoved":
                    Debug.Log("PlayerMoved event recieved by '" + name + "'.");
                    break;
                case "PlayerStoppedMoving":
                    Debug.Log("PlayerStoppedMoving event recieved by '" + name + "'.");
                    break;
            }
        }

        public bool SubscribesToEvent(string customEventName)
        {
            if (string.IsNullOrEmpty(customEventName))
            {
                return false;
            }

            return _eventsSubscribedTo.Contains(customEventName);
        }

        public void RegisterReceiver()
        {
            MasterAudio.AddCustomEventReceiver(this, transform);
        }

        public void UnregisterReceiver()
        {
            MasterAudio.RemoveCustomEventReceiver(this);
        }

        public IList<AudioEventGroup> GetAllEvents()
        {
            var events = new List<AudioEventGroup>();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _eventsSubscribedTo.Count; i++)
            {
                events.Add(new AudioEventGroup
                {
                    customEventName = _eventsSubscribedTo[i]
                });
            }

            return events;
        }

        #endregion
    }

}