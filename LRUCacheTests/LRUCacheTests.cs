using LRUCacheImplementation.LRUCache;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LRUCacheImplementation.Tests
{
    public class LRUCacheTests
    {
        LRUCacheManager<string, string> lruCacheManager;
        string evictedItemKey = string.Empty;
        int cacheSize = 100000;

        [SetUp]
        public void Setup()
        {
            lruCacheManager = new LRUCacheManager<string, string>(cacheSize);
            lruCacheManager.OldestItemEvicted += LruCacheManager_OldestItemEvicted;
        }

        private void LruCacheManager_OldestItemEvicted(object sender, string e)
        {
            evictedItemKey = e;
        }

        [Test]
        public void should_add_max_items_successfully()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.AreEqual(cacheSize, lruCacheManager.CurrentCacheSize);
        }

        [Test]
        public void should_follow_fifo_when_adding_items()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.AreEqual("Value 1", lruCacheManager.LeastRecentlyUsed);
        }

        [Test]
        public void should_put_oldest_item_on_top_when_accessed()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.AreEqual("Value 1", lruCacheManager.LeastRecentlyUsed);
            lruCacheManager.Get("Item1");
            Assert.AreEqual("Value 1", lruCacheManager.MostRecentlyUsed);
        }

        [Test]
        public void should_put_item_on_top_when_accessed()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            lruCacheManager.Get("Item5");
            Assert.AreEqual("Value 5", lruCacheManager.MostRecentlyUsed);
        }

        [Test]
        public void should_not_give_error_when_non_existent_item_removed()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.IsFalse(lruCacheManager.Delete("some item"));
        }

        [Test]
        public void should_reshuffle_items_when_first_item_removed()
        {
            for (int i = 1; i <= cacheSize; i++)
            {
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.AreEqual($"Value {cacheSize}", lruCacheManager.MostRecentlyUsed);
            Assert.IsTrue(lruCacheManager.Delete($"Item{cacheSize}"));
            Assert.IsFalse(lruCacheManager.Contains($"Item{cacheSize}"));
            Assert.AreEqual($"Value {cacheSize - 1}", lruCacheManager.MostRecentlyUsed);
        }

        #region Eviction notification test
        [Test]
        public void should_evict_least_recently_used_item_when_more_than_max_items_are_added()
        {
            string firstItemKey = string.Empty;
            for (int i = 1; i <= cacheSize + 1; i++)
            {
                if (string.IsNullOrWhiteSpace(firstItemKey))
                    firstItemKey = $"Item{i}";
                lruCacheManager.Put($"Item{i}", $"Value {i}");
            }

            Assert.AreEqual(firstItemKey, evictedItemKey);
        }
        #endregion

        #region Thread-Safe Tests
        [Test]
        public void should_add_items_from_multiple_threads()
        {
            Parallel.For(0, cacheSize, (i) => lruCacheManager.Put($"Item{i}", $"Value {i}"));
            Assert.AreEqual(cacheSize, lruCacheManager.CurrentCacheSize);
            Assert.AreEqual("Value 5", lruCacheManager.Get("Item5"));
        }

        [Test]
        public void should_evict_least_recently_used_item_when_new_items_are_added_in_parallel()
        {
            Parallel.For(0, cacheSize, (i) => lruCacheManager.Put($"Item{i}", $"Value {i}"));

            //cannot assert for a specific value of a key to be evicted as the 
            //values are inserted in parallel hence order cannot be guaranteed
            //so checking if an item has been evicted should suffice
            Assert.IsNotEmpty(evictedItemKey);
        }

        [Test]
        public void should_get_item_only_after_all_items_are_added()
        {
            Parallel.For(0, cacheSize, (i) => lruCacheManager.Put($"Item{i}", $"Value {i}"));
            var value = lruCacheManager.Get("Item5");
            Assert.AreEqual("Value 5", value);
        }
        #endregion
    }
}