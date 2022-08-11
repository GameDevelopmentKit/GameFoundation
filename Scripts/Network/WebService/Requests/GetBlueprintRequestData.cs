namespace GameFoundation.Scripts.Network.WebService.Requests
{
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.Utils;
    using Sirenix.OdinInspector;

    [HttpRequestDefinition("blueprint/get")]
    public class GetBlueprintRequestData : IHttpRequestData
    {
        [Required] public string Version { set; get; }
    }

    public class GetBlueprintResponseData : IHttpResponseData
    {
        public string Url  { set; get; }
        public string Hash { set; get; }
    }
}