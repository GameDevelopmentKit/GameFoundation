#if UNITY_EDITOR && JSON_NET_EXISTS

using DA_Assets.Exceptions;
using DA_Assets.Model;
using UnityEngine;

namespace DA_Assets.Extensions
{
    public static class Adaptivity
    {
        private static void SetAnchor(this RectTransform source, AnchorType allign, int offsetX = 0, int offsetY = 0)
        {
            source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            AnchorMinMax minMax = GetAnchor(allign);
            source.anchorMin = minMax.Min;
            source.anchorMax = minMax.Max;
        }
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        private static AnchorMinMax GetAnchor(AnchorType anchorPreset)
        {
            AnchorMinMax minMax = new AnchorMinMax();
            switch (anchorPreset)
            {
                case AnchorType.TopLeft:
                    {
                        minMax.Min = new Vector2(0, 1);
                        minMax.Max = new Vector2(0, 1);
                        break;
                    }
                case AnchorType.TopCenter:
                    {
                        minMax.Min = new Vector2(0.5f, 1);
                        minMax.Max = new Vector2(0.5f, 1);
                        break;
                    }
                case AnchorType.TopRight:
                    {
                        minMax.Min = new Vector2(1, 1);
                        minMax.Max = new Vector2(1, 1);
                        break;
                    }

                case AnchorType.MiddleLeft:
                    {
                        minMax.Min = new Vector2(0, 0.5f);
                        minMax.Max = new Vector2(0, 0.5f);
                        break;
                    }
                case AnchorType.MiddleCenter:
                    {
                        minMax.Min = new Vector2(0.5f, 0.5f);
                        minMax.Max = new Vector2(0.5f, 0.5f);
                        break;
                    }
                case AnchorType.MiddleRight:
                    {
                        minMax.Min = new Vector2(1, 0.5f);
                        minMax.Max = new Vector2(1, 0.5f);
                        break;
                    }

                case AnchorType.BottomLeft:
                    {
                        minMax.Min = new Vector2(0, 0);
                        minMax.Max = new Vector2(0, 0);
                        break;
                    }
                case AnchorType.BottomCenter:
                    {
                        minMax.Min = new Vector2(0.5f, 0);
                        minMax.Max = new Vector2(0.5f, 0);
                        break;
                    }
                case AnchorType.BottomRight:
                    {
                        minMax.Min = new Vector2(1, 0);
                        minMax.Max = new Vector2(1, 0);
                        break;
                    }

                case AnchorType.HorStretchTop:
                    {
                        minMax.Min = new Vector2(0, 1);
                        minMax.Max = new Vector2(1, 1);
                        break;
                    }
                case AnchorType.HorStretchMiddle:
                    {
                        minMax.Min = new Vector2(0, 0.5f);
                        minMax.Max = new Vector2(1, 0.5f);
                        break;
                    }
                case AnchorType.HorStretchBottom:
                    {
                        minMax.Min = new Vector2(0, 0);
                        minMax.Max = new Vector2(1, 0);
                        break;
                    }

                case AnchorType.VertStretchLeft:
                    {
                        minMax.Min = new Vector2(0, 0);
                        minMax.Max = new Vector2(0, 1);
                        break;
                    }
                case AnchorType.VertStretchCenter:
                    {
                        minMax.Min = new Vector2(0.5f, 0);
                        minMax.Max = new Vector2(0.5f, 1);
                        break;
                    }
                case AnchorType.VertStretchRight:
                    {
                        minMax.Min = new Vector2(1, 0);
                        minMax.Max = new Vector2(1, 1);
                        break;
                    }

                case AnchorType.StretchAll:
                    {
                        minMax.Min = new Vector2(0, 0);
                        minMax.Max = new Vector2(1, 1);
                        break;
                    }
            }

            return minMax;
        }
        private static void SetPivot(this RectTransform source, PivotType preset)
        {
            source.pivot = GetPivot(preset);
        }

        private static Vector2 GetPivot(PivotType preset)
        {
            switch (preset)
            {
                case PivotType.TopLeft:
                    {
                        return new Vector2(0, 1);
                    }
                case PivotType.TopCenter:
                    {
                        return new Vector2(0.5f, 1);
                    }
                case PivotType.TopRight:
                    {
                        return new Vector2(1, 1);
                    }
                case PivotType.MiddleLeft:
                    {
                        return new Vector2(0, 0.5f);
                    }
                case PivotType.MiddleCenter:
                    {
                        return new Vector2(0.5f, 0.5f);
                    }
                case PivotType.MiddleRight:
                    {
                        return new Vector2(1, 0.5f);
                    }
                case PivotType.BottomLeft:
                    {
                        return new Vector2(0, 0);
                    }
                case PivotType.BottomCenter:
                    {
                        return new Vector2(0.5f, 0);
                    }
                case PivotType.BottomRight:
                    {
                        return new Vector2(1, 0);
                    }
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }
        private static void SetPivotWithPositionSaving(this RectTransform rt, PivotType anchorPreset)
        {
            Vector2 newPivot = GetPivot(anchorPreset);

            Vector3 op = new Vector3(
                rt.rect.width * newPivot.x - rt.rect.width * rt.pivot.x,
                rt.rect.height * newPivot.y - rt.rect.height * rt.pivot.y,
                0);

            Vector3 pt = rt.TransformPoint(op);
   
            Vector3 newAnch = rt.parent.InverseTransformPoint(pt);

            rt.pivot = newPivot;
            rt.anchoredPosition = newAnch;
        }
        public static void SetFigmaSize(this FObject fobject)
        {
            RectTransform frect = fobject.GameObj.GetComponent<RectTransform>();

            frect.sizeDelta = new Vector2(
                    fobject.AbsoluteBoundingBox.Width,
                    fobject.AbsoluteBoundingBox.Height);

            frect.SetPivot(PivotType.TopLeft);
            frect.SetAnchor(AnchorType.TopLeft);

            if (fobject.RelativeTransform == null)
            {
                throw new PositioningException();
            }

            Vector2 figmaPos = new Vector2(fobject.RelativeTransform[0][2], fobject.RelativeTransform[1][2]);
            frect.localPosition = new Vector2(figmaPos.x, -figmaPos.y);
            frect.SetPivotWithPositionSaving(PivotType.TopLeft);

            AnchorType fanchor = GetFigmaAnchor(fobject);
            frect.SetSmartAnchorPreset(fanchor);
            frect.SetEnhancedPivotByPreset(FigmaConverterUnity.Instance.mainSettings.PivotType);
        }
        public static void SetSmartAnchorPreset(this RectTransform rect, AnchorType anchorType)
        {
            AnchorMinMax anchor = GetAnchor(anchorType);

            rect.SetAnchorSmart(RectAxis.Hor, anchor.Min.x, false);
            rect.SetAnchorSmart(RectAxis.Hor, anchor.Max.x, true);

            rect.SetAnchorSmart(RectAxis.Vert, anchor.Min.y, false);
            rect.SetAnchorSmart(RectAxis.Vert, anchor.Max.y, true);
        }
        private static void SetAnchorSmart(this RectTransform rect, RectAxis rectAxis, float value, bool isMax)
        {
            bool smart = true;
            int _axis = (int)rectAxis;
            RectTransform parent = null;
            if (rect.transform.parent == null)
            {
                smart = false;
            }
            else
            {
                parent = rect.transform.parent.GetComponent<RectTransform>();
                if (parent == null)
                    smart = false;
            }

            float offsetSizePixels = 0;
            float offsetPositionPixels = 0;
            if (smart)
            {
                float oldValue = isMax ? rect.anchorMax[_axis] : rect.anchorMin[_axis];

                offsetSizePixels = (value - oldValue) * parent.rect.size[_axis];

                float roundingDelta = 0;

                Canvas canvas = rect.gameObject.GetComponentInParent<Canvas>();
                bool shouldDoIntSnapping = canvas != null && canvas.renderMode != RenderMode.WorldSpace;

                if (shouldDoIntSnapping)
                    roundingDelta = Mathf.Round(offsetSizePixels) - offsetSizePixels;
                offsetSizePixels += roundingDelta;

                offsetPositionPixels = (isMax ? offsetSizePixels * rect.pivot[_axis] : (offsetSizePixels * (1 - rect.pivot[_axis])));
            }

            if (isMax)
            {
                Vector2 rectAnchorMax = rect.anchorMax;
                rectAnchorMax[_axis] = value;
                rect.anchorMax = rectAnchorMax;

                Vector2 other = rect.anchorMin;

                rect.anchorMin = other;
            }
            else
            {
                Vector2 rectAnchorMin = rect.anchorMin;
                rectAnchorMin[_axis] = value;
                rect.anchorMin = rectAnchorMin;

                Vector2 other = rect.anchorMax;
                rect.anchorMax = other;
            }

            if (smart)
            {
                Vector2 rectPosition = rect.anchoredPosition;
                rectPosition[_axis] -= offsetPositionPixels;
                rect.anchoredPosition = rectPosition;

                Vector2 rectSizeDelta = rect.sizeDelta;
                rectSizeDelta[_axis] += offsetSizePixels * (isMax ? -1 : 1);
                rect.sizeDelta = rectSizeDelta;
            }
        }
        private static void SetEnhancedPivotByPreset(this RectTransform rect, PivotType pivotType)//float value, RectAxis axis
        {
            Vector2 rectPivot = GetPivot(pivotType);
            Vector3 cornerBefore = (Vector3)rect.rect.min + rect.transform.localPosition;
            rect.pivot = rectPivot;

            Vector3 cornerAfter = (Vector3)rect.rect.min + rect.transform.localPosition;
            Vector3 cornerOffset = cornerAfter - cornerBefore;
            rect.anchoredPosition -= (Vector2)cornerOffset;

            Vector3 pos = rect.transform.position;
            pos.z -= cornerOffset.z;
            rect.transform.position = pos;
        }
        private static AnchorType GetFigmaAnchor(this FObject fobject)
        {
            string anchor = fobject.Constraints.Vertical + " " + fobject.Constraints.Horizontal;

            AnchorType anchorPreset;

            switch (anchor)
            {
                ///////////////////////////////////
                ///////////////////////////////LEFT
                ///////////////////////////////////
                case "TOP LEFT":
                    anchorPreset = AnchorType.TopLeft;
                    break;
                case "BOTTOM LEFT":
                    anchorPreset = AnchorType.BottomLeft;
                    break;
                case "TOP_BOTTOM LEFT":
                    anchorPreset = AnchorType.VertStretchLeft;
                    break;
                case "CENTER LEFT":
                    anchorPreset = AnchorType.MiddleLeft;
                    break;
                case "SCALE LEFT":
                    anchorPreset = AnchorType.VertStretchLeft;
                    break;
                ///////////////////////////////////
                //////////////////////////////RIGHT
                ///////////////////////////////////
                case "TOP RIGHT":
                    anchorPreset = AnchorType.TopRight;
                    break;
                case "BOTTOM RIGHT":
                    anchorPreset = AnchorType.BottomRight;
                    break;
                case "TOP_BOTTOM RIGHT":
                    anchorPreset = AnchorType.VertStretchRight;
                    break;
                case "CENTER RIGHT":
                    anchorPreset = AnchorType.MiddleRight;
                    break;
                case "SCALE RIGHT":
                    anchorPreset = AnchorType.VertStretchRight;
                    break;
                ///////////////////////////////////
                /////////////////////////LEFT_RIGHT
                ///////////////////////////////////
                case "TOP LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchTop;
                    break;
                case "BOTTOM LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchBottom;
                    break;
                case "TOP_BOTTOM LEFT_RIGHT":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                case "CENTER LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchMiddle;
                    break;
                case "SCALE LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchMiddle;
                    break;
                ///////////////////////////////////
                /////////////////////////////CENTER
                ///////////////////////////////////
                case "TOP CENTER":
                    anchorPreset = AnchorType.TopCenter;
                    break;
                case "BOTTOM CENTER":
                    anchorPreset = AnchorType.BottomCenter;
                    break;
                case "TOP_BOTTOM CENTER":
                    anchorPreset = AnchorType.VertStretchCenter;
                    break;
                case "CENTER CENTER":
                    anchorPreset = AnchorType.MiddleCenter;
                    break;
                case "SCALE CENTER":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                ///////////////////////////////////
                //////////////////////////////SCALE
                ///////////////////////////////////
                case "TOP SCALE":
                    anchorPreset = AnchorType.HorStretchTop;
                    break;
                case "BOTTOM SCALE":
                    anchorPreset = AnchorType.HorStretchBottom;
                    break;
                case "TOP_BOTTOM SCALE":
                    anchorPreset = AnchorType.VertStretchCenter;
                    break;
                case "CENTER SCALE":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                case "SCALE SCALE":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                ///////////////////////////////////
                //////////////////////////////DEFAULT
                ///////////////////////////////////
                default:
                    anchorPreset = AnchorType.TopLeft;
                    break;
            }

            return anchorPreset;
        }
    }
}
#endif