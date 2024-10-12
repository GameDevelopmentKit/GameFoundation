using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    public class SelectionWatcher
    {
        public delegate void NewObjectSelectedDelegate(GameObject lastGO, GameObject newGO);

        public event NewObjectSelectedDelegate NewObjectSelected;

        public  bool       Enabled            { get; set; }
        private GameObject LastSelectedObject { get; set; }

        public void OnUpdate()
        {
            this.CheckNewObjectSelection();
        }

        private bool CheckNewObjectSelection()
        {
            var last = this.LastSelectedObject;
            if (!this.Enabled)
            {
                this.LastSelectedObject = null;
                return last != this.LastSelectedObject;
            }

            var curSelected = this.GetCurrentlySelectedObject();
            if (!curSelected)
            {
                this.LastSelectedObject = null;
                return last != this.LastSelectedObject;
            }

            if (this.LastSelectedObject != curSelected)
            {
                if (curSelected) this.OnNewObjectSelected(curSelected);

                this.LastSelectedObject = curSelected;
                return true;
            }

            return false;
        }

        private GameObject GetCurrentlySelectedObject()
        {
            if (!EventSystem.current) return null;

            return EventSystem.current.currentSelectedGameObject;
        }

        private void OnNewObjectSelected(GameObject curSelected)
        {
            this.NewObjectSelected(this.LastSelectedObject, curSelected);
        }
    }
}