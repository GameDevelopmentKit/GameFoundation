#if UNITY_EDITOR
using DA_Assets.Exceptions;
using DA_Assets.Extensions;
using DA_Assets.Model;
#if JSON_NET_EXISTS
using DA_Assets.Plugins;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
#if TMPRO_EXISTS
using TMPro;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DA_Assets
{
    public static class CanvasDrawer
    {
#if JSON_NET_EXISTS
        private static List<Transform> buttons;
        private static List<InputField> inputFields;

        public static void DrawToCanvas(List<FObject> fobjects)
        {
            buttons = new List<Transform>();
            inputFields = new List<InputField>();

            int objCount = 0;
            foreach (FObject fobject in fobjects)
            {
                switch (fobject.FTag)
                {
                    case FTag.Frame:
                        InstantiateFrame(fobject);
                        break;
                    case FTag.Button:
                        InstantiateButton(fobject);
                        break;
                    case FTag.InputField:
                        InstantiateInputField(fobject);
                        break;
                    case FTag.Text:
                        InstantiateText(fobject);
                        break;
                    case FTag.Placeholder:
                        InstantiateText(fobject);
                        break;
                    case FTag.HorizontalLayoutGroup:
                        InstantiateHorizontalOrVerticalLayoutGroup(fobject, FTag.HorizontalLayoutGroup);
                        break;
                    case FTag.VerticalLayoutGroup:
                        InstantiateHorizontalOrVerticalLayoutGroup(fobject, FTag.VerticalLayoutGroup);
                        break;
                    case FTag.GridLayoutGroup:
                        InstantiateGridLayoutGroup(fobject);
                        break;
                    default:
                        CustomPrefab customPrefab = FigmaConverterUnity.Instance.customPrefabs.FirstOrDefault(x => x.Tag == fobject.CustomTag);

                        if (customPrefab != default)
                        {
                            InstantiateCustomPrefab(fobject, customPrefab.Prefab);
                        }
                        else
                        {
                            InstantiateImage(fobject);
                        }
                        break;
                }

                objCount++;
            }

            SetTargetGraphicForButtons();
            SetTargetGraphicForInputFields();
#if I2LOC_EXISTS
            if (FigmaConverterUnity.Instance.mainSettings.UseI2Localization)
            {
                InstantiateI2LocalizationSource();
            }
#endif
            buttons.Clear();
            inputFields.Clear();

            Console.WriteLine(string.Format(Localization.DRAWED, objCount)); 
        }
        private static void InstantiateCustomPrefab(FObject fobject, GameObject prefab)
        {
            fobject.SetFigmaSize();
        }

     
        private static void InstantiateHorizontalOrVerticalLayoutGroup(FObject horVertGroup, FTag layoutGroup)
        {
            GameObject gameObject = InstantiateImage(horVertGroup);
            HorizontalOrVerticalLayoutGroup _horVertGroup;

            if (layoutGroup == FTag.HorizontalLayoutGroup)
            {
                _horVertGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            else
            {
                _horVertGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            }

            _horVertGroup.spacing = horVertGroup.ItemSpacing;
            _horVertGroup.padding = new RectOffset
            {
                bottom = (int)Mathf.Round(horVertGroup.PaddingBottom),
                top = (int)Mathf.Round(horVertGroup.PaddingTop),
                left = (int)Mathf.Round(horVertGroup.PaddingLeft),
                right = (int)Mathf.Round(horVertGroup.PaddingRight)
            };

            foreach (FObject fobject in horVertGroup.Children)
            {
                ResizingType resizingType = fobject.GetResizingType();

                if (resizingType.ResizingWidth == Resizing.FillContainer)
                {
                    _horVertGroup.childControlWidth = true;
                }
                if (resizingType.ResizingHeight == Resizing.FillContainer)
                {
                    _horVertGroup.childControlHeight = true;
                }
            }
        }

        private static void InstantiateGridLayoutGroup(FObject gridGroup)
        {
            GameObject gameObject = InstantiateImage(gridGroup);
            GridLayoutGroup _gridGroup = gameObject.AddComponent<GridLayoutGroup>();

            string[] nameParts = gridGroup.Name.Split(FigmaExtensions.GetTagSeparator());
            string[] spacingArray = nameParts[nameParts.Length - 1].Split("x");
            string[] cellSizeArray = nameParts[nameParts.Length - 2].Split("x");

            int spacingX = Convert.ToInt32(spacingArray[0]);
            int spacingY = Convert.ToInt32(spacingArray[1]);

            int cellSizeX = Convert.ToInt32(cellSizeArray[0]);
            int cellSizeY = Convert.ToInt32(cellSizeArray[1]);

            _gridGroup.spacing = new Vector2(spacingX, spacingY);
            _gridGroup.cellSize = new Vector2(cellSizeX, cellSizeY);

            _gridGroup.childAlignment = TextAnchor.MiddleCenter;
        }
        private static ResizingType GetResizingType(this FObject fobject)
        {
            string layoutAlign = fobject.LayoutAlign;
            float layoutGrow = fobject.LayoutGrow;

            string resizingTypeRAW = $"{layoutAlign}|{layoutGrow}";

            ResizingType resizingType = new ResizingType();
            switch (resizingTypeRAW)
            {
                case "STRETCH|1":
                    resizingType.ResizingWidth = Resizing.FillContainer;
                    resizingType.ResizingHeight = Resizing.FillContainer;
                    break;
                case "INHERIT|1":
                    resizingType.ResizingWidth = Resizing.FillContainer;
                    resizingType.ResizingHeight = Resizing.FixedHeight;
                    break;
                case "STRETCH|0":
                    resizingType.ResizingWidth = Resizing.FixedWidth;
                    resizingType.ResizingHeight = Resizing.FillContainer;
                    break;
                case "INHERIT|0":
                    resizingType.ResizingWidth = Resizing.FixedWidth;
                    resizingType.ResizingHeight = Resizing.FixedHeight;
                    break;
                default:
                    resizingType.ResizingWidth = Resizing.FixedWidth;
                    resizingType.ResizingHeight = Resizing.FixedHeight;
                    break;
            }

            return resizingType;
        }
        private static void InstantiateInputField(FObject inputField)
        {
            GameObject gameObject = InstantiateImage(inputField);
            InputField _inputField = gameObject.AddComponent<InputField>();
            inputFields.Add(_inputField);
        }
        private static void SetTargetGraphicForInputFields()
        {
            foreach (InputField inputField in inputFields)
            {
                foreach (Transform child in inputField.GetComponentsInChildren<Transform>())
                {
                    FObject _tempFobject = new FObject
                    {
                        Name = child.name.ToLower()
                    };

                    _tempFobject.FTag = _tempFobject.GetFigmaType();
                    _tempFobject.CustomTag = _tempFobject.GetCustomTag();

                     
                    if (_tempFobject.FTag == FTag.Background)
                    {
                        if (child.TryGetComponent(out Image image))
                        {
                            inputField.targetGraphic = image;
                        }
                    }
                    else if (_tempFobject.FTag == FTag.Text)
                    {
                        if (child.TryGetComponent(out Text text))
                        {
                            text.supportRichText = false;
                            inputField.textComponent = text;
                        }
                    }
                    else if (_tempFobject.FTag == FTag.Placeholder)
                    {
                        if (child.TryGetComponent(out Text _text))
                        {
                            inputField.placeholder = _text;
                        }
                    }
                }
            }
        }
        private static void InstantiateText(FObject fobject)
        {
            if (FigmaConverterUnity.Instance.mainSettings.TextComponent == TextComponent.Standard)
            {
                InstantiateDefaultText(fobject);
            }
#if TMPRO_EXISTS
            else if (FigmaConverterUnity.Instance.mainSettings.TextComponent == TextComponent.TextMeshPro)
            {
                InstantiateTextMeshPro(fobject);
            }
#endif
        }
        private static void InstantiateDefaultText(FObject fobject)
        {
            if (fobject.GameObj == null)
            {
                return;
            }

            Text _text = fobject.GameObj.AddComponent<Text>();
#if I2LOC_EXISTS
            if (FigmaConverterUnity.Instance.mainSettings.UseI2Localization)
            {
                fobject.AddI2Localize();
            }
#endif

            fobject.SetFigmaSize();
            _text.SetDefaultTextStyle(fobject);
        }
#if TMPRO_EXISTS
        private static void InstantiateTextMeshPro(FObject fobject)
        {
            TextMeshProUGUI _textMesh = fobject.GameObj.AddComponent<TextMeshProUGUI>();
#if I2LOC_EXISTS
            if (FigmaConverterUnity.Instance.mainSettings.UseI2Localization)
            {
                fobject.AddI2Localize();
            }
#endif
            fobject.SetFigmaSize();
            _textMesh.SetTextMeshProStyle(fobject);
        }
#endif
        private static GameObject InstantiateFrame(FObject frame)
        {
            Image img = frame.GameObj.AddComponent<Image>();

            frame.SetFigmaSize();

            img.rectTransform.SetSmartAnchorPreset(AnchorType.StretchAll);
            img.rectTransform.offsetMin = new Vector2(0, 0);
            img.rectTransform.offsetMax = new Vector2(0, 0);

            img.DestroyImmediate();
            return frame.GameObj;
        }
        private static GameObject InstantiateImage(FObject fobject)
        {
            if (fobject.GameObj == null)
            {
                Debug.Log("GameObj is null");
                return null;
            }

            switch (FigmaConverterUnity.Instance.mainSettings.ImageComponent)
            {
#if MPUIKIT_EXISTS
                case ImageComponent.MPImage:
                    return MPImagePlugin.CreateMPImage(fobject);
#endif
#if PUI_EXISTS
                case ImageComponent.ProceduralImage:
                    return ProceduralImagePlugin.CreateProceduralUIImage(fobject);
#endif
                default:
                    return CreateImage(fobject);
            }
        }
        private static GameObject CreateImage(FObject fobject)
        {
            Image _img = fobject.GameObj.AddComponent<Image>();

            bool downloadable = ImportConditions.IsDownloadableImage(fobject);

            if (downloadable)
            {
                try
                {
                    fobject.SetImgTypeSprite();

                    Sprite sprite = (Sprite)AssetDatabase.LoadAssetAtPath(fobject.AssetPath, typeof(Sprite));
                    _img.sprite = sprite;
                }
                catch
                {
                    _img.sprite = null;
                    _img.color = Color.white;

                    throw new MissingSpriteException(fobject.Name);
                }
               
            }
            else
            {
                AssetDatabase.DeleteAsset(fobject.AssetPath);
                _img.color = fobject.Fills[0].Color;
            }

            fobject.SetFigmaSize();

            if (fobject.NeedDeleteBackground())
            {
                _img.DestroyImmediate();
                return fobject.GameObj;
            }

            _img.SetTrueShadow(fobject);

            return fobject.GameObj;
        }

        private static void InstantiateButton(FObject button)
        {
            GameObject gameObject = InstantiateImage(button);

            int textCount = button.Children.Where(x => x.FTag == FTag.Text).Count();
            int bgCount = button.Children.Where(x => x.FTag == FTag.Background).Count();

            if (bgCount > 1 && textCount > 1)
            {
                BetterButton _btn = gameObject.AddComponent<BetterButton>();

                foreach (FObject child in button.Children)
                {
                    if (child.FTag == FTag.Text)
                    {
                        if (child.Name.ToLower().Contains(ButtonStates.Default.GetDescription()))
                        {
                            _btn.textDefaultColor = child.GetTextColor();
                        }
                        else if (child.Name.ToLower().Contains(ButtonStates.Hover.GetDescription()))
                        {
                            _btn.textHoverColor = child.GetTextColor();
                            child.GameObj.DestroyImmediate();
                            child.GameObj = null;
                        }
                        else if (child.Name.ToLower().Contains(ButtonStates.Disabled.GetDescription()))
                        {
                            _btn.textDisabledColor = child.GetTextColor();
                            child.GameObj.DestroyImmediate();
                            child.GameObj = null;
                        }
                    }
                    else if (child.FTag == FTag.Background)
                    {
                        bgCount++;
                        SpriteState spriteState = new SpriteState();
                        spriteState = _btn.spriteState;
                        Sprite _sprite = (Sprite)AssetDatabase.LoadAssetAtPath(child.AssetPath, typeof(Sprite));

                        if (child.Name.ToLower().Contains(ButtonStates.Hover.GetDescription()))
                        {
                            spriteState.pressedSprite = _sprite;
                            child.GameObj.DestroyImmediate();
                            child.GameObj = null;
                        }
                        else if (child.Name.ToLower().Contains(ButtonStates.Disabled.GetDescription()))
                        {
                            spriteState.disabledSprite = _sprite;
                            child.GameObj.DestroyImmediate();
                            child.GameObj = null;
                        }


                        _btn.spriteState = spriteState;
                    }
                }

                _btn.transition = Selectable.Transition.SpriteSwap;
            }
            else
            {
                Button _btn = gameObject.AddComponent<Button>();
            }

            buttons.Add(gameObject.transform);
        }
        private static void SetTargetGraphicForButtons()
        {
            foreach (Transform button in buttons)
            {

                int imgCount = button.GetComponentsInChildren<Image>().Count();

                foreach (Transform child in button.GetComponentsInChildren<Transform>())
                {
                    FObject _tempFobject = new FObject
                    {
                        Name = child.name.ToLower()
                    };

                    if (child.TryGetComponent(out Image image))
                    {
                        if (button.TryGetComponent(out Button bt) && _tempFobject.GetFigmaType() == FTag.Background)
                        {
                            bt.targetGraphic = image;
                        }
                        else if (_tempFobject.GetFigmaType() == FTag.Background && _tempFobject.Name.Contains(ButtonStates.Default.GetDescription()))
                        {
                            button.GetComponent<BetterButton>().targetGraphic = image;
                            continue;
                        }
                    }
                    else if (child.TryGetComponent(out Text text))
                    {
                        if (text.name.ToLower().Contains(ButtonStates.Default.GetDescription()))
                        {
                            button.GetComponent<BetterButton>().buttonText = text;
                            continue;
                        }
                    }
                }
            }
        }
#endif
#if I2LOC_EXISTS
        public static void InstantiateI2LocalizationSource()
        {
            I2LocalizationPlugin.InstantiateI2LocalizationSource();
        }
#endif
        public static void InstantiateCanvas(Vector2 refRes)
        {
            GameObject _gameObject = CreateEmptyGameObj();

            Canvas _canvas = _gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _gameObject.name = Constants.CANVAS_GAMEOBJECT_NAME;
            _gameObject.AddComponent<FigmaConverterUnity>();

            CanvasScaler _canvasScaler = _gameObject.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = refRes;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 1f;
            _canvasScaler.referencePixelsPerUnit = 100f;

            _gameObject.AddComponent<GraphicRaycaster>();
        }
        public static void InstantiateEventSystem()
        {
            GameObject _gameObject = CreateEmptyGameObj();
            _gameObject.AddComponent<EventSystem>();
            _gameObject.AddComponent<StandaloneInputModule>();
            _gameObject.name = Constants.EVENT_SYSTEM_GAMEOBJECT_NAME;
        }
        public static GameObject CreateEmptyGameObj()
        {
            GameObject _temp = new GameObject();
            GameObject gameObj = UnityEngine.Object.Instantiate(_temp);
            UnityEngine.Object.DestroyImmediate(_temp);
            return gameObj;
        }
    }
}
#endif