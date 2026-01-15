using BlogApi.Application.Commands.Blog;
using BlogApi.Application.DTOs;
using BlogApi.Application.DTOs.Common;
using BlogApi.Application.Queries.Blog;
using BlogApi.Domain.Common;

namespace BlogApi.Application.Services;

/// <summary>
/// 博客应用服务接口
/// </summary>
public interface IBlogApplicationService
{
    /// <summary>
    /// 获取博客列表（支持分页和筛选）
    /// </summary>
    /// <param name="query">获取博客列表查询</param>
    /// <returns>分页博客列表</returns>
    Task<OperationResult<PagedResult<BlogListItemDto>>> GetBlogsAsync(GetBlogsQuery query);

    /// <summary>
    /// 根据作者获取博客列表
    /// </summary>
    /// <param name="query">根据作者获取博客查询</param>
    /// <returns>分页博客列表</returns>
    Task<OperationResult<PagedResult<BlogListItemDto>>> GetBlogsByAuthorAsync(GetBlogsByAuthorQuery query);

    /// <summary>
    /// 搜索博客
    /// </summary>
    /// <param name="query">搜索博客查询</param>
    /// <returns>分页博客列表</returns>
    Task<OperationResult<PagedResult<BlogListItemDto>>> SearchBlogsAsync(SearchBlogsQuery query);

    /// <summary>
    /// 根据ID获取单篇博客详情
    /// </summary>
    /// <param name="query">获取博客详情查询</param>
    /// <returns>博客详情</returns>
    Task<OperationResult<BlogDto>> GetBlogByIdAsync(GetBlogByIdQuery query);

    /// <summary>
    /// 创建博客文章
    /// </summary>
    /// <param name="command">创建博客命令</param>
    /// <returns>创建的博客信息</returns>
    Task<OperationResult<BlogDto>> CreateBlogAsync(CreateBlogCommand command);

    /// <summary>
    /// 更新博客文章
    /// </summary>
    /// <param name="command">更新博客命令</param>
    /// <returns>更新后的博客信息</returns>
    Task<OperationResult<BlogDto>> UpdateBlogAsync(UpdateBlogCommand command);

    /// <summary>
    /// 发布或取消发布博客
    /// </summary>
    /// <param name="command">发布博客命令</param>
    /// <returns>操作结果</returns>
    Task<OperationResult<BlogDto>> PublishBlogAsync(PublishBlogCommand command);

    /// <summary>
    /// 删除博客文章
    /// </summary>
    /// <param name="command">删除博客命令</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> DeleteBlogAsync(DeleteBlogCommand command);

    /// <summary>
    /// 验证用户是否有权限操作指定博客
    /// </summary>
    /// <param name="blogId">博客ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<bool>> ValidateBlogPermissionAsync(int blogId, int userId);
}