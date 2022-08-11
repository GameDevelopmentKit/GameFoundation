namespace GameFoundation.Scripts.Network.WebService.Requests
{
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.Utils;

    /// <summary>Will response LoginResponseData</summary>
    [HttpRequestDefinition("otp/verify")]
    public class VerifyOtpRequestData : IHttpRequestData
    {
        public string Email { get; set; }
        public string Code  { get; set; }
    }
}