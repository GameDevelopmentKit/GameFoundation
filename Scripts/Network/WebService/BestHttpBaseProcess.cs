namespace GameFoundation.Scripts.Network.WebService
{
    using System;
    using System.Text;
    using BestHTTP;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Network.WebService.Requests;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Zenject;

    public abstract class BestHttpBaseProcess
    {
        protected BestHttpBaseProcess(ILogService logger, DiContainer container, string uri)
        {
            this.Logger    = logger;
            this.Container = container;
            this.uri       = uri;
        }

        protected async UniTask MainProcess<T, TK>(HTTPRequest request, IHttpRequestData requestData)
            where T : BaseHttpRequest, IDisposable where TK : IHttpResponseData
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            this.Logger.Log(
                $"{request.Uri} - [REQUEST] - Header: {request.DumpHeaders()} - \n Body:{Encoding.UTF8.GetString(request.GetEntityBody())}");
#endif
            var response = await request.GetHTTPResponseAsync();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            this.Logger.Log($"{request.Uri} - [RESPONSE] - raw data: {response.DataAsText}");
#endif
            this.PreProcess(request, response, (statusCode) =>
                {
                    var responseData = JObject.Parse(Encoding.UTF8.GetString(response.Data));
                    this.RequestSuccessProcess<T, TK>(responseData, requestData);
                },
                (statusCode) =>
                {
                    switch (statusCode)
                    {
                        case CommonErrorCode.Unknown:
                            this.Logger.Error($"Code {statusCode}: Unknown error");
                            break;
                        case CommonErrorCode.NotFound:
                            this.Logger.Error($"Code {statusCode}: NotFound error");
                            break;
                        case CommonErrorCode.InvalidData:
                            this.Logger.Error($"Code {statusCode}: InvalidData error");
                            break;
                        case CommonErrorCode.InvalidBlueprint:
                            this.Logger.Error($"Code {statusCode}: InvalidBlueprint error");
                            break;
                        default:
                            //In the case server return a logic error but client didn't implement that logic yet 
                            try
                            {
                                this.Container.Resolve<IFactory<T>>().Create().ErrorProcess(statusCode);
                            }
                            catch (BaseHttpRequest.MissStatusCodeException e)
                            {
                                this.Logger.Error($"Didn't implement status Code: {statusCode} for {typeof(T)}");
                                this.Logger.Exception(e);
                            }

                            break;
                    }
                });
        }

        //Deserialize then process response data when request success
        protected virtual void RequestSuccessProcess<T, TK>(JObject responseData, IHttpRequestData requestData)
            where T : BaseHttpRequest, IDisposable where TK : IHttpResponseData
        {
            if (!responseData.TryGetValue("data", out var requestProcessData)) return;

            var baseHttpRequest = this.Container.Resolve<IFactory<T>>().Create();
            baseHttpRequest.Process(requestProcessData.ToObject<TK>());
            baseHttpRequest.PredictProcess(requestData);
            this.PostProcess();
        }

        /// <summary>Handle errors that are defined by Best Http/2, return false of there is any error, otherwise return true</summary>
        protected void PreProcess(HTTPRequest req, HTTPResponse resp, RequestSuccess onRequestSuccess,
            RequestError onRequestError)
        {
            switch (req.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (resp.IsSuccess)
                    {
                        onRequestSuccess(resp.StatusCode);
                    }
                    else
                    {
                        //Specific error for each requests
                        if (resp.StatusCode == 400)
                        {
                            var errorMessage = JsonConvert.DeserializeObject<ErrorResponse>(resp.DataAsText);
                            if (errorMessage != null)
                            {
                                this.Logger.Error(
                                    $"{req.Uri} request receive error code: {errorMessage.Code}-{errorMessage.Message}");
                                onRequestError(errorMessage.Code);
                            }
                        }
                        else
                        {
                            this.Logger.Error(
                                $"{req.Uri}- Request finished Successfully, but the server sent an error. Status Code: {resp.StatusCode}-{resp.Message} Message: {resp.DataAsText}");
                        }
                    }

                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    this.Logger.Error("Request Finished with Error! " + (req.Exception != null
                        ? (req.Exception.Message + "\n" + req.Exception.StackTrace)
                        : "No Exception"));
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    this.Logger.Warning("Request Aborted!");
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    this.Logger.Error("Connection Timed Out!");
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    this.Logger.Error("Processing the request Timed Out!");
                    break;
                case HTTPRequestStates.Initial:
                    break;
                case HTTPRequestStates.Queued:
                    break;
                case HTTPRequestStates.Processing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>Handler unexpected exceptions of http requests.</summary>
        protected void HandleAsyncHttpException(AsyncHTTPException ex)
        {
            this.Logger.Log("Status Code: " + ex.StatusCode);
            this.Logger.Log("Message: " + ex.Message);
            this.Logger.Log("Content: " + ex.Content);
        }

        /// <summary>Run common logic like common error, analysis, events...</summary>
        private void PostProcess()
        {
            // Do something here
        }

        protected Uri GetUri(string route) => new Uri($"{this.uri}/{route}");

        protected delegate void RequestSuccess(int statusCode);

        protected delegate void RequestError(int statusCode);

        #region Injection

        protected readonly ILogService Logger; // wrapped log 
        protected readonly DiContainer Container; // zenject container of this

        private readonly string uri; // uri of service 

        #endregion
    }
}