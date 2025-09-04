namespace BlueprintFlow.APIHandler
{
    #if GDK_NETWORK_ENABLE
    using caojweldjflwendl.Scripts.BlueprintFlow.BlueprintControlFlow;
    using caojweldjflwendl.Scripts.Network.WebService;
    using caojweldjflwendl.Scripts.Network.WebService.Requests;
    using caojweldjflwendl.Scripts.Utilities.LogService;
    using UnityEngine.Scripting;

    /// <summary>
    /// Get blueprint download link from server
    /// </summary>
    public class BlueprintDownloadRequest : BaseHttpRequest<GetBlueprintResponseData>
    {
        #region zenject

        private readonly BlueprintReaderManager blueprintReaderManager;

        #endregion

        [Preserve]
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
    using caojweldjflwendl.Scripts.Interfaces;
    using caojweldjflwendl.Scripts.Utilities.UserData;
    using Newtonsoft.Json;
    using UnityEngine;
    using UnityEngine.Scripting;

    [Preserve]
    public class BlueprintInfoData : ILocalData
    {
        public string Version;
        public string Hash;
        public string Url;

        public void Init()
        {
        }
    }

    public class FetchBlueprintInfo
    {
        private readonly IHandleUserDataServices handleUserDataServices;

        [Preserve]
        public FetchBlueprintInfo(IHandleUserDataServices handleUserDataServices)
        {
            this.handleUserDataServices = handleUserDataServices;
        }

        public async Task<BlueprintInfoData> GetBlueprintInfo(string fetchUri)
        {
            try
            {
                var request      = (HttpWebRequest)WebRequest.Create(fetchUri);
                var response     = (HttpWebResponse)await request.GetResponseAsync();
                var reader       = new StreamReader(response.GetResponseStream());
                var jsonResponse = await reader.ReadToEndAsync();
                var definition   = new { data = new { blueprint = new BlueprintInfoData() } };
                var responseData = JsonConvert.DeserializeAnonymousType(jsonResponse, definition);
                return responseData?.data.blueprint;
            }
            catch (Exception e)
            {
                //if fetch info fails get info from local
                Debug.LogException(e);
                return await this.handleUserDataServices.Load<BlueprintInfoData>();
            }
        }
    }
    #endif
}