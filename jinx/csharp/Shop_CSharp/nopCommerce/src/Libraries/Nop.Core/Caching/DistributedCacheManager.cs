using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;

namespace Nop.Core.Caching;

/// <summary>
/// 表示基本分布式缓存
/// </summary>
public abstract class DistributedCacheManager : CacheKeyService, IStaticCacheManager
{
    #region Fields

    /// <summary>
    /// 保存这个nopCommerce实例所知道的键
    /// </summary>
    protected readonly ICacheKeyManager _localKeyManager;
    protected readonly IDistributedCache _distributedCache;
    protected readonly IConcurrentCollection<object> _concurrentCollection;

    /// <summary>
    /// 保存正在进行的获取任务，用于避免重复工作
    /// </summary>
    protected readonly ConcurrentDictionary<string, Lazy<Task<object>>> _ongoing = new();

    #endregion

    #region Ctor

    protected DistributedCacheManager(AppSettings appSettings,
        IDistributedCache distributedCache,
        ICacheKeyManager cacheKeyManager,
        IConcurrentCollection<object> concurrentCollection)
        : base(appSettings)
    {
        _distributedCache = distributedCache;
        _localKeyManager = cacheKeyManager;
        _concurrentCollection = concurrentCollection;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// 清除此实例上的所有数据
    /// </summary>
    /// <returns>表示异步操作的任务</returns>
    protected virtual void ClearInstanceData()
    {
        _concurrentCollection.Clear();
        _localKeyManager.Clear();
    }

    /// <summary>
    /// 按缓存键前缀删除项
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    /// <returns>删除的 keys</returns>
    protected virtual IEnumerable<string> RemoveByPrefixInstanceData(string prefix, params object[] prefixParameters)
    {
        var keyPrefix = PrepareKeyPrefix(prefix, prefixParameters);
        _concurrentCollection.Prune(keyPrefix, out _);

        return _localKeyManager.RemoveByPrefix(keyPrefix);
    }

    /// <summary>
    /// 为传递的key准备缓存条目选项
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>缓存条目选项</returns>
    protected virtual DistributedCacheEntryOptions PrepareEntryOptions(CacheKey key)
    {
        //为传递的缓存key设置过期时间
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(key.CacheTime)
        };
    }

    /// <summary>
    /// 将指定的key和object添加到本地缓存中
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="value">Value for caching</param>
    protected virtual void SetLocal(string key, object value)
    {
        _concurrentCollection.Add(key, value);
        _localKeyManager.AddKey(key);
    }

    /// <summary>
    /// 从缓存中删除具有指定key的值
    /// </summary>
    /// <param name="key">Cache key</param>
    protected virtual void RemoveLocal(string key)
    {
        _concurrentCollection.Remove(key);
        _localKeyManager.RemoveKey(key);
    }

    /// <summary>
    /// 尝试获取cached。如果它还不在cached中，那么返回默认object
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    protected virtual async Task<(bool isSet, T item)> TryGetItemAsync<T>(string key)
    {
        var json = await _distributedCache.GetStringAsync(key);

        return string.IsNullOrEmpty(json)
            ? (false, default)
            : (true, item: JsonConvert.DeserializeObject<T>(json));
    }

    /// <summary>
    /// 从cached中删除具有指定key的值
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="removeFromInstance">Remove from instance</param>
    protected virtual async Task RemoveAsync(string key, bool removeFromInstance = true)
    {
        _ongoing.TryRemove(key, out _);
        await _distributedCache.RemoveAsync(key);

        if (!removeFromInstance)
            return;

        RemoveLocal(key);
    }

    #endregion

    #region Methods

    /// <summary>
    /// 从cached中删除具有指定key的值
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cacheKeyParameters">Parameters to create cache key</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task RemoveAsync(CacheKey cacheKey, params object[] cacheKeyParameters)
    {
        await RemoveAsync(PrepareKey(cacheKey, cacheKeyParameters).Key);
    }

    /// <summary>
    /// 获取cached项。如果它还不在cached中，那么加载并缓存它
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>
    /// 表示异步操作的任务
    /// 任务结果包含与指定键关联的缓存值
    /// </returns>
    public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
    {
        if (_concurrentCollection.TryGetValue(key.Key, out var data))
            return (T)data;

        var lazy = _ongoing.GetOrAdd(key.Key, _ => new(async () => await acquire(), true));
        var setTask = Task.CompletedTask;

        try
        {
            if (lazy.IsValueCreated)
                return (T)await lazy.Value;

            var (isSet, item) = await TryGetItemAsync<T>(key.Key);
            if (!isSet)
            {
                item = (T)await lazy.Value;

                if (key.CacheTime == 0 || item == null)
                    return item;

                setTask = _distributedCache.SetStringAsync(
                    key.Key,
                    JsonConvert.SerializeObject(item),
                    PrepareEntryOptions(key));
            }

            SetLocal(key.Key, item);

            return item;
        }
        finally
        {
            _ = setTask.ContinueWith(_ => _ongoing.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(key.Key, lazy)));
        }
    }

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key
    /// </returns>
    public Task<T> GetAsync<T>(CacheKey key, Func<T> acquire)
    {
        return GetAsync(key, () => Task.FromResult(acquire()));
    }

    public async Task<T> GetAsync<T>(CacheKey key, T defaultValue = default)
    {
        var value = await _distributedCache.GetStringAsync(key.Key);

        return value != null
            ? JsonConvert.DeserializeObject<T>(value)
            : defaultValue;
    }

    /// <summary>
    /// Get a cached item as an <see cref="object"/> instance, or null on a cache miss.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key, or null if none was found
    /// </returns>
    public async Task<object> GetAsync(CacheKey key)
    {
        return await GetAsync<object>(key);
    }

    /// <summary>
    /// Add the specified key and object to the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="data">Value for caching</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task SetAsync<T>(CacheKey key, T data)
    {
        if (data == null || (key?.CacheTime ?? 0) <= 0)
            return;

        var lazy = new Lazy<Task<object>>(() => Task.FromResult(data as object), true);

        try
        {
            _ongoing.TryAdd(key.Key, lazy);
            // await the lazy task in order to force value creation instead of directly setting data
            // this way, other cache manager instances can access it while it is being set
            SetLocal(key.Key, await lazy.Value);
            await _distributedCache.SetStringAsync(key.Key, JsonConvert.SerializeObject(data), PrepareEntryOptions(key));
        }
        finally
        {
            _ongoing.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(key.Key, lazy));
        }
    }

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public abstract Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters);

    /// <summary>
    /// Clear all cache data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public abstract Task ClearAsync();

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}