namespace Finbourne.DataCache.Tests;

public class DataCacheTests
{
    internal class DummyClass
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public void TryGetValue_Returns_Value()
    {
        var cache = GetCache();
        var expectedId = Guid.NewGuid();
        cache.AddOrUpdate(1, new DummyClass() { Id = expectedId });
        cache.TryGetValue(1, out var item);
        Assert.Equal(expectedId, item.Id);
    }

    [Fact]
    public void AddsOrUpdate_Updates_Value()
    {
        var cache = GetCache();
        var key = 1;
        var expectedId = Guid.NewGuid();
        cache.AddOrUpdate(key, new DummyClass() { Id = Guid.Empty });
        cache.AddOrUpdate(key, new DummyClass() { Id = expectedId });
        cache.TryGetValue(key, out var item);
        Assert.Equal(expectedId, item.Id);
    }

    [Theory]
    [InlineData(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9  })]
    [InlineData(new int[] { 1, 2, 6, 7, 8, 9, 3, 4, 5, 0  })]
    public void AddOrUpdate_At_Max_Capacity_Updates_Value_And_Removes_Oldest_Unaccessed_Value(int[] orderOfAccess)
    {
        // Arrange
        var maxCapacity = orderOfAccess.Length;
        var cache = GetCache(maxCapacity);
        FillCache(cache, maxCapacity);

        // Act - Access in specific order
        foreach (var key in orderOfAccess)
        {
            cache.TryGetValue(key, out var _);
        }

        // Act - Add new item and read from cache
        var newId = Guid.NewGuid();
        var unusedKey = maxCapacity + 1;
        cache.AddOrUpdate(unusedKey, new DummyClass() { Id = newId });
        var lastUnaccessedKey = orderOfAccess.First();
        cache.TryGetValue(unusedKey, out var newItem);
        cache.TryGetValue(lastUnaccessedKey, out var removedItem, throwOnNotFound: false);
        
        // Assert
        Assert.Null(removedItem);
        Assert.Equal(newId, newItem.Id);
    }

    [Fact]
    public void OnItemEvicted_Fires_When_Item_Is_Removed()
    {
        var maxCapacity = 10;
        var evictedId = Guid.Empty;
        var expectedId = Guid.NewGuid();
        var cache = GetCache(maxCapacity);
        cache.OnItemEvicted += delegate (DummyClass item)
        {
            evictedId = item.Id;
        };

        cache.AddOrUpdate(12345, new DummyClass() { Id = expectedId });
        FillCache(cache, 10);

        Assert.Equal(expectedId, evictedId);
    }

    private void FillCache(IDataCache<int, DummyClass> cache, int capacity)
    {
        for (var i = 0; i < capacity; i++)
        {
            cache.AddOrUpdate(i, new DummyClass() { Id = Guid.NewGuid() });
        }
    }

    /**
        Other tests that would be worth writing:
        - TryGetValue_Throws_If_Value_Not_Found_And_throwOnNotFound_True
        - TryGetValue_Doesnt_Throw_If_Value_Not_Found_And_throwOnNotFound_False
        - Resize_Increases_Capacity
        - Resize_Decreases_Capacity_And_Removes_Oldest_Unaccessed_Values
        - Clear_Correctly_Empties_Cache
        - Concurrent_Reads_And_Writes_From_Multiple_Threads_Does_Not_Throw_Exception
     **/

    private IDataCache<int, DummyClass> GetCache(int capacity = 10) => new DataCache<int, DummyClass>(capacity);
}