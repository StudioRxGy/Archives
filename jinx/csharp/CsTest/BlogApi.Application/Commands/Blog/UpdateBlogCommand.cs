using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Blog;

/// <summary>
/// 更新博客命令
/// </summary>
public class UpdateBlogCommand
{
    [Required]
    public int Id { get; set; }

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
    /// 是否发布
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}