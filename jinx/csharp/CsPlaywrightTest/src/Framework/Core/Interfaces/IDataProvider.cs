namespace EnterpriseAutomationFramework.Core.Interfaces;

/// <summary>
/// 数据提供者接口
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// 获取测试数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    Task<T?> GetDataAsync<T>(string key);
    
    /// <summary>
    /// 获取所有测试数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    Task<IEnumerable<T>> GetAllDataAsync<T>();
    
    /// <summary>
    /// 保存测试数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">数据键</param>
    /// <param name="data">数据对象</param>
    Task SaveDataAsync<T>(string key, T data);
}