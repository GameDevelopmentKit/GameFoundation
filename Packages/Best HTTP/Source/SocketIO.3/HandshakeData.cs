#if !BESTHTTP_DISABLE_SOCKETIO

namespace BestHTTP.SocketIO3
{
    using System.Collections.Generic;
    using BestHTTP.PlatformSupport.IL2CPP;

    /// <summary>
    /// Helper class to parse and hold handshake information.
    /// </summary>
    [Preserve]
    public sealed class HandshakeData
    {
        /// <summary>
        /// Session ID of this connection.
        /// </summary>
        [Preserve]
        public string Sid { get; private set; }

        /// <summary>
        /// List of possible upgrades.
        /// </summary>
        [Preserve]
        public List<string> Upgrades { get; private set; }

        /// <summary>
        /// What interval we have to set a ping message.
        /// </summary>
        [Preserve]
        public int PingInterval { get; private set; }

        /// <summary>
        /// What time have to pass without an answer to our ping request when we can consider the connection disconnected.
        /// </summary>
        [Preserve]
        public int PingTimeout { get; private set; }
    }
}

#endif
