#if UNITY_EDITOR && JSON_NET_EXISTS

using DA_Assets.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.Extensions
{
    public static class FigmaExtensions
    {
        public static FTag GetFigmaType(this FObject fobject)
        {
            FTag[] allTypes = Enum.GetValues(typeof(FTag))
                .Cast<FTag>()
                .Where(x => x != FTag.Null)
                .ToArray();

            foreach (FTag ftag in allTypes)
            {
                string tag = ftag.GetDescription();

                string[] nameParts = fobject.Name.ToLower().Replace(" ", "").Split(GetTagSeparator());

                if (nameParts.Length >= 1)
                {
                    string name = nameParts[0];

                    float sim = name.CalculateSimilarity(tag);
                
                    if (name == tag)
                    {
                       // Debug.Log($"{fobject.Name} | {name} | {tag}");
                        return ftag;
                    }
                    else if (sim >= Constants.PROBABILITY_MATCHING_TAGS)
                    {
                        //Debug.Log($"{fobject.Name} | {name} | {tag}");
                        return ftag;
                    }

                }
            }

            fobject.IsParent = FigmaParser.IsParent(fobject);

            if (fobject.IsParent == false)
            {
                fobject.IsImage = FigmaParser.IsImage(fobject);
              //  Debug.Log($"{fobject.Name} | {fobject.IsImage}");
            }

            if (fobject.Type == FTag.Text.ToString().ToUpper())
            {
                return FTag.Text;
            }
            else if (fobject.IsImage)
            {
                return FTag.Image;
            }
            else if (fobject.LayoutMode == "VERTICAL")
            {
                return FTag.VerticalLayoutGroup;
            }
            else if (fobject.LayoutMode == "HORIZONTAL")
            {
                return FTag.HorizontalLayoutGroup;
            }
            else if (fobject.IsParent)
            {
                return FTag.Container;
            }

            return FTag.Null;
        }
        public static string GetCustomTag(this FObject fobject)
        {
            string[] customTags = FigmaConverterUnity.Instance.customPrefabs.Select(x => x.Tag).ToArray();

            foreach (string tag in customTags)
            {
                string[] nameParts = fobject.Name.ToLower().Replace(" ", "").Split(GetTagSeparator());

                if (nameParts.Length >= 1)
                {
                    string name = nameParts[0];

                    float sim = name.CalculateSimilarity(tag);

                    if (name == tag)
                    {
                        return tag;
                    }
                    else if (sim >= Constants.PROBABILITY_MATCHING_TAGS)
                    {
                        return tag;
                    }
                }
            }

            return null;
        }
        public static char GetTagSeparator()
        {
            switch (FigmaConverterUnity.Instance.mainSettings.TagSeparator)
            {
                case TagSeparator.Slash:
                    return '/';
                case TagSeparator.Dash:
                    return '-';
                default:
                    return '-';
            }
        }
        public static string GetImageExtension()
        {
            return FigmaConverterUnity.Instance.mainSettings.ImagesFormat.ToString().ToLower();
        }

        public static float GetImageScale()
        {
            switch (FigmaConverterUnity.Instance.mainSettings.ImagesScale)
            {
                case ImageScale.X_0_5:
                    return 0.5f;
                case ImageScale.X_0_75:
                    return 0.75f;
                case ImageScale.X_1_0:
                    return 1f;
                case ImageScale.X_1_5:
                    return 1.5f;
                case ImageScale.X_2_0:
                    return 2.0f;
                case ImageScale.X_3_0:
                    return 3.0f;
                case ImageScale.X_4_0:
                    return 4.0f;
                default:
                    return 4.0f;
            }
        }
        public static string FormatName(string childName)
        {
            childName = childName.Replace($" {GetTagSeparator()} ", "_");
            childName = childName.Replace($"{GetTagSeparator()}", "_");

            if (childName.Last() == ' ')
            {
                childName = childName.Remove(childName.Length - 1);
            }

            childName = childName.Replace(" ", "_");
            Regex regex = new Regex("(?:[^a-z0-9_ ]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            childName = regex.Replace(childName, string.Empty);
            return childName;
        }
        public static string GetChildName(this string childName, bool specialChars)
        {
            if (specialChars)
            {
                childName = FormatName(childName);
            }

            childName += "." + GetImageExtension();
            return childName;
        }

        public static string GetAssetPath(this FObject fobject, bool full)
        {
            string name = fobject.Name.GetChildName(true);
            string rootFrameName = FormatName(fobject.RootFrameName);
            string spriteDir = fobject.IsMutual ? "Mutual" : rootFrameName;
            string spritesPath = string.Format("{0}/Sprites/{1}", Application.dataPath, spriteDir);

            DirectoryInfo dinfo = Directory.CreateDirectory(spritesPath);

            string fullPath = string.Format("{0}/{1}", dinfo.FullName, name);
            string shortPath = $"Assets/Sprites/{rootFrameName}/{name}";

            if (full)
            {
                return fullPath;
            }
            else
            {
                return shortPath;
            }
        }

        public static Color GetTextColor(this FObject text)
        {
            if (text.Fills[0].GradientStops != null)
            {
                return text.Fills[0].GradientStops[0].Color;
            }
            else
            {
                return text.Fills[0].Color;
            }
        }

        public static void SetImgTypeSprite(this FObject fobject)
        {
            try
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(fobject.AssetPath))
                    {
                        break;
                    }

                    TextureImporter importer = AssetImporter.GetAtPath(fobject.AssetPath) as TextureImporter;
                    if (importer.textureType == TextureImporterType.Sprite && importer.isReadable == true)
                    {
                        break;
                    }

                    importer.isReadable = true;
                    importer.textureType = TextureImporterType.Sprite;
                    AssetDatabase.WriteImportSettingsIfDirty(fobject.AssetPath);
                    AssetDatabase.Refresh();
                }
            }
            catch
            {
                Console.Warning(Localization.SPRITE_NOT_FOUND);
            }
        }

    }
}
#endif