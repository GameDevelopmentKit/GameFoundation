//-----------------------------------------------------------------------
// <copyright file="Toggle.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Sirenix.OdinInspector.Editor.Internal;
using UnityEngine;

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
    public class Toggle
    {
        public string Label;
        public float StartXMin;
        public float StartXMax;
        public Color StartColor;
        public Rect CurrentThumbRect;
        public Color CurrentColor;
        public bool Enabled;
        public SirenixAnimationUtility.InterpolatedFloat T1 = new SirenixAnimationUtility.InterpolatedFloat {Destination = 1f};
        public SirenixAnimationUtility.InterpolatedFloat T2 = new SirenixAnimationUtility.InterpolatedFloat {Destination = 1f};

        private bool _toggled;

        public bool Toggled
        {
            get => this._toggled;
            set
            {
                this._toggled = value;
                this.StartXMin = this.CurrentThumbRect.xMin;
                this.StartXMax = this.CurrentThumbRect.xMax;
                this.StartColor = this.CurrentColor;
            }
        }
    }
}