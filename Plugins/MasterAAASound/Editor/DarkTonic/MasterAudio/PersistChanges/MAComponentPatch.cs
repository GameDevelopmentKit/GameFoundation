using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public class MAComponentPatch
    {
        private const string IgnoredPropertyNames = ";useConeFriction;sleepVelocity;sleepAngularVelocity;WillCleanUpDelegatesAfterStop;roomRolloffFactor;";

        private Dictionary<string, object> _values;

        public MAComponentPatch(Component component)
        {
            ComponentObject = component;
        }
        private bool _isComponentObjectNull = true;
        private Component _componentObject;
        private Component ComponentObject {
            get {
                return _componentObject;
            }
            set {
                _componentObject = value;
                _isComponentObjectNull = _componentObject == null;
            }
        }

        public string ComponentName {
            get {
                var parts = ComponentObject.GetType().ToString().Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                return parts[parts.Length - 1];
            }
        }

        public void StoreSettings()
        {
            if (ComponentObject == null)
            {
                return;
            }

            _values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var properties = GetProperties();
            var fields = GetFields();

            foreach (var property in properties)
            {
                if (IgnoredPropertyNames.Contains(";" + property.Name + ";"))
                {
                    continue;
                }
                if (_values.ContainsKey(property.Name))
                {
                    continue;
                }
                _values.Add(property.Name, property.GetValue(ComponentObject, null));
            }
            foreach (var field in fields)
            {
                if (_values.ContainsKey(field.Name))
                {
                    continue;
                }
                _values.Add(field.Name, field.GetValue(ComponentObject));
            }
        }

        private IEnumerable<FieldInfo> GetFields()
        {
            var fields = new List<FieldInfo>();

            foreach (var fieldInfo in ComponentObject.GetType().GetFields())
            {
                if (!fieldInfo.IsPublic)
                {
                    continue;
                }

                if (!Attribute.IsDefined(fieldInfo, typeof(HideInInspector)))
                {
                    fields.Add(fieldInfo);
                }
            }

            return fields;
        }

        private IEnumerable<PropertyInfo> GetProperties()
        {
            var properties = new List<PropertyInfo>();

            foreach (var propertyInfo in ComponentObject.GetType().GetProperties())
            {
                if (Attribute.IsDefined(propertyInfo, typeof(HideInInspector)))
                {
                    continue;
                }

                var setMethod = propertyInfo.GetSetMethod();
                if (null != setMethod && setMethod.IsPublic)
                {
                    properties.Add(propertyInfo);
                }
            }

            return properties;
        }

        //return component is changes have been made
        public Component RestoreSettings()
        {
            Component resultChangedComponent = null;

            if (!_isComponentObjectNull)
            {
                ComponentObject = EditorUtility.InstanceIDToObject(ComponentObject.GetInstanceID()) as Component;
            }
            else
            {
                ComponentObject = null;
            }

            if (ComponentObject != null && _values != null)
            {
                foreach (var name in _values.Keys)
                {
                    var newValue = _values[name];

                    var property = ComponentObject.GetType().GetProperty(name);

                    if (null != property)
                    {
                        var currentValue = property.GetValue(ComponentObject, null);

                        if (!HasValueChanged(newValue, currentValue))
                        {
                            continue;
                        }

                        property.SetValue(ComponentObject, newValue, null);
                        resultChangedComponent = ComponentObject;
                    }
                    else
                    {
                        var field = ComponentObject.GetType().GetField(name);
                        var currentValue = field.GetValue(ComponentObject);

                        if (!HasValueChanged(newValue, currentValue))
                        {
                            continue;
                        }

                        if (ComponentObject.ToString().Contains("DarkTonic.MasterAudio.MasterAudio"))
                        {
                            switch (field.Name)
                            {
                                case "customEvents":
                                    FilterOutDGSCCustomEvents(ref newValue);
                                    break;
                                case "customEventCategories":
                                    FilterOutDGSCCustomEventCategories(ref newValue);
                                    break;
                                case "musicPlaylists":
                                    FilterOutDGSCPlaylists(ref newValue);
                                    break;
                                case "groupBuses":
                                    FilterOutDGSCBuses(ref newValue);
                                    break;
                                case "musicDuckingSounds":
                                    FilterOutDGSCDuckGroups(ref newValue);
                                    break;
                            }
                        }
                        field.SetValue(ComponentObject, newValue);
                        resultChangedComponent = ComponentObject;
                    }
                }
            }

            _values = null;
            return resultChangedComponent;
        }

        private static void FilterOutDGSCDuckGroups(ref object duckGroups)
        {
            var ducks = duckGroups as List<DuckGroupInfo>;
            if (ducks == null)
            {
                return;
            }

            var itemsToDelete = new List<DuckGroupInfo>(ducks.Count);

            for (var i = 0; i < ducks.Count; i++)
            {
                var aDuck = ducks[i];
                if (aDuck.isTemporary)
                {
                    itemsToDelete.Add(aDuck);
                }
            }

            for (var i = 0; i < itemsToDelete.Count; i++)
            {
                ducks.Remove(itemsToDelete[i]);
            }

            duckGroups = ducks;
        }

        private static void FilterOutDGSCBuses(ref object busList)
        {
            var buses = busList as List<GroupBus>;
            if (buses == null)
            {
                return;
            }

            var itemsToDelete = new List<GroupBus>(buses.Count);

            for (var i = 0; i < buses.Count; i++)
            {
                var aBus = buses[i];
                if (aBus.isTemporary)
                {
                    itemsToDelete.Add(aBus);
                }
            }

            for (var i = 0; i < itemsToDelete.Count; i++)
            {
                buses.Remove(itemsToDelete[i]);
            }

            busList = buses;
        }

        private static void FilterOutDGSCPlaylists(ref object pList)
        {
            var playlists = pList as List<MasterAudio.Playlist>;
            if (playlists == null)
            {
                return;
            }

            var itemsToDelete = new List<MasterAudio.Playlist>(playlists.Count);

            for (var i = 0; i < playlists.Count; i++)
            {
                var aPlaylist = playlists[i];
                if (aPlaylist.isTemporary)
                {
                    itemsToDelete.Add(aPlaylist);
                }
            }

            for (var i = 0; i < itemsToDelete.Count; i++)
            {
                playlists.Remove(itemsToDelete[i]);
            }

            pList = playlists;
        }

        private static void FilterOutDGSCCustomEventCategories(ref object cats)
        {
            var custEventCats = cats as List<CustomEventCategory>;
            if (custEventCats == null)
            {
                return;
            }

            var itemsToDelete = new List<CustomEventCategory>(custEventCats.Count);

            for (var i = 0; i < custEventCats.Count; i++)
            {
                var aCat = custEventCats[i];
                if (aCat.IsTemporary)
                {
                    itemsToDelete.Add(aCat);
                }
            }

            for (var i = 0; i < itemsToDelete.Count; i++)
            {
                custEventCats.Remove(itemsToDelete[i]);
            }

            cats = custEventCats;
        }

        private static void FilterOutDGSCCustomEvents(ref object events)
        {
            var custEvents = events as List<CustomEvent>;
            if (custEvents == null)
            {
                return;
            }

            var itemsToDelete = new List<CustomEvent>(custEvents.Count);

            for (var i = 0; i < custEvents.Count; i++)
            {
                var anEvent = custEvents[i];
                if (anEvent.isTemporary)
                {
                    itemsToDelete.Add(anEvent);
                }
            }

            for (var i = 0; i < itemsToDelete.Count; i++)
            {
                custEvents.Remove(itemsToDelete[i]);
            }

            events = custEvents;
        }

        private static bool HasValueChanged(object newValue, object oldValue)
        {
            var valuesChanged = true;

            if (null != newValue && null != oldValue)
            {
                var valueToCompare = newValue as IComparable;

                if (null == valueToCompare)
                {
                    try
                    {
                        var serializer = new XmlSerializer(newValue.GetType());

                        using (var streamNew = new MemoryStream())
                        {
                            serializer.Serialize(streamNew, newValue);

                            var encoding = new UTF8Encoding();

                            var oldValueSerialized = encoding.GetString(streamNew.ToArray());

                            using (var streamOld = new MemoryStream())
                            {
                                serializer.Serialize(streamOld, oldValue);

                                var newValueSerialized = encoding.GetString(streamOld.ToArray());

                                valuesChanged = !string.Equals(newValueSerialized, oldValueSerialized);
                            }
                        }
                    }
                    catch
                    {
                        valuesChanged = true;
                    }
                }
                else
                {
                    valuesChanged = valueToCompare.CompareTo(oldValue) != 0;
                }
            }
            else if (null == oldValue && null == newValue)
            {
                valuesChanged = false;
            }

            return valuesChanged;
        }
    }
}