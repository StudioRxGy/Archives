using BlogApi.Domain.Common;

namespace BlogApi.Application.Queries.Blog;

/// <summary>
/// 获取博客列表查询
/// </summary>
public class GetBlogsQuery : BlogQueryParameters
{
    /// <summary>
    /// 请求用户ID（用于权限过滤，可选）
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 是否只返回已发布的博客（对于非作者用户）
    /// </summary>
    public bool OnlyPublished { get; set; } = true;
}