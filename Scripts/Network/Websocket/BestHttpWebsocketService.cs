namespace GameFoundation.Scripts.Network.Websocket
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BestHTTP.SignalRCore;
    using BestHTTP.SignalRCore.Encoders;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.LogService;
    using UniRx;

    /// <summary>Temporary websocket service (signalR) for battle.</summary>
    public class BestHttpWebsocketService : IWebSocketService
    {
        #region zenject

        protected readonly GameFoundationLocalData localData;
        protected readonly ILogService             logger;

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
                AuthenticationProvider = new CustomAuthenticator(token, MechVersion.Version),
                Options                = { SkipNegotiation = true, PreferedTransport = TransportTypes.WebSocket }
            };

            this.HubConnection.OnConnected    += this.OnConnected;
            this.HubConnection.OnClosed       += this.OnClose;
            this.HubConnection.OnError        += this.OnHubError;
            this.HubConnection.OnReconnecting += this.OnReconnecting;
            this.HubConnection.OnReconnected  += this.OnReconnected;

            this.State.Value = ServiceStatus.Initialized;
        }

        public async Task OpenConnection()
        {
            this.logger.Log($"[SignalR] OpenConnection");
            await this.HubConnection.ConnectAsync();
            this.State.Value = ServiceStatus.Connected;
        }

        public async Task CloseConnection()
        {
            this.logger.Log($"[SignalR] CloseConnection");
            await this.HubConnection.CloseAsync();
            this.State.Value = ServiceStatus.Closed;
        }

        protected Task<TResult> InvokeAsync<TResult>(string target, params object[] args)
        {
            try
            {
                if (this.State.Value == ServiceStatus.Connected)
                {
                    return this.HubConnection.InvokeAsync<TResult>(target, this.CancellationTokenSource.Token, args);
                }

                this.logger.Warning($"Not in Connected state! Current state: {this.State.Value}");
            }
            catch (Exception e)
            {
                this.OnInvokeError(e);
            }

            return Task.FromResult(default(TResult));
        }

        protected Task SendAsync(string target, params object[] args)
        {
            try
            {
                if (this.State.Value == ServiceStatus.Connected)
                {
                    return this.HubConnection.SendAsync(target, this.CancellationTokenSource.Token, args);;
                }
                
                this.logger.Warning($"Not in Connected state! Current state: {this.State.Value}");
            }
            catch (Exception e)
            {
                this.OnInvokeError(e);
            }
            
            return Task.CompletedTask;
        }

        private void OnReconnected(HubConnection connection) { this.logger.Log($"[SignalR] {connection.Uri} reconnected!!"); }

        private void OnReconnecting(HubConnection connection, string arg2) { this.logger.Log($"[SignalR] {connection.Uri} is reconnecting!!"); }

        protected virtual void OnHubError(HubConnection connection, string error)
        {
            this.CancellationTokenSource.Cancel();
            this.logger.Log($"[SignalR] {connection.Uri} error: {error}");
        }

        private void OnClose(HubConnection connection)
        {
            this.logger.Log($"[SignalR] closed connection to {connection.Uri}");
            this.CancellationTokenSource.Cancel();
        }

        private void OnConnected(HubConnection connection) { this.logger.Log($"[SignalR] connected to {connection.Uri}"); }

        protected virtual void OnInvokeError(Exception exception) { this.logger.Exception(exception); }
    }
}