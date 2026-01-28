using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.File;

/// <summary>
/// 更新文件可见性命令
/// </summary>
public class UpdateFileVisibilityCommand
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}