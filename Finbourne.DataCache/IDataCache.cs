using System.Collections.Concurrent;

namespace Finbourne.DataCache;
public interface IDataCache<TKey, TValue>
{
    event Action<TValue>? OnItemEvicted;
    bool TryGetValue(TKey key, out TValue value, bool throwOnNotFound = true);
    TValue AddOrUpdate(TKey key, TValue value);
    void Resize(int newCapacity);
    void Clear();
    int MaximumCapacity { get; }
}