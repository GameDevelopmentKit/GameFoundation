namespace GameFoundation.Scripts.Network.WebService
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Network.WebService.Interface;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>Provide a way to send http request, download content.</summary>
    public interface IHttpService
    {
        /// <summary>Send http request async with a IHttpRequestData</summary>
        UniTask SendAsync<T, TK>(IHttpRequestData httpRequestData = null) where T : BaseHttpRequest, IDisposable
            where TK : IHttpResponseData;

        /// <summary>Download from <paramref name="address"/> to <paramref name="filePath"/>, download progress will be updated into <paramref name="onDownloadProgress"/>.</summary>
        UniTask Download(string address, string filePath, OnDownloadProgressDelegate onDownloadProgress);

        /// <summary>Return the real download path.</summary>
        string GetDownloadPath(string path);
    }

    /// <summary>Download progress delegate.</summary>
    public delegate void OnDownloadProgressDelegate(long downloaded, long downloadLength);


    /// <summary>All http request object will implement this class.</summary>
    public abstract class BaseHttpRequest
    {
        public abstract void Process(IHttpResponseData responseData);
        public virtual  void ErrorProcess(int statusCode) { throw new MissStatusCodeException(); }

        public virtual void PredictProcess(IHttpRequestData requestData) { }

        public class MissStatusCodeException : Exception
        {
        }
    }

    /// <summary>
    /// All http request class will extend this. It will manage main flow to process response data, pool circle,..
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    public abstract class BaseHttpRequest<T> : BaseHttpRequest, IDisposable, IPoolable<IMemoryPool>
        where T : IHttpResponseData
    {
        protected readonly ILogService Logger;

        private IMemoryPool pool;

        protected BaseHttpRequest(ILogService logger) { this.Logger = logger; }


        public void Dispose()                   { this.pool.Despawn(this); }
        public void OnDespawned()               { this.Logger.Log($"spawned {this}"); }
        public void OnSpawned(IMemoryPool pool) { this.pool = pool; }

        public override void Process(IHttpResponseData responseData)
        {
            this.PreProcess((T)responseData);
            this.Process((T)responseData);
            this.PostProcess((T)responseData);
        }

        public abstract void Process(T responseData);
        public virtual  void PostProcess(T responseData) { }
        public virtual  void PreProcess(T responseData)  { }
    }

    public interface IFakeResponseAble<out T> : IHttpResponseData
    {
        T FakeResponse();
    }
}