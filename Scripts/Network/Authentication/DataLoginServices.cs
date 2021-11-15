namespace Mech.Scenes.LoadingScene.LoginScreen
{
    using UniRx;

    public enum AuthenticationStatus
    {
        NonAuthen,
        Authenticated,
        Authenticating,
        FailWithFbToken,
        FailWithGoogleToken,
        FailWithRefreshToken,
        FailWithNoInternetOrTimeout
    }

    public enum TypeLogIn
    {
        None     = 0,
        Facebook = 1,
        Google   = 2
    }

    /// <summary>Check user authentication with google, facebook...  </summary>
    public class DataLoginServices
    {
        public TypeLogIn                              currentTypeLogin = TypeLogIn.None;
        public ReactiveProperty<AuthenticationStatus> Status           = new ReactiveProperty<AuthenticationStatus>(AuthenticationStatus.NonAuthen);
    }
}