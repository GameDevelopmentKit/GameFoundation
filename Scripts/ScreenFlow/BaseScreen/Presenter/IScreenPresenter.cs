namespace GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter
{
    using System;
    using System.Threading.Tasks;
    using GameFoundation.Scripts.MVP;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// The Presenter is the link between the Model and the View. It holds the state of the View and updates it depending on that state and on external events:
    /// - Holds the application state needed for that view
    /// - Controls view flow
    /// - Shows/hides/activates/deactivates/updates the view or parts of the view depending on the state.
    /// - Handles events either triggered by the player in the View (e.g. the player touched a button) or triggered by the Model (e.g. the player has gained XP and that triggered a Level Up event so the controller updates the level Number in the view)
    /// </summary>
    public interface IScreenPresenter : IUIPresenter, IInitializable, IDisposable
    {
        public bool         IsClosePrevious { get; }
        public ScreenStatus ScreenStatus    { get; }

        public void      SetViewParent(Transform parent);
        public Transform GetViewParent();
        public void      BindData();

        public Task OpenViewAsync();
        public Task CloseViewAsync();
        public void CloseView();
        public void HideView();
        public void DestroyView();

        /// <summary>
        /// Called when the screen is overlap by another screen 
        /// </summary>
        public void OnOverlap();

        public int ViewSiblingIndex { get; set; }
    }

    public interface IScreenPresenter<in TModel> : IScreenPresenter
    {
        public Task OpenView(TModel model);
    }

    public enum ScreenStatus
    {
        Opened,
        Closed,
        Hide,
        Destroyed,
    }
}