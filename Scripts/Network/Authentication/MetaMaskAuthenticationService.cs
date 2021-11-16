namespace GameFoundation.Scripts.Network.Authentication
{
    using System.Threading;
    using Cysharp.Threading.Tasks;

    /// <summary>MetaMaskLogin handle  </summary>
    public class MetaMaskAuthenticationService : BaseAuthenticationService
    {
        public async override UniTask<string> OnLogIn(CancellationTokenSource cancellationToken) { return ""; }
    }
}