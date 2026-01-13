using BlogApi.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.Blog;

/// <summary>
/// 根据作者获取博客列表查询
/// </summary>
public class GetBlogsByAuthorQuery : BaseQueryParameters
{
    [Required]
    public int AuthorId { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 是否只返回已发布的博客
    /// </summary>
    public bool OnlyPublished { get; set; } = true;

    /// <summary>
    /// 按发布状态过滤
    /// </summary>
    public bool? IsPublished { get; set; }
}