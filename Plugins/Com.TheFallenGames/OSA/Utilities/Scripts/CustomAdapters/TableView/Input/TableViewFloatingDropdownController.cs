using System;
using System.Collections.Generic;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input
{
    public class TableViewFloatingDropdownController : MonoBehaviour
    {
        private ITableViewFloatingDropdown _Dropdown;
        private Action<int>                _CurrentCallback;

        private int _ValueToReturn = -1;

        //Vector3 _PosToReset;
        private List<object> _Values = new();

        // Excluding the Close option
        private List<string> _ValueNames = new();

        protected void Awake()
        {
            (this.transform as RectTransform).pivot =  new(0f, 1f);
            this._Dropdown                          =  this.GetComponent(typeof(ITableViewFloatingDropdown)) as ITableViewFloatingDropdown;
            this._Dropdown.Closed                   += this.OnDropdownClosed;
        }

        public void InitWithEnum(Type enumType, bool sortByEnumNameInsteadOfValue = false, Action<List<object>> valuesFilter = null)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                enumType = null;
                this.ClearOptionsAndTypes();
                return;
            }

            var values = Enum.GetValues(enumType);
            var list   = new List<object>();
            var names  = new List<string>();
            for (var i = 0; i < values.Length; i++)
            {
                var value = (int)values.GetValue(i);
                list.Add(value);
                names.Add(Enum.GetName(enumType, value));
            }

            if (valuesFilter != null) valuesFilter(list);

            if (sortByEnumNameInsteadOfValue)
                list.Sort((a, b) => Enum.GetName(enumType, a).CompareTo(Enum.GetName(enumType, b)));
            else
                list.Sort();

            this.InitWithValues(list, names);
        }

        public void ShowFloating(RectTransform atParent, Action<object> onValueSelected, object invalidValue)
        {
            Action<int> onSelected = i =>
            {
                if (onValueSelected == null || this._Values == null || this._Values.Count <= i) return;

                var val = i == -1 ? invalidValue : this._Values[i];
                onValueSelected(val);
            };

            this.ShowFloating(atParent, onSelected);
        }

        public void ClearOptionsAndTypes()
        {
            this._Values.Clear();
            this._ValueNames.Clear();
            this._Dropdown.ClearOptions();
        }

        public void Hide()
        {
            this._Dropdown.Hide();
        }

        private void InitWithValues(IList<object> values, IList<string> names)
        {
            this.ClearOptionsAndTypes();
            this._Values.AddRange(values);
            this._ValueNames.AddRange(names);
        }

        private void ShowFloating(RectTransform atParent, Action<int> onSelected)
        {
            this._CurrentCallback = onSelected;
            this._ValueToReturn   = -1;

            this._Dropdown.onValueChanged.RemoveListener(this.OnValueChanged);

            this.gameObject.SetActive(true);
            this.transform.position = atParent.position;

            this._Dropdown.ClearOptions();
            var options = new List<string>(this._ValueNames); // modifying a copy

            options.Add("<Close>");
            this._Dropdown.AddOptions(options);
            this._Dropdown.value = options.Count - 1; // selecting an invalid value by default
            //RefreshShownValue();
            this._Dropdown.Show();

            this._Dropdown.onValueChanged.AddListener(this.OnValueChanged);

            //_PosToReset = transform.localPosition;
            var asRT = this.transform as RectTransform;
            asRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, atParent.rect.width);
            asRT.TryClampPositionToParentBoundary();
        }

        private void OnValueChanged(int value)
        {
            // The invalid value is returned as -1
            this._ValueToReturn = value == this._Dropdown.OptionsCount - 1 ? -1 : value;

            this._Dropdown.onValueChanged.RemoveListener(this.OnValueChanged);
            this._Dropdown.Hide();
        }

        private void OnDropdownClosed()
        {
            this.gameObject.SetActive(false);
            if (this._CurrentCallback != null)
            {
                var callback = this._CurrentCallback;
                this._CurrentCallback = null;
                callback(this._ValueToReturn);
            }
        }
    }
}