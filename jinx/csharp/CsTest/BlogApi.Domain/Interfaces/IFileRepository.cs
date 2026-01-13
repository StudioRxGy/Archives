using BlogApi.Domain.Entities;
using BlogApi.Domain.Common;

namespace BlogApi.Domain.Interfaces;

/// <summary>
/// 文件实体仓储接口
/// </summary>
public interface IFileRepository : IRepository<FileEntity, int>
{
    /// <summary>
    /// 获取指定用户上传的所有文件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="includePrivate">是否包含私有文件</param>
    /// <returns>该用户上传的文件列表</returns>
    Task<List<FileEntity>> GetByUserIdAsync(int userId, bool includePrivate = true);
    
    /// <summary>
    /// 获取分页的公开文件列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页项目数</param>
    /// <returns>分页的公开文件结果</returns>
    Task<PagedResult<FileEntity>> GetPublicFilesAsync(int page = 1, int pageSize = 10);
    
    /// <summary>
    /// 根据内容类型获取文件
    /// </summary>
    /// <param name="contentType">要过滤的内容类型</param>
    /// <param name="userId">可选的用户ID，用于按上传者过滤</param>
    /// <returns>匹配内容类型的文件列表</returns>
    Task<List<FileEntity>> GetByContentTypeAsync(string contentType, int? userId = null);
    
    /// <summary>
    /// 获取分页和过滤的文件列表
    /// </summary>
    /// <param name="parameters">查询参数，用于过滤和分页</param>
    /// <returns>分页的文件结果</returns>
    Task<PagedResult<FileEntity>> GetPagedAsync(FileQueryParameters parameters);
}