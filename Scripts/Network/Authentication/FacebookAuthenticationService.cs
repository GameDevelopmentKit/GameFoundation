namespace GameFoundation.Scripts.Network.Authentication
{
    using System.IO;
    using System.Net;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>Facebook login  </summary>
    public class FacebookAuthenticationService : BaseAuthenticationService
    {
        private       Stream                  responseOutput;
        private const string                  AppId       = "1017989705439878";
        private const string                  RedirectUrl = "https://dev-auth.mechmaster.io/api/callback";
        private       CancellationTokenSource tokenSource;

        private const string FacebookGraphApi =
            "https://graph.facebook.com/v11.0/me?fields=id%2Clink%2Cfirst_name%2Ccurrency%2Clast_name%2Cemail%2Cgender%2Clocale%2Ctimezone%2Cverified%2Cpicture%2Cage_range&access_token=";


        private void CancelHttp()
        {
            if (this.Http is { IsListening: true })
            {
                this.Http.Stop();
            }
        }
        public override async UniTask<string> OnLogIn(CancellationTokenSource tokenSource)
        {
            this.DataLoginServices.Status.Value = AuthenticationStatus.Authenticating;
            // this.tokenSource = cancellationToken;
            this.CancelHttp();
            //string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort());
            var redirectURI = "http://localhost:5000/";
            //Debug.Log("redirect URI: " + redirectURI);
            // Creates an HttpListener to listen for requests on that redirect URI.
            this.Http = new HttpListener();

            this.Http.Prefixes.Add(redirectURI);
            this.Logger.Log("Listening..");


            this.Http.Start();
            var authorizationRequest =
                "https://www.facebook.com/dialog/oauth?" +
                "client_id=" + AppId +
                "&redirect_uri=" + RedirectUrl + "&display=popup";
            Application.OpenURL(authorizationRequest);
            // Sends an HTTP response to the browser.
            var context = await this.Http.GetContextAsync().AsUniTask().AttachExternalCancellation(tokenSource.Token);

            var response = context.Response;

            var responseString =
                "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            this.responseOutput      = response.OutputStream;
            await this.responseOutput.WriteAsync(buffer, 0, buffer.Length, tokenSource.Token).ContinueWith((task) =>
            {
                this.responseOutput.Close();
                this.Http.Stop();
                // logger.Log("HTTP server stopped.");
            });
            this.AccessToken = context.Request.QueryString.Get("access_token");
            this.userinfoCall(this.AccessToken);
            return this.AccessToken;
        }

        private async void userinfoCall(string access_token)
        {
            this.Logger.Log("Making API Call to Userinfo...");

            // builds the  request
            var userinfoRequestURI = FacebookGraphApi + access_token;
            // sends the request
            HttpWebRequest userinfoRequest = (HttpWebRequest)WebRequest.Create(userinfoRequestURI);
            userinfoRequest.Method = "GET";
            userinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", access_token));
            userinfoRequest.ContentType = "application/x-www-form-urlencoded";
            userinfoRequest.Accept      = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // gets the response
            WebResponse userinfoResponse = await userinfoRequest.GetResponseAsync();
            using (StreamReader userinfoResponseReader = new StreamReader(userinfoResponse.GetResponseStream()))
            {
                // reads response body
                string userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
                // Dictionary<string, string> rawData              = JsonConvert.DeserializeObject<Dictionary<string, string>>(userinfoResponseText);
                // var                        name                 = rawData["name"];
                // var                        urlImage             = rawData["picture"];
                FacebookModel model = JsonConvert.DeserializeObject<FacebookModel>(userinfoResponseText);
                if (model == null) return;
                this.localData.UserDataLogin.FacebookLogin.UserName = model.LastName + " " + model.FirstName;
                this.localData.UserDataLogin.FacebookLogin.URLImage = model.Picture.Data.Url;

                this.playerData.Name   = model.LastName + " " + model.FirstName;
                this.playerData.Avatar = model.Picture.Data.Url;
            }
        }

        public partial class FacebookModel
        {
            [JsonProperty("id")] public string Id { get; set; }

            [JsonProperty("first_name")] public string FirstName { get; set; }

            [JsonProperty("last_name")] public string LastName { get; set; }

            [JsonProperty("picture")] public Picture Picture { get; set; }
        }

        public partial class Picture
        {
            [JsonProperty("data")] public Data Data { get; set; }
        }

        public partial class Data
        {
            [JsonProperty("height")] public long Height { get; set; }

            [JsonProperty("is_silhouette")] public bool IsSilhouette { get; set; }

            [JsonProperty("url")] public string Url { get; set; }

            [JsonProperty("width")] public long Width { get; set; }
        }
    }
}