using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.File;

/// <summary>
/// 删除文件命令
/// </summary>
public class DeleteFileCommand
{
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 请求用户ID（用于权限验证）
    /// </summary>
    [Required]
    public int UserId { get; set; }
}