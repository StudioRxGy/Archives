using System.ComponentModel.DataAnnotations;

namespace BlogApi.Application.Commands.File;

/// <summary>
/// 文件上传命令
/// </summary>
public class UploadFileCommand
{
    [Required(ErrorMessage = "文件流不能为空")]
    public Stream FileStream { get; set; } = null!;

    [Required(ErrorMessage = "文件名不能为空")]
    [StringLength(255, ErrorMessage = "文件名长度不能超过255个字符")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "内容类型不能为空")]
    [StringLength(100, ErrorMessage = "内容类型长度不能超过100个字符")]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "文件大小必须大于0")]
    public long Size { get; set; }

    /// <summary>
    /// 是否公开文件（默认为私有）
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 上传者用户ID
    /// </summary>
    [Required]
    public int UploadedBy { get; set; }
}