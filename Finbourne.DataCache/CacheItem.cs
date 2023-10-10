namespace Finbourne.DataCache;
public class CacheItem<T> where T : new()
{
    public CacheItem(T item)
    {
        Item = item;
        LastAccessed = DateTime.UtcNow;
    }

    public T Item { get; set; }
    public DateTime LastAccessed { get; set; }
}