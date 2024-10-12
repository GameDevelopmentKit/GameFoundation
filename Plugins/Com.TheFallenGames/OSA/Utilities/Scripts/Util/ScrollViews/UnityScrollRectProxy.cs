using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using frame8.Logic.Misc.Other.Extensions;
using frame8.Logic.Misc.Visual.UI;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.Util.ScrollViews
{
    /// <summary>
    /// Provides access to a Unity's ScrollRect through <see cref="IScrollRectProxy"/>.
    /// For example, it can be added to a regular ScrollRect so <see cref="ScrollbarFixer8"/> can communicate with it, in case you want to use the <see cref="ScrollbarFixer8"/> without OSA.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class UnityScrollRectProxy : MonoBehaviour, IScrollRectProxy
    {
        #region IScrollRectProxy properties implementation

        public bool          IsInitialized                 => this.ScrollRect != null;
        public Vector2       Velocity                      { get => this.ScrollRect.velocity; set => this.ScrollRect.velocity = value; }
        public bool          IsHorizontal                  => this.ScrollRect.horizontal;
        public bool          IsVertical                    => this.ScrollRect.vertical;
        public RectTransform Content                       => this.ScrollRect.content;
        public RectTransform Viewport                      => this.ScrollRect.viewport;
        public double        ContentInsetFromViewportStart => this.Content.GetInsetFromParentEdge(this.Viewport, (this as IScrollRectProxy).GetStartEdge());
        public double        ContentInsetFromViewportEnd   => this.Content.GetInsetFromParentEdge(this.Viewport, (this as IScrollRectProxy).GetEndEdge());

        #endregion

        private ScrollRect ScrollRect
        {
            get
            {
                if (!this._ScrollRect) this._ScrollRect = this.GetComponent<ScrollRect>();
                return this._ScrollRect;
            }
        }

        private ScrollRect _ScrollRect;

        private void Awake()
        {
            if (this.ScrollRect == null) throw new UnityException(this.GetType().Name + ": No ScrollRect component found");
        }

        #region IScrollRectProxy methods implementation

#pragma warning disable 0067
        public event Action<double> ScrollPositionChanged;
#pragma warning restore 0067
        public void SetNormalizedPosition(double normalizedPosition)
        {
            if (this.IsHorizontal)
                this.ScrollRect.horizontalNormalizedPosition = (float)normalizedPosition;
            else
                this.ScrollRect.verticalNormalizedPosition = (float)normalizedPosition;
        }

        public double GetNormalizedPosition()
        {
            return this.IsHorizontal ? this.ScrollRect.horizontalNormalizedPosition : this.ScrollRect.verticalNormalizedPosition;
        }

        public double GetContentSize()
        {
            return this.IsHorizontal ? this.Content.rect.width : this.Content.rect.height;
        }

        public double GetViewportSize()
        {
            return this.IsHorizontal ? this.Viewport.rect.width : this.Viewport.rect.height;
        }

        public void StopMovement()
        {
            this.ScrollRect.StopMovement();
        }

        #endregion
    }
}