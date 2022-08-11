namespace GameFoundation.Scripts.Network.WebService.Requests
{
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Network.WebService.Models.UserData;
    using GameFoundation.Scripts.Utilities.Utils;

    [HttpRequestDefinition("user/data/get")]
    public class GetUserDataRequestData : IHttpRequestData
    {
    }

    public class GetUserDataResponseData : IHttpResponseData
    {
        public UserData UserData { get; set; } = new();
    }
}