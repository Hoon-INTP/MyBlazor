
namespace XXXBlazor.Client.Models
{
    public class XXXCacheData<T>
    {
        public T Value { get; set; }

        public XXXCacheData(T value)
        {
            Value = value;
        }
    }

    public class XXXCache<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, XXXCacheData<TValue>> _cache;
        private readonly ReaderWriterLockSlim _lock;
        private readonly Queue<TKey> _queue;
        private readonly int _limitCount;

        public XXXCache(int limitCount = 10)
        {
            if (limitCount <= 0) throw new ArgumentException("제한 개수는 0보다 커야 합니다.", nameof(limitCount));

            _limitCount = limitCount;
            _cache = new Dictionary<TKey, XXXCacheData<TValue>>(limitCount);
            _lock = new ReaderWriterLockSlim();
            _queue = new Queue<TKey>(limitCount);
        }

        public void Set(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_cache.ContainsKey(key))
                {
                    _cache[key] = new XXXCacheData<TValue>(value);
                    Console.WriteLine($"캐시 키 중복: {key}");
                }
                else
                {
                    if (_cache.Count >= _limitCount)
                    {
                        TKey oldestKey = _queue.Dequeue();

                        if (!_cache.ContainsKey(oldestKey))
                        {
                            while ( _queue.Count > 0 && !_cache.ContainsKey(_queue.Peek()) )
                            {
                                oldestKey = _queue.Dequeue();
                            }

                            if ( _queue.Count == 0)
                            {
                                _cache.Clear();
                            }
                        }

                        _cache.Remove(oldestKey);
                    }

                    _cache[key] = new XXXCacheData<TValue>(value);
                    _queue.Enqueue(key);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryGet(TKey key, out TValue value)
        {
            value = default!;

            _lock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var cacheData))
                {
                    value = cacheData.Value;
                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_cache.Remove(key))
                {
                    Console.WriteLine($"항목 제거됨: {key}");
                    //_queue contents will be deleted when Dequeue sequence
                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _queue.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _cache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public int Limit => _limitCount;

        public void Dispose()
        {
            _lock?.Dispose();
        }

    }
}
