using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Blog;

/// <summary>
/// 发布博客命令
/// </summary>
public class PublishBlogCommand
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}

/// <summary>
/// 取消发布博客命令
/// </summary>
public class UnpublishBlogCommand
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}