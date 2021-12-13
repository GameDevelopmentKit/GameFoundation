#if UNITY_EDITOR && JSON_NET_EXISTS

using DA_Assets.Exceptions;
using DA_Assets.Extensions;
using DA_Assets.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace DA_Assets
{
    public static class WebClient
    {
        public static int webRequestDelay = 100;
        public static float pbarProgress;
        public static string pbarContent = "0 kB";

        public static async Task<string> Authorize()
        {
            string code = "";
            bool gettingCode = true;

            Thread thread = null;

            Console.WriteLine(Localization.OPEN_AUTH_PAGE);

            thread = new Thread(x =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 1923);

                server.Bind(endpoint);
                server.Listen(1);

                Socket socket = server.Accept();

                byte[] bytes = new byte[1000];
                socket.Receive(bytes);
                string rawCode = Encoding.UTF8.GetString(bytes);

                string toSend = "HTTP/1.1 200 OK\nContent-Type: text/html\nConnection: close\n\n" + @"
                    <html>
                        <head>
                            <style type='text/css'>body,html{background-color: #000000;color: #fff;font-family: Segoe UI;text-align: center;}h2{left: 0; position: absolute; top: calc(50% - 25px); width: 100%;}</style>
                            <title>Wait for redirect...</title>
                            <script type='text/javascript'> window.onload=function(){window.location.href='https://figma.com';}</script> 
                        </head>
                        <body>
                            <h2>Authorization completed. The page will close automatically.</h2>
                        </body>
                    </html>";
                bytes = Encoding.UTF8.GetBytes(toSend);

                NetworkStream stream = new NetworkStream(socket);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                stream.Close();
                socket.Close();
                server.Close();

                code = rawCode.GetBetween("?code=", "&state=");
                gettingCode = false;
                thread.Abort();
            });

            thread.Start();

            int state = Random.Range(0, int.MaxValue);
            string formattedOauthUrl = string.Format(Constants.OAUTH_URL, Constants.CLIENT_ID, Constants.REDIRECT_URI, state.ToString());

            Application.OpenURL(formattedOauthUrl);

            while (string.IsNullOrWhiteSpace(code) && gettingCode)
            {
                await Task.Delay(webRequestDelay);
            }

            if (string.IsNullOrWhiteSpace(code) == false)
            {
                Console.WriteLine(Localization.TRY_GET_API_KEY);
                return await GetToken(code);
            }

            throw new InvalidAuthException();
        }

        private static async Task<string> GetToken(string code)
        {
            string query = string.Format(Constants.AUTH_URL, Constants.CLIENT_ID, Constants.CLIENT_SECRET, Constants.REDIRECT_URI, code);//ClientCode

            AuthResult authResult = await MakeRequest<AuthResult>(new Request
            {
                Query = query,
                RequestType = RequestType.Post,
                WWWForm = new WWWForm()
            });

            return authResult.access_token;
        }

        public static async Task<FigmaProject> GetProject()
        {
            string query = string.Format(Constants.API_LINK, FigmaConverterUnity.Instance.mainSettings.ProjectUrl.GetBetween("file/", "/"));
            FigmaProject fproject = await MakeRequest<FigmaProject>(new Request
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = new RequestHeader
                {
                    Name = "Authorization",
                    Value = $"Bearer {FigmaConverterUnity.Instance.mainSettings.ApiKey}"
                }
            });

            return fproject;
        }

        public static async Task<List<FObject>> GetImageLinksForFObjects(List<FObject> fobjects, short chunkSize = 256)
        {
            Console.WriteLine(Localization.START_ADD_LINKS);

            List<string> _fobjects = fobjects
                .Where(x => x.Visible != false)
                .Select(x => x.Id).ToList();

            List<List<string>> idChunks = _fobjects.ToChunks(chunkSize);

            Dictionary<string, string> idsLinks = new Dictionary<string, string>();
            int gettedCount = 0;
            for (int i = 0; i < idChunks.Count(); i++)
            {
                gettedCount += idChunks[i].Count();
                Console.WriteLine(string.Format(Localization.GETTING_LINKS, gettedCount, _fobjects.Count())); 

                ObjectsLinks imagesLinks = await MakeRequest<ObjectsLinks>(new Request
                {
                    RequestType = RequestType.Get,
                    Query = CreateImagesQuery(idChunks[i]),
                    RequestHeader = new RequestHeader
                    {
                        Name = "Authorization",
                        Value = $"Bearer {FigmaConverterUnity.Instance.mainSettings.ApiKey}"
                    }
                });

                idsLinks.AddRange(imagesLinks.IdsLinks);
            }

            List<FObject> _children = new List<FObject>();

            foreach (string id in idsLinks.Keys)
            {
                FObject childWithLink = fobjects.FirstOrDefault(x => x.Id == id);

                childWithLink.Link = idsLinks[id];
                _children.Add(childWithLink);
            }

            Console.WriteLine(Localization.LINKS_ADDED);

            return _children;
        }

        private static string CreateImagesQuery(List<string> chunk)
        {
            string joinedIds = string.Join(",", chunk);
            if (joinedIds[0] == ',')
            {
                joinedIds = joinedIds.Remove(0, 1);
            }

            string projectId = FigmaConverterUnity.Instance.mainSettings.ProjectUrl.GetBetween("file/", "/");
            string extension = FigmaExtensions.GetImageExtension();
            float scale = FigmaExtensions.GetImageScale();
            string query = $"https://api.figma.com/v1/images/{projectId}?ids={joinedIds}&format={extension}&scale={scale}";
            return query;
        }

        public static async Task<List<FObject>> DownloadSpritesAsync(List<FObject> children)
        {
            Console.WriteLine(Localization.START_SPRITES_DOWNLOAD);

            List<FObject> _children = new List<FObject>();

            foreach (FObject child in children)
            {
                if (child.IsDownloadable() == false)
                {
                 //   Debug.Log(child.Name);
                    _children.Add(child);
                    continue;
                }

                string path = child.GetAssetPath(true);
                string assetPath = child.GetAssetPath(false);

                bool imageExists = File.Exists(path);

                if (imageExists == false || FigmaConverterUnity.Instance.mainSettings.ReDownloadSprites == true)
                {
                    try
                    {
                        byte[] image = await MakeRequest<byte[]>(new Request
                        {
                            RequestType = RequestType.GetFile,
                            Query = child.Link
                        });
                     
                        File.WriteAllBytes(path, image);
                        AssetDatabase.Refresh();
                    }
                    catch (Exception ex)
                    {
                        Console.Error(ex.ToString());
                    }
                }

                child.AssetPath = assetPath;
                _children.Add(child);
            }

            Console.WriteLine(string.Format(Localization.DRAW_COMPONENTS, _children.Count())); 

            return _children;
        }

        private static async Task<T> MakeRequest<T>(Request request)
        {
            UnityWebRequest webRequest;

            if (request.RequestType == RequestType.Get || request.RequestType == RequestType.GetFile)
            {
                webRequest = UnityWebRequest.Get(request.Query);
            }
            else
            {
                webRequest = UnityWebRequest.Post(request.Query, request.WWWForm);
            }

            using (webRequest)
            {
                Editor editor = new Editor();
                if (request.RequestHeader != null)
                {
                    webRequest.SetRequestHeader(request.RequestHeader.Name, request.RequestHeader.Value);
                }

                webRequest.SendWebRequest();

                while (webRequest.isDone == false)
                {
                    if (pbarProgress < 1f)
                    {
                        pbarProgress += 0.1f;
                    }
                    else
                    {
                        pbarProgress = 0;
                    }

                    pbarContent = string.Format("{0} kB", webRequest.downloadedBytes / 1024);
                    editor.Repaint();

                    await Task.Delay(webRequestDelay);
                }

                bool isRequestError;
#if UNITY_2020_1_OR_NEWER
                isRequestError = webRequest.result == UnityWebRequest.Result.ConnectionError;
#else
                isRequestError = webRequest.isNetworkError || webRequest.isHttpError;
#endif
                if (isRequestError)
                {
                    throw new InvalidApiKeyException();
                }

                T result = default;

                try
                {
                    if (request.RequestType == RequestType.GetFile)
                    {
                        result = (T)(object)webRequest.downloadHandler.data;
                    }
                    else
                    {
                        string text = webRequest.downloadHandler.text;

                        if (FigmaConverterUnity.Instance.mainSettings.SaveJsonFile)
                        {
                            string path = $"{Application.dataPath}/{Constants.PUBLISHER}/{Constants.PRODUCT_NAME}/{Constants.JSON_FILE_NAME}";
                            JToken parsedJson = JToken.Parse(text);
                            var beautified = parsedJson.ToString(Formatting.Indented);

                            File.WriteAllText(path, beautified);
                        }

                        result = JsonConvert.DeserializeObject<T>(text, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            Error = (sender, error) =>
                            {
                                error.ErrorContext.Handled = true;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw new CustomException(ex);
                }

                pbarProgress = 0f;
                pbarContent = "0 kB";
                editor.Repaint();
                editor.DestroyImmediate();

                return result;
            }
        }
        private struct Request
        {
            public string Query;
            public RequestType RequestType;
            public RequestHeader RequestHeader;
            public WWWForm WWWForm;
        }
        private class RequestHeader
        {
            public string Name;
            public string Value;
        }
        private enum RequestType
        {
            Get,
            Post,
            GetFile,
        }
        private struct AuthResult
        {
            public string access_token;
            public string expires_in;
            public string refresh_token;
        }
        private class ObjectsLinks
        {
            [JsonProperty("err")]
            public string Err;

            [JsonProperty("images")]
            public Dictionary<string, string> IdsLinks;
        }
    }
}

#endif