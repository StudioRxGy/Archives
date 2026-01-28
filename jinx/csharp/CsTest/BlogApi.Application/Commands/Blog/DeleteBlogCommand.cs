using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Blog;

/// <summary>
/// 删除博客命令
/// </summary>
public class DeleteBlogCommand
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}