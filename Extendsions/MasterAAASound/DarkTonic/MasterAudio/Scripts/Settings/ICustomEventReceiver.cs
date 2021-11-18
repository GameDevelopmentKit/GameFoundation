using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    // ReSharper disable once CheckNamespace
    /// <summary>
    /// This interface is used by EventSounds and can be implemented on your custom classes to listen for Custom Events that are fired.
    /// </summary>
    public interface ICustomEventReceiver {
        // this interface is used to "listen" to custom events that MasterAudio transmits.
        /// <summary>
        /// This checks for events that are not found in MasterAudio. It's a good idea to call this in Start (Awake is too early), and save yourself some troubleshooting time! Optional
        /// </summary>
        void CheckForIllegalCustomEvents();

        /// <summary>
        /// This receives the event when it's fired.
        /// </summary>
        void ReceiveEvent(string customEventName, Vector3 originPoint);

        /// <summary>
        /// This returns a bool of whether the specified custom event is subscribed to in this class
        /// </summary>
        bool SubscribesToEvent(string customEventName);

        /// <summary>
        /// Registers the receiver with MasterAudio. Call this in OnEnable
        /// </summary>
        void RegisterReceiver();

        /// <summary>
        /// Unregisters the receiver with MasterAudio. Call this in OnDisable
        /// </summary>
        void UnregisterReceiver();

        /// <summary>
        /// Retrieves a list of all the events this receiver subscribes to
        /// </summary>
        IList<AudioEventGroup> GetAllEvents();
    }
}