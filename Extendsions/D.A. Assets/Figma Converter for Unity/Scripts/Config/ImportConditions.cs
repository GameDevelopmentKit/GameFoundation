#if UNITY_EDITOR && JSON_NET_EXISTS
using DA_Assets.Extensions;
using DA_Assets.Model;
using System.Linq;
using UnityEngine;

namespace DA_Assets
{
    static class ImportConditions
    {
        public static bool IsDownloadable(this FObject fobject)
        {
            if (IsDownloadableImage(fobject) == false)
            {
                return false;
            }

            bool value = fobject.IsParent ||
                         fobject.FTag == FTag.Button ||
                         fobject.FTag == FTag.Text ||
                         fobject.FTag == FTag.Frame ||
                         fobject.FTag == FTag.HorizontalLayoutGroup ||
                         fobject.FTag == FTag.VerticalLayoutGroup ||
                         fobject.FTag == FTag.GridLayoutGroup ||
                         fobject.CustomTag != null;

            return !value;
        }
        public static bool IsDownloadableImage(FObject fobject)
        {
            if (fobject.Children != null)
            {
                return true;
            }

            if (fobject.Fills == null)
            {
                return true;
            }

            bool solidFill = fobject.Fills.Count() == 1 && fobject.Fills[0].Type == "SOLID";
            bool linearFill = fobject.Fills.Count() == 1 && fobject.Fills[0].Type == "GRADIENT_LINEAR";

            if (FigmaConverterUnity.Instance.mainSettings.ImageComponent == ImageComponent.UnityImage)
            {
                if (solidFill == false)
                {
                    return true;
                }

                if (fobject.CornerRadius > 0)
                {
                    return true;
                }

                if (fobject.RectangleCornerRadius == null)
                {
                    return false;
                }

                foreach (var item in fobject.RectangleCornerRadius)
                {
                    if (item > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
#if MPUIKIT_EXISTS
            else if(FigmaConverterUnity.Instance.mainSettings.ImageComponent == ImageComponent.MPImage)
            {
                if (solidFill == false && linearFill == false)
                {
                    return true;
                }
                else
                {
                    return false;
                }
              
            }
#endif
#if PUI_EXISTS
            else if (FigmaConverterUnity.Instance.mainSettings.ImageComponent == ImageComponent.ProceduralImage)
            {
                if (solidFill == false)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
#endif
            return true;
        }
        public static bool NeedDeleteBackground(this FObject fobject)
        {
            bool value = fobject.IsParent ||
                fobject.FTag == FTag.Button ||
                fobject.FTag == FTag.InputField ||
                fobject.FTag == FTag.Frame ||
                fobject.FTag == FTag.HorizontalLayoutGroup ||
                fobject.FTag == FTag.VerticalLayoutGroup ||
                fobject.FTag == FTag.GridLayoutGroup;

            return value;
        }
    }
}
#endif