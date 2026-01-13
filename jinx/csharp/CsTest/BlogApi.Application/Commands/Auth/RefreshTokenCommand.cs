using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.Auth;

/// <summary>
/// 刷新令牌命令
/// </summary>
public class RefreshTokenCommand
{
    [Required(ErrorMessage = "访问令牌不能为空")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}