namespace Mech.Core.MVP
{
    using System;
    using Cysharp.Threading.Tasks;
    using Mech.Core.AssetLibrary;
    using UnityEngine;
    using Zenject;
    using Object = UnityEngine.Object;

    public interface IUIItemPresenter<TView, TModel>
    {
        public UniTask SetView(Transform parent);
        public void    SetView(TView viewInstance);
        public void    BindData(TModel param);
    }

    /// <summary>
    /// Base UI presenter for item
    /// </summary>
    /// <typeparam name="TView">Type of view</typeparam>
    public abstract class BaseUIItemPresenter<TView> : IUIPresenter where TView : IUIView
    {
        protected         TView  View;
        protected virtual string PrefabPath { get; } = typeof(TView).Name;

        /// <summary>
        /// Set view automatically
        /// </summary>
        /// <param name="parent"></param>
        public async UniTask SetView(Transform parent) { this.View ??= Object.Instantiate(await GameAssets.LoadAssetAsync<GameObject>(this.PrefabPath), parent).GetComponent<TView>(); }

        /// <summary>
        /// Set view manually
        /// </summary>
        /// <param name="viewInstance"></param>
        public void SetView(TView viewInstance) { this.View = viewInstance; }
        public void SetView(IUIView viewInstance) { this.View = (TView)viewInstance; }
    }

    public abstract class BaseUIItemPresenter<TView, TModel> : BaseUIItemPresenter<TView>, IUIItemPresenter<TView, TModel> where TView : IUIView
    {
        public abstract void BindData(TModel param);
    }

    public abstract class BaseUIItemPresenter<TView, TModel1, TModel2> : BaseUIItemPresenter<TView> where TView : IUIView
    {
        public abstract void BindData(TModel1 param1, TModel2 param2);
    }

    /// <summary>
    /// Base UI presenter for item that can poolable
    /// </summary>
    /// <typeparam name="TView">Type of view</typeparam>
    public abstract class BaseUIItemPoolablePresenter<TView> : BaseUIItemPresenter<TView>, IPoolable<IMemoryPool>, IDisposable where TView :  IUIView
    {
        private IMemoryPool pool;

        public void SetActiveView(bool value)
        {
            if (this.View != null) (this.View as MonoBehaviour)?.gameObject.SetActive(value);
        }

        public void OnDespawned()
        {
            this.pool = null;
            this.SetActiveView(false);
        }

        public void OnSpawned(IMemoryPool pool)
        {
            this.pool = pool;
            this.SetActiveView(true);
        }

        public virtual void Dispose() { this.pool.Despawn(this); }
    }

    public abstract class BaseUIItemPoolablePresenter<TView, TModel> : BaseUIItemPoolablePresenter<TView>, IUIItemPresenter<TView, TModel> where TView : MonoBehaviour, IUIView
    {
        public abstract void BindData(TModel param);
    }

    public abstract class BaseUIItemPoolablePresenter<TView, TModel1, TModel2> : BaseUIItemPoolablePresenter<TView> where TView : MonoBehaviour, IUIView
    {
        public abstract void BindData(TModel1 param1, TModel2 param2);
    }
}