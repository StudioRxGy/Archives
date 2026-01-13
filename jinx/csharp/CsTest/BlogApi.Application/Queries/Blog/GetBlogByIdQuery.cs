using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.Blog;

/// <summary>
/// 根据ID获取博客查询
/// </summary>
public class GetBlogByIdQuery
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证，可选）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 是否包含未发布的博客（仅作者可见）
    /// </summary>
    public bool IncludeUnpublished { get; set; } = false;
}