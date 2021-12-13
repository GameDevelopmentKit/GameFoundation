#if UNITY_EDITOR && JSON_NET_EXISTS
using System;
using System.Collections.Generic;
using DA_Assets.Model;
using UnityEngine.UI;
using UnityEngine;
#if TRUESHADOW_EXISTS
#endif
#if TMPRO_EXISTS
using TMPro;
#endif
namespace DA_Assets.Extensions
{
    public static class TextStyle
    {
        private static FigmaConverterUnity figmaConverterUnity => UnityEngine.Object.FindObjectOfType<FigmaConverterUnity>();

#if TMPRO_EXISTS
        public static void SetTextMeshProStyle(this TextMeshProUGUI text, FObject fobject)
        {
            text.text = fobject.Characters;
            text.fontSize = fobject.Style.FontSize;

            if (fobject.Fills[0].GradientStops != null)
            {
                text.color = fobject.Fills[0].GradientStops[0].Color;
            }
            else
            {
                text.color = fobject.Fills[0].Color;
            }

            text.overrideColorTags = figmaConverterUnity.textMeshProSettings.OverrideTags;
            text.enableAutoSizing = figmaConverterUnity.textMeshProSettings.AutoSize;
            text.enableWordWrapping = figmaConverterUnity.textMeshProSettings.Wrapping;
            text.richText = figmaConverterUnity.textMeshProSettings.RichText;
            text.raycastTarget = figmaConverterUnity.textMeshProSettings.RaycastTarget;
            text.parseCtrlCharacters = figmaConverterUnity.textMeshProSettings.ParseEscapeCharacters;
            text.useMaxVisibleDescender = figmaConverterUnity.textMeshProSettings.VisibleDescender;
            text.enableKerning = figmaConverterUnity.textMeshProSettings.Kerning;
            text.extraPadding = figmaConverterUnity.textMeshProSettings.ExtraPadding;
            text.overflowMode = figmaConverterUnity.textMeshProSettings.Overflow;
            text.horizontalMapping = figmaConverterUnity.textMeshProSettings.HorizontalMapping;
            text.verticalMapping = figmaConverterUnity.textMeshProSettings.VerticalMapping;
            text.geometrySortingOrder = figmaConverterUnity.textMeshProSettings.GeometrySorting;

            text.SetTextMeshProAligment(fobject);
            text.SetFigmaFont(fobject);
        }
        public static void SetTextMeshProAligment(this TextMeshProUGUI text, FObject fobject)
        {
            string textAligment = fobject.Style.TextAlignVertical + " " + fobject.Style.TextAlignHorizontal;

            switch (textAligment)
            {
                case "BOTTOM CENTER":
                    text.alignment = TextAlignmentOptions.Bottom;
                    break;
                case "BOTTOM LEFT":
                    text.alignment = TextAlignmentOptions.BottomLeft;
                    break;
                case "BOTTOM RIGHT":
                    text.alignment = TextAlignmentOptions.BottomRight;
                    break;
                case "CENTER CENTER":
                    text.alignment = TextAlignmentOptions.Center;
                    break;
                case "CENTER LEFT":
                    text.alignment = TextAlignmentOptions.Left;
                    break;
                case "CENTER RIGHT":
                    text.alignment = TextAlignmentOptions.Right;
                    break;
                case "TOP CENTER":
                    text.alignment = TextAlignmentOptions.Top;
                    break;
                case "TOP LEFT":
                    text.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case "TOP RIGHT":
                    text.alignment = TextAlignmentOptions.TopRight;
                    break;
                default:
                    text.alignment = TextAlignmentOptions.Center;
                    break;
            }
        }
#endif
        public static void SetDefaultTextStyle(this Text text, FObject fobject)
        {
            text.resizeTextForBestFit = figmaConverterUnity.defaultTextSettings.BestFit;
            text.text = fobject.Characters;
            text.resizeTextMinSize = 4;

            text.resizeTextMaxSize = Convert.ToInt32(fobject.Style.FontSize);
            text.fontSize = Convert.ToInt32(fobject.Style.FontSize);

            text.verticalOverflow = figmaConverterUnity.defaultTextSettings.VerticalWrapMode;
            text.horizontalOverflow = figmaConverterUnity.defaultTextSettings.HorizontalWrapMode;
            text.lineSpacing = figmaConverterUnity.defaultTextSettings.FontLineSpacing;

            if (fobject.Fills[0].GradientStops != null)
            {
                text.color = fobject.Fills[0].GradientStops[0].Color;
            }
            else
            {
                text.color = fobject.Fills[0].Color;
            }

            text.SetFigmaFont(fobject);
            text.SetDefaultTextFontStyle(fobject);
            text.SetDefaultTextAligment(fobject);
        }
        public static void SetDefaultTextFontStyle(this Text text, FObject fobject)
        {
            string fontStyleRaw = fobject.Style.FontPostScriptName;

            if (fontStyleRaw != null)
            {
                if (fontStyleRaw.Contains(FontStyle.Bold.ToString()))
                {
                    if (fobject.Style.Italic)
                    {
                        text.fontStyle = FontStyle.BoldAndItalic;
                    }
                    else
                    {
                        text.fontStyle = FontStyle.Bold;
                    }
                }
                else if (fobject.Style.Italic)
                {
                    text.fontStyle = FontStyle.Italic;
                }
                else
                {
                    text.fontStyle = FontStyle.Normal;
                }
            }
        }
        public static void SetDefaultTextAligment(this Text text, FObject fobject)
        {
            string textAligment = fobject.Style.TextAlignVertical + " " + fobject.Style.TextAlignHorizontal;

            switch (textAligment)
            {
                case "BOTTOM CENTER":
                    text.alignment = TextAnchor.LowerCenter;
                    break;
                case "BOTTOM LEFT":
                    text.alignment = TextAnchor.LowerLeft;
                    break;
                case "BOTTOM RIGHT":
                    text.alignment = TextAnchor.LowerRight;
                    break;
                case "CENTER CENTER":
                    text.alignment = TextAnchor.MiddleCenter;
                    break;
                case "CENTER LEFT":
                    text.alignment = TextAnchor.MiddleLeft;
                    break;
                case "CENTER RIGHT":
                    text.alignment = TextAnchor.MiddleRight;
                    break;
                case "TOP CENTER":
                    text.alignment = TextAnchor.UpperCenter;
                    break;
                case "TOP LEFT":
                    text.alignment = TextAnchor.UpperLeft;
                    break;
                case "TOP RIGHT":
                    text.alignment = TextAnchor.UpperRight;
                    break;
                default:
                    text.alignment = TextAnchor.MiddleCenter;
                    break;
            }
        }

#if TMPRO_EXISTS
        /// <summary> This method written by <see href="https://github.com/HyperLethalVector"/> </summary>
        public static void SetFigmaFont(this TextMeshProUGUI text, FObject fobject)
        {
            List<TMP_FontAsset> fonts = FigmaConverterUnity.Instance.textMeshProFonts;

            if (fonts == null)
            {
                text.font = Resources.GetBuiltinResource<TMP_FontAsset>("Arial.ttf");
                return;
            }

            foreach (TMP_FontAsset font in fonts)
            {
                if (font == null)
                    continue;

                float _sim = fobject.Style.FontPostScriptName.CalculateSimilarity(font.name.Replace(" SDF",""));

                if (_sim >= Constants.PROBABILITY_MATCHING_FONS)
                {
                    text.font = font;
                    text.fontSharedMaterial  = font.material;
                    return;
                }
            }

            text.font = Resources.GetBuiltinResource<TMP_FontAsset>("Arial.ttf");
        }
#endif

        public static void SetFigmaFont(this Text text, FObject fobject)
        {
            List<Font> fonts = UnityEngine.Object
               .FindObjectOfType<Canvas>()
               .GetComponent<FigmaConverterUnity>().fonts;

            if (fonts == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return;
            }

            foreach (Font font in fonts)
            {
                if (font == null)
                    continue;

                float _sim = fobject.Style.FontPostScriptName.CalculateSimilarity(font.name);
                if (_sim >= Constants.PROBABILITY_MATCHING_FONS)
                {
                    text.font = font;
                    return;
                }
            }

            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
#endif