namespace GameFoundation.Scripts.Network.WebService
{
    using System;
    using System.IO;
    using System.Text;
    using BestHTTP;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Network.NetworkConfig;
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.Utils;
    using Newtonsoft.Json;
    using UnityEngine;
    using Zenject;

    /// <summary>Use BestHttp plugin from unity asset store to manage all http request and process request follow.</summary>
    public class BestHttpService : BestHttpBaseProcess, IHttpService
    {
        #region Injection

        private readonly NetworkConfig           networkConfig; // Our global network config data
        private readonly GameFoundationLocalData localData;

        #endregion

        #region HttpRequest

        /// <summary>Zenject injection will go here.</summary>
        public BestHttpService(string uri, NetworkConfig networkConfig, ILogService logger, DiContainer container,
            GameFoundationLocalData localData) : base(logger, container, uri)
        {
            this.networkConfig = networkConfig;
            this.localData     = localData;
        }

        /// <summary>
        /// Call a Async http post request to web service, 
        /// </summary>
        /// <param name="httpRequestData"></param>
        /// <typeparam name="T">http request object type</typeparam>
        /// <typeparam name="TK">usable game data type</typeparam>
        /// <returns>Return usable game data</returns>
        public async UniTask SendAsync<T, TK>(IHttpRequestData httpRequestData) where T : BaseHttpRequest, IDisposable
            where TK : IHttpResponseData
        {
            if (!(Attribute.GetCustomAttribute(httpRequestData.GetType(), typeof(HttpRequestDefinitionAttribute)) is
                    HttpRequestDefinitionAttribute httpRequestDefinition))
            {
                throw new Exception($"{typeof(T)} didn't define yet!!!");
            }

#if (DEVELOPMENT_BUILD || UNITY_EDITOR) &&FAKE_DATA
            if (typeof(IFakeResponseAble<TK>).IsAssignableFrom(typeof(T)))
            {
                var baseHttpRequest = this.Container.Resolve<IFactory<T>>().Create();
                var responseData = ((IFakeResponseAble<TK>)baseHttpRequest).FakeResponse();
                baseHttpRequest.Process(responseData);
                return;
            }
#endif
            //Init request
            var request = new HTTPRequest(this.GetUri(httpRequestDefinition.Route), HTTPMethods.Post);
            request.Timeout = TimeSpan.FromSeconds(this.GetHttpTimeout());
            using (var wrappedData = this.Container.Resolve<IFactory<ClientWrappedHttpRequestData>>().Create())
            {
                wrappedData.Data = httpRequestData;
                request.AddHeader("Content-Type", "application/json");

                var jwtToken = this.localData.ServerToken.JwtToken;

                if (!string.IsNullOrEmpty(jwtToken))
                {
                    request.AddHeader("Authorization", "Bearer " + jwtToken);
                }

                if (!string.IsNullOrEmpty(MechVersion.Version))
                {
                    request.AddHeader("game-version", MechVersion.Version);
                }

                request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(wrappedData));
                // var result = Encoding.UTF8.GetString(request.GetEntityBody());
            }

            try
            {
                await this.MainProcess<T, TK>(request, httpRequestData);
            }
            catch (AsyncHTTPException ex)
            {
                this.Logger.Log($"Request {request.Uri} Error");
                this.HandleAsyncHttpException(ex);
            }
        }

        //TODO need to test and improve code here, this is just a temporary logic
        /// <summary>
        /// Temporary logic for download, streaming data
        /// </summary>
        /// <param name="address">Download uri</param>
        /// <param name="filePath">output file path</param>
        /// <param name="onDownloadProgress">% of download will be presented through this</param>
        public async UniTask Download(string address, string filePath, OnDownloadProgressDelegate onDownloadProgress)
        {
            filePath = this.GetDownloadPath(filePath);

            var request = new HTTPRequest(new Uri(address));
            request.Timeout            =  TimeSpan.FromSeconds(this.GetDownloadTimeout());
            request.OnDownloadProgress =  (httpRequest, downloaded, length) => onDownloadProgress(downloaded, length);
            request.OnStreamingData    += OnData;
            request.DisableCache       =  true;

            var response = await request.GetHTTPResponseAsync();

            if (request.Tag is FileStream fs)
                fs.Dispose();

            switch (request.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (response.IsSuccess)
                    {
                        this.Logger.Log($"Download {filePath} Done!");
                    }
                    else
                    {
                        this.Logger.Warning(string.Format(
                            "Request finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                            response.StatusCode, response.Message,
                            response.DataAsText));
                    }

                    break;

                default:
                    // There were an error while downloading the content.
                    // The incomplete file should be deleted.
                    File.Delete(filePath);
                    break;
            }

            bool OnData(HTTPRequest req, HTTPResponse resp, byte[] dataFragment, int dataFragmentLength)
            {
                if (resp.IsSuccess)
                {
                    if (!(req.Tag is FileStream fileStream))
                        req.Tag = fileStream = new FileStream(filePath, FileMode.Create);

                    fileStream.Write(dataFragment, 0, dataFragmentLength);
                }

                // Return true if dataFragment is processed so the plugin can recycle it
                return true;
            }
        }

        public string GetDownloadPath(string path) => $"{Application.persistentDataPath}/{path}";

        private double GetHttpTimeout()     => this.networkConfig.HttpRequestTimeout;
        private double GetDownloadTimeout() => this.networkConfig.DownloadRequestTimeout;

        #endregion
    }
}