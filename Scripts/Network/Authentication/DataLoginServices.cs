namespace GameFoundation.Scripts.Network.Authentication
{
    using System;
    using UniRx;

    public enum AuthenticationStatus
    {
        NonAuthen,
        Authenticated,
        Authenticating,
        FailWithFbToken,
        FailWithGoogleToken,
        FailWithRefreshToken,
        FailWithNoInternetOrTimeout,
        FailWithInvalidOTP
    }

    public enum TypeLogIn
    {
        None     = 0,
        Facebook = 1,
        Google   = 2,
        OTPCode  = 3
    }

    public enum SendCodeStatus
    {
        None,
        Sending,
        Complete,
        Error
    }

    /// <summary>Check user authentication with google, facebook...  </summary>
    public class DataLoginServices
    {
        public TypeLogIn                              currentTypeLogin = TypeLogIn.None;
        public ReactiveProperty<AuthenticationStatus> Status           = new ReactiveProperty<AuthenticationStatus>(AuthenticationStatus.NonAuthen);
        public ReactiveProperty<SendCodeStatus>       SendCodeComplete = new ReactiveProperty<SendCodeStatus>(SendCodeStatus.None);
    }
}