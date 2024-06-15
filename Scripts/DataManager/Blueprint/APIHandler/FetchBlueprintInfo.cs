namespace DataManager.Blueprint.APIHandler
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
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using DataManager.LocalData;
    using Newtonsoft.Json;
    using UnityEngine;

    public class BlueprintInfoData : ILocalData
    {
        public string Version;
        public string Hash;
        public string Url;
    }

    public class FetchBlueprintInfo
    {
        private readonly IHandleLocalDataServices handleLocalDataServices;
        public FetchBlueprintInfo(IHandleLocalDataServices handleLocalDataServices) { this.handleLocalDataServices = handleLocalDataServices; }
        public async Task<BlueprintInfoData> GetBlueprintInfo(string fetchUri)
        {
            try
            {
                var request      = (HttpWebRequest)WebRequest.Create(fetchUri);
                var response     = (HttpWebResponse)(await request.GetResponseAsync());
                var reader       = new StreamReader(response.GetResponseStream());
                var jsonResponse = await reader.ReadToEndAsync();
                var definition   = new { data = new {blueprint = new BlueprintInfoData()} };
                var responseData = JsonConvert.DeserializeAnonymousType(jsonResponse, definition);
                return responseData?.data.blueprint ;
            }
            catch (Exception e)
            {
                //if fetch info fails get info from local
                Debug.LogException(e);
                return await this.handleLocalDataServices.Load<BlueprintInfoData>();
            }
        }
    }
#endif
}