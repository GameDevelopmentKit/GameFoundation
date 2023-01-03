namespace BlueprintFlow.Downloader
{
#if GDK_NETWORK_ENABLE
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Network.WebService.Requests;
    using GameFoundation.Scripts.Utilities.LogService;

    /// <summary>
    /// Get blueprint download link from server
    /// </summary>
    public class BlueprintDownloadRequest : BaseHttpRequest<GetBlueprintResponseData>
    {
        #region zenject

        private readonly BlueprintReaderManager blueprintReaderManager;

        #endregion

        public BlueprintDownloadRequest(ILogService logger, BlueprintReaderManager blueprintReaderManager) :
            base(logger)
        {
            this.blueprintReaderManager = blueprintReaderManager;
        }

        public override void Process(GetBlueprintResponseData responseDataData)
        {
            this.Logger.Log($"Blueprint download link: {responseDataData.Url}");
            this.blueprintReaderManager.LoadBlueprint(responseDataData.Url, responseDataData.Hash);
        }
    }

#else
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public class BlueprintResponseData
    {
        public string Version;
        public string Hash;
        public string Url;
    }

    public class BlueprintDownloadRequest
    {
        private async Task<BlueprintResponseData> GetBlueprintInfo(string uri, string blueprintVersion)
        {
            var request = (HttpWebRequest)WebRequest.Create(
                $"https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{blueprintVersion}/info");
            var response     = (HttpWebResponse)(await request.GetResponseAsync());
            var reader       = new StreamReader(response.GetResponseStream());
            var jsonResponse = await reader.ReadToEndAsync();
            var responseData = JObject.Parse(jsonResponse);
            return !responseData.TryGetValue("data", out var jsonBlueprintInfo) ? null : jsonBlueprintInfo.ToObject<BlueprintResponseData>();
        }
    }
#endif
}