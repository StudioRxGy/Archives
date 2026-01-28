using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.User;

/// <summary>
/// 根据用户名获取用户查询
/// </summary>
public class GetUserByUsernameQuery
{
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    public int? RequestUserId { get; set; }
}