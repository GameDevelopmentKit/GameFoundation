namespace GameFoundation.Scripts.Network
{
    /// <summary>Our global network config for HttpRequest and SignalR.</summary>
    public class NetworkConfig
    {
        public string HttpServerUri      { get; set; } = "https://www.google.com/"; // our web service server URI
        public double HttpRequestTimeout { get; set; } = 10;                        // Default timeout for all http request
        public double DownloadRequestTimeout { get; set; } = 600;                   // Default timeout for download
        public string BattleWebsocketUri { get; set; }                              //our websocket service server URI
    }
}