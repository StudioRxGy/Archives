# 故障排除和常见问题解答

## 概述

本指南提供了使用企业级 C# + Playwright + xUnit 自动化测试框架时可能遇到的常见问题的解决方案。

## 📋 目录

- [环境和安装问题](#环境和安装问题)
- [浏览器和驱动问题](#浏览器和驱动问题)
- [元素定位问题](#元素定位问题)
- [测试执行问题](#测试执行问题)
- [数据和配置问题](#数据和配置问题)
- [性能问题](#性能问题)
- [CI/CD 集成问题](#cicd-集成问题)
- [调试技巧](#调试技巧)

## 🔧 环境和安装问题

### Q1: .NET SDK 版本不兼容

**问题描述：**
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 6.0.
```

**解决方案：**
1. 检查当前 .NET 版本：
```bash
dotnet --version
```

2. 安装正确的 .NET 6.0 SDK：
```bash
# Windows
winget install Microsoft.DotNet.SDK.6

# macOS
brew install --cask dotnet

# Linux (Ubuntu)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

3. 验证安装：
```bash
dotnet --list-sdks
```

### Q2: NuGet 包还原失败

**问题描述：**
```
error NU1101: Unable to find package Microsoft.Playwright
```

**解决方案：**
1. 清理 NuGet 缓存：
```bash
dotnet nuget locals all --clear
```

2. 还原包：
```bash
dotnet restore --force
```

3. 如果仍然失败，检查 NuGet 源：
```bash
dotnet nuget list source
```

4. 添加官方 NuGet 源（如果缺失）：
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Q3: Playwright 安装失败

**问题描述：**
```
Failed to install browsers
```

**解决方案：**
1. 手动安装 Playwright 浏览器：
```bash
# Windows PowerShell
pwsh bin/Debug/net6.0/playwright.ps1 install

# Linux/macOS
./bin/Debug/net6.0/playwright.sh install
```

2. 如果权限不足：
```bash
# Linux/macOS
sudo ./bin/Debug/net6.0/playwright.sh install

# Windows (以管理员身份运行)
pwsh -Command "& { bin/Debug/net6.0/playwright.ps1 install }"
```

3. 安装系统依赖（Linux）：
```bash
sudo ./bin/Debug/net6.0/playwright.sh install-deps
```

## 🌐 浏览器和驱动问题
### Q4: 浏览器启动失败

**问题描述：**
```
browserType.launch: Executable doesn't exist
```

**解决方案：**
1. 重新安装浏览器：
```bash
pwsh bin/Debug/net6.0/playwright.ps1 install chromium
```

2. 检查浏览器路径：
```csharp
// 在代码中添加调试信息
var browserPath = await Playwright.CreateAsync();
Console.WriteLine($"Playwright version: {browserPath.Version}");
```

3. 使用系统浏览器（临时解决方案）：
```csharp
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    ExecutablePath = "/usr/bin/google-chrome", // Linux
    // ExecutablePath = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe", // Windows
});
```

### Q5: 无头模式问题

**问题描述：**
测试在有头模式下正常，但在无头模式下失败。

**解决方案：**
1. 临时启用有头模式进行调试：
```json
{
  "Browser": {
    "Headless": false
  }
}
```

2. 检查视口大小：
```csharp
var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
});
```

3. 添加用户代理：
```csharp
var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
});
```

## 🎯 元素定位问题

### Q6: 元素未找到

**问题描述：**
```
ElementNotFoundException: Element not found: #submit-button
```

**解决方案：**
1. 验证选择器：
```csharp
// 使用浏览器开发者工具验证选择器
// F12 -> Console -> document.querySelector('#submit-button')
```

2. 增加等待时间：
```csharp
await page.WaitForSelectorAsync("#submit-button", new PageWaitForSelectorOptions
{
    Timeout = 30000
});
```

3. 使用多种定位策略：
```csharp
// CSS 选择器
await page.ClickAsync("#submit-button");

// XPath
await page.ClickAsync("xpath=//button[@id='submit-button']");

// 文本内容
await page.ClickAsync("text=提交");

// 部分文本匹配
await page.ClickAsync("text=/提交|Submit/");
```

4. 检查元素是否在 iframe 中：
```csharp
var frame = page.Frame("frame-name");
await frame.ClickAsync("#submit-button");
```

### Q7: 元素不可点击

**问题描述：**
```
Element is not clickable at point (x, y)
```

**解决方案：**
1. 等待元素可见：
```csharp
await page.WaitForSelectorAsync("#button", new PageWaitForSelectorOptions
{
    State = WaitForSelectorState.Visible
});
```

2. 滚动到元素：
```csharp
await page.EvaluateAsync("document.querySelector('#button').scrollIntoView()");
await page.ClickAsync("#button");
```

3. 使用 JavaScript 点击：
```csharp
await page.EvaluateAsync("document.querySelector('#button').click()");
```

4. 检查元素是否被遮挡：
```csharp
// 等待遮挡元素消失
await page.WaitForSelectorAsync(".loading-overlay", new PageWaitForSelectorOptions
{
    State = WaitForSelectorState.Hidden
});
```

### Q8: 动态内容加载问题

**问题描述：**
页面内容是动态加载的，元素定位不稳定。

**解决方案：**
1. 等待网络空闲：
```csharp
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

2. 等待特定请求完成：
```csharp
await page.WaitForResponseAsync(response => 
    response.Url.Contains("/api/data") && response.Status == 200);
```

3. 使用自定义等待条件：
```csharp
await page.WaitForFunctionAsync(@"
    () => document.querySelectorAll('.data-item').length > 0
");
```

4. 轮询检查：
```csharp
var maxAttempts = 10;
var attempt = 0;
while (attempt < maxAttempts)
{
    try
    {
        var element = await page.QuerySelectorAsync(".dynamic-content");
        if (element != null && await element.IsVisibleAsync())
            break;
    }
    catch { }
    
    await Task.Delay(1000);
    attempt++;
}
```

## 🏃‍♂️ 测试执行问题

### Q9: 测试超时

**问题描述：**
```
Test exceeded timeout of 30000ms
```

**解决方案：**
1. 增加全局超时：
```csharp
[Fact(Timeout = 60000)] // 60秒超时
public async Task MyTest() { }
```

2. 配置 Playwright 超时：
```csharp
page.SetDefaultTimeout(60000);
page.SetDefaultNavigationTimeout(60000);
```

3. 优化等待策略：
```csharp
// 避免固定等待
// await Task.Delay(5000); // ❌

// 使用智能等待
await page.WaitForSelectorAsync("#element"); // ✅
```

### Q10: 并行执行冲突

**问题描述：**
并行执行时测试相互干扰。

**解决方案：**
1. 确保测试隔离：
```csharp
[Collection("NonParallel")]
public class DatabaseTests { }

[CollectionDefinition("NonParallel", DisableParallelization = true)]
public class NonParallelCollection { }
```

2. 使用独立的浏览器上下文：
```csharp
public class TestFixture : IAsyncLifetime
{
    private IBrowser _browser;
    
    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await _browser.NewContextAsync(); // 每个测试独立上下文
    }
}
```

3. 避免共享状态：
```csharp
// ❌ 共享静态变量
public static string SharedData = "";

// ✅ 使用测试特定数据
public class TestData
{
    public string TestSpecificData { get; set; }
}
```

### Q11: 内存泄漏

**问题描述：**
长时间运行测试后内存使用量持续增长。

**解决方案：**
1. 正确释放资源：
```csharp
public class BrowserFixture : IAsyncLifetime
{
    private IBrowser _browser;
    private readonly List<IBrowserContext> _contexts = new();
    
    public async Task<IBrowserContext> CreateContextAsync()
    {
        var context = await _browser.NewContextAsync();
        _contexts.Add(context);
        return context;
    }
    
    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.CloseAsync();
        }
        await _browser?.CloseAsync();
    }
}
```

2. 限制并发数：
```csharp
private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);

public async Task RunTestAsync()
{
    await _semaphore.WaitAsync();
    try
    {
        // 执行测试
    }
    finally
    {
        _semaphore.Release();
    }
}
```

## 📊 数据和配置问题

### Q12: CSV 数据读取失败

**问题描述：**
```
FileNotFoundException: Could not find file 'TestData/data.csv'
```

**解决方案：**
1. 检查文件路径：
```csharp
var currentDirectory = Directory.GetCurrentDirectory();
var filePath = Path.Combine(currentDirectory, "TestData", "data.csv");
Console.WriteLine($"Looking for file at: {filePath}");
Console.WriteLine($"File exists: {File.Exists(filePath)}");
```

2. 确保文件被复制到输出目录：
```xml
<ItemGroup>
  <None Include="TestData\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

3. 使用绝对路径（临时解决方案）：
```csharp
var projectRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
var filePath = Path.Combine(projectRoot, "TestData", "data.csv");
```

### Q13: 配置文件未加载

**问题描述：**
配置值为默认值，自定义配置未生效。

**解决方案：**
1. 检查配置文件名称：
```
appsettings.json          // 基础配置
appsettings.Development.json  // 开发环境
appsettings.Test.json     // 测试环境
```

2. 设置环境变量：
```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Test

# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Test
```

3. 验证配置加载：
```csharp
public class ConfigurationTests
{
    [Fact]
    public void Configuration_ShouldLoadCorrectly()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .Build();
        
        var baseUrl = config["Environment:BaseUrl"];
        Assert.NotNull(baseUrl);
    }
}
```

### Q14: YAML 元素配置错误

**问题描述：**
```
YamlException: Invalid YAML format
```

**解决方案：**
1. 验证 YAML 格式：
```yaml
# ✅ 正确格式
HomePage:
  SearchBox:
    selector: "#search"
    type: Input
    timeout: 5000

# ❌ 错误格式（缩进不一致）
HomePage:
SearchBox:
  selector: "#search"
```

2. 使用 YAML 验证工具：
```bash
# 在线验证：https://yamlchecker.com/
# 或使用命令行工具
yamllint elements.yaml
```

3. 添加错误处理：
```csharp
public class YamlElementReader
{
    public PageElementCollection LoadElements(string filePath)
    {
        try
        {
            var yaml = File.ReadAllText(filePath);
            return _deserializer.Deserialize<PageElementCollection>(yaml);
        }
        catch (YamlException ex)
        {
            throw new YamlDataException("", filePath, $"YAML 格式错误: {ex.Message}");
        }
    }
}
```

## ⚡ 性能问题

### Q15: 测试执行缓慢

**问题描述：**
测试执行时间过长，影响开发效率。

**解决方案：**
1. 启用并行执行：
```xml
<!-- xunit.runner.json -->
{
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

2. 优化等待策略：
```csharp
// ❌ 固定等待
await Task.Delay(5000);

// ✅ 智能等待
await page.WaitForSelectorAsync("#element", new PageWaitForSelectorOptions
{
    Timeout = 5000
});
```

3. 禁用不必要的资源加载：
```csharp
await context.RouteAsync("**/*.{png,jpg,jpeg,gif,svg,ico}", route => route.AbortAsync());
```

4. 使用更快的浏览器选项：
```csharp
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Args = new[]
    {
        "--no-sandbox",
        "--disable-dev-shm-usage",
        "--disable-gpu",
        "--disable-extensions"
    }
});
```

### Q16: 内存使用过高

**问题描述：**
测试运行时内存使用量过高。

**解决方案：**
1. 限制并发浏览器实例：
```csharp
private readonly SemaphoreSlim _browserSemaphore = new(2); // 最多2个浏览器实例

public async Task<IBrowser> GetBrowserAsync()
{
    await _browserSemaphore.WaitAsync();
    try
    {
        return await _playwright.Chromium.LaunchAsync();
    }
    finally
    {
        _browserSemaphore.Release();
    }
}
```

2. 及时关闭页面和上下文：
```csharp
public async Task RunTestAsync()
{
    var context = await _browser.NewContextAsync();
    var page = await context.NewPageAsync();
    
    try
    {
        // 执行测试
    }
    finally
    {
        await page.CloseAsync();
        await context.CloseAsync();
    }
}
```

3. 监控内存使用：
```csharp
[Fact]
public async Task MonitorMemoryUsage()
{
    var initialMemory = GC.GetTotalMemory(false);
    
    // 执行测试
    await RunTestLogic();
    
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    var finalMemory = GC.GetTotalMemory(false);
    var memoryUsed = finalMemory - initialMemory;
    
    _logger.LogInformation($"Memory used: {memoryUsed / 1024 / 1024} MB");
}
```

## 🔄 CI/CD 集成问题

### Q17: GitHub Actions 中测试失败

**问题描述：**
本地测试通过，但在 GitHub Actions 中失败。

**解决方案：**
1. 安装系统依赖：
```yaml
- name: Install dependencies
  run: |
    sudo apt-get update
    sudo apt-get install -y xvfb
```

2. 设置显示环境：
```yaml
- name: Run tests
  run: xvfb-run --auto-servernum --server-args="-screen 0 1280x960x24" dotnet test
```

3. 增加超时时间：
```yaml
- name: Run tests
  run: dotnet test --logger trx
  timeout-minutes: 30
```

4. 上传失败截图：
```yaml
- name: Upload screenshots
  uses: actions/upload-artifact@v3
  if: failure()
  with:
    name: screenshots
    path: src/conclusion/screenshots/
```

### Q18: Docker 容器中运行问题

**问题描述：**
在 Docker 容器中运行测试时浏览器启动失败。

**解决方案：**
1. 使用正确的基础镜像：
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0

# 安装浏览器依赖
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgbm1 \
    libxss1 \
    libasound2
```

2. 设置无头模式：
```csharp
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,
    Args = new[] { "--no-sandbox", "--disable-dev-shm-usage" }
});
```

3. 使用 Docker Compose：
```yaml
version: '3.8'
services:
  tests:
    build: .
    environment:
      - DISPLAY=:99
    volumes:
      - /tmp/.X11-unix:/tmp/.X11-unix:rw
```

## 🔍 调试技巧

### 调试技巧 1: 启用详细日志

```csharp
// 在 appsettings.json 中
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Playwright": "Information"
    }
  }
}
```

### 调试技巧 2: 使用浏览器开发者工具

```csharp
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    Devtools = true, // 自动打开开发者工具
    SlowMo = 1000    // 减慢操作速度
});
```

### 调试技巧 3: 截图调试

```csharp
public async Task DebugWithScreenshots()
{
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = "before-action.png" });
    
    // 执行操作
    await page.ClickAsync("#button");
    
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = "after-action.png" });
}
```

### 调试技巧 4: 页面内容检查

```csharp
public async Task InspectPageContent()
{
    var content = await page.ContentAsync();
    File.WriteAllText("page-content.html", content);
    
    var title = await page.TitleAsync();
    var url = page.Url;
    
    _logger.LogInformation($"Page title: {title}");
    _logger.LogInformation($"Page URL: {url}");
}
```

### 调试技巧 5: 元素状态检查

```csharp
public async Task CheckElementState(string selector)
{
    var element = await page.QuerySelectorAsync(selector);
    if (element == null)
    {
        _logger.LogWarning($"Element not found: {selector}");
        return;
    }
    
    var isVisible = await element.IsVisibleAsync();
    var isEnabled = await element.IsEnabledAsync();
    var text = await element.InnerTextAsync();
    
    _logger.LogInformation($"Element {selector}: Visible={isVisible}, Enabled={isEnabled}, Text='{text}'");
}
```

## 📞 获取帮助

如果以上解决方案都无法解决您的问题，可以通过以下方式获取帮助：

1. **查看日志文件**：`src/conclusion/logs/` 目录下的详细日志
2. **检查截图**：`src/conclusion/screenshots/` 目录下的失败截图
3. **提交 Issue**：在项目仓库中提交详细的问题描述
4. **联系团队**：通过内部沟通渠道联系开发团队

### 提交 Issue 时请包含：

- 错误的完整堆栈跟踪
- 相关的配置文件内容
- 测试代码片段
- 环境信息（操作系统、.NET 版本、浏览器版本）
- 重现步骤
- 期望的行为和实际行为

这样可以帮助我们更快地定位和解决问题。