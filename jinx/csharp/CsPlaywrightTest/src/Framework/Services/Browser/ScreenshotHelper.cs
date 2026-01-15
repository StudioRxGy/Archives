using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Services.Browser;

/// <summary>
/// 截图辅助工具类
/// </summary>
public static class ScreenshotHelper
{
    /// <summary>
    /// 生成截图文件名
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="browserType">浏览器类型</param>
    /// <param name="timestamp">时间戳</param>
    /// <returns>截图文件名</returns>
    public static string GenerateFileName(string testName, string browserType, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("测试名称不能为空", nameof(testName));
        }

        if (string.IsNullOrWhiteSpace(browserType))
        {
            throw new ArgumentException("浏览器类型不能为空", nameof(browserType));
        }

        var time = timestamp ?? DateTime.Now;
        var sanitizedTestName = SanitizeFileName(testName);
        var timeString = time.ToString("yyyyMMdd_HHmmss_fff");
        
        return $"{sanitizedTestName}_{browserType}_{timeString}.png";
    }

    /// <summary>
    /// 生成截图文件路径
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="browserType">浏览器类型</param>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="timestamp">时间戳</param>
    /// <returns>截图文件完整路径</returns>
    public static string GenerateFilePath(string testName, string browserType, string outputDirectory, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory = "Screenshots";
        }

        var fileName = GenerateFileName(testName, browserType, timestamp);
        return Path.Combine(outputDirectory, fileName);
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>清理后的文件名</returns>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unknown";
        }

        // 获取非法字符
        var invalidChars = Path.GetInvalidFileNameChars();
        
        // 替换非法字符为下划线
        var sanitized = fileName;
        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        // 替换空格为下划线
        sanitized = sanitized.Replace(' ', '_');
        
        // 限制长度
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized;
    }

    /// <summary>
    /// 确保截图目录存在
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// 清理旧的截图文件
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <param name="daysToKeep">保留天数</param>
    /// <returns>删除的文件数量</returns>
    public static int CleanupOldScreenshots(string directoryPath, int daysToKeep = 7)
    {
        if (daysToKeep < 0)
        {
            throw new ArgumentException("保留天数不能为负数", nameof(daysToKeep));
        }

        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return 0;
        }

        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        var files = Directory.GetFiles(directoryPath, "*.png");
        var deletedCount = 0;

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTime < cutoffDate)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch
                {
                    // 忽略删除失败的文件
                }
            }
        }

        return deletedCount;
    }
}