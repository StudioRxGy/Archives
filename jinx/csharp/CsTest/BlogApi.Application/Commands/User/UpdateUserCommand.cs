using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.User;

/// <summary>
/// 更新用户信息命令
/// </summary>
public class UpdateUserCommand
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int RequestUserId { get; set; }
}