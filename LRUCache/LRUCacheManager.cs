using System;
using System.Collections.Generic;

namespace LRUCacheImplementation.LRUCache
{
    public class LRUCacheManager<TKey, TValue> : IDisposable
    {
        private Dictionary<TKey, LinkedListNode<TValue>> _cacheRegister;
        private LinkedList<TValue> _cache;

        private readonly object Lock = new object();

        public event EventHandler<TKey> OldestItemEvicted;

        public LRUCacheManager(int cacheSize)
        {
            CacheCapacity = cacheSize;
            _cache = new LinkedList<TValue>();
            _cacheRegister = new Dictionary<TKey, LinkedListNode<TValue>>();
        }

        public LRUCacheManager() : this(1000)
        { }

        public int CacheCapacity { get; private set; }

        public bool Contains(TKey key)
        {
            return _cacheRegister.ContainsKey(key);
        }

        public TValue MostRecentlyUsed { get { lock (Lock) { return _cache.First.Value; } } }
        public TValue LeastRecentlyUsed { get { lock (Lock) { return _cache.Last.Value; } } }

        public int CurrentCacheSize { get { lock (Lock) { return _cacheRegister.Count; } } }

        /// <summary>
        /// The item is always put on the top of the list when inserted or updated.
        /// If item already exists, its value on the cache is updated.
        /// If item doesn't exist, it's added and if the cache size is reached, the least recently used item on the cache is removed.
        /// </summary>
        /// <param name="key">Key for the item to be put into cache. </param>
        /// <param name="item">Item to be put into cache. </param>
        public void Put(TKey key, TValue item)
        {
            LinkedListNode<TValue> node = null;

            lock (Lock)
            {
                if (Contains(key))
                    _cacheRegister.Remove(key, out node);

                if (node == null)
                    node = new LinkedListNode<TValue>(item);

                //both of these operations should be performed atomically to ensure 
                //dictionary and cache doesn't hold items more than the capacity
                if (_cache.Count >= CacheCapacity)
                {
                    DeleteOldestItem();
                }

                _cache.AddFirst(node);
                _cacheRegister[key] = node;
            }
        }

        /// <summary>
        /// If the key is found on the cache, its corresponding value is retrieved and returned.
        /// If key is not found, null is returned. The idea is that the consumer not having found the item on the cache will
        /// retrieve the value from the source and eventually call PutItem to put the item on the cache.
        /// </summary>
        /// <param name="key">The key for the item to be retrieved</param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            TValue value = default(TValue);

            lock (Lock)
            {
                if (Contains(key))
                {
                    var node = _cacheRegister[key];
                    value = node.Value;

                    _cache.Remove(node);
                    _cache.AddFirst(node);
                }
            }
            return value;
        }

        /// <summary>
        /// Method to delete the the item from the cache
        /// </summary>
        /// <param name="key">Key for the item to be removed</param>
        /// <returns></returns>
        public bool Delete(TKey key)
        {
            lock (Lock)
            {
                if (_cacheRegister.ContainsKey(key))
                {
                    LinkedListNode<TValue> node;
                    _cacheRegister.Remove(key, out node);
                    _cache.Remove(node);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// This method removes the oldest item from the cache.
        /// Any method calling this should implement locking around this call
        /// </summary>
        /// <returns></returns>
        private bool DeleteOldestItem()
        {
            if (_cache != null && _cache.Last != null)
            {
                foreach (var cacheEntry in _cacheRegister)
                {
                    if (cacheEntry.Value == _cache.Last)
                    {
                        OldestItemEvicted?.Invoke(this, cacheEntry.Key);
                        _cache.RemoveLast();
                        return _cacheRegister.Remove(cacheEntry.Key, out LinkedListNode<TValue> node);
                    }
                }
            }

            return false;
        }

        public void Clear()
        {
            lock (Lock)
            {
                _cache.Clear();
                _cacheRegister.Clear();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Clear();
                    _cache = null;
                    _cacheRegister = null;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}