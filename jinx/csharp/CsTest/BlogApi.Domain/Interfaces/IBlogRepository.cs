using BlogApi.Domain.Entities;
using BlogApi.Domain.Common;

namespace BlogApi.Domain.Interfaces;

/// <summary>
/// 博客实体仓储接口
/// </summary>
public interface IBlogRepository : IRepository<Blog, int>
{
    /// <summary>
    /// 获取分页的博客列表，支持可选的过滤条件
    /// </summary>
    /// <param name="parameters">查询参数，用于过滤和分页</param>
    /// <returns>分页的博客结果</returns>
    Task<PagedResult<Blog>> GetPagedAsync(BlogQueryParameters parameters);
    
    /// <summary>
    /// 根据作者ID获取博客列表
    /// </summary>
    /// <param name="authorId">作者的用户ID</param>
    /// <param name="includeUnpublished">是否包含未发布的博客</param>
    /// <returns>该作者的博客列表</returns>
    Task<List<Blog>> GetByAuthorIdAsync(int authorId, bool includeUnpublished = false);
}