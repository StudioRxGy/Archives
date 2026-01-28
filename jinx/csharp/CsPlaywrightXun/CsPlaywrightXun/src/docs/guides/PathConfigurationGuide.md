# PathConfiguration 使用指南

## 概述

`PathConfiguration` 是企业级自动化测试框架的核心路径管理组件，提供集中化的文件路径配置和管理功能。它确保所有组件使用一致的路径结构，支持跨平台兼容性，并提供类型安全的路径访问方法。

## 核心特性

- **集中路径管理**：统一管理所有文件路径，避免硬编码
- **跨平台兼容**：自动处理不同操作系统的路径分隔符
- **类型安全**：提供强类型的路径访问方法
- **自动目录创建**：按需创建必要的目录结构
- **线程安全**：支持多线程并发访问
- **性能优化**：使用缓存机制提高性能

## 目录结构

PathConfiguration 管理以下标准目录结构：

```
项目根目录/
├── src/
│   ├── config/
│   │   ├── environments/     # 环境配置文件
│   │   ├── elements/         # 页面元素配置
│   │   └── date/            # 测试数据文件
│   └── output/              # 输出文件
├── reports/                 # 测试报告
├── logs/                    # 日志文件
└── screenshots/             # 截图文件
```

## 基本用法

### 1. 获取基础目录路径

```csharp
using CsPlaywrightXun.src.playwright.Core.Configuration;

// 获取项目根目录
string baseDirectory = PathConfiguration.BaseDirectory;

// 获取配置文件目录
string configDirectory = PathConfiguration.ConfigDirectory;

// 获取测试数据目录
string testDataDirectory = PathConfiguration.TestDataDirectory;

// 获取报告输出目录
string reportsDirectory = PathConfiguration.ReportsDirectory;
```

### 2. 获取特定文件路径

```csharp
// 获取环境配置文件路径
string devConfigPath = PathConfiguration.GetEnvironmentConfigPath("Development");
string prodConfigPath = PathConfiguration.GetEnvironmentConfigPath("Production");

// 获取页面元素配置文件路径
string homePageElementsPath = PathConfiguration.GetElementsConfigPath("HomePage");
string loginPageElementsPath = PathConfiguration.GetElementsConfigPath("LoginPage");

// 获取测试数据文件路径
string csvDataPath = PathConfiguration.GetTestDataPath("users.csv");
string jsonDataPath = PathConfiguration.GetTestDataPath("api_test_data.json", "API");

// 获取截图文件路径
string screenshotPath = PathConfiguration.GetScreenshotPath("LoginTest", "chromium");

// 获取日志文件路径
string logPath = PathConfiguration.GetLogPath();
string customLogPath = PathConfiguration.GetLogPath("custom.log");

// 获取报告文件路径
string reportPath = PathConfiguration.GetReportPath();
string customReportPath = PathConfiguration.GetReportPath("test_report.html");
```

### 3. 目录管理

```csharp
// 初始化所有必要目录
PathConfiguration.InitializeDirectories();

// 确保特定目录存在
string customDir = Path.Combine(PathConfiguration.BaseDirectory, "custom");
PathConfiguration.EnsureDirectoryExists(customDir);

// 验证路径是否存在
bool fileExists = PathConfiguration.ValidatePath("/path/to/file.txt", false);
bool dirExists = PathConfiguration.ValidatePath("/path/to/directory", true);

// 获取所有配置的目录
Dictionary<string, string> allDirectories = PathConfiguration.GetAllDirectories();
```

## 高级用法

### 1. 在服务中使用 PathConfiguration

#### CSV 数据读取器示例

```csharp
public class CsvDataReader
{
    public IEnumerable<T> ReadData<T>(string fileName, string? subDirectory = null) 
        where T : class, new()
    {
        // 使用 PathConfiguration 获取完整路径
        var filePath = PathConfiguration.GetTestDataPath(fileName, subDirectory);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"CSV文件不存在: {filePath}");
        }

        // 读取文件内容...
        return ReadDataFromPath<T>(filePath);
    }
}

// 使用示例
var csvReader = new CsvDataReader();
var users = csvReader.ReadData<User>("users.csv");
var apiData = csvReader.ReadData<ApiTestData>("test_data.csv", "API");
```

#### YAML 元素读取器示例

```csharp
public class YamlElementReader
{
    public PageElementCollection LoadElements(string pageName)
    {
        // 使用 PathConfiguration 获取元素配置文件路径
        var filePath = PathConfiguration.GetElementsConfigPath(pageName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"元素配置文件不存在: {filePath}");
        }

        return LoadElementsFromPath(filePath);
    }
}

// 使用示例
var elementReader = new YamlElementReader();
var homePageElements = elementReader.LoadElements("HomePage");
var loginPageElements = elementReader.LoadElements("LoginPage");
```

#### 浏览器服务截图示例

```csharp
public class BrowserService
{
    public async Task TakeScreenshotToFileAsync(IPage page, string testName, string browserType = "chromium")
    {
        // 使用 PathConfiguration 获取截图文件路径
        var filePath = PathConfiguration.GetScreenshotPath(testName, browserType);
        
        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            PathConfiguration.EnsureDirectoryExists(directory);
        }

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true,
            Type = ScreenshotType.Png
        });
    }
}

// 使用示例
var browserService = new BrowserService();
await browserService.TakeScreenshotToFileAsync(page, "LoginTest", "chromium");
```

### 2. 在配置服务中使用

```csharp
public class ConfigurationService
{
    public TestConfiguration LoadConfiguration(string environment)
    {
        // 确保配置目录存在
        PathConfiguration.InitializeDirectories();
        
        // 使用 PathConfiguration 获取配置文件路径
        var configPath = PathConfiguration.GetEnvironmentConfigPath(environment);
        
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"配置文件不存在: {configPath}");
        }

        var builder = new ConfigurationBuilder()
            .SetBasePath(PathConfiguration.EnvironmentsDirectory)
            .AddJsonFile($"appsettings.{environment}.json", optional: false);

        // 加载和返回配置...
        return LoadConfigurationFromBuilder(builder);
    }
}
```

### 3. 在报告生成器中使用

```csharp
public class HtmlReportGenerator
{
    public async Task<string> GenerateReportAsync(TestReport testReport, string? outputPath = null)
    {
        // 使用 PathConfiguration 获取默认报告路径
        var reportPath = outputPath ?? PathConfiguration.GetReportPath($"{testReport.ReportName}_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.html");
        
        // 确保输出目录存在
        var outputDir = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            PathConfiguration.EnsureDirectoryExists(outputDir);
        }

        // 生成报告内容...
        var htmlContent = await GenerateHtmlContentAsync(testReport);
        await File.WriteAllTextAsync(reportPath, htmlContent, Encoding.UTF8);

        return reportPath;
    }
}
```

## 最佳实践

### 1. 初始化

在应用程序启动时初始化目录结构：

```csharp
public class TestFrameworkInitializer
{
    public static void Initialize()
    {
        // 初始化所有必要的目录
        PathConfiguration.InitializeDirectories();
        
        // 验证关键目录是否存在
        var directories = PathConfiguration.GetAllDirectories();
        foreach (var kvp in directories)
        {
            if (!Directory.Exists(kvp.Value))
            {
                throw new DirectoryNotFoundException($"关键目录不存在: {kvp.Key} -> {kvp.Value}");
            }
        }
    }
}
```

### 2. 错误处理

始终处理路径相关的异常：

```csharp
public class SafeFileOperations
{
    public static async Task<string> ReadFileAsync(string fileName, string? subDirectory = null)
    {
        try
        {
            var filePath = PathConfiguration.GetTestDataPath(fileName, subDirectory);
            
            if (!PathConfiguration.ValidatePath(filePath, false))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"无效的文件路径参数: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"没有权限访问文件: {ex.Message}", ex);
        }
    }
}
```

### 3. 单元测试

在单元测试中使用临时目录：

```csharp
public class PathConfigurationTestBase : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly string _originalDirectory;

    public PathConfigurationTestBase()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "TestFramework", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);
        
        // 设置测试环境
        PathConfiguration.Reset();
        PathConfiguration.SetCustomBaseDirectory(_testBaseDirectory);
    }

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
}

[Fact]
public void TestPathConfiguration()
{
    // 测试代码...
    var testDataPath = PathConfiguration.GetTestDataPath("test.csv");
    Assert.Contains(_testBaseDirectory, testDataPath);
}
```

### 4. 性能优化

利用 PathConfiguration 的缓存机制：

```csharp
public class OptimizedFileService
{
    // 缓存常用路径
    private static readonly string ConfigDirectory = PathConfiguration.ConfigDirectory;
    private static readonly string TestDataDirectory = PathConfiguration.TestDataDirectory;
    
    public async Task<T> LoadConfigurationAsync<T>(string fileName) where T : class
    {
        // 直接使用缓存的目录路径
        var filePath = Path.Combine(ConfigDirectory, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"配置文件不存在: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("反序列化失败");
    }
}
```

## 跨平台兼容性

PathConfiguration 自动处理不同操作系统的路径差异：

```csharp
// Windows: C:\Projects\TestFramework\src\config
// Linux/macOS: /home/user/projects/TestFramework/src/config
string configPath = PathConfiguration.ConfigDirectory;

// Windows: C:\Projects\TestFramework\screenshots\LoginTest_chromium_2024-01-15-10-30-45.png
// Linux/macOS: /home/user/projects/TestFramework/screenshots/LoginTest_chromium_2024-01-15-10-30-45.png
string screenshotPath = PathConfiguration.GetScreenshotPath("LoginTest", "chromium");
```

## 故障排除

### 常见问题

1. **目录不存在错误**
   ```csharp
   // 解决方案：确保初始化目录
   PathConfiguration.InitializeDirectories();
   ```

2. **权限访问错误**
   ```csharp
   // 解决方案：检查目录权限
   try
   {
       PathConfiguration.EnsureDirectoryExists(path);
   }
   catch (UnauthorizedAccessException ex)
   {
       // 处理权限问题
       Console.WriteLine($"权限不足: {ex.Message}");
   }
   ```

3. **路径过长错误**
   ```csharp
   // 解决方案：使用较短的文件名或子目录
   string shortPath = PathConfiguration.GetTestDataPath("data.csv", "short");
   ```

4. **无效字符错误**
   ```csharp
   // PathConfiguration 会自动清理无效字符
   string cleanPath = PathConfiguration.GetScreenshotPath("Test<>Name", "chromium");
   // 结果: Test__Name_chromium_timestamp.png
   ```

### 调试技巧

```csharp
public static void DebugPathConfiguration()
{
    Console.WriteLine("=== PathConfiguration 调试信息 ===");
    Console.WriteLine($"基础目录: {PathConfiguration.BaseDirectory}");
    
    var directories = PathConfiguration.GetAllDirectories();
    foreach (var kvp in directories)
    {
        var exists = Directory.Exists(kvp.Value) ? "✓" : "✗";
        Console.WriteLine($"{exists} {kvp.Key}: {kvp.Value}");
    }
    
    Console.WriteLine("=== 示例路径 ===");
    Console.WriteLine($"开发环境配置: {PathConfiguration.GetEnvironmentConfigPath("Development")}");
    Console.WriteLine($"测试数据: {PathConfiguration.GetTestDataPath("sample.csv")}");
    Console.WriteLine($"截图: {PathConfiguration.GetScreenshotPath("SampleTest", "chromium")}");
    Console.WriteLine($"日志: {PathConfiguration.GetLogPath()}");
    Console.WriteLine($"报告: {PathConfiguration.GetReportPath()}");
}
```

## 总结

PathConfiguration 提供了一个强大、灵活且易于使用的路径管理解决方案。通过集中管理所有文件路径，它确保了代码的可维护性、跨平台兼容性和类型安全性。遵循本指南中的最佳实践，可以充分利用 PathConfiguration 的优势，构建更加健壮和可维护的自动化测试框架。