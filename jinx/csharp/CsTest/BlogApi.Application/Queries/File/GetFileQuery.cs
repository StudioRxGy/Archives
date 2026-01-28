using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Queries.File;

/// <summary>
/// 获取文件查询
/// </summary>
public class GetFileQuery
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证，可选）
    /// </summary>
    public int? UserId { get; set; }
}