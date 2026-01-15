using System.Reflection;

namespace CsPlaywrightXun.src.playwright.Core.Configuration;

/// <summary>
/// 路径配置类 - 集中管理所有文件路径
/// 实现基于项目根目录的相对路径计算和目录自动创建功能
/// </summary>
public static class PathConfiguration
{
    private static readonly object _lock = new();
    private static string? _baseDirectory;
    private static bool _isInitialized = false;

    /// <summary>
    /// 项目根目录
    /// 基于当前程序集的位置计算项目根目录
    /// </summary>
    public static string BaseDirectory
    {
        get
        {
            if (_baseDirectory == null)
            {
                lock (_lock)
                {
                    if (_baseDirectory == null)
                    {
                        _baseDirectory = CalculateBaseDirectory();
                    }
                }
            }
            return _baseDirectory;
        }
    }

    /// <summary>
    /// 配置文件根目录
    /// </summary>
    public static string ConfigDirectory => Path.Combine(BaseDirectory, "src", "config");

    /// <summary>
    /// 环境配置目录
    /// </summary>
    public static string EnvironmentsDirectory => Path.Combine(ConfigDirectory, "environments");

    /// <summary>
    /// 页面元素配置目录
    /// </summary>
    public static string ElementsDirectory => Path.Combine(ConfigDirectory, "elements");

    /// <summary>
    /// 测试数据目录
    /// </summary>
    public static string TestDataDirectory => Path.Combine(ConfigDirectory, "date");

    /// <summary>
    /// 报告输出目录
    /// </summary>
    public static string ReportsDirectory => Path.Combine(BaseDirectory, "src", "output", "reports");

    /// <summary>
    /// 日志文件目录
    /// </summary>
    public static string LogsDirectory => Path.Combine(BaseDirectory, "src", "output", "logs");

    /// <summary>
    /// 截图文件目录
    /// </summary>
    public static string ScreenshotsDirectory => Path.Combine(BaseDirectory, "src", "output", "screenshots");

    /// <summary>
    /// 输出文件目录
    /// </summary>
    public static string OutputDirectory => Path.Combine(BaseDirectory, "src", "output");

    /// <summary>
    /// 获取环境配置文件路径
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <returns>配置文件完整路径</returns>
    /// <exception cref="ArgumentException">当环境名称为空或无效时抛出</exception>
    public static string GetEnvironmentConfigPath(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("环境名称不能为空", nameof(environment));
        }

        ValidateEnvironmentName(environment);
        return Path.Combine(EnvironmentsDirectory, $"appsettings.{environment}.json");
    }

    /// <summary>
    /// 获取页面元素配置文件路径
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <returns>元素配置文件完整路径</returns>
    /// <exception cref="ArgumentException">当页面名称为空时抛出</exception>
    public static string GetElementsConfigPath(string pageName)
    {
        if (string.IsNullOrWhiteSpace(pageName))
        {
            throw new ArgumentException("页面名称不能为空", nameof(pageName));
        }

        ValidateFileName(pageName);
        return Path.Combine(ElementsDirectory, $"{pageName}.yaml");
    }

    /// <summary>
    /// 获取测试数据文件路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="subDirectory">子目录（可选）</param>
    /// <returns>测试数据文件完整路径</returns>
    /// <exception cref="ArgumentException">当文件名为空时抛出</exception>
    public static string GetTestDataPath(string fileName, string? subDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        ValidateFileName(fileName);

        var basePath = TestDataDirectory;
        if (!string.IsNullOrWhiteSpace(subDirectory))
        {
            ValidateDirectoryName(subDirectory);
            basePath = Path.Combine(basePath, subDirectory);
        }

        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    /// <param name="logFileName">日志文件名（可选，默认使用当前日期）</param>
    /// <returns>日志文件完整路径</returns>
    public static string GetLogPath(string? logFileName = null)
    {
        var fileName = logFileName ?? $"test-{DateTime.Now:yyyy-MM-dd}.log";
        
        if (!string.IsNullOrWhiteSpace(logFileName))
        {
            ValidateFileName(logFileName);
        }

        return Path.Combine(LogsDirectory, fileName);
    }

    /// <summary>
    /// 获取报告文件路径
    /// </summary>
    /// <param name="reportFileName">报告文件名（可选，默认使用当前时间戳）</param>
    /// <returns>报告文件完整路径</returns>
    public static string GetReportPath(string? reportFileName = null)
    {
        var fileName = reportFileName ?? $"test-report-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.html";
        
        if (!string.IsNullOrWhiteSpace(reportFileName))
        {
            ValidateFileName(reportFileName);
        }

        return Path.Combine(ReportsDirectory, fileName);
    }

    /// <summary>
    /// 获取截图文件路径
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="browserType">浏览器类型（默认为chromium）</param>
    /// <returns>截图文件完整路径</returns>
    /// <exception cref="ArgumentException">当测试名称为空时抛出</exception>
    public static string GetScreenshotPath(string testName, string browserType = "chromium")
    {
        if (string.IsNullOrWhiteSpace(testName))
        {
            throw new ArgumentException("测试名称不能为空", nameof(testName));
        }

        if (string.IsNullOrWhiteSpace(browserType))
        {
            browserType = "chromium";
        }

        // 清理文件名中的无效字符
        var cleanTestName = CleanFileName(testName);
        var cleanBrowserType = CleanFileName(browserType);
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var fileName = $"{cleanTestName}_{cleanBrowserType}_{timestamp}.png";
        
        return Path.Combine(ScreenshotsDirectory, fileName);
    }

    /// <summary>
    /// 获取输出文件路径
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="subDirectory">子目录（可选）</param>
    /// <returns>输出文件完整路径</returns>
    /// <exception cref="ArgumentException">当文件名为空时抛出</exception>
    public static string GetOutputPath(string fileName, string? subDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("文件名不能为空", nameof(fileName));
        }

        ValidateFileName(fileName);

        var basePath = OutputDirectory;
        if (!string.IsNullOrWhiteSpace(subDirectory))
        {
            ValidateDirectoryName(subDirectory);
            basePath = Path.Combine(basePath, subDirectory);
        }

        return Path.Combine(basePath, fileName);
    }

    /// <summary>
    /// 确保目录存在，如果不存在则创建
    /// </summary>
    /// <param name="directoryPath">目录路径</param>
    /// <exception cref="ArgumentException">当目录路径为空时抛出</exception>
    /// <exception cref="UnauthorizedAccessException">当没有权限创建目录时抛出</exception>
    /// <exception cref="DirectoryNotFoundException">当父目录不存在且无法创建时抛出</exception>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("目录路径不能为空", nameof(directoryPath));
        }

        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"没有权限创建目录: {directoryPath}");
        }
        catch (DirectoryNotFoundException ex)
        {
            throw new DirectoryNotFoundException($"无法创建目录，父目录不存在: {directoryPath}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"创建目录时发生错误: {directoryPath}", ex);
        }
    }

    /// <summary>
    /// 初始化所有必要的目录
    /// </summary>
    /// <exception cref="InvalidOperationException">当初始化过程中发生错误时抛出</exception>
    public static void InitializeDirectories()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                var directories = new[]
                {
                    ConfigDirectory,
                    EnvironmentsDirectory,
                    ElementsDirectory,
                    TestDataDirectory,
                    ReportsDirectory,
                    LogsDirectory,
                    ScreenshotsDirectory,
                    OutputDirectory
                };

                foreach (var directory in directories)
                {
                    EnsureDirectoryExists(directory);
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("初始化目录时发生错误", ex);
            }
        }
    }

    /// <summary>
    /// 验证路径是否存在
    /// </summary>
    /// <param name="path">要验证的路径</param>
    /// <param name="isDirectory">是否为目录路径</param>
    /// <returns>路径是否存在</returns>
    public static bool ValidatePath(string path, bool isDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            return isDirectory ? Directory.Exists(path) : File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取相对于基础目录的相对路径
    /// </summary>
    /// <param name="fullPath">完整路径</param>
    /// <returns>相对路径</returns>
    /// <exception cref="ArgumentException">当路径为空时抛出</exception>
    public static string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new ArgumentException("路径不能为空", nameof(fullPath));
        }

        try
        {
            return Path.GetRelativePath(BaseDirectory, fullPath);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"无法计算相对路径: {fullPath}", nameof(fullPath), ex);
        }
    }

    /// <summary>
    /// 获取所有已配置的目录路径
    /// </summary>
    /// <returns>目录路径字典</returns>
    public static Dictionary<string, string> GetAllDirectories()
    {
        return new Dictionary<string, string>
        {
            { "Base", BaseDirectory },
            { "Config", ConfigDirectory },
            { "Environments", EnvironmentsDirectory },
            { "Elements", ElementsDirectory },
            { "TestData", TestDataDirectory },
            { "Reports", ReportsDirectory },
            { "Logs", LogsDirectory },
            { "Screenshots", ScreenshotsDirectory },
            { "Output", OutputDirectory }
        };
    }

    /// <summary>
    /// 重置路径配置（主要用于测试）
    /// </summary>
    internal static void Reset()
    {
        lock (_lock)
        {
            _baseDirectory = null;
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 设置自定义基础目录（主要用于测试）
    /// </summary>
    /// <param name="customBaseDirectory">自定义基础目录</param>
    internal static void SetCustomBaseDirectory(string customBaseDirectory)
    {
        if (string.IsNullOrWhiteSpace(customBaseDirectory))
        {
            throw new ArgumentException("自定义基础目录不能为空", nameof(customBaseDirectory));
        }

        lock (_lock)
        {
            _baseDirectory = Path.GetFullPath(customBaseDirectory);
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 计算项目根目录
    /// </summary>
    /// <returns>项目根目录路径</returns>
    private static string CalculateBaseDirectory()
    {
        // 获取当前程序集的位置
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrEmpty(assemblyDirectory))
        {
            // 如果无法获取程序集目录，使用当前工作目录
            return Directory.GetCurrentDirectory();
        }

        // 向上查找项目根目录（包含.csproj文件的目录）
        var currentDirectory = new DirectoryInfo(assemblyDirectory);
        
        while (currentDirectory != null)
        {
            // 查找.csproj文件
            if (currentDirectory.GetFiles("*.csproj").Any() || 
                currentDirectory.GetFiles("*.sln").Any() ||
                currentDirectory.Name.Equals("CsPlaywrightXun", StringComparison.OrdinalIgnoreCase))
            {
                return currentDirectory.FullName;
            }
            
            currentDirectory = currentDirectory.Parent;
        }

        // 如果找不到项目根目录，使用程序集所在目录
        return assemblyDirectory;
    }

    /// <summary>
    /// 验证环境名称是否有效
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <exception cref="ArgumentException">当环境名称无效时抛出</exception>
    private static void ValidateEnvironmentName(string environment)
    {
        var validEnvironments = new[] { "Development", "Test", "Staging", "Production" };
        if (!validEnvironments.Contains(environment, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"无效的环境名称: {environment}。有效值: {string.Join(", ", validEnvironments)}", nameof(environment));
        }
    }

    /// <summary>
    /// 验证文件名是否有效
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <exception cref="ArgumentException">当文件名包含无效字符时抛出</exception>
    private static void ValidateFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException($"文件名包含无效字符: {fileName}", nameof(fileName));
        }
    }

    /// <summary>
    /// 验证目录名是否有效
    /// </summary>
    /// <param name="directoryName">目录名</param>
    /// <exception cref="ArgumentException">当目录名包含无效字符时抛出</exception>
    private static void ValidateDirectoryName(string directoryName)
    {
        var invalidChars = Path.GetInvalidPathChars();
        if (directoryName.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException($"目录名包含无效字符: {directoryName}", nameof(directoryName));
        }
    }

    /// <summary>
    /// 清理文件名中的无效字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>清理后的文件名</returns>
    private static string CleanFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = fileName;
        
        foreach (var invalidChar in invalidChars)
        {
            cleanName = cleanName.Replace(invalidChar, '_');
        }
        
        // 替换一些常见的特殊字符
        cleanName = cleanName.Replace(' ', '_')
                            .Replace(':', '_')
                            .Replace('/', '_')
                            .Replace('\\', '_');
        
        return cleanName;
    }
}