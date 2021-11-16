namespace GameFoundation.Scripts.Network.Websocket
{
    using System.Threading.Tasks;
    using UniRx;

    public enum ServiceStatus
    {
        NotInitialize,
        Initialized,
        Connected,
        Closed
    }

    public interface IWebSocketService
    {
        public ReactiveProperty<ServiceStatus> State { get;}

        Task OpenConnection();

        Task CloseConnection();
    }
}