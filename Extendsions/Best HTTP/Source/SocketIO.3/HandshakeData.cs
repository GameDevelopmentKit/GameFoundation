#if !BESTHTTP_DISABLE_SOCKETIO

using System.Collections.Generic;

namespace BestHTTP.SocketIO3
{
    /// <summary>
    /// Helper class to parse and hold handshake information.
    /// </summary>
    public sealed class HandshakeData
    {
        /// <summary>
        /// Session ID of this connection.
        /// </summary>
        public string Sid { get; private set; }

        /// <summary>
        /// List of possible upgrades.
        /// </summary>
        public List<string> Upgrades { get; private set; }

        /// <summary>
        /// What interval we have to set a ping message.
        /// </summary>
        public int PingInterval { get; private set; }

        /// <summary>
        /// What time have to pass without an answer to our ping request when we can consider the connection disconnected.
        /// </summary>
        public int PingTimeout { get; private set; }
    }
}

#endif
