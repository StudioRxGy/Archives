using System.ComponentModel.DataAnnotations;

namespace BlogApi.Domain.Entities;

public class FileEntity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string StoredName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public long Size { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; }
    
    [Required]
    public int UploadedBy { get; set; }
    
    public bool IsPublic { get; set; } = false;
    
    // Navigation property
    public User Uploader { get; set; } = null!;
    
    // Business rules and methods
    public bool CanBeAccessedBy(int? userId)
    {
        // Public files can be accessed by anyone
        if (IsPublic)
            return true;
        
        // Private files can only be accessed by the uploader
        return userId.HasValue && UploadedBy == userId.Value;
    }
    
    public bool CanBeDeletedBy(int userId)
    {
        return UploadedBy == userId;
    }
    
    public void MakePublic()
    {
        IsPublic = true;
    }
    
    public void MakePrivate()
    {
        IsPublic = false;
    }
    
    public bool IsImage()
    {
        return ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
    
    public bool IsDocument()
    {
        var documentTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain"
        };
        
        return documentTypes.Contains(ContentType, StringComparer.OrdinalIgnoreCase);
    }
    
    public string GetFileExtension()
    {
        return Path.GetExtension(OriginalName).ToLowerInvariant();
    }
    
    public string GetFormattedSize()
    {
        const int byteConversion = 1024;
        double bytes = Size;
        
        if (bytes >= Math.Pow(byteConversion, 3))
            return $"{bytes / Math.Pow(byteConversion, 3):F2} GB";
        
        if (bytes >= Math.Pow(byteConversion, 2))
            return $"{bytes / Math.Pow(byteConversion, 2):F2} MB";
        
        if (bytes >= byteConversion)
            return $"{bytes / byteConversion:F2} KB";
        
        return $"{bytes} bytes";
    }
    
    public static bool IsAllowedFileType(string contentType)
    {
        var allowedTypes = new[]
        {
            // Images
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
            // Documents
            "application/pdf", "text/plain", "text/markdown",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        
        return allowedTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }
    
    public static bool IsValidFileSize(long size, long maxSizeInBytes = 10 * 1024 * 1024) // Default 10MB
    {
        return size > 0 && size <= maxSizeInBytes;
    }
}