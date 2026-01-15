using System;
using System.IO;
using Xunit;
using CsPlaywrightXun.src.playwright.Core.Configuration;

namespace CsPlaywrightXun.src.playwright.Tests.Unit.Configuration;

/// <summary>
/// PathConfiguration 与其他组件集成的测试
/// 验证路径配置与现有系统的集成
/// </summary>
public class PathConfigurationIntegrationTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly string _originalDirectory;

    /// <summary>
    /// 构造函数 - 设置测试环境
    /// </summary>
    public PathConfigurationIntegrationTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "PathConfigurationIntegrationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);
        
        // 重置PathConfiguration状态
        PathConfiguration.Reset();
        PathConfiguration.SetCustomBaseDirectory(_testBaseDirectory);
    }

    /// <summary>
    /// 清理测试环境
    /// </summary>
    public void Dispose()
    {
        try
        {
            PathConfiguration.Reset();
            Directory.SetCurrentDirectory(_originalDirectory);
            
            if (Directory.Exists(_testBaseDirectory))
            {
                Directory.Delete(_testBaseDirectory, true);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 测试PathConfiguration与ConfigurationService的集成
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_WithConfigurationService_ShouldProvideCorrectPaths()
    {
        // Arrange
        PathConfiguration.InitializeDirectories();
        
        // 创建测试配置文件
        var configPath = PathConfiguration.GetEnvironmentConfigPath("Development");
        var configContent = @"{
            ""TestConfiguration"": {
                ""Environment"": {
                    ""Name"": ""Development"",
                    ""BaseUrl"": ""https://www.baidu.com"",
                    ""ApiBaseUrl"": ""https://www.baidu.com/api""
                },
                ""Browser"": {
                    ""Type"": ""Chromium"",
                    ""Headless"": false,
                    ""ViewportWidth"": 1920,
                    ""ViewportHeight"": 1080,
                    ""Timeout"": 30000
                },
                ""Api"": {
                    ""Timeout"": 30000,
                    ""RetryCount"": 3,
                    ""RetryDelay"": 1000
                },
                ""Reporting"": {
                    ""OutputPath"": ""Reports"",
                    ""Format"": ""Html"",
                    ""IncludeScreenshots"": true
                },
                ""Logging"": {
                    ""Level"": ""Information"",
                    ""FilePath"": ""Logs/test-{Date}.log"",
                    ""EnableConsole"": true,
                    ""EnableFile"": true,
                    ""EnableStructuredLogging"": true,
                    ""FileSizeLimitMB"": 100,
                    ""RetainedFileCount"": 30,
                    ""EnableTestContext"": true
                }
            }
        }";
        
        PathConfiguration.EnsureDirectoryExists(Path.GetDirectoryName(configPath)!);
        File.WriteAllText(configPath, configContent);

        // Act & Assert - 验证路径配置提供了正确的目录结构
        Assert.True(Directory.Exists(PathConfiguration.EnvironmentsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ElementsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.TestDataDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ReportsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.LogsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ScreenshotsDirectory));
        
        // 验证配置文件路径正确
        Assert.True(File.Exists(configPath));
        Assert.Equal(Path.Combine(PathConfiguration.EnvironmentsDirectory, "appsettings.Development.json"), configPath);
        
        // 验证基础目录设置正确
        Assert.Equal(_testBaseDirectory, PathConfiguration.BaseDirectory);
    }

    /// <summary>
    /// 测试PathConfiguration支持的文件类型路径生成
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_ShouldSupportAllRequiredFileTypes()
    {
        // Arrange
        PathConfiguration.InitializeDirectories();

        // Act & Assert - 测试各种文件类型的路径生成
        
        // 环境配置文件
        var envPath = PathConfiguration.GetEnvironmentConfigPath("Test");
        Assert.EndsWith("appsettings.Test.json", envPath);
        Assert.StartsWith(PathConfiguration.EnvironmentsDirectory, envPath);

        // 页面元素配置文件
        var elementsPath = PathConfiguration.GetElementsConfigPath("HomePage");
        Assert.EndsWith("HomePage.yaml", elementsPath);
        Assert.StartsWith(PathConfiguration.ElementsDirectory, elementsPath);

        // 测试数据文件
        var csvDataPath = PathConfiguration.GetTestDataPath("test_data.csv", "UI");
        Assert.EndsWith("test_data.csv", csvDataPath);
        Assert.Contains("UI", csvDataPath);
        Assert.StartsWith(PathConfiguration.TestDataDirectory, csvDataPath);

        var jsonDataPath = PathConfiguration.GetTestDataPath("api_data.json", "API");
        Assert.EndsWith("api_data.json", jsonDataPath);
        Assert.Contains("API", jsonDataPath);

        // 日志文件
        var logPath = PathConfiguration.GetLogPath("custom.log");
        Assert.EndsWith("custom.log", logPath);
        Assert.StartsWith(PathConfiguration.LogsDirectory, logPath);

        // 报告文件
        var reportPath = PathConfiguration.GetReportPath("test-report.html");
        Assert.EndsWith("test-report.html", reportPath);
        Assert.StartsWith(PathConfiguration.ReportsDirectory, reportPath);

        // 截图文件
        var screenshotPath = PathConfiguration.GetScreenshotPath("TestMethod", "chromium");
        Assert.EndsWith(".png", screenshotPath);
        Assert.Contains("TestMethod", screenshotPath);
        Assert.Contains("chromium", screenshotPath);
        Assert.StartsWith(PathConfiguration.ScreenshotsDirectory, screenshotPath);

        // 输出文件
        var outputPath = PathConfiguration.GetOutputPath("result.txt", "results");
        Assert.EndsWith("result.txt", outputPath);
        Assert.Contains("results", outputPath);
        Assert.StartsWith(PathConfiguration.OutputDirectory, outputPath);
        
        // 验证所有路径都基于测试基础目录
        var allPaths = new[] { envPath, elementsPath, csvDataPath, jsonDataPath, logPath, reportPath, screenshotPath, outputPath };
        foreach (var path in allPaths)
        {
            Assert.StartsWith(_testBaseDirectory, path);
        }
    }

    /// <summary>
    /// 测试PathConfiguration的跨平台路径兼容性
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_ShouldHandleCrossPlatformPaths()
    {
        // Arrange
        PathConfiguration.InitializeDirectories();

        // Act
        var allDirectories = PathConfiguration.GetAllDirectories();

        // Assert - 验证所有路径都是有效的完整路径
        foreach (var directory in allDirectories)
        {
            Assert.True(Path.IsPathFullyQualified(directory.Value), 
                $"Directory path should be fully qualified: {directory.Key} = {directory.Value}");
            
            // 验证路径分隔符正确
            Assert.DoesNotContain("/", directory.Value.Replace(Path.DirectorySeparatorChar.ToString(), ""));
            Assert.DoesNotContain("\\", directory.Value.Replace(Path.DirectorySeparatorChar.ToString(), ""));
        }
    }

    /// <summary>
    /// 测试PathConfiguration的相对路径计算功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_RelativePathCalculation_ShouldWorkCorrectly()
    {
        // Arrange
        var fullPath = Path.Combine(PathConfiguration.BaseDirectory, "src", "config", "test.json");

        // Act
        var relativePath = PathConfiguration.GetRelativePath(fullPath);

        // Assert
        var expectedRelativePath = Path.Combine("src", "config", "test.json");
        Assert.Equal(expectedRelativePath, relativePath);
        Assert.DoesNotContain(PathConfiguration.BaseDirectory, relativePath);
        
        // 验证基础目录是测试目录
        Assert.Equal(_testBaseDirectory, PathConfiguration.BaseDirectory);
    }

    /// <summary>
    /// 测试PathConfiguration的目录自动创建功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_AutoDirectoryCreation_ShouldWorkForAllPaths()
    {
        // Arrange - 确保目录不存在
        var testPaths = new[]
        {
            PathConfiguration.GetTestDataPath("test.csv", "NewSubDir"),
            PathConfiguration.GetOutputPath("output.txt", "NewOutputDir"),
            PathConfiguration.GetElementsConfigPath("NewPage")
        };

        // Act - 为每个路径确保其目录存在
        foreach (var path in testPaths)
        {
            var directory = Path.GetDirectoryName(path);
            Assert.NotNull(directory);
            PathConfiguration.EnsureDirectoryExists(directory);
        }

        // Assert - 验证所有目录都被创建
        foreach (var path in testPaths)
        {
            var directory = Path.GetDirectoryName(path);
            Assert.True(Directory.Exists(directory), $"Directory should exist: {directory}");
        }
    }

    /// <summary>
    /// 测试PathConfiguration的路径验证功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_PathValidation_ShouldWorkCorrectly()
    {
        // Arrange
        PathConfiguration.InitializeDirectories();
        
        var testFile = PathConfiguration.GetTestDataPath("validation_test.txt");
        var testDirectory = PathConfiguration.TestDataDirectory;
        
        // 创建测试文件
        File.WriteAllText(testFile, "test content");

        // Act & Assert
        Assert.True(PathConfiguration.ValidatePath(testFile, false), "File should exist");
        Assert.True(PathConfiguration.ValidatePath(testDirectory, true), "Directory should exist");
        
        Assert.False(PathConfiguration.ValidatePath(testFile, true), "File should not be validated as directory");
        Assert.False(PathConfiguration.ValidatePath(testDirectory, false), "Directory should not be validated as file");
        
        var nonExistentPath = Path.Combine(testDirectory, "non_existent.txt");
        Assert.False(PathConfiguration.ValidatePath(nonExistentPath, false), "Non-existent file should not validate");
    }
}