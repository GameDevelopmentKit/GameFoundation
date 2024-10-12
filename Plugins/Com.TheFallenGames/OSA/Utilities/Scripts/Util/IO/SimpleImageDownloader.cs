// WWW class usage

#pragma warning disable 0618

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Com.ForbiddenByte.OSA.Util.IO
{
    /// <summary>
    /// <para>A utility singleton class for downloading images using a LIFO queue for the requests. <see cref="MaxConcurrentRequests"/> can be used to limit the number of concurrent requests. </para> 
    /// <para>Default is <see cref="DEFAULT_MAX_CONCURRENT_REQUESTS"/>. Each request is executed immediately if there's room for it. When the queue is full, the downloder starts checking each second if a slot is freed, after which re-enters the loop.</para> 
    /// </summary>
    public class SimpleImageDownloader : MonoBehaviour
    {
        public static SimpleImageDownloader Instance
        {
            get
            {
                if (_Instance == null) _Instance = new GameObject(typeof(SimpleImageDownloader).Name).AddComponent<SimpleImageDownloader>();

                return _Instance;
            }
        }

        private static SimpleImageDownloader _Instance;

        public int MaxConcurrentRequests { get; set; }

        private const int DEFAULT_MAX_CONCURRENT_REQUESTS = 20;

        private List<Request>  _QueuedRequests    = new();
        private List<Request>  _ExecutingRequests = new();
        private WaitForSeconds _Wait1Sec          = new(1f);

        private IEnumerator Start()
        {
            if (this.MaxConcurrentRequests == 0) this.MaxConcurrentRequests = DEFAULT_MAX_CONCURRENT_REQUESTS;

            while (true)
            {
                while (this._ExecutingRequests.Count >= this.MaxConcurrentRequests) yield return this._Wait1Sec;

                var lastIndex = this._QueuedRequests.Count - 1;
                if (lastIndex >= 0)
                {
                    var lastRequest = this._QueuedRequests[lastIndex];
                    this._QueuedRequests.RemoveAt(lastIndex);

                    this.StartCoroutine(this.DownloadCoroutine(lastRequest));
                }

                yield return null;
            }
        }

        private void OnDestroy()
        {
            _Instance = null;
        }

        public void Enqueue(Request request)
        {
            this._QueuedRequests.Add(request);
        }

        private IEnumerator DownloadCoroutine(Request request)
        {
            this._ExecutingRequests.Add(request);
            var www = UnityWebRequestTexture.GetTexture(request.url);

            yield return www.SendWebRequest();

            if (string.IsNullOrEmpty(www.error))
            {
                if (request.onDone != null)
                {
                    var result = new Result(www);
                    request.onDone(result);
                }
            }
            else
            {
                if (request.onError != null) request.onError();
            }
            www.Dispose();
            this._ExecutingRequests.Remove(request);
        }

        public class Request
        {
            public string         url;
            public Action<Result> onDone;
            public Action         onError;
        }

        public class Result
        {
            private UnityWebRequest _UsedRequest;

            public Result(UnityWebRequest www)
            {
                this._UsedRequest = www;
            }

            public Texture2D CreateTextureFromReceivedData()
            {
                return DownloadHandlerTexture.GetContent(this._UsedRequest);
            }
        }
    }
}
#pragma warning restore 0618