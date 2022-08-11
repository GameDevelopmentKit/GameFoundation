namespace GameFoundation.Scripts.Network.WebService.Requests
{
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.Utils;

    [HttpRequestDefinition("login/authentication")]
    public class LoginRequestData : IHttpRequestData
    {
        public string DeviceToken { get; set; }
        public string FbToken     { get; set; }
        public string GgToken     { get; set; }
    }

    /// <summary>
    ///     1. unauthorize
    ///     2. invalid token
    ///     3. refresh token is invalid
    ///     4. refresh token not found
    ///     5. Invalid otp
    ///     6. Invalid email
    /// </summary>
    public class LoginResponseData : IHttpResponseData
    {
        public static int OtpExpireTime  = 180; // OTP expire time in seconds.
        public static int ResendableTime = 10; // after ResendableTime seconds, user can request the OTP again.
        public static int TimeRemain     = 10; // maximum: 10 times to send.

        public string JwtToken       { get; set; }
        public string RefreshToken   { get; set; }
        public long   ExpirationTime { get; set; }
        public string WalletAddress  { get; set; }
        public string Email          { get; set; }
        public string Fullname       { get; set; }
        public string Picture        { get; set; }
    }
}