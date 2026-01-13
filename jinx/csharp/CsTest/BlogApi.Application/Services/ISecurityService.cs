namespace BlogApi.Application.Services;

/// <summary>
/// 安全服务接口
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// 验证文件内容是否安全
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>验证结果</returns>
    Task<FileSecurityValidationResult> ValidateFileSecurityAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// 检测文件是否包含恶意内容
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <returns>是否包含恶意内容</returns>
    Task<bool> ContainsMaliciousContentAsync(Stream fileStream);

    /// <summary>
    /// 验证图片文件是否安全
    /// </summary>
    /// <param name="imageStream">图片流</param>
    /// <returns>是否安全</returns>
    Task<bool> IsImageSafeAsync(Stream imageStream);

    /// <summary>
    /// 清理文件名，移除不安全的字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>清理后的文件名</returns>
    string SanitizeFileName(string fileName);

    /// <summary>
    /// 生成安全的文件存储路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="userId">用户ID</param>
    /// <returns>安全的存储路径</returns>
    string GenerateSecureFilePath(string fileName, int userId);

    /// <summary>
    /// 验证用户访问权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="action">操作类型</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasPermissionAsync(int userId, int resourceId, string resourceType, string action);
}

/// <summary>
/// 文件安全验证结果
/// </summary>
public class FileSecurityValidationResult
{
    /// <summary>
    /// 是否安全
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告信息列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 检测到的文件类型
    /// </summary>
    public string DetectedContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }
}