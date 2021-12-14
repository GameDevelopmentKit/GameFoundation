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
        InvalidRefreshToken,
        RefreshTokenNotFound,
        FailWithNoInternetOrTimeout,
        FailWithRefreshToken,
        InvalidOTP,
        InvalidEmail
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
        Sent,
        EmailIsInvalid,
        TooManyRequest
    }

    /// <summary>Check user authentication with google, facebook...  </summary>
    public class DataLoginServices
    {
        public TypeLogIn                              currentTypeLogin = TypeLogIn.None;
        public ReactiveProperty<AuthenticationStatus> Status           = new ReactiveProperty<AuthenticationStatus>(AuthenticationStatus.NonAuthen);
        public ReactiveProperty<SendCodeStatus>       SendCodeStatus   = new ReactiveProperty<SendCodeStatus>(Authentication.SendCodeStatus.None);
    }
}