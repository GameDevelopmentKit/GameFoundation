namespace GameFoundation.Scripts.Utilities.Extension
{
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class UnitySerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] [HideInInspector] private List<TKey> keyData = new();

        [SerializeField] [HideInInspector] private List<TValue> valueData = new();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            for (var i = 0; i < this.keyData.Count && i < this.valueData.Count; i++) this[this.keyData[i]] = this.valueData[i];
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.keyData.Clear();
            this.valueData.Clear();

            foreach (var item in this)
            {
                this.keyData.Add(item.Key);
                this.valueData.Add(item.Value);
            }
        }
    }
}