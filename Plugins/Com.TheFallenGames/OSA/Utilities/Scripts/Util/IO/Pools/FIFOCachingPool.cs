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

		public int Capacity { get; private set; }
		public int CurrentCount { get { return _Keys.Count; } }

		readonly Queue<object> _Keys;
		readonly Dictionary<object, object> _Cache;
		readonly ObjectDestroyer _ObjectDestroyer;


		/// <summary>First in, First out caching</summary>
		/// <param name="capacity"></param>
		/// <param name="objectDestroyer">
		/// When an object is kicked out of the cache, this will be used to process its destruction, 
		/// in case special code needs to be executed. This is also called for each value when the cache is cleared usinc <see cref="Clear"/>
		/// </param>
		public FIFOCachingPool(int capacity, ObjectDestroyer objectDestroyer = null)
		{
			Capacity = capacity;
			_Keys = new Queue<object>(capacity);
			_Cache = new Dictionary<object, object>(capacity);
			_ObjectDestroyer = objectDestroyer;
		}


		public object Get(object key)
		{
			object value;
			if (_Cache.TryGetValue(key, out value))
			{
				return value;
			}

			return null;
		}

		public void Put(object key, object value)
		{
			if (CurrentCount == Capacity)
			{
				object keyToDiscard = _Keys.Dequeue();
				object oldValue = _Cache[keyToDiscard];
				_Cache.Remove(keyToDiscard);

				if (_ObjectDestroyer != null)
					_ObjectDestroyer(keyToDiscard, oldValue);
			}

			_Keys.Enqueue(key);
			_Cache[key] = value;
		}

		public void Clear()
		{
			if (_ObjectDestroyer != null)
			{
				foreach (var kv in _Cache)
				{
					if (kv.Value != null)
						_ObjectDestroyer(kv.Key, kv.Value);
				}
			}
			_Cache.Clear();
		}
	}
}
