namespace Finbourne.DataCache;
public class CacheItemComparer<T> : IComparer<CacheItem<T>> where T : new()
{
    public int Compare(CacheItem<T>? x, CacheItem<T>? y)
    {
        if (x?.LastAccessed > y?.LastAccessed)
            return 1;
        if (x?.LastAccessed < y?.LastAccessed)
            return -1;
        else
            return 0;
    }
}