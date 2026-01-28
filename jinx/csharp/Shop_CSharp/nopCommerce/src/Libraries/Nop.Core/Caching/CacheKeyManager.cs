using Nop.Core.Infrastructure;

namespace Nop.Core.Caching;

/// <summary>
/// 缓存密钥管理器
/// </summary>
/// <remarks>
/// 这个类应该作为单例实例在IoC上注册
/// </remarks>
public partial class CacheKeyManager : ICacheKeyManager
{
    protected readonly IConcurrentCollection<byte> _keys;

    public CacheKeyManager(IConcurrentCollection<byte> keys)
    {
        _keys = keys;
    }

    /// <summary>
    /// 添加密钥 key
    /// </summary>
    /// <param name="key">The key to add</param>
    public void AddKey(string key)
    {
        _keys.Add(key, default);
    }

    /// <summary>
    /// 删除密钥 key
    /// </summary>
    /// <param name="key">The key to remove</param>
    public void RemoveKey(string key)
    {
        _keys.Remove(key);
    }

    /// <summary>
    /// 删除所有的密钥 keys
    /// </summary>
    public void Clear()
    {
        _keys.Clear();
    }

    /// <summary>
    /// 按前缀删除密钥 key
    /// </summary>
    /// <param name="prefix">Prefix to delete keys</param>
    /// <returns>删除的键的列表</returns>
    public IEnumerable<string> RemoveByPrefix(string prefix)
    {
        if (!_keys.Prune(prefix, out var subtree) || subtree?.Keys == null)
            return Enumerable.Empty<string>();

        return subtree.Keys;
    }

    /// <summary>
    /// 密钥列表 keys
    /// </summary>
    public IEnumerable<string> Keys => _keys.Keys;
}