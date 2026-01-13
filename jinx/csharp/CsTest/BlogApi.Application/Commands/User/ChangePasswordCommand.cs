using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.User;

/// <summary>
/// 修改密码命令
/// </summary>
public class ChangePasswordCommand
{
    [Required]
    public int UserId { get; set; }

    [Required(ErrorMessage = "当前密码不能为空")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "新密码长度必须在8-100个字符之间")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认新密码不能为空")]
    [Compare(nameof(NewPassword), ErrorMessage = "新密码和确认密码不匹配")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}