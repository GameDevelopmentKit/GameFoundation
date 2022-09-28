namespace GameFoundation.Scripts.Network.Authentication.ApiHandler
{
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Network.Authentication;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Network.WebService.Requests;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;

    public class LoginHttpRequest : BaseHttpRequest<LoginResponseData>
    {
        private readonly DataLoginServices       dataLoginServices;
        private readonly HandleLocalDataServices handleLocalDataServices;
        private readonly GameFoundationLocalData localData;
        private readonly PlayerState             mechPlayerState;

        public LoginHttpRequest(ILogService logger, DataLoginServices dataLoginServices,
            GameFoundationLocalData localData,
            HandleLocalDataServices handleLocalDataServices, PlayerState mechPlayerState) : base(logger)
        {
            this.dataLoginServices       = dataLoginServices;
            this.localData               = localData;
            this.handleLocalDataServices = handleLocalDataServices;
            this.mechPlayerState         = mechPlayerState;
        }
        public override void Process(LoginResponseData responseData)
        {
            var jwtToken       = responseData.JwtToken;
            var refreshToken   = responseData.RefreshToken;
            var expirationTime = responseData.ExpirationTime;
            var email          = responseData.Email;
            var fullName       = responseData.Fullname;
            var avatar         = responseData.Picture;
            var wallet         = responseData.WalletAddress;
            if (string.IsNullOrEmpty(jwtToken)) return;
            this.SaveDataToLocalData(jwtToken, refreshToken, expirationTime, email, fullName, avatar, wallet);
        }

        private void SaveDataToLocalData(string jwtToken, string refreshToken, long expirationTime, string email,
            string fullName, string avatar, string wallet)
        {
            this.localData.ServerToken.JwtToken       = jwtToken;
            this.localData.ServerToken.RefreshToken   = refreshToken;
            this.localData.ServerToken.ExpirationTime = expirationTime;
            this.localData.ServerToken.WalletAddress  = wallet;
            switch (this.dataLoginServices.currentTypeLogin)
            {
                case TypeLogIn.Facebook:
                    this.localData.UserDataLogin.LastLogin = (int)TypeLogIn.Facebook;
                    this.mechPlayerState.PlayerData.Name   = this.localData.UserDataLogin.FacebookLogin.UserName;
                    this.mechPlayerState.PlayerData.Avatar = this.localData.UserDataLogin.FacebookLogin.URLImage;
                    break;
                case TypeLogIn.Google:
                    this.localData.UserDataLogin.LastLogin = (int)TypeLogIn.Google;
                    this.mechPlayerState.PlayerData.Name   = this.localData.UserDataLogin.GoogleLogin.UserName;
                    this.mechPlayerState.PlayerData.Avatar = this.localData.UserDataLogin.GoogleLogin.URLImage;
                    break;
                case TypeLogIn.OTPCode:
                    this.localData.UserDataLogin.LastLogin = (int)TypeLogIn.OTPCode;
                    this.mechPlayerState.PlayerData.Name   = fullName;
                    this.mechPlayerState.PlayerData.Avatar = avatar ?? "";
                    this.mechPlayerState.PlayerData.Email  = email;
                    break;
                case TypeLogIn.None:
                    break;
            }

            this.handleLocalDataServices.Save(this.localData, true);
            this.dataLoginServices.Status.Value = AuthenticationStatus.Authenticated;
        }

        public override void ErrorProcess(int statusCode)
        {
            switch (statusCode)
            {
                case 1:
                    // ToDo
                    break;
                case 2:
                    this.dataLoginServices.Status.Value = this.dataLoginServices.currentTypeLogin == TypeLogIn.Google
                        ? AuthenticationStatus.FailWithGoogleToken
                        : AuthenticationStatus.FailWithFbToken;
                    break;
                case 3:
                    this.dataLoginServices.Status.Value = AuthenticationStatus.InvalidRefreshToken;
                    break;
                case 4:
                    this.dataLoginServices.Status.Value = AuthenticationStatus.RefreshTokenNotFound;
                    break;
                case 5:
                    this.dataLoginServices.Status.Value = AuthenticationStatus.InvalidOTP;
                    break;
                case 6:
                    this.dataLoginServices.Status.Value = AuthenticationStatus.InvalidEmail;
                    break;
                default:
                    base.ErrorProcess(statusCode);
                    break;
            }
        }
    }
}