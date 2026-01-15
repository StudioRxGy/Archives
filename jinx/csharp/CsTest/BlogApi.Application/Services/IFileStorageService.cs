namespace BlogApi.Application.Services;

/// <summary>
/// 文件存储服务接口
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 保存文件
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>存储路径</returns>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// 获取文件流
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件流</returns>
    Task<Stream> GetFileStreamAsync(string filePath);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>删除是否成功</returns>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件是否存在</returns>
    Task<bool> FileExistsAsync(string filePath);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件信息</returns>
    Task<FileStorageInfo?> GetFileInfoAsync(string filePath);

    /// <summary>
    /// 验证文件类型是否被允许
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>验证结果</returns>
    bool IsFileTypeAllowed(string fileName, string contentType);

    /// <summary>
    /// 验证文件大小是否在允许范围内
    /// </summary>
    /// <param name="fileSize">文件大小（字节）</param>
    /// <returns>验证结果</returns>
    bool IsFileSizeAllowed(long fileSize);

    /// <summary>
    /// 生成唯一的文件名
    /// </summary>
    /// <param name="originalFileName">原始文件名</param>
    /// <returns>唯一文件名</returns>
    string GenerateUniqueFileName(string originalFileName);

    /// <summary>
    /// 获取文件的MIME类型
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>MIME类型</returns>
    string GetMimeType(string fileName);

    /// <summary>
    /// 格式化文件大小显示
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化的文件大小字符串</returns>
    string FormatFileSize(long bytes);
}

/// <summary>
/// 文件存储信息
/// </summary>
public class FileStorageInfo
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// 是否存在
    /// </summary>
    public bool Exists { get; set; }
}