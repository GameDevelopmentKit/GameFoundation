using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.Util.IO.Pools
{
    /// <summary>
    /// First in, First out caching. Use it when it's very unlikely or impossible to scroll back to older items.
    /// </summary>
    /// <remarks>In most cases, this is not actually the best way of doing caching. Consider <see cref="LRUCachingPool"/> instead, as it's more versatile.</remarks>
    public class FIFOCachingPool : IPool
    {
        public delegate void ObjectDestroyer(object key, object value);

        public int Capacity     { get; private set; }
        public int CurrentCount => this._Keys.Count;

        private readonly Queue<object>              _Keys;
        private readonly Dictionary<object, object> _Cache;
        private readonly ObjectDestroyer            _ObjectDestroyer;

        /// <summary>First in, First out caching</summary>
        /// <param name="capacity"></param>
        /// <param name="objectDestroyer">
        /// When an object is kicked out of the cache, this will be used to process its destruction, 
        /// in case special code needs to be executed. This is also called for each value when the cache is cleared usinc <see cref="Clear"/>
        /// </param>
        public FIFOCachingPool(int capacity, ObjectDestroyer objectDestroyer = null)
        {
            this.Capacity         = capacity;
            this._Keys            = new(capacity);
            this._Cache           = new(capacity);
            this._ObjectDestroyer = objectDestroyer;
        }

        public object Get(object key)
        {
            object value;
            if (this._Cache.TryGetValue(key, out value)) return value;

            return null;
        }

        public void Put(object key, object value)
        {
            if (this.CurrentCount == this.Capacity)
            {
                var keyToDiscard = this._Keys.Dequeue();
                var oldValue     = this._Cache[keyToDiscard];
                this._Cache.Remove(keyToDiscard);

                if (this._ObjectDestroyer != null) this._ObjectDestroyer(keyToDiscard, oldValue);
            }

            this._Keys.Enqueue(key);
            this._Cache[key] = value;
        }

        public void Clear()
        {
            if (this._ObjectDestroyer != null)
                foreach (var kv in this._Cache)
                    if (kv.Value != null)
                        this._ObjectDestroyer(kv.Key, kv.Value);
            this._Cache.Clear();
        }
    }
}