namespace Nop.Core.Caching;

public static class CachingExtensions
{
    /// <summary>
    /// 获取缓存项。如果它还不在缓存中，那么加载并缓存它。
    /// 注意：此方法仅用于向后兼容：首选异步重载！
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>The cached value associated with the specified key</returns>
    public static T Get<T>(this IStaticCacheManager cacheManager, CacheKey key, Func<T> acquire)
    {
        return cacheManager.GetAsync(key, acquire).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 按缓存键前缀删除项
    /// </summary>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    public static void RemoveByPrefix(this IStaticCacheManager cacheManager, string prefix, params object[] prefixParameters)
    {
        cacheManager.RemoveByPrefixAsync(prefix, prefixParameters).Wait();
    }
}