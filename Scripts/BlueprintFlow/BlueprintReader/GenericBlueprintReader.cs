namespace BlueprintFlow.BlueprintReader
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TheOne.Data.Serialization;
    using TheOne.Data.Storage;
    using TheOne.Extensions;
    using UnityEngine.Scripting;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class BlueprintReaderAttribute : Attribute
    {
        public string Key     { get; }
        public bool   IsLocal { get; }

        public BlueprintReaderAttribute(string key, bool isLocal = false)
        {
            this.Key     = key;
            this.IsLocal = isLocal;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CsvHeaderKeyAttribute : Attribute
    {
        public CsvHeaderKeyAttribute(string key)
        {
        }
    }

    public interface IGenericBlueprintReader : IReadableData
    {
    }

    public abstract class GenericBlueprintReaderByCol : ICsvData, IGenericBlueprintReader
    {
        Type ICsvData.RowType => this.GetType();

        void ICsvData.Add(object key, object value)
        {
            value.CopyTo(this);
            this.HasValue = true;
        }

        IEnumerator ICsvData.GetValues()
        {
            yield return this;
        }

        [field: CsvIgnore] public bool HasValue { get; private set; }
    }

    public abstract class GenericBlueprintReaderByRow<T> : BlueprintByRow<T>, IGenericBlueprintReader
    {
    }

    public abstract class GenericBlueprintReaderByRow<TKey, TValue> : BlueprintByRow<TKey, TValue>, IGenericBlueprintReader
    {
        public new TValue this[TKey key] => base[key];

        public TValue GetDataById(TKey key)
        {
            return this.TryGetValue(key, out var value) ? value : throw new Exception($"{this.GetType().Name} does not contain key {key}");
        }
    }

    [Preserve]
    public class BlueprintByRow<T> : ICsvData, IReadOnlyList<T>
    {
        Type ICsvData.RowType => typeof(T);

        void ICsvData.Add(object key, object value) => this.list.Add((T)value);

        IEnumerator ICsvData.GetValues() => this.list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.list.GetEnumerator();

        private readonly List<T> list = new List<T>();

        public int Count => this.list.Count;

        public T this[int index] => this.list[index];
    }

    [Preserve]
    public class BlueprintByRow<TKey, TValue> : ICsvData, IReadOnlyDictionary<TKey, TValue>
    {
        Type ICsvData.RowType => typeof(TValue);

        void ICsvData.Add(object key, object value) => this.dictionary.Add((TKey)key, (TValue)value);

        IEnumerator ICsvData.GetValues() => this.dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.dictionary.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => this.dictionary.GetEnumerator();

        private readonly Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        public int Count => this.dictionary.Count;

        public TValue this[TKey key] => this.dictionary[key];

        public IEnumerable<TKey> Keys => this.dictionary.Keys;

        public IEnumerable<TValue> Values => this.dictionary.Values;

        public bool ContainsKey(TKey key) => this.dictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => this.dictionary.TryGetValue(key, out value);
    }
}