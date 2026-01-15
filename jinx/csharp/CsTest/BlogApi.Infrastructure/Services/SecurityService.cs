using BlogApi.Application.Services;
using System.Text;
using System.Text.RegularExpressions;

namespace BlogApi.Infrastructure.Services;

/// <summary>
/// 安全服务实现
/// </summary>
public class SecurityService : ISecurityService
{
    // 文件魔数签名，用于检测真实文件类型
    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        { "image/jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { "image/png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { "image/gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } } },
        { "application/pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { "application/zip", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 } } },
        { "text/plain", new[] { Array.Empty<byte>() } } // 文本文件没有固定签名
    };

    // 恶意内容模式
    private static readonly string[] MaliciousPatterns = new[]
    {
        @"<script[^>]*>.*?</script>",
        @"javascript:",
        @"vbscript:",
        @"data:text/html",
        @"eval\s*\(",
        @"document\.write",
        @"window\.location",
        @"<iframe[^>]*>",
        @"<object[^>]*>",
        @"<embed[^>]*>",
        @"onload\s*=",
        @"onclick\s*=",
        @"onerror\s*="
    };

    // 危险文件扩展名
    private static readonly HashSet<string> DangerousExtensions = new()
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".app", ".deb", ".pkg", ".dmg",
        ".asp", ".aspx", ".php", ".jsp", ".py", ".rb", ".pl", ".sh", ".ps1", ".psm1"
    };

    public async Task<FileSecurityValidationResult> ValidateFileSecurityAsync(Stream fileStream, string fileName, string contentType)
    {
        var result = new FileSecurityValidationResult
        {
            IsSafe = true,
            FileSize = fileStream.Length
        };

        try
        {
            // 验证文件扩展名
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (DangerousExtensions.Contains(extension))
            {
                result.IsSafe = false;
                result.Errors.Add($"危险的文件扩展名: {extension}");
            }

            // 验证文件大小
            if (fileStream.Length == 0)
            {
                result.IsSafe = false;
                result.Errors.Add("文件为空");
            }
            else if (fileStream.Length > 10 * 1024 * 1024) // 10MB
            {
                result.IsSafe = false;
                result.Errors.Add("文件大小超过限制");
            }

            // 检测真实文件类型
            var detectedType = await DetectFileTypeAsync(fileStream);
            result.DetectedContentType = detectedType;

            // 验证声明的内容类型与实际类型是否匹配
            if (!string.IsNullOrEmpty(detectedType) && !IsContentTypeMatch(contentType, detectedType))
            {
                result.Warnings.Add($"声明的内容类型 ({contentType}) 与检测到的类型 ({detectedType}) 不匹配");
            }

            // 检测恶意内容
            if (await ContainsMaliciousContentAsync(fileStream))
            {
                result.IsSafe = false;
                result.Errors.Add("文件包含恶意内容");
            }

            // 如果是图片文件，进行额外的图片安全检查
            if (contentType.StartsWith("image/"))
            {
                if (!await IsImageSafeAsync(fileStream))
                {
                    result.IsSafe = false;
                    result.Errors.Add("图片文件包含不安全的内容");
                }
            }
        }
        catch (Exception ex)
        {
            result.IsSafe = false;
            result.Errors.Add($"文件安全验证过程中发生错误: {ex.Message}");
        }

        return result;
    }

    public async Task<bool> ContainsMaliciousContentAsync(Stream fileStream)
    {
        try
        {
            fileStream.Position = 0;
            
            // 读取文件内容的前几KB进行检查
            var buffer = new byte[Math.Min(fileStream.Length, 8192)];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);
            
            // 将字节转换为字符串进行模式匹配
            var content = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            
            // 检查恶意模式
            foreach (var pattern in MaliciousPatterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            // 检查二进制文件中的可疑字符串
            if (content.Contains("eval(") || content.Contains("document.write") || content.Contains("<script"))
            {
                return true;
            }

            return false;
        }
        catch
        {
            // 如果检查过程中出错，为了安全起见返回true
            return true;
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    public async Task<bool> IsImageSafeAsync(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            
            // 读取图片文件的前几KB
            var buffer = new byte[Math.Min(imageStream.Length, 4096)];
            await imageStream.ReadAsync(buffer, 0, buffer.Length);
            
            // 检查是否包含脚本标签或其他恶意内容
            var content = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            
            // 图片文件不应该包含HTML或脚本内容
            var dangerousPatterns = new[]
            {
                "<script", "</script>", "javascript:", "vbscript:", 
                "<iframe", "<object", "<embed", "eval(", "document."
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            imageStream.Position = 0;
        }
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unnamed_file";

        // 移除路径分隔符和其他危险字符
        var sanitized = fileName;
        var dangerousChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0' };
        
        foreach (var c in dangerousChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        // 移除控制字符
        sanitized = Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "_");

        // 移除连续的点（防止路径遍历）
        sanitized = Regex.Replace(sanitized, @"\.{2,}", ".");

        // 确保文件名不以点开头或结尾
        sanitized = sanitized.Trim('.');

        // 限制文件名长度
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, 200 - extension.Length) + extension;
        }

        // 如果清理后文件名为空，使用默认名称
        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = "unnamed_file";
        }

        return sanitized;
    }

    public string GenerateSecureFilePath(string fileName, int userId)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var randomSuffix = Guid.NewGuid().ToString("N")[..8];
        
        // 创建基于用户ID的子目录
        var userDirectory = $"user_{userId}";
        var dateDirectory = DateTimeOffset.UtcNow.ToString("yyyy/MM");
        
        // 生成唯一的文件名
        var extension = Path.GetExtension(sanitizedFileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedFileName);
        var uniqueFileName = $"{nameWithoutExt}_{timestamp}_{randomSuffix}{extension}";
        
        return Path.Combine(userDirectory, dateDirectory, uniqueFileName);
    }

    public async Task<bool> HasPermissionAsync(int userId, int resourceId, string resourceType, string action)
    {
        // 这里可以实现复杂的权限检查逻辑
        // 目前简化实现，只检查用户是否是资源的所有者
        
        try
        {
            // 根据资源类型进行不同的权限检查
            return resourceType.ToLowerInvariant() switch
            {
                "blog" => await CheckBlogPermissionAsync(userId, resourceId, action),
                "file" => await CheckFilePermissionAsync(userId, resourceId, action),
                "user" => await CheckUserPermissionAsync(userId, resourceId, action),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> DetectFileTypeAsync(Stream fileStream)
    {
        try
        {
            fileStream.Position = 0;
            var buffer = new byte[8];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);
            
            foreach (var kvp in FileSignatures)
            {
                foreach (var signature in kvp.Value)
                {
                    if (signature.Length == 0) continue; // 跳过空签名
                    
                    if (buffer.Take(signature.Length).SequenceEqual(signature))
                    {
                        return kvp.Key;
                    }
                }
            }
            
            return "application/octet-stream";
        }
        catch
        {
            return "application/octet-stream";
        }
        finally
        {
            fileStream.Position = 0;
        }
    }

    private bool IsContentTypeMatch(string declaredType, string detectedType)
    {
        if (string.IsNullOrEmpty(declaredType) || string.IsNullOrEmpty(detectedType))
            return false;

        // 完全匹配
        if (declaredType.Equals(detectedType, StringComparison.OrdinalIgnoreCase))
            return true;

        // 检查主类型是否匹配（如 image/* 匹配 image/jpeg）
        var declaredMainType = declaredType.Split('/')[0];
        var detectedMainType = detectedType.Split('/')[0];
        
        return declaredMainType.Equals(detectedMainType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> CheckBlogPermissionAsync(int userId, int blogId, string action)
    {
        // 这里应该查询数据库检查博客的所有者
        // 简化实现，实际应该注入仓储服务
        await Task.Delay(1); // 模拟异步操作
        return true; // 简化返回true，实际应该检查数据库
    }

    private async Task<bool> CheckFilePermissionAsync(int userId, int fileId, string action)
    {
        // 这里应该查询数据库检查文件的所有者
        // 简化实现，实际应该注入仓储服务
        await Task.Delay(1); // 模拟异步操作
        return true; // 简化返回true，实际应该检查数据库
    }

    private async Task<bool> CheckUserPermissionAsync(int userId, int targetUserId, string action)
    {
        // 用户只能操作自己的资源
        await Task.Delay(1); // 模拟异步操作
        return userId == targetUserId;
    }
}