using frame8.Logic.Misc.Other;
using frame8.Logic.Misc.Other.Extensions;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.Core
{
    public class OSADebugger : MonoBehaviour
        #if !UNITY_WSA && !UNITY_WSA_10_0
        ,
        IDragHandler
    #endif
    {
        public bool     onlyAcceptedGameObjects;
        public string[] acceptedGameObjectsNames;

        private IOSA       _AdapterImpl;
        public  Text       debugText1, debugText2, debugText3, debugText4;
        public  bool       allowReinitializationWithOtherAdapter;
        private Toggle     _EndStationary;
        private InputField _AmountInputField;

        #if !UNITY_WSA && !UNITY_WSA_10_0 // UNITY_WSA uses .net core, which does not contain reflection code
        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private void Update()
        {
            if (this._AdapterImpl == null) return;

            var vhs                = this.GetFieldValue("_VisibleItems") as IList;
            int indexInViewOfFirst = -1, indexInViewOfLast = -1;
            if (vhs != null && vhs.Count > 0)
            {
                indexInViewOfFirst = (vhs[0] as BaseItemViewsHolder).itemIndexInView;
                indexInViewOfLast  = (vhs[vhs.Count - 1] as BaseItemViewsHolder).itemIndexInView;
            }

            var recyclable               = this.GetFieldValue("_RecyclableItems") as IList;
            var recyclableSiblingIndices = "";
            if (recyclable != null && recyclable.Count > 0)
                for (var i = 0; i < recyclable.Count; i++)
                    recyclableSiblingIndices += (recyclable[i] as BaseItemViewsHolder).root.GetSiblingIndex() + ", ";
            //indexInViewOfFirst = (vhs[0] as BaseItemViewsHolder).itemIndexInView;
            //indexInViewOfLast = (vhs[vhs.Count - 1] as BaseItemViewsHolder).itemIndexInView;
            this.debugText1.text =
                "ctVrtIns: "
                + this.GetInternalStatePropertyValue("ctVirtualInsetFromVPS_Cached")
                + "\n"
                + "indexInViewOfFirst: "
                + indexInViewOfFirst
                + "\n"
                + "indexInViewOfLast: "
                + indexInViewOfLast
                + "\n"
                + "visCount: "
                + this.GetPropertyValue("VisibleItemsCount")
                + "\n"
                + "recyclableSiblingIndices: "
                + recyclableSiblingIndices
                + "\n"
                +
                //"ctRealSz: " + GetInternalStateFieldValue("contentPanelSize") + "\n" +
                "ctVrtSz: "
                + this.GetInternalStateFieldValue("ctVirtualSize")
                + "\n"
                +
                //"rec: " + GetPropertyValue("RecyclableItemsCount") + "\n" +
                "rec: "
                + this._AdapterImpl.RecyclableItemsCount
                + "bufRec: "
                + this._AdapterImpl.BufferedRecyclableItemsCount;
        }

        internal void InitWithAdapter(IOSA adapterImpl)
        {
            if ((this._AdapterImpl != null && !this.allowReinitializationWithOtherAdapter)
                || (this.onlyAcceptedGameObjects
                    && this.acceptedGameObjectsNames != null
                    && Array.IndexOf(this.acceptedGameObjectsNames, adapterImpl.gameObject.name) == -1)
            )
                return;

            this._AdapterImpl = adapterImpl;

            Button b;
            this.transform.GetComponentAtPath("ComputePanel/ComputeNowButton", out b);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.Call("ComputeVisibilityForCurrentPosition", true, false));

            this.transform.GetComponentAtPath("ComputePanel/ComputeNowButton_PlusDelta", out b);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.Call("ComputeVisibilityForCurrentPositionRawParams", true, false, .1f));

            this.transform.GetComponentAtPath("ComputePanel/ComputeNowButton_MinusDelta", out b);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.Call("ComputeVisibilityForCurrentPositionRawParams", true, false, -.1f));

            this.transform.GetComponentAtPath("ComputePanel/CorrectNowButton", out b);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.Call("CorrectPositionsOfVisibleItems", true, true));

            this.transform.GetComponentAtPath("DataManipPanel/EndStationaryToggle", out this._EndStationary);
            this.transform.GetComponentAtPath("DataManipPanel/AmountInputField", out this._AmountInputField);

            this.transform.GetComponentAtPath("DataManipPanel/head", out b);
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.AddOrRemove(true));
            this.transform.GetComponentAtPath("DataManipPanel/tail", out b);
            b.onClick.AddListener(() => this.AddOrRemove(false));

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => this.Call("RemoveItems", adapterImpl.GetItemsCount() - 2, int.Parse(this._AmountInputField.text), this._EndStationary.isOn, false));
        }

        private void AddOrRemove(bool atStart)
        {
            var endIdx = this._AdapterImpl.GetItemsCount() - 1;
            var amount = int.Parse(this._AmountInputField.text);

            if (amount < 0)
                this.Call("RemoveItems", atStart ? 0 : endIdx + amount, -amount, this._EndStationary.isOn, false);
            else
                this.Call("InsertItems", atStart ? 0 : endIdx, amount, this._EndStationary.isOn, false);
        }

        private object GetFieldValue(string field)
        {
            var fi = this.GetBaseType().GetField(field, BINDING_FLAGS);
            return fi.GetValue(this._AdapterImpl);
        }

        private object GetPropertyValue(string prop)
        {
            var pi = this.GetBaseType().GetProperty(prop, BINDING_FLAGS);
            return pi.GetValue(this._AdapterImpl, null);
        }

        private object GetInternalStateFieldValue(string field)
        {
            var internalState         = this.GetFieldValue("_InternalState");
            var internalStateBaseType = this.GetInternalStateBaseType(internalState);

            var fi = internalStateBaseType.GetField(field, BINDING_FLAGS);
            return fi.GetValue(internalState);
        }

        private object GetInternalStatePropertyValue(string prop)
        {
            var internalState         = this.GetFieldValue("_InternalState");
            var internalStateBaseType = this.GetInternalStateBaseType(internalState);

            var fi = internalStateBaseType.GetProperty(prop, BINDING_FLAGS);
            return fi.GetValue(internalState, null);
        }

        private Type GetBaseType()
        {
            var t                                                 = this._AdapterImpl.GetType();
            while (!t.Name.Contains("OSA") || !t.IsGenericType) t = t.BaseType;
            //if (t == typeof(object))
            //	return;
            return t;
        }

        private Type GetInternalStateBaseType(object internalState)
        {
            return internalState.GetType();

            //Type t = internalState.GetType();
            //while (!t.Name.ToLowerInvariant().Equals("internalstate"))
            //{
            //	t = t.BaseType;
            //	//if (t == typeof(object))
            //	//	return;
            //}

            //return t;
        }

        private void Call(string methodName, params object[] parameters)
        {
            if (this._AdapterImpl == null) return;

            var t = this.GetBaseType();

            //foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            //	if (m.Name.ToLower().Contains("compute"))
            //	Debug.Log(m);

            var mi = t.GetMethod(
                methodName,
                BINDING_FLAGS,
                null,
                DotNETCoreCompat.ConvertAllToArray(parameters, p => p.GetType()),
                null
            );
            mi.Invoke(this._AdapterImpl, parameters);
        }

        public void OnDrag(PointerEventData eventData)
        {
            this.transform.position += (Vector3)eventData.delta;
        }
        #endif
    }
}