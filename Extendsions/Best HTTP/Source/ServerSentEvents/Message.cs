#if !BESTHTTP_DISABLE_SERVERSENT_EVENTS

namespace BestHTTP.ServerSentEvents
{
    using System;
    using BestHTTP.PlatformSupport.IL2CPP;

    [Preserve]
    public sealed class Message
    {
        /// <summary>
        /// Event Id of the message. If it's null, then it's not present.
        /// </summary>
        [Preserve]
        public string Id { get; internal set; }

        /// <summary>
        /// Name of the event, or an empty string.
        /// </summary>
        [Preserve]
        public string Event { get; internal set; }

        /// <summary>
        /// The actual payload of the message.
        /// </summary>
        [Preserve]
        public string Data { get; internal set; }

        /// <summary>
        /// A reconnection time, in milliseconds. This must initially be a user-agent-defined value, probably in the region of a few seconds.
        /// </summary>
        [Preserve]
        public TimeSpan Retry { get; internal set; }

        /// <summary>
        /// If this is true, the Data property holds the comment sent by the server.
        /// </summary>
        [Preserve]
        internal bool IsComment { get; set; }

        public override string ToString()
        {
            return string.Format("\"{0}\": \"{1}\"", Event, Data);
        }
    }
}

#endif
