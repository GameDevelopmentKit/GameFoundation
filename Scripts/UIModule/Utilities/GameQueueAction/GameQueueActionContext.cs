namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using System;
    using DG.Tweening;
    using GameFoundation.Scripts.UIModule.ScreenFlow;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;
    using UnityEngine.Playables;
    using UnityEngine.Scripting;

    public class GameQueueActionContext
    {
        private readonly GameQueueActionServices gameQueueActionServices;
        private readonly IScreenManager          screenManager;

        [Preserve]
        public GameQueueActionContext(GameQueueActionServices gameQueueActionServices, IScreenManager screenManager)
        {
            this.gameQueueActionServices = gameQueueActionServices;
            this.screenManager           = screenManager;
        }

        /// <summary>
        /// Need to set autoHandleComplete = false if handle completion manually.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actionId"></param>
        /// <param name="location"></param>
        /// <param name="autoHandleComplete"></param>
        /// <returns></returns>
        public IGameQueueAction AddCommonActionToQueueAction(Action<IGameQueueAction> action, string actionId, string location, bool autoHandleComplete = true)
        {
            var baseAction = new BaseQueueAction(actionId, location);
            baseAction.OnStart += queueAction =>
            {
                action?.Invoke(baseAction);
                if (autoHandleComplete) baseAction.Complete();
            };

            this.gameQueueActionServices.Append(baseAction);
            return baseAction;
        }

        public IGameQueueAction AddScreenToQueueAction<TPresenter, TLocationView>(string actionId = "") where TPresenter : IScreenPresenter where TLocationView : IScreenView
        {
            return this.AddScreenToQueueAction<TPresenter>(actionId, ScreenHelper.GetScreenId<TLocationView>());
        }

        public IGameQueueAction AddScreenToQueueAction<TPresenter>(string actionId = "", string location = "") where TPresenter : IScreenPresenter
        {
            var action = new ShowPopupQueueAction<TPresenter>(this.screenManager, string.IsNullOrEmpty(actionId) ? $"ShowScreen_{typeof(TPresenter).Name}" : actionId, this.GetCurrentLocation(location));
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction AddScreenToQueueAction<TPresenter, TModel, TLocationView>(TModel model, string actionId = "") where TPresenter : IScreenPresenter<TModel> where TLocationView : IScreenView
        {
            return this.AddScreenToQueueAction<TPresenter, TModel>(model, actionId, ScreenHelper.GetScreenId<TLocationView>());
        }

        public IGameQueueAction AddScreenToQueueAction<TPresenter, TModel>(TModel model, string actionId = "", string location = "") where TPresenter : IScreenPresenter<TModel>
        {
            var action = new ShowPopupQueueAction<TPresenter, TModel>(this.screenManager, string.IsNullOrEmpty(actionId) ? $"ShowScreen_{typeof(TPresenter).Name}" : actionId, this.GetCurrentLocation(location));
            action.SetState(model);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        private string GetCurrentLocation(string location)
        {
            return string.IsNullOrEmpty(location) ? this.screenManager.CurrentActiveScreen.Value.ScreenId : location;
        }

        public IGameQueueAction AddTimelineToQueueAction<T>(T timeline, string actionId, string location) where T : PlayableDirector
        {
            var action = new PlayTimelineQueueAction(timeline, actionId, location);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction AddTweenToQueueAction<T>(T tween, string actionId, string location) where T : Tween
        {
            var action = new PlayTweenQueueAction(tween, actionId, location);
            this.gameQueueActionServices.Append(action);
            return action;
        }

        public IGameQueueAction SetPriority(IGameQueueAction action, int priority)
        {
            this.gameQueueActionServices.UpdateIndexInQueue(action, priority);
            return action;
        }
    }
}