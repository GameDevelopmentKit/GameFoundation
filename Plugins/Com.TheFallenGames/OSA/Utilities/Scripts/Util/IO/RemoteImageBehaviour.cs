using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Util;
using System;
using Com.ForbiddenByte.OSA.Util.IO.Pools;

namespace Com.ForbiddenByte.OSA.Util.IO
{
    /// <summary>Utility behavior to be attached to a GameObject containing a RawImage for loading remote images using <see cref="SimpleImageDownloader"/>, optionally displaying a specific image during loading and/or on error</summary>
    [RequireComponent(typeof(RawImage))]
    public class RemoteImageBehaviour : MonoBehaviour
    {
        public delegate void LoadCompleteDelegate(bool fromCache, bool success);

        [Tooltip("If not assigned, will try to find it in this game object")] [SerializeField] private RawImage _RawImage = null;
#pragma warning disable 0649
        [SerializeField] private Texture2D _LoadingTexture = null;
        [SerializeField] private Texture2D _ErrorTexture   = null;
#pragma warning restore 0649

        private string    _CurrentRequestedURL;
        private bool      _DestroyPending;
        private Texture2D _Texture;
        private IPool     _Pool;

        public void InitializeWithPool(IPool pool)
        {
            this._Pool = pool;
        }

        private void Awake()
        {
            if (!this._RawImage) this._RawImage = this.GetComponent<RawImage>();
        }

        /// <summary>Starts the loading, setting the current image to <see cref="_LoadingTexture"/>, if available. If the image is already in cache, and <paramref name="loadCachedIfAvailable"/>==true, will load that instead</summary>
        public void Load(string imageURL, bool loadCachedIfAvailable = true, LoadCompleteDelegate onCompleted = null, Action onCanceled = null)
        {
            var currentRequestedURLAlreadyLoaded = this._CurrentRequestedURL == imageURL;
            this._CurrentRequestedURL = imageURL;

            if (loadCachedIfAvailable)
            {
                var foundCached = false;
                // Don't re-request if the url is the same. This is useful if there's no pool provided
                if (currentRequestedURLAlreadyLoaded)
                    foundCached = this._Texture != null;
                else if (this._Pool != null)
                {
                    var cachedInPool = this._Pool.Get(imageURL) as Texture2D;
                    if (cachedInPool)
                    {
                        this._Texture             = cachedInPool;
                        foundCached               = true;
                        this._CurrentRequestedURL = imageURL;
                    }
                }

                if (foundCached)
                {
                    this._RawImage.texture = this._Texture;
                    if (onCompleted != null) onCompleted(true, true);

                    return;
                }
            }

            this._RawImage.texture = this._LoadingTexture;
            var request = new SimpleImageDownloader.Request()
            {
                url = imageURL,
                onDone = result =>
                {
                    if (!this._DestroyPending && imageURL == this._CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
                    {
                        // Commented: not reusing textures to load data into them anymore, since in most cases we'll use a pool
                        //result.LoadTextureInto(_Texture);

                        if (this._Pool == null)
                        {
                            // Non-pooled textures should be destroyed
                            if (this._Texture) this.DisposeTexture(this._Texture);

                            this._Texture = result.CreateTextureFromReceivedData();
                        }
                        else
                        {
                            var textureAlreadyStoredMeanwhile = this._Pool.Get(imageURL);
                            var someoneStoredTheImageSooner   = textureAlreadyStoredMeanwhile != null;
                            if (someoneStoredTheImageSooner)
                                // Happens when the same URL is requested multiple times for the first time, and of course only the first 
                                // downloaded image should be kept. In this case, someone else already have downloaded and cached the image, so we just discard the one we downloaded
                                this._Texture = textureAlreadyStoredMeanwhile as Texture2D;
                            else
                            {
                                // First time downloaded => cache
                                this._Texture = result.CreateTextureFromReceivedData();
                                this._Pool.Put(imageURL, this._Texture);
                            }
                        }

                        this._RawImage.texture = this._Texture;

                        if (onCompleted != null) onCompleted(false, true);
                    }
                    else if (onCanceled != null) onCanceled();
                },
                onError = () =>
                {
                    if (!this._DestroyPending && imageURL == this._CurrentRequestedURL) // this will be false if a new request was done during downloading, case in which the result will be ignored
                    {
                        this._RawImage.texture = this._ErrorTexture;

                        if (onCompleted != null) onCompleted(false, false);
                    }
                    else if (onCanceled != null) onCanceled();
                },
            };
            SimpleImageDownloader.Instance.Enqueue(request);
        }

        private void OnDestroy()
        {
            this._DestroyPending = true;

            // Non-pooled textures should be destroyed
            if (this._Pool == null && this._Texture) this.DisposeTexture(this._Texture);
        }

        private void DisposeTexture(Texture2D texture)
        {
            try
            {
                Destroy(texture);
            }
            catch
            {
            }
        }
    }
}