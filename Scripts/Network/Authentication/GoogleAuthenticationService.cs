namespace GameFoundation.Scripts.Network.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Newtonsoft.Json;
    using UnityEngine;

    /// <summary>Google login  </summary>
    public class GoogleAuthenticationService : BaseAuthenticationService
    {
        private       CancellationTokenSource tokenSource           = new CancellationTokenSource();
        private const string                  ClientID              = "90240111744-puemal60h389pe8gr3gqvkk26i58n3re.apps.googleusercontent.com";
        private const string                  ClientSecret          = "kmpSlxGG4lUjKkmt8iU_jTcH";
        private const string                  AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string                  TokenEndpoint         = "https://www.googleapis.com/oauth2/v4/token";
        private const string                  UserInfoEndpoint      = "https://www.googleapis.com/oauth2/v3/userinfo";


        private void CancelHttp()
        {
            if (this.Http != null && this.Http.IsListening)
                this.Http.Stop();
            this.tokenSource.Cancel();
            this.tokenSource = new CancellationTokenSource();
        }
        /// <summary> https://github.com/googlesamples/oauth-apps-for-windows </summary>
        public override async UniTask<string> OnLogIn(CancellationTokenSource cancellationToken)
        {
            this.DataLoginServices.Status.Value = AuthenticationStatus.Authenticating;
            this.CancelHttp();
            var          state               = randomDataBase64url(32);
            var          codeVerifier        = randomDataBase64url(32);
            var          codeChallenge       = base64urlencodeNoPadding(sha256(codeVerifier));
            const string codeChallengeMethod = "S256";

            // Creates a demo redirect URI using an available port on the loopback address.
            var redirectUri = string.Format("http://{0}:{1}/", IPAddress.Loopback, this.GetRandomUnusedPort());
            //string redirectURI = "https://localhost:8080/";
            // logger.Log("redirect URI: " + redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            this.Http = new HttpListener();
            this.Http.Prefixes.Add(redirectUri);
            // logger.Log("Listening..");
            this.Http.Start();
            // Creates the OAuth 2.0 authorization request.
            var authorizationRequest = string.Format("{0}?response_type=code&scope=openid%20profile%20email&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                AuthorizationEndpoint, Uri.EscapeDataString(redirectUri), ClientID, state, codeChallenge, codeChallengeMethod);

            // Opens request in the browser.
            Application.OpenURL(authorizationRequest);
            // Waits for the OAuth authorization response.
            var context = await this.Http.GetContextAsync().AsUniTask().AttachExternalCancellation(this.tokenSource.Token);
            // Sends an HTTP response to the browser.
            var response       = context.Response;
            var responseString = "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>";
            var buffer         = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length, this.tokenSource.Token).ContinueWith((task) =>
            {
                responseOutput.Close();
                this.Http.Stop();
                // logger.Log("HTTP server stopped.");
            });
            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                var reason = string.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error"));
                // logger.Log(reason);
                return this.AccessToken;
            }

            if (context.Request.QueryString.Get("code") == null || context.Request.QueryString.Get("state") == null)
            {
                var reason = "Malformed authorization response. " + context.Request.QueryString;
                // logger.Log(reson);
                return this.AccessToken;
            }

            // extracts the code
            var code          = context.Request.QueryString.Get("code");
            var incomingState = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incomingState != state)
            {
                this.Logger.Log(String.Format("Received request with invalid state ({0})", incomingState));
                return this.AccessToken;
            }

            // logger.Log("Authorization code: " + code);

            // Starts the code exchange at the Token Endpoint.
            this.AccessToken = await this.performCodeExchange(code, codeVerifier, redirectUri);
            return this.AccessToken;
        }


        async UniTask<string> performCodeExchange(string code, string code_verifier, string redirectURI)
        {
            var accesstoken = "";
            // logger.Log("Exchanging code for tokens...");

            // builds the  request
            var tokenRequestURI = TokenEndpoint;
            var tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code", code,
                Uri.EscapeDataString(redirectURI), ClientID, code_verifier, ClientSecret);

            // sends the request
            var tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
            tokenRequest.Method      = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept      = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            var _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            var stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                var tokenResponse = await tokenRequest.GetResponseAsync();
                using (var reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    // logger.Log(responseText);

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                    accesstoken = tokenEndpointDecoded["access_token"];
                    // Debug.Log(access_token);
                    this.userinfoCall(accesstoken);
                    return accesstoken;
                    // userinfoCall(access_token);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        this.Logger.Log("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            this.Logger.Log(responseText);
                            return accesstoken;
                        }
                    }
                }
            }

            return accesstoken;
        }

        /// <summary>
        /// Get UserData with access token
        /// </summary>
        async void userinfoCall(string access_token)
        {
            this.Logger.Log("Making API Call to Userinfo...");

            // builds the  request
            string userinfoRequestURI = UserInfoEndpoint;

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
                string                     userinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
                Dictionary<string, string> rawData              = JsonConvert.DeserializeObject<Dictionary<string, string>>(userinfoResponseText);
                if (rawData == null) return;
                var name     = rawData["name"];
                var urlImage = rawData["picture"];
                this.localData.UserDataLogin.GoogleLogin.UserName = name;
                this.localData.UserDataLogin.GoogleLogin.URLImage = urlImage;
                this.playerData.Name                              = name;
                this.playerData.Avatar                            = urlImage;
            }
        }


        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng   = new RNGCryptoServiceProvider();
            byte[]                   bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        private static byte[] sha256(string inputStirng)
        {
            byte[]        bytes  = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static string base64urlencodeNoPadding(byte[] buffer)
        {
            var base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        /// <summary>
        /// Get Random unsed port.
        /// </summary>
        private int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}