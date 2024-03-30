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

		public int Capacity { get; private set; }
		public int CurrentCount { get { return _Cache.Count; } }

		readonly Dictionary<object, LinkedListNode<CacheItem>> _Cache;
		readonly LinkedList<CacheItem> _LruOrderList;
		readonly ObjectDestroyer _ObjectDestroyer;


		public LRUCachingPool(int capacity, ObjectDestroyer objectDestroyer = null)
		{
			Capacity = capacity;
			_Cache = new Dictionary<object, LinkedListNode<CacheItem>>(capacity);
			_LruOrderList = new LinkedList<CacheItem>();
			_ObjectDestroyer = objectDestroyer;
		}


		public object Get(object key)
		{
			if (_Cache.TryGetValue(key, out var node))
			{
				// Move accessed node to the end to show that it was recently used
				_LruOrderList.Remove(node); // remove current node
				_LruOrderList.AddLast(node); // add it back to the end
				return node.Value.Value;
			}

			return null;
		}

		public void Put(object key, object value)
		{
			// If the key already exists, remove it first
			if (_Cache.TryGetValue(key, out var existingNode))
			{
				_LruOrderList.Remove(existingNode);
				_Cache.Remove(key);
			}

			if (_Cache.Count == Capacity)
			{
				// Remove the least recently used item
				LinkedListNode<CacheItem> oldestNode = _LruOrderList.First;
				_LruOrderList.RemoveFirst();
				_Cache.Remove(oldestNode.Value.Key);

				if (_ObjectDestroyer != null)
					_ObjectDestroyer(oldestNode.Value.Key, oldestNode.Value.Value);
			}

			var newNode = new LinkedListNode<CacheItem>(new CacheItem { Key = key, Value = value });
			_LruOrderList.AddLast(newNode);
			_Cache[key] = newNode;
		}

		public void Clear()
		{
			if (_ObjectDestroyer != null)
			{
				foreach (var cached in _LruOrderList)
				{
					if (cached.Value != null)
						_ObjectDestroyer(cached.Key, cached.Value);
				}
			}
			_Cache.Clear();
			_LruOrderList.Clear();
		}


		class CacheItem
		{
			public object Key { get; set; }
			public object Value { get; set; }
		}
	}
}