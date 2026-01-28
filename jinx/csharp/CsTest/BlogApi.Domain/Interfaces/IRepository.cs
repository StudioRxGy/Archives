namespace BlogApi.Domain.Interfaces;

/// <summary>
/// 通用仓储接口，定义基本的CRUD操作
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>找到的实体，如果不存在则返回null</returns>
    Task<TEntity?> GetByIdAsync(TKey id);
    
    /// <summary>
    /// 创建新实体
    /// </summary>
    /// <param name="entity">要创建的实体</param>
    /// <returns>创建后的实体</returns>
    Task<TEntity> CreateAsync(TEntity entity);
    
    /// <summary>
    /// 更新现有实体
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <returns>更新后的实体</returns>
    Task<TEntity> UpdateAsync(TEntity entity);
    
    /// <summary>
    /// 根据ID删除实体
    /// </summary>
    /// <param name="id">要删除的实体ID</param>
    /// <returns>删除成功返回true，否则返回false</returns>
    Task<bool> DeleteAsync(TKey id);
    
    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(TKey id);
}