namespace GameFoundation.Scripts.Network.Authentication
{
    using System.Net;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>Base authentication with google, facebook...  </summary>
    public abstract class BaseAuthenticationService
    {
        protected string            AccessToken = "";
        protected ILogService       Logger;
        protected HttpListener      Http;
        protected GameFoundationLocalData         localData;
        protected PlayerData        playerData;
        protected DataLoginServices DataLoginServices;
        [Inject]
        private void Init(SignalBus signalBus, ILogService loggerParam, DataLoginServices servicesParam, GameFoundationLocalData localData, PlayerState playerState,
            DataLoginServices dataLoginServices)
        {
            this.Logger            = loggerParam;
            this.localData         = localData;
            this.playerData        = playerState.PlayerData;
            this.DataLoginServices = dataLoginServices;
        }

        public abstract UniTask<string> OnLogIn(CancellationTokenSource cancellationToken);
    }
}