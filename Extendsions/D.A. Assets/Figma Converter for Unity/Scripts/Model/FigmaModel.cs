

#if UNITY_EDITOR && JSON_NET_EXISTS
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.Model
{
    [Serializable]
    public class AbsoluteBoundingBox
    {

        [JsonProperty("x")]
        public float X;

        [JsonProperty("y")]
        public float Y;

        [JsonProperty("width")]
        public float Width;

        [JsonProperty("height")]
        public float Height;
    }
    [Serializable]
    public class Constraints
    {

        [JsonProperty("vertical")]
        public string Vertical;

        [JsonProperty("horizontal")]
        public string Horizontal;
    }

    [Serializable]
    public class GradientStop
    {

        [JsonProperty("color")]
        public Color Color;

        [JsonProperty("position")]
        public float Position;
    }
    [Serializable]
    public class Fill
    {
        [JsonProperty("blendMode")]
        public string BlendMode;

        [JsonProperty("opacity")]
        public float? Opacity;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("scaleMode")]
        public string ScaleMode;

        [JsonProperty("imageRef")]
        public string ImageRef;

        [JsonProperty("color")]
        public Color Color;

        [JsonProperty("visible")]
        public bool? Visible;

        [JsonProperty("gradientHandlePositions")]
        public List<Vector2> GradientHandlePositions;

        [JsonProperty("gradientStops")]
        public List<GradientStop> GradientStops;
    }
    [Serializable]
    public class Geometry
    {

        [JsonProperty("path")]
        public string Path;

        [JsonProperty("windingRule")]
        public string WindingRule;
    }
    [Serializable]
    public class Effect
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("visible")]
        public bool Visible;

        [JsonProperty("color")]
        public Color Color;

        [JsonProperty("blendMode")]
        public string BlendMode;

        [JsonProperty("offset")]
        public Vector2 Offset;

        [JsonProperty("radius")]
        public float Radius;
    }
    [Serializable]

    public class Stroke
    {

        [JsonProperty("blendMode")]
        public string BlendMode;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("color")]
        public Color Color;
    }
    [Serializable]
    public class Styles
    {
        [JsonProperty("fill")]
        public string Fill;

        [JsonProperty("effect")]
        public string Effect;

        [JsonProperty("stroke")]
        public string Stroke;
    }
    [Serializable]
    public class Background
    {

        [JsonProperty("blendMode")]
        public string BlendMode;

        [JsonProperty("visible")]
        public bool Visible;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("color")]
        public Color Color;
    }

    [Serializable]
    public class Style
    {

        [JsonProperty("fontFamily")]
        public string FontFamily;

        [JsonProperty("fontPostScriptName")]
        public string FontPostScriptName;

        [JsonProperty("italic")]
        public bool Italic;

        [JsonProperty("fontWeight")]
        public int FontWeight;

        [JsonProperty("textAutoResize")]
        public string TextAutoResize;

        [JsonProperty("fontSize")]
        public float FontSize;

        [JsonProperty("textAlignHorizontal")]
        public string TextAlignHorizontal;

        [JsonProperty("textAlignVertical")]
        public string TextAlignVertical;

        [JsonProperty("letterSpacing")]
        public float LetterSpacing;

        [JsonProperty("lineHeightPx")]
        public float LineHeightPx;

        [JsonProperty("lineHeightPercent")]
        public float LineHeightPercent;

        [JsonProperty("lineHeightUnit")]
        public string LineHeightUnit;
    }

    [Serializable]
    public class Constraint
    {

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("value")]
        public float Value;
    }
    [Serializable]
    public class ExportSetting
    {

        [JsonProperty("suffix")]
        public string Suffix;

        [JsonProperty("format")]
        public string Format;

        [JsonProperty("constraint")]
        public Constraint Constraint;
    }
    [Serializable]
    public class FigmaSize
    {

        [JsonProperty("width")]
        public float Width;

        [JsonProperty("height")]
        public float Height;
    }
    [Serializable]
    public class PrototypeDevice
    {

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("size")]
        public FigmaSize Size;

        [JsonProperty("presetIdentifier")]
        public string PresetIdentifier;

        [JsonProperty("rotation")]
        public string Rotation;
    }
    [Serializable]
    public class FObject
    {
        public AnchorType AnchorPreset;
        public bool IsMutual;
        public string RootFrameName;
        public int Level => Parent is null ? 0 : Parent.Level + 1;
        public GameObject GameObj;
        public bool IsParent;
        public bool IsImage;
        public FObject Parent;
        public FTag FTag;
        public string CustomTag;
        public string AssetPath;
        public string Link;
        public bool NeedDownload;
        /// <summary>
        /// /
        /// </summary>
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("children")]
        public List<FObject> Children;

        [JsonProperty("backgroundColor")]
        public Color BackgroundColor;

        [JsonProperty("prototypeStartNodeID")]
        public object PrototypeStartNodeID;

        [JsonProperty("prototypeDevice")]
        public PrototypeDevice PrototypeDevice;

        [JsonProperty("blendMode")]
        public string BlendMode;

        [JsonProperty("absoluteBoundingBox")]
        public AbsoluteBoundingBox AbsoluteBoundingBox;

        [JsonProperty("preserveRatio")]
        public bool PreserveRatio;

        [JsonProperty("constraints")]
        public Constraints Constraints;

        [JsonProperty("relativeTransform")]
        public List<List<float>> RelativeTransform;

        [JsonProperty("size")]
        public Vector2 Size;

        [JsonProperty("fills")]
        public List<Fill> Fills;

        [JsonProperty("fillGeometry")]
        public List<Geometry> FillGeometry;

        [JsonProperty("strokes")]
        public List<Stroke> Strokes;

        [JsonProperty("strokeWeight")]
        public float StrokeWeight;

        [JsonProperty("strokeAlign")]
        public string StrokeAlign;

        [JsonProperty("strokeGeometry")]
        public List<Geometry> StrokeGeometry;

        [JsonProperty("effects")]
        public List<Effect> Effects;

        [JsonProperty("clipsContent")]
        public bool? ClipsContent;

        [JsonProperty("background")]
        public List<Background> Background;

        [JsonProperty("exportSettings")]
        public List<ExportSetting> ExportSettings;

        [JsonProperty("componentId")]
        public string ComponentId;

        [JsonProperty("cornerRadius")]
        public float CornerRadius;

        [JsonProperty("rectangleCornerRadii")]
        public List<float> RectangleCornerRadius;

        [JsonProperty("styles")]
        public Styles Styles;

        [JsonProperty("visible")]
        public bool? Visible;

        [JsonProperty("opacity")]
        public float? Opacity;

        [JsonProperty("layoutGrids")]
        public List<object> LayoutGrids;

        [JsonProperty("layoutMode")]
        public string LayoutMode;

        [JsonProperty("itemSpacing")]
        public float ItemSpacing;

        [JsonProperty("paddingLeft")]
        public float PaddingLeft;

        [JsonProperty("paddingRight")]
        public float PaddingRight;

        [JsonProperty("paddingTop")]
        public float PaddingTop;

        [JsonProperty("paddingBottom")]
        public float PaddingBottom;

        [JsonProperty("characters")]
        public string Characters;

        [JsonProperty("style")]
        public Style Style;

        [JsonProperty("characterStyleOverrides")]
        public List<object> CharacterStyleOverrides;

        [JsonProperty("styleOverrideTable")]
        public object StyleOverrideTable;

        [JsonProperty("strokeCap")]
        public string StrokeCap;

        [JsonProperty("strokeJoin")]
        public string StrokeJoin;

        [JsonProperty("strokeDashes")]
        public List<float> StrokeDashes;

        [JsonProperty("strokeMiterAngle")]
        public float? StrokeMiterAngle;

        [JsonProperty("layoutAlign")]
        public string LayoutAlign;

        [JsonProperty("layoutGrow")]
        public float LayoutGrow;

        [JsonProperty("isMask")]
        public bool? IsMask;
    }
    [Serializable]
    public class FigmaProject
    {
        [JsonProperty("document")]
        public FObject Document;
    }
}
#endif