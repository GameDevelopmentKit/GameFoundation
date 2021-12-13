#if UNITY_EDITOR
using DA_Assets.Model;
#if TMPRO_EXISTS
using TMPro;
#endif
using UnityEngine;

namespace DA_Assets
{
    public class DefaultSettings
    {
        public static MainSettings mainSettings = new MainSettings
        {
            ApiKey = "",
            ProjectUrl = "",
            TagSeparator = TagSeparator.Dash,
            ImagesFormat = ImageFormat.Png,
            ImagesScale = ImageScale.X_4_0,
            ImageComponent = ImageComponent.UnityImage,
            PivotType = PivotType.MiddleCenter,
            UseCustomPrefabs = false,
            IncludeTagInGameObjectName = true,
            SaveJsonFile = false,
            TextComponent = TextComponent.Standard, 
#if I2LOC_EXISTS
            UseI2Localization = false,
#endif
        };

        public static StandardTextSettings defaultTextSettings = new StandardTextSettings
        {
            FontLineSpacing = 1.0f,
            HorizontalWrapMode = HorizontalWrapMode.Wrap,
            VerticalWrapMode = VerticalWrapMode.Truncate,
            BestFit = true
        };

        public static ProceduralImageSettings proceduralImageSettings = new ProceduralImageSettings
        {
            Type = UnityImageType.Simple,
            RaycastTarget = true,
            ModifierType = ModifierType.Free,
            BorderWidth = 0,
            FalloffDistance = 1
        };

#if TMPRO_EXISTS
        public static TextMeshProSettings textMeshProSettings = new TextMeshProSettings
        {
            AutoSize = false,
            OverrideTags = false,
            Wrapping = true,
            RichText = true,
            RaycastTarget = true,
            ParseEscapeCharacters = true,
            VisibleDescender = true,
            Kerning = true,
            ExtraPadding = false,
            Overflow = TextOverflowModes.Overflow,
            HorizontalMapping = TextureMappingOptions.Character,
            VerticalMapping = TextureMappingOptions.Character,
            GeometrySorting = VertexSortingOrder.Normal,
        };
#endif
    }
}
#endif