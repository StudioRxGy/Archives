using BlogApi.Domain.Common;

namespace BlogApi.Application.Queries.Blog;

/// <summary>
/// 搜索博客查询
/// </summary>
public class SearchBlogsQuery : BaseQueryParameters
{
    /// <summary>
    /// 搜索关键词（在标题、摘要、内容中搜索）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 标签过滤
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 作者用户名过滤
    /// </summary>
    public string? AuthorUsername { get; set; }

    /// <summary>
    /// 创建日期范围过滤（开始日期）
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// 创建日期范围过滤（结束日期）
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public BlogSortField SortBy { get; set; } = BlogSortField.CreatedAt;

    /// <summary>
    /// 排序方向
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// 请求用户ID（用于权限过滤）
    /// </summary>
    public int? UserId { get; set; }
}

/// <summary>
/// 博客排序字段
/// </summary>
public enum BlogSortField
{
    CreatedAt,
    UpdatedAt,
    Title,
    AuthorUsername
}

/// <summary>
/// 排序方向
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}