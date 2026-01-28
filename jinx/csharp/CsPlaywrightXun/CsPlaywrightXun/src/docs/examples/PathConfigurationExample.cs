using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Services.Data;

namespace CsPlaywrightXun.src.docs.examples;

/// <summary>
/// PathConfiguration 使用示例
/// 演示如何在实际项目中使用 PathConfiguration 进行路径管理
/// </summary>
public class PathConfigurationExample
{
    private readonly ILogger<PathConfigurationExample> _logger;

    public PathConfigurationExample(ILogger<PathConfigurationExample> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 示例1：基础路径获取和使用
    /// </summary>
    public void BasicPathUsageExample()
    {
        Console.WriteLine("=== PathConfiguration 基础使用示例 ===");

        // 获取基础目录信息
        Console.WriteLine($"项目根目录: {PathConfiguration.BaseDirectory}");
        Console.WriteLine($"配置目录: {PathConfiguration.ConfigDirectory}");
        Console.WriteLine($"测试数据目录: {PathConfiguration.TestDataDirectory}");
        Console.WriteLine($"报告目录: {PathConfiguration.ReportsDirectory}");
        Console.WriteLine($"日志目录: {PathConfiguration.LogsDirectory}");
        Console.WriteLine($"截图目录: {PathConfiguration.ScreenshotsDirectory}");

        // 获取特定文件路径
        var devConfigPath = PathConfiguration.GetEnvironmentConfigPath("Development");
        var testConfigPath = PathConfiguration.GetEnvironmentConfigPath("Test");
        var prodConfigPath = PathConfiguration.GetEnvironmentConfigPath("Production");

        Console.WriteLine($"\n环境配置文件路径:");
        Console.WriteLine($"  开发环境: {devConfigPath}");
        Console.WriteLine($"  测试环境: {testConfigPath}");
        Console.WriteLine($"  生产环境: {prodConfigPath}");

        // 获取测试数据文件路径
        var csvDataPath = PathConfiguration.GetTestDataPath("users.csv");
        var jsonDataPath = PathConfiguration.GetTestDataPath("api_test_data.json", "API");
        var uiDataPath = PathConfiguration.GetTestDataPath("search_data.csv", "UI");

        Console.WriteLine($"\n测试数据文件路径:");
        Console.WriteLine($"  用户数据: {csvDataPath}");
        Console.WriteLine($"  API测试数据: {jsonDataPath}");
        Console.WriteLine($"  UI测试数据: {uiDataPath}");

        // 获取页面元素配置路径
        var homePageElementsPath = PathConfiguration.GetElementsConfigPath("HomePage");
        var loginPageElementsPath = PathConfiguration.GetElementsConfigPath("LoginPage");

        Console.WriteLine($"\n页面元素配置路径:");
        Console.WriteLine($"  首页元素: {homePageElementsPath}");
        Console.WriteLine($"  登录页元素: {loginPageElementsPath}");
    }

    /// <summary>
    /// 示例2：目录管理和初始化
    /// </summary>
    public void DirectoryManagementExample()
    {
        Console.WriteLine("\n=== 目录管理示例 ===");

        try
        {
            // 初始化所有必要的目录
            Console.WriteLine("正在初始化目录结构...");
            PathConfiguration.InitializeDirectories();
            Console.WriteLine("目录初始化完成");

            // 验证目录是否存在
            var directories = PathConfiguration.GetAllDirectories();
            Console.WriteLine("\n目录状态检查:");
            foreach (var kvp in directories)
            {
                var exists = Directory.Exists(kvp.Value);
                var status = exists ? "✓ 存在" : "✗ 不存在";
                Console.WriteLine($"  {kvp.Key}: {status} - {kvp.Value}");
            }

            // 创建自定义目录
            var customDir = Path.Combine(PathConfiguration.BaseDirectory, "custom", "temp");
            Console.WriteLine($"\n创建自定义目录: {customDir}");
            PathConfiguration.EnsureDirectoryExists(customDir);
            
            var customDirExists = Directory.Exists(customDir);
            Console.WriteLine($"自定义目录创建状态: {(customDirExists ? "成功" : "失败")}");

            // 清理自定义目录
            if (customDirExists)
            {
                Directory.Delete(customDir, true);
                Console.WriteLine("自定义目录已清理");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"目录管理过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例3：文件操作集成
    /// </summary>
    public async Task FileOperationIntegrationExample()
    {
        Console.WriteLine("\n=== 文件操作集成示例 ===");

        try
        {
            // 确保目录存在
            PathConfiguration.InitializeDirectories();

            // 示例：创建和读取配置文件
            await CreateSampleConfigurationFile();
            await ReadSampleConfigurationFile();

            // 示例：创建和读取测试数据文件
            await CreateSampleTestDataFile();
            await ReadSampleTestDataFile();

            // 示例：生成示例报告
            await GenerateSampleReport();

            // 示例：模拟截图文件创建
            await CreateSampleScreenshot();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"文件操作过程中发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例4：在服务类中使用 PathConfiguration
    /// </summary>
    public class ExampleDataService
    {
        private readonly ILogger<ExampleDataService> _logger;

        public ExampleDataService(ILogger<ExampleDataService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 加载用户测试数据
        /// </summary>
        public async Task<List<UserTestData>> LoadUserTestDataAsync()
        {
            try
            {
                var filePath = PathConfiguration.GetTestDataPath("users.json", "UI");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("用户测试数据文件不存在: {FilePath}", filePath);
                    return new List<UserTestData>();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var users = JsonSerializer.Deserialize<List<UserTestData>>(json);
                
                _logger.LogInformation("成功加载 {Count} 个用户测试数据", users?.Count ?? 0);
                return users ?? new List<UserTestData>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户测试数据失败");
                throw;
            }
        }

        /// <summary>
        /// 保存测试结果
        /// </summary>
        public async Task SaveTestResultAsync(TestResultData result)
        {
            try
            {
                var fileName = $"test_result_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = PathConfiguration.GetOutputPath(fileName, "results");
                
                // 确保输出目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    PathConfiguration.EnsureDirectoryExists(directory);
                }

                var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.LogInformation("测试结果已保存到: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存测试结果失败");
                throw;
            }
        }

        /// <summary>
        /// 获取页面元素配置
        /// </summary>
        public async Task<Dictionary<string, object>> LoadPageElementsAsync(string pageName)
        {
            try
            {
                var filePath = PathConfiguration.GetElementsConfigPath(pageName);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("页面元素配置文件不存在: {FilePath}", filePath);
                    return new Dictionary<string, object>();
                }

                // 这里简化处理，实际应该使用 YamlElementReader
                var content = await File.ReadAllTextAsync(filePath);
                _logger.LogInformation("成功加载页面元素配置: {PageName}", pageName);
                
                // 返回简化的字典结构
                return new Dictionary<string, object> { { "content", content } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载页面元素配置失败: {PageName}", pageName);
                throw;
            }
        }
    }

    /// <summary>
    /// 示例5：错误处理和验证
    /// </summary>
    public void ErrorHandlingExample()
    {
        Console.WriteLine("\n=== 错误处理示例 ===");

        // 测试无效环境名称
        try
        {
            var invalidConfigPath = PathConfiguration.GetEnvironmentConfigPath("InvalidEnvironment");
            Console.WriteLine($"不应该到达这里: {invalidConfigPath}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✓ 正确捕获无效环境名称错误: {ex.Message}");
        }

        // 测试空文件名
        try
        {
            var invalidPath = PathConfiguration.GetTestDataPath("");
            Console.WriteLine($"不应该到达这里: {invalidPath}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✓ 正确捕获空文件名错误: {ex.Message}");
        }

        // 测试路径验证
        var existingPath = PathConfiguration.BaseDirectory;
        var nonExistingPath = Path.Combine(PathConfiguration.BaseDirectory, "non_existing_file.txt");

        Console.WriteLine($"现有路径验证: {PathConfiguration.ValidatePath(existingPath, true)}");
        Console.WriteLine($"不存在路径验证: {PathConfiguration.ValidatePath(nonExistingPath, false)}");

        // 测试特殊字符处理
        var testNameWithSpecialChars = "Test<>Name|With:Special\"Chars";
        var cleanScreenshotPath = PathConfiguration.GetScreenshotPath(testNameWithSpecialChars, "chromium");
        Console.WriteLine($"特殊字符清理结果: {Path.GetFileName(cleanScreenshotPath)}");
    }

    /// <summary>
    /// 示例6：性能测试
    /// </summary>
    public void PerformanceExample()
    {
        Console.WriteLine("\n=== 性能测试示例 ===");

        const int iterations = 10000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 测试路径获取性能
        for (int i = 0; i < iterations; i++)
        {
            _ = PathConfiguration.BaseDirectory;
            _ = PathConfiguration.ConfigDirectory;
            _ = PathConfiguration.GetTestDataPath($"test{i}.csv");
            _ = PathConfiguration.GetScreenshotPath($"test{i}", "chromium");
        }

        stopwatch.Stop();
        Console.WriteLine($"执行 {iterations} 次路径操作耗时: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"平均每次操作耗时: {(double)stopwatch.ElapsedMilliseconds / iterations:F4}ms");

        // 内存使用测试
        var initialMemory = GC.GetTotalMemory(true);
        
        for (int i = 0; i < 1000; i++)
        {
            _ = PathConfiguration.GetTestDataPath($"memory_test_{i}.csv");
            _ = PathConfiguration.GetScreenshotPath($"memory_test_{i}", "firefox");
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        Console.WriteLine($"内存增长: {memoryIncrease} 字节 ({memoryIncrease / 1024.0:F2} KB)");
    }

    #region 私有辅助方法

    private async Task CreateSampleConfigurationFile()
    {
        var configPath = PathConfiguration.GetEnvironmentConfigPath("Development");
        var configDir = Path.GetDirectoryName(configPath);
        
        if (!string.IsNullOrEmpty(configDir))
        {
            PathConfiguration.EnsureDirectoryExists(configDir);
        }

        var sampleConfig = new
        {
            TestConfiguration = new
            {
                Environment = new { Name = "Development", BaseUrl = "https://dev.example.com" },
                Browser = new { Type = "Chromium", Headless = false },
                Logging = new { Level = "Information" }
            }
        };

        var json = JsonSerializer.Serialize(sampleConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json);
        Console.WriteLine($"示例配置文件已创建: {configPath}");
    }

    private async Task ReadSampleConfigurationFile()
    {
        var configPath = PathConfiguration.GetEnvironmentConfigPath("Development");
        
        if (File.Exists(configPath))
        {
            var content = await File.ReadAllTextAsync(configPath);
            Console.WriteLine($"配置文件内容长度: {content.Length} 字符");
        }
    }

    private async Task CreateSampleTestDataFile()
    {
        var testDataPath = PathConfiguration.GetTestDataPath("sample_users.json", "UI");
        var testDataDir = Path.GetDirectoryName(testDataPath);
        
        if (!string.IsNullOrEmpty(testDataDir))
        {
            PathConfiguration.EnsureDirectoryExists(testDataDir);
        }

        var sampleUsers = new[]
        {
            new { Username = "testuser1", Email = "test1@example.com", Role = "User" },
            new { Username = "testuser2", Email = "test2@example.com", Role = "Admin" },
            new { Username = "testuser3", Email = "test3@example.com", Role = "User" }
        };

        var json = JsonSerializer.Serialize(sampleUsers, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(testDataPath, json);
        Console.WriteLine($"示例测试数据文件已创建: {testDataPath}");
    }

    private async Task ReadSampleTestDataFile()
    {
        var testDataPath = PathConfiguration.GetTestDataPath("sample_users.json", "UI");
        
        if (File.Exists(testDataPath))
        {
            var content = await File.ReadAllTextAsync(testDataPath);
            var users = JsonSerializer.Deserialize<UserTestData[]>(content);
            Console.WriteLine($"读取到 {users?.Length ?? 0} 个测试用户");
        }
    }

    private async Task GenerateSampleReport()
    {
        var reportPath = PathConfiguration.GetReportPath("sample_report.html");
        var reportDir = Path.GetDirectoryName(reportPath);
        
        if (!string.IsNullOrEmpty(reportDir))
        {
            PathConfiguration.EnsureDirectoryExists(reportDir);
        }

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <title>示例测试报告</title>
</head>
<body>
    <h1>测试报告</h1>
    <p>生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    <p>这是一个使用 PathConfiguration 生成的示例报告。</p>
</body>
</html>";

        await File.WriteAllTextAsync(reportPath, htmlContent);
        Console.WriteLine($"示例报告已生成: {reportPath}");
    }

    private async Task CreateSampleScreenshot()
    {
        var screenshotPath = PathConfiguration.GetScreenshotPath("SampleTest", "chromium");
        var screenshotDir = Path.GetDirectoryName(screenshotPath);
        
        if (!string.IsNullOrEmpty(screenshotDir))
        {
            PathConfiguration.EnsureDirectoryExists(screenshotDir);
        }

        // 创建一个空的 "截图" 文件作为示例
        await File.WriteAllTextAsync(screenshotPath.Replace(".png", ".txt"), 
            $"这是一个模拟的截图文件，创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"示例截图文件已创建: {screenshotPath.Replace(".png", ".txt")}");
    }

    #endregion

    #region 数据模型

    public class UserTestData
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class TestResultData
    {
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}

/// <summary>
/// 示例程序入口点
/// </summary>
public class PathConfigurationExampleProgram
{
    public static async Task Main(string[] args)
    {
        // 创建日志记录器（简化版本）
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PathConfigurationExample>();

        var example = new PathConfigurationExample(logger);

        try
        {
            // 运行所有示例
            example.BasicPathUsageExample();
            example.DirectoryManagementExample();
            await example.FileOperationIntegrationExample();
            example.ErrorHandlingExample();
            example.PerformanceExample();

            Console.WriteLine("\n=== 所有示例执行完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例执行过程中发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}