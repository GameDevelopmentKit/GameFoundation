#if UNITY_EDITOR && JSON_NET_EXISTS
using DA_Assets.Extensions;
using DA_Assets.Model;
#if MPUIKIT_EXISTS
using MPUIKIT;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.Plugins
{
    class MPImagePlugin
    {
#if MPUIKIT_EXISTS
        public static GameObject CreateMPImage(FObject fobject)
        {
            MPImage mpImage = fobject.GameObj.AddComponent<MPImage>();

            fobject.SetImgTypeSprite();

            if (ImportConditions.IsDownloadableImage(fobject))
            {
                Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath(fobject.AssetPath, typeof(Sprite));
                mpImage.sprite = sprite;
            }
            else
            {
                AssetDatabase.DeleteAsset(fobject.AssetPath);

                foreach (Fill fill in fobject.Fills)
                {
                    if (fill.Type == "SOLID")
                    {
                        if (fill.Opacity != null)
                        {
                            Color _color = fill.Color;
                            _color.a = (float)fill.Opacity;
                            mpImage.color = _color;
                        }
                        else
                        {
                            mpImage.color = fill.Color;
                        }
                    }
                    else if (fill.Type == "GRADIENT_LINEAR")
                    {
                        Gradient gradient = new Gradient
                        {
                            mode = GradientMode.Blend,
                        };

                        List<GradientColorKey> gradientColorKeys = new List<GradientColorKey>();

                        foreach (GradientStop gradientStop in fill.GradientStops)
                        {
                            gradientColorKeys.Add(new GradientColorKey
                            {
                                color = gradientStop.Color,
                                time = gradientStop.Position
                            });
                        }

                        gradient.colorKeys = gradientColorKeys.ToArray();

                        mpImage.GradientEffect = new GradientEffect
                        {
                            Enabled = true,
                            GradientType = GradientType.Linear,
                            Gradient = gradient,
                            Rotation = GetAngle(fill.GradientHandlePositions[0], fill.GradientHandlePositions[1])
                        };
                    }
                }
            }

            if (fobject.Type == "RECTANGLE" || fobject.Type == "FRAME")
            {
                mpImage.DrawShape = DrawShape.Rectangle;

                float cornerCoof = 1.25f;

                if (fobject.RectangleCornerRadius != null)
                {
                    mpImage.Rectangle = new Rectangle
                    {
                        CornerRadius = new Vector4
                        {
                            x = fobject.RectangleCornerRadius[3] / cornerCoof,
                            y = fobject.RectangleCornerRadius[2] / cornerCoof,
                            z = fobject.RectangleCornerRadius[1] / cornerCoof,
                            w = fobject.RectangleCornerRadius[0] / cornerCoof
                        }
                    };
                }
                else if (fobject.CornerRadius != 0)
                {
                    mpImage.Rectangle = new Rectangle
                    {
                        CornerRadius = new Vector4
                        {
                            x = fobject.CornerRadius / cornerCoof,
                            y = fobject.CornerRadius / cornerCoof,
                            z = fobject.CornerRadius / cornerCoof,
                            w = fobject.CornerRadius / cornerCoof,
                        }
                    };
                }
            }
            else if (fobject.Type == "ELLIPSE")
            {
                mpImage.DrawShape = DrawShape.Circle;
                mpImage.Circle = new Circle
                {
                    FitToRect = true
                };
            }

            fobject.SetFigmaSize();

            if (fobject.NeedDeleteBackground())
            {
                mpImage.DestroyImmediate();
                return fobject.GameObj;
            }

            mpImage.SetTrueShadow(fobject);

            return fobject.GameObj;
        }
#endif

        private static float GetAngle(Vector2 startHandle, Vector2 endHandle)
        {
            float f = (endHandle.y - startHandle.y) / (endHandle.x - startHandle.x) * -1;
            float radians = Mathf.Atan(f);
            return (180 * radians) / Mathf.PI;
        }

    }
}
#endif