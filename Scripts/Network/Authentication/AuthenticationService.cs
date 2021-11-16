namespace GameFoundation.Scripts.Network.Authentication
{
    using System;
    using System.Text;
    using BestHTTP;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.Utils;
    using MechSharingCode.WebService.Interface;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Zenject;

    /// <summary>Unlike other requests, login request will use another Uri and the DTO will not be wrapped. It will be process in a different follow with other.</summary>
    public class AuthenticationService : BestHttpBaseProcess
    {
        private const int               LoginTimeOut = 10;
        private       DataLoginServices dataLoginServices;

        public AuthenticationService(string uri, ILogService logger, DiContainer container, DataLoginServices dataLoginServices) : base(logger, container, uri)
        {
            this.dataLoginServices = dataLoginServices;
        }

        public async UniTask SendAsync<T, TK>(IHttpRequestData authenticationData) where T : BaseHttpRequest, IDisposable where TK : IHttpResponseData
        {
            if (!(Attribute.GetCustomAttribute(authenticationData.GetType(), typeof(HttpRequestDefinitionAttribute)) is HttpRequestDefinitionAttribute httpRequestDefinition))
            {
                throw new Exception($"{typeof(T)} didn't define yet!!!");
            }

            var request = new HTTPRequest(this.GetUri(httpRequestDefinition.Route), HTTPMethods.Post);
            request.Timeout = TimeSpan.FromSeconds(LoginTimeOut);
            request.AddHeader("Content-Type", "application/json");
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authenticationData));

            try
            {
                await this.MainProcess<T, TK>(request);
            }
            catch (AsyncHTTPException ex)
            {
                this.dataLoginServices.Status.Value = AuthenticationStatus.FailWithNoInternetOrTimeout;
                this.HandleAsyncHttpException(ex);
            }
        }

        protected override void RequestSuccessProcess<T, TK>(JObject responseData) { this.Container.Resolve<IFactory<T>>().Create().Process(responseData.ToObject<TK>()); }
    }
}