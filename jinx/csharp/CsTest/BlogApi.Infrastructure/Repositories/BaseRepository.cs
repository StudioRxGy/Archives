using Microsoft.EntityFrameworkCore;
using BlogApi.Domain.Interfaces;
using BlogApi.Infrastructure.Data;

namespace BlogApi.Infrastructure.Repositories;

/// <summary>
/// 基础仓储实现，提供通用的CRUD操作
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class BaseRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly BlogDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected BaseRepository(BlogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<TEntity>();
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// 创建新实体
    /// </summary>
    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// 更新现有实体
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// 根据ID删除实体
    /// </summary>
    public virtual async Task<bool> DeleteAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public virtual async Task<bool> ExistsAsync(TKey id)
    {
        var entity = await GetByIdAsync(id);
        return entity != null;
    }
}