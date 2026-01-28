using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.Auth;

/// <summary>
/// 获取当前用户信息查询
/// </summary>
public class GetCurrentUserQuery
{
    [Required]
    public int UserId { get; set; }
}