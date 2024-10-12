using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.Util.IO.Pools
{
    /// <summary>
    /// Clears the least accessed items in favor of the most recently accessed ones. 
    /// </summary>
    /// <remarks>This is more versatile than <see cref="FIFOCachingPool"/>.</remarks>
    public class LRUCachingPool : IPool
    {
        public delegate void ObjectDestroyer(object key, object value);

        public int Capacity     { get; private set; }
        public int CurrentCount => this._Cache.Count;

        private readonly Dictionary<object, LinkedListNode<CacheItem>> _Cache;
        private readonly LinkedList<CacheItem>                         _LruOrderList;
        private readonly ObjectDestroyer                               _ObjectDestroyer;

        public LRUCachingPool(int capacity, ObjectDestroyer objectDestroyer = null)
        {
            this.Capacity         = capacity;
            this._Cache           = new(capacity);
            this._LruOrderList    = new();
            this._ObjectDestroyer = objectDestroyer;
        }

        public object Get(object key)
        {
            if (this._Cache.TryGetValue(key, out var node))
            {
                // Move accessed node to the end to show that it was recently used
                this._LruOrderList.Remove(node);  // remove current node
                this._LruOrderList.AddLast(node); // add it back to the end
                return node.Value.Value;
            }

            return null;
        }

        public void Put(object key, object value)
        {
            // If the key already exists, remove it first
            if (this._Cache.TryGetValue(key, out var existingNode))
            {
                this._LruOrderList.Remove(existingNode);
                this._Cache.Remove(key);
            }

            if (this._Cache.Count == this.Capacity)
            {
                // Remove the least recently used item
                var oldestNode = this._LruOrderList.First;
                this._LruOrderList.RemoveFirst();
                this._Cache.Remove(oldestNode.Value.Key);

                if (this._ObjectDestroyer != null) this._ObjectDestroyer(oldestNode.Value.Key, oldestNode.Value.Value);
            }

            var newNode = new LinkedListNode<CacheItem>(new() { Key = key, Value = value });
            this._LruOrderList.AddLast(newNode);
            this._Cache[key] = newNode;
        }

        public void Clear()
        {
            if (this._ObjectDestroyer != null)
                foreach (var cached in this._LruOrderList)
                    if (cached.Value != null)
                        this._ObjectDestroyer(cached.Key, cached.Value);
            this._Cache.Clear();
            this._LruOrderList.Clear();
        }

        private class CacheItem
        {
            public object Key   { get; set; }
            public object Value { get; set; }
        }
    }
}