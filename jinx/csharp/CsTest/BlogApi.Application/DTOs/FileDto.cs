namespace BlogApi.Application.DTOs;

/// <summary>
/// 文件数据传输对象
/// </summary>
public class FileDto
{
    public int Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string FormattedSize { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsPublic { get; set; }
    public UserSummaryDto Uploader { get; set; } = null!;
}

/// <summary>
/// 文件上传结果DTO
/// </summary>
public class FileUploadResultDto
{
    public int Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string FormattedSize { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsPublic { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

/// <summary>
/// 文件下载结果DTO
/// </summary>
public class FileDownloadResultDto
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}