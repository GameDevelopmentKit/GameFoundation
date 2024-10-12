using System;
using UnityEngine;
using UnityEngine.Events;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input
{
    public class TableViewFloatingDropdown : UnityEngine.UI.Dropdown, ITableViewFloatingDropdown
    {
        public event Action                        Closed;
        public int                                 OptionsCount   => this.options.Count;
        UnityEvent<int> ITableViewFloatingDropdown.onValueChanged => base.onValueChanged;

        public new DropdownEvent onValueChanged { get => throw new InvalidOperationException("FloatingDropdown.onValueChanged: Not available for this class"); set => throw new InvalidOperationException("FloatingDropdown.onValueChanged: Not available for this class"); }

        public new void Show()
        {
            throw new InvalidOperationException("FloatingDropdown.Show() Not available for this class ");
        }

        void ITableViewFloatingDropdown.Show()
        {
            base.Show();
        }

        protected override void DestroyDropdownList(GameObject dropdownList)
        {
            base.DestroyDropdownList(dropdownList);

            if (this.Closed != null) this.Closed();
        }
    }
}