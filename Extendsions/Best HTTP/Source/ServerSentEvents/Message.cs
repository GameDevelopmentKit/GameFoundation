#if !BESTHTTP_DISABLE_SERVERSENT_EVENTS

using System;

namespace BestHTTP.ServerSentEvents
{
    public sealed class Message
    {
        /// <summary>
        /// Event Id of the message. If it's null, then it's not present.
        /// </summary>
        public string Id { get; internal set; }

        /// <summary>
        /// Name of the event, or an empty string.
        /// </summary>
        public string Event { get; internal set; }

        /// <summary>
        /// The actual payload of the message.
        /// </summary>
        public string Data { get; internal set; }

        /// <summary>
        /// A reconnection time, in milliseconds. This must initially be a user-agent-defined value, probably in the region of a few seconds.
        /// </summary>
        public TimeSpan Retry { get; internal set; }

        /// <summary>
        /// If this is true, the Data property holds the comment sent by the server.
        /// </summary>
        internal bool IsComment { get; set; }

        public override string ToString()
        {
            return string.Format("\"{0}\": \"{1}\"", Event, Data);
        }
    }
}

#endif
