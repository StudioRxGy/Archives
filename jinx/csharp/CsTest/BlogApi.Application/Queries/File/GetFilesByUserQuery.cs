using BlogApi.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.File;

/// <summary>
/// 根据用户获取文件列表查询
/// </summary>
public class GetFilesByUserQuery : BaseQueryParameters
{
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    public int? RequestUserId { get; set; }

    /// <summary>
    /// 按内容类型过滤
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 按公开/私有状态过滤
    /// </summary>
    public bool? IsPublic { get; set; }
}