using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using CsPlaywrightXun.src.playwright.Core.Configuration;

namespace CsPlaywrightXun.src.playwright.Tests.Unit.Configuration;

/// <summary>
/// PathConfiguration 类的单元测试
/// 验证集中路径配置管理的功能
/// </summary>
public class PathConfigurationTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly string _originalDirectory;

    /// <summary>
    /// 构造函数 - 设置测试环境
    /// </summary>
    public PathConfigurationTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "PathConfigurationTests", Guid.NewGuid().ToString());
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

    #region 基础目录测试

    /// <summary>
    /// 测试基础目录属性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void BaseDirectory_ShouldReturnValidPath()
    {
        // Act
        var baseDirectory = PathConfiguration.BaseDirectory;

        // Assert
        Assert.NotNull(baseDirectory);
        Assert.NotEmpty(baseDirectory);
        Assert.True(Path.IsPathFullyQualified(baseDirectory));
        Assert.Equal(_testBaseDirectory, baseDirectory);
    }

    /// <summary>
    /// 测试配置目录属性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void ConfigDirectory_ShouldReturnCorrectPath()
    {
        // Act
        var configDirectory = PathConfiguration.ConfigDirectory;

        // Assert
        var expectedPath = Path.Combine(_testBaseDirectory, "src", "config");
        Assert.Equal(expectedPath, configDirectory);
    }

    /// <summary>
    /// 测试所有预定义目录属性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void AllDirectoryProperties_ShouldReturnValidPaths()
    {
        // Act & Assert
        var directories = PathConfiguration.GetAllDirectories();

        Assert.NotEmpty(directories);
        Assert.Equal(9, directories.Count); // 验证所有目录都被包含

        foreach (var directory in directories)
        {
            Assert.NotNull(directory.Value);
            Assert.NotEmpty(directory.Value);
            Assert.True(Path.IsPathFullyQualified(directory.Value));
            Assert.StartsWith(_testBaseDirectory, directory.Value);
        }
    }

    #endregion

    #region 路径获取方法测试

    /// <summary>
    /// 测试获取环境配置文件路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void GetEnvironmentConfigPath_WithValidEnvironment_ShouldReturnCorrectPath(string environment)
    {
        // Act
        var path = PathConfiguration.GetEnvironmentConfigPath(environment);

        // Assert
        var expectedPath = Path.Combine(PathConfiguration.EnvironmentsDirectory, $"appsettings.{environment}.json");
        Assert.Equal(expectedPath, path);
        Assert.Contains(environment, path);
        Assert.EndsWith(".json", path);
    }

    /// <summary>
    /// 测试获取环境配置文件路径 - 无效环境名称
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetEnvironmentConfigPath_WithInvalidEnvironment_ShouldThrowArgumentException(string environment)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetEnvironmentConfigPath(environment));
    }

    /// <summary>
    /// 测试获取环境配置文件路径 - 不支持的环境名称
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("InvalidEnv")]
    [InlineData("Local")]
    [InlineData("Custom")]
    public void GetEnvironmentConfigPath_WithUnsupportedEnvironment_ShouldThrowArgumentException(string environment)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetEnvironmentConfigPath(environment));
    }

    /// <summary>
    /// 测试获取页面元素配置文件路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("HomePage")]
    [InlineData("LoginPage")]
    [InlineData("SearchPage")]
    public void GetElementsConfigPath_WithValidPageName_ShouldReturnCorrectPath(string pageName)
    {
        // Act
        var path = PathConfiguration.GetElementsConfigPath(pageName);

        // Assert
        var expectedPath = Path.Combine(PathConfiguration.ElementsDirectory, $"{pageName}.yaml");
        Assert.Equal(expectedPath, path);
        Assert.Contains(pageName, path);
        Assert.EndsWith(".yaml", path);
    }

    /// <summary>
    /// 测试获取页面元素配置文件路径 - 无效页面名称
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetElementsConfigPath_WithInvalidPageName_ShouldThrowArgumentException(string pageName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetElementsConfigPath(pageName));
    }

    /// <summary>
    /// 测试获取测试数据文件路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("test_data.csv", null)]
    [InlineData("search_data.json", "UI")]
    [InlineData("api_data.xml", "API")]
    public void GetTestDataPath_WithValidParameters_ShouldReturnCorrectPath(string fileName, string subDirectory)
    {
        // Act
        var path = PathConfiguration.GetTestDataPath(fileName, subDirectory);

        // Assert
        Assert.Contains(fileName, path);
        Assert.StartsWith(PathConfiguration.TestDataDirectory, path);
        
        if (!string.IsNullOrWhiteSpace(subDirectory))
        {
            Assert.Contains(subDirectory, path);
        }
    }

    /// <summary>
    /// 测试获取日志文件路径
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetLogPath_WithDefaultFileName_ShouldReturnCorrectPath()
    {
        // Act
        var path = PathConfiguration.GetLogPath();

        // Assert
        Assert.StartsWith(PathConfiguration.LogsDirectory, path);
        Assert.EndsWith(".log", path);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), path);
    }

    /// <summary>
    /// 测试获取日志文件路径 - 自定义文件名
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetLogPath_WithCustomFileName_ShouldReturnCorrectPath()
    {
        // Arrange
        var customFileName = "custom-log.log";

        // Act
        var path = PathConfiguration.GetLogPath(customFileName);

        // Assert
        var expectedPath = Path.Combine(PathConfiguration.LogsDirectory, customFileName);
        Assert.Equal(expectedPath, path);
    }

    /// <summary>
    /// 测试获取报告文件路径
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetReportPath_WithDefaultFileName_ShouldReturnCorrectPath()
    {
        // Act
        var path = PathConfiguration.GetReportPath();

        // Assert
        Assert.StartsWith(PathConfiguration.ReportsDirectory, path);
        Assert.EndsWith(".html", path);
        Assert.Contains("test-report-", path);
    }

    /// <summary>
    /// 测试获取截图文件路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("TestMethod1", "chromium")]
    [InlineData("TestMethod2", "firefox")]
    [InlineData("Test With Spaces", "webkit")]
    public void GetScreenshotPath_WithValidParameters_ShouldReturnCorrectPath(string testName, string browserType)
    {
        // Act
        var path = PathConfiguration.GetScreenshotPath(testName, browserType);

        // Assert
        Assert.StartsWith(PathConfiguration.ScreenshotsDirectory, path);
        Assert.EndsWith(".png", path);
        Assert.Contains(testName.Replace(' ', '_'), path);
        Assert.Contains(browserType, path);
    }

    /// <summary>
    /// 测试获取截图文件路径 - 无效测试名称
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetScreenshotPath_WithInvalidTestName_ShouldThrowArgumentException(string testName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetScreenshotPath(testName));
    }

    /// <summary>
    /// 测试获取输出文件路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("output.txt", null)]
    [InlineData("report.html", "reports")]
    [InlineData("data.json", "data")]
    public void GetOutputPath_WithValidParameters_ShouldReturnCorrectPath(string fileName, string subDirectory)
    {
        // Act
        var path = PathConfiguration.GetOutputPath(fileName, subDirectory);

        // Assert
        Assert.Contains(fileName, path);
        Assert.StartsWith(PathConfiguration.OutputDirectory, path);
        
        if (!string.IsNullOrWhiteSpace(subDirectory))
        {
            Assert.Contains(subDirectory, path);
        }
    }

    #endregion

    #region 目录管理测试

    /// <summary>
    /// 测试确保目录存在功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void EnsureDirectoryExists_WithValidPath_ShouldCreateDirectory()
    {
        // Arrange
        var testDirectory = Path.Combine(_testBaseDirectory, "test_directory");
        Assert.False(Directory.Exists(testDirectory));

        // Act
        PathConfiguration.EnsureDirectoryExists(testDirectory);

        // Assert
        Assert.True(Directory.Exists(testDirectory));
    }

    /// <summary>
    /// 测试确保目录存在功能 - 目录已存在
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void EnsureDirectoryExists_WithExistingDirectory_ShouldNotThrow()
    {
        // Arrange
        var testDirectory = Path.Combine(_testBaseDirectory, "existing_directory");
        Directory.CreateDirectory(testDirectory);
        Assert.True(Directory.Exists(testDirectory));

        // Act & Assert
        var exception = Record.Exception(() => PathConfiguration.EnsureDirectoryExists(testDirectory));
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试确保目录存在功能 - 无效路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EnsureDirectoryExists_WithInvalidPath_ShouldThrowArgumentException(string directoryPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.EnsureDirectoryExists(directoryPath));
    }

    /// <summary>
    /// 测试初始化所有目录功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void InitializeDirectories_ShouldCreateAllRequiredDirectories()
    {
        // Act
        PathConfiguration.InitializeDirectories();

        // Assert
        var directories = PathConfiguration.GetAllDirectories();
        foreach (var directory in directories.Values)
        {
            Assert.True(Directory.Exists(directory), $"Directory should exist: {directory}");
        }
    }

    /// <summary>
    /// 测试初始化目录功能 - 多次调用应该是安全的
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void InitializeDirectories_MultipleCallsShouldBeSafe()
    {
        // Act
        PathConfiguration.InitializeDirectories();
        PathConfiguration.InitializeDirectories();
        PathConfiguration.InitializeDirectories();

        // Assert
        var directories = PathConfiguration.GetAllDirectories();
        foreach (var directory in directories.Values)
        {
            Assert.True(Directory.Exists(directory), $"Directory should exist: {directory}");
        }
    }

    #endregion

    #region 路径验证测试

    /// <summary>
    /// 测试路径验证功能 - 文件路径
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void ValidatePath_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var testFile = Path.Combine(_testBaseDirectory, "test_file.txt");
        File.WriteAllText(testFile, "test content");

        // Act
        var result = PathConfiguration.ValidatePath(testFile, false);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试路径验证功能 - 目录路径
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void ValidatePath_WithExistingDirectory_ShouldReturnTrue()
    {
        // Arrange
        var testDirectory = Path.Combine(_testBaseDirectory, "test_directory");
        Directory.CreateDirectory(testDirectory);

        // Act
        var result = PathConfiguration.ValidatePath(testDirectory, true);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试路径验证功能 - 不存在的路径
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void ValidatePath_WithNonExistentPath_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testBaseDirectory, "non_existent_path");

        // Act
        var result = PathConfiguration.ValidatePath(nonExistentPath, false);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试路径验证功能 - 无效路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidatePath_WithInvalidPath_ShouldReturnFalse(string path)
    {
        // Act
        var result = PathConfiguration.ValidatePath(path, false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region 相对路径测试

    /// <summary>
    /// 测试获取相对路径功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetRelativePath_WithValidFullPath_ShouldReturnRelativePath()
    {
        // Arrange
        var fullPath = Path.Combine(_testBaseDirectory, "src", "config", "test.json");

        // Act
        var relativePath = PathConfiguration.GetRelativePath(fullPath);

        // Assert
        Assert.NotNull(relativePath);
        Assert.DoesNotContain(_testBaseDirectory, relativePath);
        Assert.Equal(Path.Combine("src", "config", "test.json"), relativePath);
    }

    /// <summary>
    /// 测试获取相对路径功能 - 无效路径
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void GetRelativePath_WithInvalidPath_ShouldThrowArgumentException(string fullPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetRelativePath(fullPath));
    }

    #endregion

    #region 文件名验证测试

    /// <summary>
    /// 测试文件名包含无效字符的处理
    /// </summary>
    [Theory]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    [InlineData("test<file>.txt")]
    [InlineData("test|file.txt")]
    [InlineData("test?file.txt")]
    public void GetElementsConfigPath_WithInvalidCharacters_ShouldThrowArgumentException(string fileName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetElementsConfigPath(fileName));
    }

    /// <summary>
    /// 测试截图文件名的特殊字符清理
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetScreenshotPath_WithSpecialCharacters_ShouldCleanFileName()
    {
        // Arrange
        var testName = "Test:With/Special\\Characters";

        // Act
        var path = PathConfiguration.GetScreenshotPath(testName);

        // Assert
        // 检查文件名部分（不包括驱动器路径）是否清理了特殊字符
        var fileName = Path.GetFileName(path);
        Assert.DoesNotContain(":", fileName);
        Assert.DoesNotContain("/", fileName);
        Assert.DoesNotContain("\\", fileName);
        Assert.Contains("Test_With_Special_Characters", fileName);
    }

    #endregion

    #region 边界条件测试

    /// <summary>
    /// 测试长文件名的处理
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetTestDataPath_WithLongFileName_ShouldHandleCorrectly()
    {
        // Arrange
        var longFileName = new string('a', 200) + ".txt";

        // Act
        var path = PathConfiguration.GetTestDataPath(longFileName);

        // Assert
        Assert.Contains(longFileName, path);
        Assert.StartsWith(PathConfiguration.TestDataDirectory, path);
    }

    /// <summary>
    /// 测试深层子目录的处理
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetTestDataPath_WithDeepSubDirectory_ShouldHandleCorrectly()
    {
        // Arrange
        var fileName = "test.txt";
        var deepSubDirectory = Path.Combine("level1", "level2", "level3");

        // Act
        var path = PathConfiguration.GetTestDataPath(fileName, deepSubDirectory);

        // Assert
        Assert.Contains(fileName, path);
        Assert.Contains("level1", path);
        Assert.Contains("level2", path);
        Assert.Contains("level3", path);
    }

    #endregion

    #region 线程安全测试

    /// <summary>
    /// 测试并发访问基础目录的线程安全性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void BaseDirectory_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var results = new List<string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var baseDir = PathConfiguration.BaseDirectory;
                lock (results)
                {
                    results.Add(baseDir);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(10, results.Count);
        Assert.True(results.All(r => r == _testBaseDirectory));
    }

    /// <summary>
    /// 测试并发初始化目录的线程安全性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void InitializeDirectories_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() => PathConfiguration.InitializeDirectories()));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var directories = PathConfiguration.GetAllDirectories();
        foreach (var directory in directories.Values)
        {
            Assert.True(Directory.Exists(directory), $"Directory should exist: {directory}");
        }
    }

    /// <summary>
    /// 测试跨平台路径兼容性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void CrossPlatformPathCompatibility_ShouldWorkOnAllPlatforms()
    {
        // Arrange & Act
        var configDir = PathConfiguration.ConfigDirectory;
        var elementsDir = PathConfiguration.ElementsDirectory;
        var testDataDir = PathConfiguration.TestDataDirectory;
        var reportsDir = PathConfiguration.ReportsDirectory;
        var logsDir = PathConfiguration.LogsDirectory;
        var screenshotsDir = PathConfiguration.ScreenshotsDirectory;

        // Assert - 验证路径使用正确的分隔符
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), configDir);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), elementsDir);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), testDataDir);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), reportsDir);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), logsDir);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), screenshotsDir);

        // 验证路径是绝对路径
        Assert.True(Path.IsPathRooted(configDir));
        Assert.True(Path.IsPathRooted(elementsDir));
        Assert.True(Path.IsPathRooted(testDataDir));
        Assert.True(Path.IsPathRooted(reportsDir));
        Assert.True(Path.IsPathRooted(logsDir));
        Assert.True(Path.IsPathRooted(screenshotsDir));
    }

    /// <summary>
    /// 测试路径配置的初始化幂等性
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void InitializeDirectories_ShouldBeIdempotent()
    {
        // Arrange & Act - 多次初始化
        PathConfiguration.InitializeDirectories();
        PathConfiguration.InitializeDirectories();
        PathConfiguration.InitializeDirectories();

        // Assert - 验证目录存在且只创建一次
        Assert.True(Directory.Exists(PathConfiguration.ConfigDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ElementsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.TestDataDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ReportsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.LogsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.ScreenshotsDirectory));
        Assert.True(Directory.Exists(PathConfiguration.OutputDirectory));
    }

    /// <summary>
    /// 测试特殊字符在文件名中的处理
    /// </summary>
    [Theory]
    [InlineData("test<>file", "test__file")]
    [InlineData("test|file", "test_file")]
    [InlineData("test:file", "test_file")]
    [InlineData("test\"file", "test_file")]
    [InlineData("test file", "test_file")]
    [InlineData("test/file\\name", "test_file_name")]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetScreenshotPath_ShouldCleanInvalidCharacters(string testName, string expectedCleanName)
    {
        // Act
        var screenshotPath = PathConfiguration.GetScreenshotPath(testName, "chromium");

        // Assert
        var fileName = Path.GetFileName(screenshotPath);
        Assert.Contains(expectedCleanName, fileName);
        Assert.DoesNotContain("<", fileName);
        Assert.DoesNotContain(">", fileName);
        Assert.DoesNotContain("|", fileName);
        Assert.DoesNotContain(":", fileName);
        Assert.DoesNotContain("\"", fileName);
        Assert.DoesNotContain("/", fileName);
        Assert.DoesNotContain("\\", fileName);
    }

    /// <summary>
    /// 测试长路径的处理
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_ShouldHandleLongPaths()
    {
        // Arrange
        var longFileName = new string('a', 200) + ".txt";
        var longSubDirectory = new string('b', 100);

        // Act & Assert - 应该不抛出异常
        var testDataPath = PathConfiguration.GetTestDataPath(longFileName, longSubDirectory);
        var outputPath = PathConfiguration.GetOutputPath(longFileName, longSubDirectory);

        Assert.NotNull(testDataPath);
        Assert.NotNull(outputPath);
        Assert.Contains(longFileName, testDataPath);
        Assert.Contains(longSubDirectory, testDataPath);
    }

    /// <summary>
    /// 测试环境名称验证的边界情况
    /// </summary>
    [Theory]
    [InlineData("development")]  // 小写
    [InlineData("DEVELOPMENT")]  // 大写
    [InlineData("Development")]  // 正常大小写
    [InlineData("test")]
    [InlineData("staging")]
    [InlineData("production")]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetEnvironmentConfigPath_ShouldAcceptValidEnvironments(string environment)
    {
        // Act & Assert - 应该不抛出异常
        var configPath = PathConfiguration.GetEnvironmentConfigPath(environment);
        
        Assert.NotNull(configPath);
        Assert.Contains($"appsettings.{environment}.json", configPath);
        Assert.Contains(PathConfiguration.EnvironmentsDirectory, configPath);
    }

    /// <summary>
    /// 测试无效环境名称
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("local")]
    [InlineData("custom")]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void GetEnvironmentConfigPath_ShouldRejectInvalidEnvironments(string environment)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PathConfiguration.GetEnvironmentConfigPath(environment));
    }

    /// <summary>
    /// 测试路径配置的内存使用
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_ShouldNotLeakMemory()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act - 大量访问路径配置
        for (int i = 0; i < 1000; i++)
        {
            _ = PathConfiguration.BaseDirectory;
            _ = PathConfiguration.ConfigDirectory;
            _ = PathConfiguration.GetTestDataPath($"test{i}.csv");
            _ = PathConfiguration.GetScreenshotPath($"test{i}", "chromium");
        }

        // Assert - 内存使用应该保持稳定
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        
        // 允许少量内存增长（小于1MB）
        Assert.True(memoryIncrease < 1024 * 1024, $"Memory increased by {memoryIncrease} bytes");
    }

    /// <summary>
    /// 测试路径配置的性能
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void PathConfiguration_ShouldHaveGoodPerformance()
    {
        // Arrange
        const int iterations = 10000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = PathConfiguration.BaseDirectory;
            _ = PathConfiguration.GetTestDataPath($"test{i}.csv");
            _ = PathConfiguration.GetScreenshotPath($"test{i}", "chromium");
        }

        stopwatch.Stop();

        // Assert - 应该在合理时间内完成（小于1秒）
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Performance test took {stopwatch.ElapsedMilliseconds}ms for {iterations} iterations");
    }

    /// <summary>
    /// 测试目录创建的权限处理
    /// </summary>
    [Fact]
    [Trait("TestType", "Unit")]
    [Trait("Component", "PathConfiguration")]
    public void EnsureDirectoryExists_ShouldHandlePermissionIssues()
    {
        // Arrange
        var restrictedPath = Path.Combine(_testBaseDirectory, "restricted");

        // Act & Assert - 在正常情况下应该成功
        PathConfiguration.EnsureDirectoryExists(restrictedPath);
        Assert.True(Directory.Exists(restrictedPath));

        // 清理
        Directory.Delete(restrictedPath);
    }

    #endregion
}