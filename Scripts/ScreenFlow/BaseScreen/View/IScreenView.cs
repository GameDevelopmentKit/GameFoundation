namespace GameFoundation.Scripts.ScreenFlow.BaseScreen.View
{
    using System;
    using GameFoundation.Scripts.MVP;
    using UnityEngine;

    /// <summary>
    /// The responsibilities of a view are:
    /// -Handle references to elements needed for drawing (Textures, FXs, etc)
    /// -Perform Animations
    /// -Receive User Input
    /// -..
    /// </summary>
    public interface IScreenView : IUIView
    {
        public RectTransform RectTransform { get; }
        public bool          IsReadyToUse  { get; }
        public void          Open();
        public void          Close();
        public void          Hide();

        public void DestroySelf();

        public event Action ViewDidClose;
        public event Action ViewDidOpen;
        public event Action ViewDidDestroy;
    }
}