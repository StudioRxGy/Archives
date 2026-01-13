using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Auth;

/// <summary>
/// 用户登录命令
/// </summary>
public class LoginCommand
{
    [Required(ErrorMessage = "邮箱或用户名不能为空")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 是否记住登录状态（影响令牌过期时间）
    /// </summary>
    public bool RememberMe { get; set; } = false;
}