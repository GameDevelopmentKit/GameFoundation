namespace GameFoundation.Scripts.UIModule.Utilities.LoadImage
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Scripts.Utilities.ObjectPool;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Scripting;
    using UnityEngine.UI;

    public class LoadImageHelper
    {
        private Dictionary<string, Sprite>  spriteCache  = new();
        private Dictionary<string, Texture> textureCache = new();

        public Dictionary<string, UnityWebRequestAsyncOperation> DownloadingOperation = new();

        #region ZenJect

        private          IGameAssets       gameAssets;
        private          ILogService       logger;
        private readonly ObjectPoolManager objectPoolManager;

        #endregion

        private string iconLoadingAssetPath = "LoadingIcon";

        [Preserve]
        private LoadImageHelper(IGameAssets gameAssets, ILogService logger, ObjectPoolManager objectPoolManager)
        {
            this.gameAssets        = gameAssets;
            this.logger            = logger;
            this.objectPoolManager = objectPoolManager;
        }

        private bool inValidKey;

        public async UniTask<Sprite> LoadLocalSprite(object key)
        {
            try
            {
                var sprite = await this.gameAssets.LoadAssetAsync<Sprite>(key);
                if (sprite != null) return sprite;

                key = "None_Texture";
            }
            catch (Exception)
            {
                // ignored
                key = "None_Texture";
            }

            return await this.gameAssets.LoadAssetAsync<Sprite>(key);
        }

        public async UniTask LoadSpriteFromUrl(Image imageComponent, string url, Action<Image> onLoadingIconLoaded = null)
        {
            if (this.spriteCache.TryGetValue(url, out var sprite))
            {
                imageComponent.sprite = sprite;
                return;
            }

            if (this.DownloadingOperation.ContainsKey(url)) return;

            var loadTextureFromUrlTask = this.DownloadSpriteInternal(url);
            var originPreserveAspect   = imageComponent.preserveAspect;
            var originAlpha            = imageComponent.color.a;

            // set placeholder while download image from cdn
            imageComponent.preserveAspect = true;
            imageComponent.color          = imageComponent.color.CloneAndSetAlpha(0f);
            var loadingIconObj = await this.objectPoolManager.Spawn(this.iconLoadingAssetPath, imageComponent.transform);
            onLoadingIconLoaded?.Invoke(loadingIconObj.GetComponent<Image>());
            sprite = await loadTextureFromUrlTask;
            if (sprite == null) return;
            loadingIconObj.Recycle();

            imageComponent.preserveAspect = originPreserveAspect;
            imageComponent.sprite         = sprite;
            imageComponent.DOFade(originAlpha, 0.5f);
        }

        public async UniTask<Sprite> LoadSpriteFromUrl(string url)
        {
            if (this.spriteCache.TryGetValue(url, out var sprite)) return sprite;
            sprite = await this.DownloadSpriteInternal(url);
            return sprite;
        }

        private void CacheSpriteWhenDownloadTextextComplete(string url, Texture texture)
        {
            if (string.IsNullOrEmpty(url) || texture == null) return;
            var outputSprite = this.CreateSpriteFromTexture((Texture2D)texture);
            if (!this.spriteCache.ContainsKey(url)) this.spriteCache.Add(url, outputSprite);
        }

        private async UniTask<Sprite> DownloadSpriteInternal(string url)
        {
            var texture = await this.LoadTextureFromUrl(url);
            if (texture == null) return null;
            var sprite = this.CreateSpriteFromTexture((Texture2D)texture);
            if (!this.spriteCache.ContainsKey(url)) this.spriteCache.Add(url, sprite);

            return sprite;
        }

        public async UniTask<Texture> LoadTextureFromUrl(string url)
        {
            if (this.textureCache.TryGetValue(url, out var texture)) return texture;
            texture = await this.DownloadTextureFromUrlInternal(url);
            if (texture == null)
            {
                this.logger.Error($"LoadTextureFromUrl {url} - Null texture");
                return null;
            }

            if (!this.textureCache.ContainsKey(url)) this.textureCache.Add(url, texture);

            this.CacheSpriteWhenDownloadTextextComplete(url, texture);
            return texture;
        }

        private async UniTask<Texture> DownloadTextureFromUrlInternal(string url)
        {
            try
            {
                if (!this.DownloadingOperation.TryGetValue(url, out var downloadOperation))
                {
                    var request = UnityWebRequestTexture.GetTexture(url);
                    downloadOperation = request.SendWebRequest();
                    this.DownloadingOperation.Add(url, downloadOperation);
                }

                await downloadOperation;
                if (downloadOperation.webRequest.result != UnityWebRequest.Result.Success)
                {
                    this.logger.Error(downloadOperation.webRequest.error);
                    return null;
                }

                var myTexture = DownloadHandlerTexture.GetContent(downloadOperation.webRequest);
                this.DownloadingOperation.Remove(url);
                return myTexture;
            }
            catch (Exception e)
            {
                this.logger.Exception(e);
            }

            return null;
        }

        public Sprite CreateSpriteFromTexture(Texture2D tex)
        {
            return Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(0, 0));
        }
    }
}