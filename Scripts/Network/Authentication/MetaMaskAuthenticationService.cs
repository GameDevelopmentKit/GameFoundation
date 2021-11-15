namespace Mech.Services.Network.Authentication
{
    using System.Threading;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using Mech.Scenes.LoadingScene.LoginScreen;

    /// <summary>MetaMaskLogin handle  </summary>
    public class MetaMaskAuthenticationService : BaseAuthenticationService
    {
        public async override UniTask<string> OnLogIn(CancellationTokenSource cancellationToken) { return ""; }
    }
}