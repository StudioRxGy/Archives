using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Auth;

/// <summary>
/// 用户注册命令
/// </summary>
public class RegisterCommand
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度必须在8-100个字符之间")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare(nameof(Password), ErrorMessage = "密码和确认密码不匹配")]
    public string ConfirmPassword { get; set; } = string.Empty;
}