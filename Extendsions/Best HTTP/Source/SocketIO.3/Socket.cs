#if !BESTHTTP_DISABLE_SOCKETIO

using System;

namespace BestHTTP.SocketIO3
{
    using BestHTTP;
    using BestHTTP.Logger;
    using BestHTTP.SocketIO3.Events;

    public delegate void SocketIOCallback(Socket socket, IncomingPacket packet, params object[] args);
    public delegate void SocketIOAckCallback(Socket socket, IncomingPacket packet, params object[] args);

    public struct EmitBuilder
    {
        private Socket socket;
        internal bool isVolatile;
        internal int id;

        internal EmitBuilder(Socket s)
        {
            this.socket = s;
            this.isVolatile = false;
            this.id = -1;
        }

        public EmitBuilder ExpectAcknowledgement(Action callback)
        {
            this.id = this.socket.Manager.NextAckId;
            string name = IncomingPacket.GenerateAcknowledgementNameFromId(this.id);

            this.socket.TypedEventTable.Register(name, null, _ => callback(), true);
            return this;
        }

        public EmitBuilder ExpectAcknowledgement<T>(Action<T> callback)
        {
            this.id = this.socket.Manager.NextAckId;
            string name = IncomingPacket.GenerateAcknowledgementNameFromId(this.id);

            this.socket.TypedEventTable.Register(name, new Type[] { typeof(T) }, (args) => callback((T)args[0]), true);

            return this;
        }

        public EmitBuilder Volatile()
        {
            this.isVolatile = true;
            return this;
        }

        public Socket Emit(string eventName, params object[] args)
        {
            bool blackListed = EventNames.IsBlacklisted(eventName);
            if (blackListed)
                throw new ArgumentException("Blacklisted event: " + eventName);

            var packet = this.socket.Manager.Parser.CreateOutgoing(this.socket, SocketIOEventTypes.Event, this.id, eventName, args);
            packet.IsVolatile = this.isVolatile;
            (this.socket.Manager as IManager).SendPacket(packet);

            return this.socket;
        }
    }

    /// <summary>
    /// This class represents a Socket.IO namespace.
    /// </summary>
    public sealed class Socket : ISocket
    {
        #region Public Properties

        /// <summary>
        /// The SocketManager instance that created this socket.
        /// </summary>
        public SocketManager Manager { get; private set; }

        /// <summary>
        /// The namespace that this socket is bound to.
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Unique Id of the socket.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// True if the socket is connected and open to the server. False otherwise.
        /// </summary>
        public bool IsOpen { get; private set; }

        public LoggingContext Context { get; private set; }

        #endregion

        #region Privates

        internal TypedEventTable TypedEventTable;

        #endregion

        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal Socket(string nsp, SocketManager manager)
        {
            this.Context = new LoggingContext(this);
            this.Context.Add("nsp", nsp);

            this.Namespace = nsp;
            this.Manager = manager;
            this.IsOpen = false;
            this.TypedEventTable = new TypedEventTable(this);

            this.On<ConnectResponse>(EventNames.GetNameFor(SocketIOEventTypes.Connect), OnConnected);
        }

        private void OnConnected(ConnectResponse resp)
        {
            this.Id = resp.sid;
            this.IsOpen = true;
        }

        #region Socket Handling

        /// <summary>
        /// Internal function to start opening the socket.
        /// </summary>
        void ISocket.Open()
        {
            HTTPManager.Logger.Information("Socket", string.Format("Open - Manager.State = {0}", Manager.State), this.Context);

            // The transport already established the connection
            if (Manager.State == SocketManager.States.Open)
                OnTransportOpen();
            else if (Manager.Options.AutoConnect && Manager.State == SocketManager.States.Initial)
                    Manager.Open();
        }

        /// <summary>
        /// Disconnects this socket/namespace.
        /// </summary>
        public void Disconnect()
        {
            (this as ISocket).Disconnect(true);
        }

        /// <summary>
        /// Disconnects this socket/namespace.
        /// </summary>
        void ISocket.Disconnect(bool remove)
        {
            // Send a disconnect packet to the server
            if (IsOpen)
            {
                var packet = this.Manager.Parser.CreateOutgoing(this, SocketIOEventTypes.Disconnect, -1, null, null);
                (Manager as IManager).SendPacket(packet);

                // IsOpen must be false, because in the OnPacket preprocessing the packet would call this function again
                IsOpen = false;
                (this as ISocket).OnPacket(new IncomingPacket(TransportEventTypes.Message, SocketIOEventTypes.Disconnect, this.Namespace, -1));
            }

            if (remove)
            {
                this.TypedEventTable.Clear();
                
                (Manager as IManager).Remove(this);
            }
        }

        #endregion

        #region Emit Implementations

        /// <summary>
        /// By emitting a volatile event, if the transport isn't ready the event is going to be discarded.
        /// </summary>
        public EmitBuilder Volatile()
        {
            return new EmitBuilder(this) { isVolatile = true };
        }

        public EmitBuilder ExpectAcknowledgement(Action callback)
        {
            return new EmitBuilder(this).ExpectAcknowledgement(callback);
        }

        public EmitBuilder ExpectAcknowledgement<T>(Action<T> callback)
        {
            return new EmitBuilder(this).ExpectAcknowledgement<T>(callback);
        }

        public Socket Emit(string eventName, params object[] args)
        {
            return new EmitBuilder(this).Emit(eventName, args);
        }

        private IncomingPacket currentPacket = IncomingPacket.Empty;
        public Socket EmitAck(params object[] args)
        {
            if (this.currentPacket.Equals(IncomingPacket.Empty))
                throw new ArgumentNullException("currentPacket");

            if (currentPacket.Id < 0 || (currentPacket.SocketIOEvent != SocketIOEventTypes.Event && currentPacket.SocketIOEvent != SocketIOEventTypes.BinaryEvent))
                throw new ArgumentException("Wrong packet - you can't send an Ack for a packet with id < 0 or SocketIOEvent != Event or SocketIOEvent != BinaryEvent!");

            var eventType = currentPacket.SocketIOEvent == SocketIOEventTypes.Event ? SocketIOEventTypes.Ack : SocketIOEventTypes.BinaryAck;

            (Manager as IManager).SendPacket(this.Manager.Parser.CreateOutgoing(this, eventType, currentPacket.Id, null, args));

            return this;
        }

        #endregion

        #region On Implementations

        public void On(SocketIOEventTypes eventType, Action callback)
        {
            this.TypedEventTable.Register(EventNames.GetNameFor(eventType), null, _ => callback());
        }

        public void On<T>(SocketIOEventTypes eventType, Action<T> callback)
        {
            this.TypedEventTable.Register(EventNames.GetNameFor(eventType), new Type[] { typeof(T) }, (args) => callback((T)args[0]));
        }

        public void On(string eventName, Action callback)
        {
            this.TypedEventTable.Register(eventName, null, _ => callback());
        }

        public void On<T>(string eventName, Action<T> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T) }, (args) => callback((T)args[0]));
        }

        public void On<T1, T2>(string eventName, Action<T1, T2> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2) }, (args) => callback((T1)args[0], (T2)args[1]));
        }

        public void On<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2]));
        }

        public void On<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));
        }

        public void On<T1, T2, T3, T4, T5>(string eventName, Action<T1, T2, T3, T4, T5> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]));
        }

        #endregion

        #region Once Implementations

        public void Once(string eventName, Action callback)
        {
            this.TypedEventTable.Register(eventName, null, _ => callback(), true);
        }

        public void Once<T>(string eventName, Action<T> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T) }, (args) => callback((T)args[0]), true);
        }

        public void Once<T1, T2>(string eventName, Action<T1, T2> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2) }, (args) => callback((T1)args[0], (T2)args[1]), true);
        }

        public void Once<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2]), true);
        }

        public void Once<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]), true);
        }

        public void Once<T1, T2, T3, T4, T5>(string eventName, Action<T1, T2, T3, T4, T5> callback)
        {
            this.TypedEventTable.Register(eventName, new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }, (args) => callback((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]), true);
        }

        #endregion

        #region Off Implementations

        /// <summary>
        /// Remove all callbacks for all events.
        /// </summary>
        public void Off()
        {
            this.TypedEventTable.Clear();
        }

        /// <summary>
        /// Removes all callbacks to the given event.
        /// </summary>
        public void Off(string eventName)
        {
            this.TypedEventTable.Unregister(eventName);
        }

        /// <summary>
        /// Removes all callbacks to the given event.
        /// </summary>
        public void Off(SocketIOEventTypes type)
        {
            Off(EventNames.GetNameFor(type));
        }

        #endregion

        #region Packet Handling

        /// <summary>
        /// Last call of the OnPacket chain(Transport -> Manager -> Socket), we will dispatch the event if there is any callback
        /// </summary>
        void ISocket.OnPacket(IncomingPacket packet)
        {
            // Some preprocessing of the packet
            switch(packet.SocketIOEvent)
            {
                case SocketIOEventTypes.Connect:
                    break;

                case SocketIOEventTypes.Disconnect:
                    if (IsOpen)
                    {
                        IsOpen = false;
                        this.TypedEventTable.Call(packet);
                        Disconnect();
                    }
                    break;
            }

            try
            {
                this.currentPacket = packet;

                // Dispatch the event to all subscriber
                this.TypedEventTable.Call(packet);
            }
            finally
            {
                this.currentPacket = IncomingPacket.Empty;
            }
        }

        #endregion

        public Subscription GetSubscription(string name)
        {
            return this.TypedEventTable.GetSubscription(name);
        }

        /// <summary>
        /// Emits an internal packet-less event to the user level.
        /// </summary>
        void ISocket.EmitEvent(SocketIOEventTypes type, params object[] args)
        {
            (this as ISocket).EmitEvent(EventNames.GetNameFor(type), args);
        }

        /// <summary>
        /// Emits an internal packet-less event to the user level.
        /// </summary>
        void ISocket.EmitEvent(string eventName, params object[] args)
        {
            if (!string.IsNullOrEmpty(eventName))
                this.TypedEventTable.Call(eventName, args);
        }

        void ISocket.EmitError(string msg)
        {
            var outcoming = this.Manager.Parser.CreateOutgoing(this, SocketIOEventTypes.Error, -1, null, new Error(msg));
            IncomingPacket packet = IncomingPacket.Empty;
            if (outcoming.IsBinary)
                packet = this.Manager.Parser.Parse(this.Manager, outcoming.PayloadData);
            else
                packet = this.Manager.Parser.Parse(this.Manager, outcoming.Payload);

            (this as ISocket).EmitEvent(SocketIOEventTypes.Error, packet.DecodedArg ?? packet.DecodedArgs);
        }

        #region Private Helper Functions

        /// <summary>
        /// Called when the underlying transport is connected
        /// </summary>
        internal void OnTransportOpen()
        {
            HTTPManager.Logger.Information("Socket", "OnTransportOpen - IsOpen: " + this.IsOpen, this.Context);

            if (this.IsOpen)
                return;

            object authData = null;
            try
            {
                authData = this.Manager.Options.Auth != null ? this.Manager.Options.Auth(this.Manager, this) : null;
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("Socket", "OnTransportOpen - Options.Auth", ex, this.Context);
            }

            try
            {
                (Manager as IManager).SendPacket(this.Manager.Parser.CreateOutgoing(this, SocketIOEventTypes.Connect, -1, null, authData));
            }
            catch (Exception ex)
            {
                HTTPManager.Logger.Exception("Socket", "OnTransportOpen", ex, this.Context);
            }
        }

        #endregion
    }
}

#endif
