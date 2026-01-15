using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.User;

/// <summary>
/// 根据ID获取用户查询
/// </summary>
public class GetUserByIdQuery
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    public int? RequestUserId { get; set; }
}