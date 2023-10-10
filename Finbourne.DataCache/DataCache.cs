using System.Collections.Concurrent;

namespace Finbourne.DataCache;
public class DataCache<TKey, TValue> : IDataCache<TKey, TValue> where TKey : IEquatable<TKey> where TValue : new()
{
    // ConcurrentDictionary to manage the cache items
    private readonly ConcurrentDictionary<TKey, TValue> _dictionary;
    // LinkedList to track the last accessed ordering of cache keys
    private readonly LinkedList<TKey> _lastAccess;
    // LinkedList is not thread safe so use a lock
    private readonly object _lock = new object();
    private int _maximumCapacity;
    public int MaximumCapacity => _maximumCapacity;
    public event Action<TValue>? OnItemEvicted;

    public DataCache(int maximumCapacity)
    {
        if (maximumCapacity <= 0) throw new ArgumentOutOfRangeException($"{nameof(maximumCapacity)} must be greater than 0");
        _dictionary = new ConcurrentDictionary<TKey, TValue>();
        _lastAccess = new LinkedList<TKey>();
        _maximumCapacity = maximumCapacity;
    }

    public bool TryGetValue(TKey key, out TValue value, bool throwOnNotFound = true)
    {
        if (_dictionary.TryGetValue(key, out var item))
        {
            UpdateLastAccessed(key);
            value = item;
            return true;
        }
        else
        {
            if (throwOnNotFound)
            {
                throw new KeyNotFoundException();
            }
            else
            {
                value = default!;
                return false;
            }
        }
    }

    public TValue AddOrUpdate(TKey key, TValue value)
    {
        if (_dictionary.ContainsKey(key))
        {
            UpdateLastAccessed(key);
        }
        else
        {
           AddOrReplaceOldestKey(key);
        }
        return _dictionary.AddOrUpdate(key, (_) => value, (_, _) => value);
    }

    public void Resize(int newCapacity)
    {
        if (newCapacity <= 0) throw new ArgumentOutOfRangeException($"{nameof(newCapacity)} must be greater than 0");
        lock (_lock)
        {
            var capacity = Interlocked.Exchange(ref _maximumCapacity, newCapacity);
            if (capacity < newCapacity)
            {
                var diff = capacity - newCapacity;
                var keysToRemove = _lastAccess.Take(diff);
                foreach (var keyToRemove in keysToRemove)
                {
                    _lastAccess.Remove(keyToRemove);
                    _dictionary.TryRemove(keyToRemove, out var _);
                }
            }
        }
    }

    public void Clear()
    {
        var removedItems = new List<TValue>();
        lock (_lock)
        {
            foreach (var keyToRemove in _lastAccess)
            {
                _lastAccess.Remove(keyToRemove);
                if (_dictionary.TryGetValue(keyToRemove, out var value))
                {
                    removedItems.Add(value);
                }
            }
            _lastAccess.Clear();
            _dictionary.Clear();
        }

        foreach (var item in removedItems)
        {
            OnItemEvicted?.Invoke(item);
        }
    }

    private void UpdateLastAccessed(TKey key)
    {
        lock (_lock)
        {
            _lastAccess.Remove(key);
            _lastAccess.AddLast(key);
        }
    }

    private void AddOrReplaceOldestKey(TKey keyToAdd)
    {
        lock (_lock)
        {
            if (_lastAccess.Count >= _maximumCapacity)
            {
                TValue? removedItem = default;
                var keyToRemove = _lastAccess.First();
                _lastAccess.Remove(keyToRemove);
                _dictionary.TryRemove(keyToRemove, out removedItem);

                if (removedItem != null)
                {
                    OnItemEvicted?.Invoke(removedItem);
                }
            }
            _lastAccess.AddLast(keyToAdd);
        }
    }
}
