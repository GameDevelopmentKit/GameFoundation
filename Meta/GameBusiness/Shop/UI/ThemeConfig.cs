namespace GameBusiness.Shop.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Serializable theme descriptor. Stores a list of <see cref="ColorableElement"/> entries
    /// and applies a color palette to them at runtime.
    /// Used by shop item views and section views for dynamic color theming.
    /// </summary>
    [Serializable]
    public class ThemeConfig
    {
        public List<ColorableElement> ColorableElements;

        [Serializable]
        public class ColorableElement
        {
            public Color       DefaultColor;
            public List<Image> Elements;
        }

        /// <summary>
        /// Applies a hex color palette to all registered <see cref="ColorableElement"/> entries.
        /// Falls back to <see cref="ColorableElement.DefaultColor"/> when the palette has fewer entries.
        /// </summary>
        /// <param name="colorPalette">Ordered list of hex color strings (e.g. "#FF5500").</param>
        public virtual void ApplyTheme(List<string> colorPalette)
        {
            for (var i = 0; i < this.ColorableElements.Count; i++)
            {
                var colorableElements = this.ColorableElements[i];
                var color             = colorableElements.DefaultColor;
                if (i < colorPalette.Count && ColorUtility.TryParseHtmlString(colorPalette[i], out color)) { }

                foreach (var graphic in colorableElements.Elements)
                {
                    graphic.color = color;
                }
            }
        }
    }
}
