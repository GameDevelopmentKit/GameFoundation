namespace GameFoundation.Scripts.Network.Websocket
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BestHTTP.SignalRCore;
    using BestHTTP.SignalRCore.Encoders;
    using GameFoundation.Scripts.GameManager;
    using GameFoundation.Scripts.Utilities.LogService;
    using UniRx;
    using UnityEngine;

    /// <summary>Temporary websocket service (signalR) for battle.</summary>
    public class BestHttpWebsocketService : IWebSocketService
    {
        #region zenject

        protected readonly GameFoundationLocalData   localData;
        protected readonly ILogService logger;

        #endregion

        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public ReactiveProperty<ServiceStatus> State         { get; private set; }
        public HubConnection                   HubConnection { get; set; }

        public BestHttpWebsocketService(GameFoundationLocalData localData, ILogService logger)
        {
            this.localData = localData;
            this.logger    = logger;
            this.State     = new ReactiveProperty<ServiceStatus>(ServiceStatus.NotInitialize);
        }

        public virtual void Init(string uri, string token)
        {
            this.HubConnection = new HubConnection(new Uri(uri), new MessagePackProtocol())
            {
                AuthenticationProvider = new CustomAuthenticator(token, Application.version),
            };
            this.HubConnection.OnConnected    += this.OnConnected;
            this.HubConnection.OnClosed       += this.OnClose;
            this.HubConnection.OnError        += this.OnError;
            this.HubConnection.OnReconnecting += this.OnReconnecting;
            this.HubConnection.OnReconnected  += this.OnReconnected;

            this.State.Value = ServiceStatus.Initialized;
        }

        public async Task OpenConnection()
        {
            await this.HubConnection.ConnectAsync();
            this.State.Value = ServiceStatus.Connected;
        }

        public async Task CloseConnection()
        {
            await this.HubConnection.CloseAsync();
            this.State.Value = ServiceStatus.Closed;
        }

        private void OnReconnected(HubConnection connection) { this.logger.Log($"{connection.Uri} reconnected!!"); }

        private void OnReconnecting(HubConnection connection, string arg2) { this.logger.Log($"{connection.Uri} is reconnecting!!"); }

        private void OnError(HubConnection connection, string error)
        {
            this.CancellationTokenSource.Cancel();
            this.logger.Log($"{connection.Uri} error: {error}");
        }

        private void OnClose(HubConnection connection)
        {
            this.logger.Log($"SignalR closed connection to {connection.Uri}");
            this.CancellationTokenSource.Cancel();
        }

        private void OnConnected(HubConnection connection) { this.logger.Log($"SignalR connected to {connection.Uri}"); }
    }
}