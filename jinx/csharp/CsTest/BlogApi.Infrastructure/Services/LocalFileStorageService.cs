using System.Text.RegularExpressions;
using BlogApi.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// Local file system implementation of file storage service
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly HashSet<string> _allowedExtensions;
    private readonly Dictionary<string, string> _mimeTypes;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        
        // Get configuration values
        _uploadPath = configuration["FileStorage:UploadPath"] ?? "uploads";
        _maxFileSize = long.Parse(configuration["FileStorage:MaxFileSizeBytes"] ?? "10485760"); // 10MB default
        
        var allowedExtensionsConfig = configuration["FileStorage:AllowedExtensions"] ?? 
            ".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf,.doc,.docx,.txt,.md,.zip,.rar";
        _allowedExtensions = allowedExtensionsConfig.Split(',')
            .Select(ext => ext.Trim().ToLowerInvariant())
            .ToHashSet();

        // Initialize MIME type mappings
        _mimeTypes = InitializeMimeTypes();

        // Ensure upload directory exists
        EnsureUploadDirectoryExists();
    }

    /// <summary>
    /// Saves a file to local storage
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">Content type</param>
    /// <returns>Storage path</returns>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (fileStream == null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
        }

        // Validate file type
        if (!IsFileTypeAllowed(fileName, contentType))
        {
            throw new InvalidOperationException($"File type not allowed: {Path.GetExtension(fileName)}");
        }

        // Validate file size
        if (!IsFileSizeAllowed(fileStream.Length))
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size: {FormatFileSize(_maxFileSize)}");
        }

        try
        {
            // Generate unique file name
            var uniqueFileName = GenerateUniqueFileName(fileName);
            var relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), uniqueFileName);
            var fullPath = Path.Combine(_uploadPath, relativePath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file
            using var fileStreamOutput = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamOutput);

            _logger.LogInformation("File saved successfully: {FilePath}", relativePath);
            return relativePath.Replace('\\', '/'); // Normalize path separators
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to save file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a file stream from storage
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>File stream</returns>
    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        var fullPath = Path.Combine(_uploadPath, filePath.Replace('/', '\\'));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // Security check: ensure the file is within the upload directory
        var normalizedUploadPath = Path.GetFullPath(_uploadPath);
        var normalizedFilePath = Path.GetFullPath(fullPath);
        
        if (!normalizedFilePath.StartsWith(normalizedUploadPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access to file outside upload directory is not allowed");
        }

        try
        {
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file stream: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to get file stream: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>True if deleted successfully</returns>
    public async Task<bool> DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var fullPath = Path.Combine(_uploadPath, filePath.Replace('/', '\\'));

        if (!File.Exists(fullPath))
        {
            return false;
        }

        // Security check: ensure the file is within the upload directory
        var normalizedUploadPath = Path.GetFullPath(_uploadPath);
        var normalizedFilePath = Path.GetFullPath(fullPath);
        
        if (!normalizedFilePath.StartsWith(normalizedUploadPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside upload directory: {FilePath}", filePath);
            return false;
        }

        try
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>True if file exists</returns>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var fullPath = Path.Combine(_uploadPath, filePath.Replace('/', '\\'));
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Gets file information
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>File information</returns>
    public async Task<FileStorageInfo?> GetFileInfoAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var fullPath = Path.Combine(_uploadPath, filePath.Replace('/', '\\'));

        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(fullPath);
            return new FileStorageInfo
            {
                FilePath = filePath,
                Size = fileInfo.Length,
                ContentType = GetMimeType(Path.GetFileName(filePath)),
                CreatedAt = fileInfo.CreationTimeUtc,
                LastModifiedAt = fileInfo.LastWriteTimeUtc,
                Exists = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file info: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Validates if file type is allowed
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">Content type</param>
    /// <returns>True if allowed</returns>
    public bool IsFileTypeAllowed(string fileName, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates if file size is within allowed limits
    /// </summary>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>True if allowed</returns>
    public bool IsFileSizeAllowed(long fileSize)
    {
        return fileSize > 0 && fileSize <= _maxFileSize;
    }

    /// <summary>
    /// Generates a unique file name
    /// </summary>
    /// <param name="originalFileName">Original file name</param>
    /// <returns>Unique file name</returns>
    public string GenerateUniqueFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name cannot be null or empty", nameof(originalFileName));
        }

        // Sanitize file name
        var sanitizedName = SanitizeFileName(originalFileName);
        var extension = Path.GetExtension(sanitizedName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedName);

        // Generate unique identifier
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Use first 8 characters
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// Gets MIME type for a file
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>MIME type</returns>
    public string GetMimeType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "application/octet-stream";
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _mimeTypes.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
    }

    /// <summary>
    /// Formats file size for display
    /// </summary>
    /// <param name="bytes">File size in bytes</param>
    /// <returns>Formatted file size string</returns>
    public string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    private void EnsureUploadDirectoryExists()
    {
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation("Created upload directory: {UploadPath}", _uploadPath);
        }
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Remove potentially dangerous patterns
        sanitized = Regex.Replace(sanitized, @"\.{2,}", ".", RegexOptions.IgnoreCase); // Multiple dots
        sanitized = Regex.Replace(sanitized, @"^\.+|\.+$", "", RegexOptions.IgnoreCase); // Leading/trailing dots

        // Ensure reasonable length
        if (sanitized.Length > 100)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension.Substring(0, 100 - extension.Length) + extension;
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private Dictionary<string, string> InitializeMimeTypes()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },

            // Documents
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

            // Text
            { ".txt", "text/plain" },
            { ".md", "text/markdown" },
            { ".html", "text/html" },
            { ".htm", "text/html" },
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },

            // Archives
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" },
            { ".7z", "application/x-7z-compressed" },
            { ".tar", "application/x-tar" },
            { ".gz", "application/gzip" },

            // Audio
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },

            // Video
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" },
            { ".wmv", "video/x-ms-wmv" }
        };
    }
}