namespace GameFoundation.Scripts.Network.WebService.Requests
{
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.Utils;

    [HttpRequestDefinition("login/refresh")]
    public class RefreshTokenRequestData : IHttpRequestData
    {
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenResponseData : LoginResponseData
    {
    }
}