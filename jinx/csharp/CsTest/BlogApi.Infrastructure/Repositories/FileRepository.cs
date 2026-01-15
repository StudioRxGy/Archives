using Microsoft.EntityFrameworkCore;
using BlogApi.Domain.Entities;
using BlogApi.Domain.Interfaces;
using BlogApi.Domain.Common;
using BlogApi.Infrastructure.Data;

namespace BlogApi.Infrastructure.Repositories;

/// <summary>
/// 文件仓储实现
/// </summary>
public class FileRepository : BaseRepository<FileEntity, int>, IFileRepository
{
    public FileRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 获取指定用户上传的所有文件
    /// </summary>
    public async Task<List<FileEntity>> GetByUserIdAsync(int userId, bool includePrivate = true)
    {
        var query = _dbSet.Include(f => f.Uploader)
            .Where(f => f.UploadedBy == userId);

        if (!includePrivate)
        {
            query = query.Where(f => f.IsPublic);
        }

        return await query
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取分页的公开文件列表
    /// </summary>
    public async Task<PagedResult<FileEntity>> GetPublicFilesAsync(int page = 1, int pageSize = 10)
    {
        var query = _dbSet.Include(f => f.Uploader)
            .Where(f => f.IsPublic)
            .OrderByDescending(f => f.UploadedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<FileEntity>.Create(items, totalCount, page, pageSize);
    }

    /// <summary>
    /// 根据内容类型获取文件
    /// </summary>
    public async Task<List<FileEntity>> GetByContentTypeAsync(string contentType, int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return new List<FileEntity>();

        var query = _dbSet.Include(f => f.Uploader)
            .Where(f => f.ContentType.ToLower() == contentType.ToLower());

        if (userId.HasValue)
        {
            query = query.Where(f => f.UploadedBy == userId.Value);
        }

        return await query
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取分页和过滤的文件列表
    /// </summary>
    public async Task<PagedResult<FileEntity>> GetPagedAsync(FileQueryParameters parameters)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var query = _dbSet.Include(f => f.Uploader).AsQueryable();

        // 应用过滤条件
        if (parameters.UploadedBy.HasValue)
        {
            query = query.Where(f => f.UploadedBy == parameters.UploadedBy.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.ContentType))
        {
            query = query.Where(f => f.ContentType.ToLower().Contains(parameters.ContentType.ToLower()));
        }

        if (parameters.IsPublic.HasValue)
        {
            query = query.Where(f => f.IsPublic == parameters.IsPublic.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(f => 
                f.OriginalName.ToLower().Contains(searchTerm) ||
                f.ContentType.ToLower().Contains(searchTerm));
        }

        if (parameters.UploadedAfter.HasValue)
        {
            query = query.Where(f => f.UploadedAt >= parameters.UploadedAfter.Value);
        }

        if (parameters.UploadedBefore.HasValue)
        {
            query = query.Where(f => f.UploadedAt <= parameters.UploadedBefore.Value);
        }

        // 按上传时间降序排序
        query = query.OrderByDescending(f => f.UploadedAt);

        // 获取总数
        var totalCount = await query.CountAsync();

        // 应用分页
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return PagedResult<FileEntity>.Create(items, totalCount, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// 重写GetByIdAsync以包含上传者信息
    /// </summary>
    public override async Task<FileEntity?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(f => f.Uploader)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    /// <summary>
    /// 重写CreateAsync以设置上传时间
    /// </summary>
    public override async Task<FileEntity> CreateAsync(FileEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        entity.UploadedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity);
    }
}