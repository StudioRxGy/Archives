using Microsoft.EntityFrameworkCore;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;
using BlogApi.Domain.Common;
using BlogApi.Infrastructure.Data;

namespace BlogApi.Infrastructure.Repositories;

/// <summary>
/// 博客仓储实现
/// </summary>
public class BlogRepository : BaseRepository<Blog, int>, IBlogRepository
{
    public BlogRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 获取分页的博客列表，支持可选的过滤条件
    /// </summary>
    public async Task<PagedResult<Blog>> GetPagedAsync(BlogQueryParameters parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var query = _dbSet.Include(b => b.Author).AsQueryable();

        // 应用过滤条件
        if (parameters.IsPublished.HasValue)
        {
            query = query.Where(b => b.IsPublished == parameters.IsPublished.Value);
        }

        if (parameters.AuthorId.HasValue)
        {
            query = query.Where(b => b.AuthorId == parameters.AuthorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(b => 
                b.Title.ToLower().Contains(searchTerm) ||
                b.Content.ToLower().Contains(searchTerm) ||
                b.Summary.ToLower().Contains(searchTerm));
        }

        if (parameters.Tags != null && parameters.Tags.Any())
        {
            // 对于JSON存储的标签，使用JSON_CONTAINS或类似的查询
            foreach (var tag in parameters.Tags)
            {
                query = query.Where(b => b.Tags.Contains($"\"{tag}\""));
            }
        }

        if (parameters.CreatedAfter.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= parameters.CreatedAfter.Value);
        }

        if (parameters.CreatedBefore.HasValue)
        {
            query = query.Where(b => b.CreatedAt <= parameters.CreatedBefore.Value);
        }

        // 按创建时间降序排序
        query = query.OrderByDescending(b => b.CreatedAt);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 应用分页
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<Blog>.Create(items, totalCount, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// 根据作者ID获取博客列表
    /// </summary>
    public async Task<List<Blog>> GetByAuthorIdAsync(int authorId, bool includeUnpublished = false)
    {
        var query = _dbSet.Include(b => b.Author)
            .Where(b => b.AuthorId == authorId);

        if (!includeUnpublished)
        {
            query = query.Where(b => b.IsPublished);
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 重写GetByIdAsync以包含作者信息
    /// </summary>
    public override async Task<Blog?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// 重写CreateAsync以设置创建和更新时间
    /// </summary>
    public override async Task<Blog> CreateAsync(Blog entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity);
    }

    /// <summary>
    /// 重写UpdateAsync以更新修改时间
    /// </summary>
    public override async Task<Blog> UpdateAsync(Blog entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        entity.UpdatedAt = DateTime.UtcNow;

        return await base.UpdateAsync(entity);
    }
}