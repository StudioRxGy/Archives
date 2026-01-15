using Nop.Core.Configuration;
using Nop.Core.Infrastructure;

namespace Nop.Core.Caching;

/// <summary>
/// 表示缓存对象的键
/// </summary>
public partial class CacheKey
{
    #region Ctor

    /// <summary>
    /// 用键和前缀初始化一个新实例
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="prefixes">Prefixes for remove by prefix functionality</param>
    public CacheKey(string key, params string[] prefixes)
    {
        Key = key;
        Prefixes.AddRange(prefixes.Where(prefix => !string.IsNullOrEmpty(prefix)));
    }

    #endregion

    #region Methods

    /// <summary>
    /// 从当前实例创建一个新实例，并用传递的参数填充它
    /// </summary>
    /// <param name="createCacheKeyParameters">Function to create parameters</param>
    /// <param name="keyObjects">Objects to create parameters</param>
    /// <returns>Cache key</returns>
    public virtual CacheKey Create(Func<object, object> createCacheKeyParameters, params object[] keyObjects)
    {
        var cacheKey = new CacheKey(Key, Prefixes.ToArray());

        if (!keyObjects.Any())
            return cacheKey;

        cacheKey.Key = string.Format(cacheKey.Key, keyObjects.Select(createCacheKeyParameters).ToArray());

        for (var i = 0; i < cacheKey.Prefixes.Count; i++)
            cacheKey.Prefixes[i] = string.Format(cacheKey.Prefixes[i], keyObjects.Select(createCacheKeyParameters).ToArray());

        return cacheKey;
    }

    #endregion

    #region Properties

    /// <summary>
    /// 获取或设置缓存键
    /// </summary>
    public string Key { get; protected set; }

    /// <summary>
    /// 获取或设置按前缀删除功能的前缀
    /// </summary>
    public List<string> Prefixes { get; protected set; } = new();

    /// <summary>
    /// 获取或设置以分钟为单位的缓存时间
    /// </summary>
    public int CacheTime { get; set; } = Singleton<AppSettings>.Instance.Get<CacheConfig>().DefaultCacheTime;

    #endregion
}