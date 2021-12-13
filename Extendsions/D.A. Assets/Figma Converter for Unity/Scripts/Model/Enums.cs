#if UNITY_EDITOR
using System.ComponentModel;
using UnityEngine;

namespace DA_Assets
{
    public enum FTag
    {
        Null,
        [Description("cont")] Container,
        [Description("frame")] Frame,
        [Description("img")] Image,
        [Description("btn")] Button,
        [Description("txt")] Text,
        [Description("bg")] Background,
        [Description("canvas")] Page,
        [Description("field")] InputField,
        [Description("pholder")] Placeholder,
        [Description("hor")] HorizontalLayoutGroup,
        [Description("vert")] VerticalLayoutGroup,
        [Description("grid")] GridLayoutGroup
    }
    public enum ImageFormat
    {
        Png,
        Jpg
    }
    
    public enum ImageScale
    {
        X_0_5,
        X_0_75,
        X_1_0,
        X_1_5,
        X_2_0,
        X_3_0,
        X_4_0
    }

    public enum ButtonStates
    {
        [Description("default")] Default,
        [Description("hover")] Hover,
        [Description("disabled")] Disabled//!!!
    }

    public enum LayoutMode
    {
        Undefined = -1,
        Min = 0,
        Middle = 1,
        Max = 2,
        Stretch = 3
    }
    public enum RectAxis
    {
        Hor,
        Vert
    }
    public enum TextComponent
    {
        Standard,
#if TMPRO_EXISTS
        TextMeshPro
#endif
    }

    public enum ShadowType
    {
        None,
#if TRUESHADOW_EXISTS
        TrueShadow
#endif
    }

    public enum AnchorType
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
        BottomStretch,

        VertStretchLeft,
        VertStretchRight,
        VertStretchCenter,

        HorStretchTop,
        HorStretchMiddle,
        HorStretchBottom,

        StretchAll,
    }

    public enum PivotType
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
    }
    public struct AnchorMinMax
    {
        public Vector2 Min;
        public Vector2 Max;
    }

    public enum Resizing
    {
        FixedWidth,
        FixedHeight,
        FillContainer
    }
    public enum ModifierType
    {
        Free,
        OnlyOneEdge,
        Round,
        Uniform
    }

    public enum UnityImageType
    {
        Simple,
        Filled
    }

    public enum ImageComponent
    {
        UnityImage,
#if MPUIKIT_EXISTS
        MPImage,
#endif
#if PUI_EXISTS
        ProceduralImage
#endif
    }

    public enum TagSeparator
    {
        Slash,
        Dash
    } 
}
#endif