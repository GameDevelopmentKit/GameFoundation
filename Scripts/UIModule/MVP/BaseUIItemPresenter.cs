namespace GameFoundation.Scripts.UIModule.MVP
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public interface IUIItemPresenter : IUIPresenter
    {
        /// <summary>
        /// Show/Hide view
        /// </summary>
        /// <param name="value"></param>
        void SetActiveView(bool value);
    }

    public interface IUIItemPresenter<in TView, in TModel>
    {
        public UniTask SetView(Transform parent);
        public void    SetView(TView     viewInstance);
        public void    BindData(TModel   param);
    }

    /// <summary>
    /// Base UI presenter for item
    /// </summary>
    /// <typeparam name="TView">Type of view</typeparam>
    public abstract class BaseUIItemPresenter<TView> : IUIItemPresenter where TView : MonoBehaviour, IUIView
    {
        public            TView  View       { get; private set; }
        protected virtual string PrefabPath { get; } = typeof(TView).Name;

        protected readonly IGameAssets GameAssets;

        protected BaseUIItemPresenter(IGameAssets gameAssets)
        {
            this.GameAssets = gameAssets;
        }

        /// <summary>
        /// Set view automatically
        /// </summary>
        /// <param name="parent"></param>
        public virtual async UniTask SetView(Transform parent)
        {
            if (this.View == null) this.SetView(Object.Instantiate(await this.GameAssets.LoadAssetAsync<GameObject>(this.PrefabPath), parent).GetComponent<TView>());
        }

        /// <summary>
        /// Set view automatically
        /// </summary>
        /// <param name="parent"></param>
        public async UniTask SetView(Vector3 worldPosition)
        {
            if (this.View == null)
            {
                var view = Object.Instantiate(await this.GameAssets.LoadAssetAsync<GameObject>(this.PrefabPath)).GetComponent<TView>();
                this.SetView(view);
                view.transform.position = worldPosition;
            }
        }

        /// <summary>
        /// Set view automatically
        /// </summary>
        /// <param name="prefabView"></param>
        /// <param name="parent"></param>
        public void SetView(GameObject prefabView, Transform parent)
        {
            if (this.View == null) this.SetView(Object.Instantiate(prefabView, parent).GetComponent<TView>());
        }

        /// <summary>
        /// Set view manually
        /// </summary>
        /// <param name="viewInstance"></param>
        public virtual void SetView(TView viewInstance)
        {
            this.View = viewInstance;
        }

        public void SetView(IUIView viewInstance)
        {
            this.View = (TView)viewInstance;
        }

        public virtual void SetActiveView(bool value)
        {
            if (this.View != null) this.View.gameObject.SetActive(value);
        }
    }

    public abstract class BaseUIItemPresenter<TView, TModel> : BaseUIItemPresenter<TView>, IUIItemPresenter<TView, TModel>, IDisposable where TView : MonoBehaviour, IUIView
    {
        protected BaseUIItemPresenter(IGameAssets gameAssets) : base(gameAssets)
        {
        }

        public virtual void OnViewReady()
        {
        }

        public abstract void BindData(TModel param);

        public virtual void Dispose()
        {
        }
    }

    public abstract class BaseUIItemPresenter<TView, TModel1, TModel2> : BaseUIItemPresenter<TView> where TView : MonoBehaviour, IUIView
    {
        protected BaseUIItemPresenter(IGameAssets gameAssets) : base(gameAssets)
        {
        }

        public abstract void BindData(TModel1 param1, TModel2 param2);
    }

    public class TViewMono : MonoBehaviour, IUIView
    {
    }
}