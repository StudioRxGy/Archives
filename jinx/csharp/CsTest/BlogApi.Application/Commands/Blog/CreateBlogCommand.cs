using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Blog;

/// <summary>
/// 创建博客命令
/// </summary>
public class CreateBlogCommand
{
    [Required(ErrorMessage = "标题不能为空")]
    [StringLength(200, ErrorMessage = "标题长度不能超过200个字符")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "内容不能为空")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "摘要长度不能超过500个字符")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 是否发布（默认为草稿）
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// 作者ID（通常从认证上下文获取）
    /// </summary>
    [Required]
    public int AuthorId { get; set; }
}