#if UNITY_EDITOR && JSON_NET_EXISTS
using DA_Assets.Extensions;
using DA_Assets.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DA_Assets
{
    class FigmaParser
    {
        public static List<FObject> GetChildrenOfPage(FObject page)
        {
            List<FObject> result = new List<FObject>();
            AddChildren(page, result);
            Console.WriteLine(string.Format(Localization.PARSED, result.Count(), page.Name));
            return result;
        }
        private static CustomPrefab GetCustomPrefab(FObject _child)
        {
            CustomPrefab customPrefab = FigmaConverterUnity.Instance.customPrefabs.FirstOrDefault(x => x.Tag == _child.CustomTag);

            if (customPrefab != default)
            {
                return customPrefab;
            }

            return default;
        }
        public static bool IsImage(FObject fobject)
        {
            bool solidFill = false;
            bool gradientFill = false;
            bool isImageTag = fobject.FTag == FTag.Image || fobject.FTag == FTag.Background;
            bool isFillImage = false;

            if (fobject.Fills != null)
            {
                solidFill = fobject.Fills.Any(x => x.Type == "SOLID");
                gradientFill = fobject.Fills.Any(x => x.Type == "GRADIENT_LINEAR");
                isFillImage = fobject.Fills.Select(x => x.Type).Contains("IMAGE");
            }

            return isFillImage || solidFill || gradientFill || isImageTag;
        }
        public static bool IsParent(FObject fobject)
        {
            List<bool> values = new List<bool>();
            IsContainer(fobject, values);
            return values.Contains(true);
        }

        private static void IsContainer(FObject fobject, List<bool> values)
        {
            if (fobject.Children == null)
            {
                values.Add(false);
                return;
            }

            foreach (FObject child in fobject.Children)
            {
                if (child.FTag == FTag.Null ||
                    child.FTag == FTag.Image ||
                    child.FTag == FTag.Background)
                {
                    values.Add(true);
                }
                else
                {
                    Debug.Log($"IsContainer | {child.Name} | {child.FTag}");
                }

                IsContainer(child, values);
            }
        }
        private static void AddChildren(FObject fobject, List<FObject> list)
        {
            if (fobject.Children == null)
            {
                return;
            }

            foreach (FObject _child in fobject.Children)
            {
                CustomPrefab customPrefab = default;
                if (_child.FTag != FTag.Page && _child.FTag != FTag.Frame)
                {
                    _child.FTag = _child.GetFigmaType();

                    if (FigmaConverterUnity.Instance.mainSettings.UseCustomPrefabs)
                    {
                        _child.CustomTag = _child.GetCustomTag();
                        customPrefab = GetCustomPrefab(_child);
                    }
                    else
                    {
                        if (fobject.FTag == FTag.Null ||
                            fobject.FTag == FTag.Image ||
                            fobject.FTag == FTag.Background)
                        {
                            continue;
                        }
                    }
                }

                //The page cannot have a parent component
                if (fobject.FTag != FTag.Page)
                {
                    _child.Parent = fobject;
                }

                if (customPrefab != default)
                {
                    _child.GameObj = UnityEngine.Object.Instantiate(customPrefab.Prefab);
                }
                else
                {
                    _child.GameObj = CanvasDrawer.CreateEmptyGameObj();
                }

                if (string.IsNullOrWhiteSpace(_child.CustomTag))
                {
                    _child.GameObj.name = _child.Name.Replace(_child.FTag.GetDescription(), "");
                  
                }
                else
                {
                    _child.GameObj.name = _child.Name.Replace(_child.CustomTag, "");
                }
             

                while (true)
                {
                    if (_child.GameObj.name[0] == ' ')
                    {
                        _child.GameObj.name = _child.GameObj.name.Remove(0, 1);
                    }
                    else if (_child.GameObj.name[0] == FigmaExtensions.GetTagSeparator())
                    {
                        _child.GameObj.name = _child.GameObj.name.Remove(0, 1);
                    }
                    else
                    {
                        break;
                    }
                }

                if (FigmaConverterUnity.Instance.mainSettings.IncludeTagInGameObjectName)
                {
                    string _name = FigmaExtensions.GetTagSeparator() + " " + _child.GameObj.name.Substring(maxLength: Constants.GAMEOBJECT_NAME_MAX_LENGHT);

                    if (string.IsNullOrWhiteSpace(_child.CustomTag))
                    {
                        _child.GameObj.name = _child.FTag.GetDescription() + " " + _name;
                    }
                    else
                    {
                        _child.GameObj.name = _child.CustomTag + " " + _name;
                    }
                }


                if (_child.FTag == FTag.Frame)
                {
                    //If the component is a frame, it will be at the root of the canvas
                    _child.GameObj.transform.transform.SetParent(FigmaConverterUnity.Instance.transform);
                }
                else if (fobject.FTag != FTag.Page)
                {
                    //If the component is not a frame or a page, it will bind to its parent component
                    _child.GameObj.transform.transform.SetParent(fobject.GameObj.transform);
                }

                list.Add(_child);
                AddChildren(_child, list);
            }
        }

        /// <summary>
        /// Checks for mutual (shared) elements in the frames selected for loading. 
        /// <para> Sets the IsMutual property of such elements to true. </para> 
        /// </summary>
        public static List<FObject> GetMutualFObjects(List<FObject> frames, List<FObject> parsed)
        {
            List<string> uniqueFobjects = new List<string>();
            List<string> mutualFobjects = new List<string>();
            foreach (FObject item in frames)
            {
                List<FObject> fobjectsByFrame = parsed
                    .Where(x => x.RootFrameName == item.Name)
                    .GroupBy(x => x.Name)
                    .Select(x => x.First())
                    .ToList();

                foreach (FObject fobject in fobjectsByFrame)
                {
                    if (uniqueFobjects.Contains(fobject.Name))
                    {
                        mutualFobjects.Add(fobject.Name);
                    }
                    else
                    {
                        uniqueFobjects.Add(fobject.Name);
                    }
                }
            }

            List<FObject> _parsed = new List<FObject>();

            foreach (FObject fobject in parsed)
            {
                FObject _fobject = fobject;
                _fobject.IsMutual = mutualFobjects.Contains(fobject.Name);
                _parsed.Add(_fobject);
            }

            return _parsed;
        }

        /// <summary> Detecting which frame the object belongs to. </summary>
        public static List<FObject> GetSetRootFrameForFObjects(List<FObject> fobjects)
        {
            List<FObject> parsedWithRootFrame = new List<FObject>();
            foreach (FObject fobject in fobjects)
            {
                FObject _fobject = fobject;
                for (int i = 0; i < fobject.Level; i++)
                {
                    _fobject = _fobject.Parent;
                }

                FObject __fobject = fobject;
                __fobject.RootFrameName = _fobject.Name;
                parsedWithRootFrame.Add(__fobject);
            }
            return parsedWithRootFrame;
        }
    }
}
#endif