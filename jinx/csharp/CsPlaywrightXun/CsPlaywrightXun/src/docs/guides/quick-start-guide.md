# 快速开始指南

## 概述

本指南将帮助您快速上手企业级 C# + Playwright + xUnit 自动化测试框架。通过本指南，您将学会如何设置环境、编写第一个测试用例，并运行测试。

## 🚀 环境准备

### 系统要求

- **操作系统**：Windows 10/11, macOS, Linux
- **.NET 版本**：.NET 6.0 或更高版本
- **IDE**：Visual Studio 2022, VS Code, 或 JetBrains Rider
- **内存**：至少 8GB RAM（推荐 16GB）
- **磁盘空间**：至少 2GB 可用空间

### 安装步骤

#### 1. 安装 .NET SDK

```bash
# Windows (使用 winget)
winget install Microsoft.DotNet.SDK.6

# macOS (使用 Homebrew)
brew install --cask dotnet

# Linux (Ubuntu/Debian)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

#### 2. 验证安装

```bash
dotnet --version
# 应该显示 6.0.x 或更高版本
```

#### 3. 克隆项目

```bash
git clone <repository-url>
cd CsPlaywrightXun
```

#### 4. 还原依赖包

```bash
dotnet restore
```

#### 5. 安装 Playwright 浏览器

```bash
# Windows PowerShell
pwsh bin/Debug/net6.0/playwright.ps1 install

# Linux/macOS
./bin/Debug/net6.0/playwright.sh install
```

## 📝 编写第一个测试

### 1. 创建简单的页面对象

创建文件 `src/playwright/Pages/UI/example/ExamplePage.cs`：

```csharp
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Services.Data;

namespace CsPlaywrightXun.src.playwright.Pages.UI.example
{
    public class ExamplePage : BasePageObjectWithPlaywright
    {
        // 页面元素选择器
        private const string SearchBoxSelector = "#kw";
        private const string SearchButtonSelector = "#su";
        private const string ResultsSelector = ".result";
        
        public ExamplePage(IPage page, ILogger logger, YamlElementReader elementReader = null) 
            : base(page, logger, elementReader)
        {
        }
        
        /// <summary>
        /// 执行搜索操作
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        public async Task SearchAsync(string searchTerm)
        {
            Logger.LogInformation($"开始搜索: {searchTerm}");
            
            // 输入搜索关键词
            await TypeAsync(SearchBoxSelector, searchTerm);
            
            // 点击搜索按钮
            await ClickAsync(SearchButtonSelector);
            
            // 等待结果加载
            await WaitForElementAsync(ResultsSelector, 10000);
            
            Logger.LogInformation("搜索完成");
        }
        
        /// <summary>
        /// 获取搜索结果数量
        /// </summary>
        /// <returns>结果数量</returns>
        public async Task<int> GetSearchResultCountAsync()
        {
            var elements = await _page.QuerySelectorAllAsync(ResultsSelector);
            return elements.Count;
        }
        
        /// <summary>
        /// 检查页面是否已加载
        /// </summary>
        public override async Task<bool> IsLoadedAsync()
        {
            return await IsElementExistAsync(SearchBoxSelector);
        }
        
        /// <summary>
        /// 等待页面加载完成
        /// </summary>
        public override async Task WaitForLoadAsync(int timeoutMs = 30000)
        {
            await WaitForElementAsync(SearchBoxSelector, timeoutMs);
        }
    }
}
```

### 2. 创建业务流程

创建文件 `src/playwright/Flows/example/SearchFlow.cs`：

```csharp
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Pages.UI.example;

namespace CsPlaywrightXun.src.playwright.Flows.example
{
    public class SearchFlow : BaseFlow
    {
        private readonly ExamplePage _examplePage;
        
        public SearchFlow(ExamplePage examplePage, ILogger logger) : base(logger)
        {
            _examplePage = examplePage;
        }
        
        public override async Task ExecuteAsync(Dictionary<string, object> parameters = null)
        {
            var searchTerm = parameters?["searchTerm"]?.ToString() ?? "默认搜索";
            var expectedMinResults = Convert.ToInt32(parameters?["expectedMinResults"] ?? 1);
            
            Logger.LogInformation($"开始执行搜索流程，关键词：{searchTerm}");
            
            // 执行搜索
            await _examplePage.SearchAsync(searchTerm);
            
            // 验证结果数量
            var resultCount = await _examplePage.GetSearchResultCountAsync();
            
            if (resultCount >= expectedMinResults)
            {
                Logger.LogInformation($"搜索成功，找到 {resultCount} 个结果");
            }
            else
            {
                Logger.LogWarning($"搜索结果不足，期望至少 {expectedMinResults} 个，实际 {resultCount} 个");
            }
            
            Logger.LogInformation("搜索流程执行完成");
        }
    }
}
```

### 3. 创建测试数据

创建文件 `src/config/date/UI/example_search_data.csv`：

```csv
TestName,SearchTerm,ExpectedMinResults,BaseUrl
搜索测试1,playwright,5,https://www.baidu.com
搜索测试2,自动化测试,3,https://www.baidu.com
搜索测试3,C#,10,https://www.baidu.com
```

### 4. 创建测试数据模型

创建文件 `src/playwright/Tests/UI/example/ExampleTestData.cs`：

```csharp
namespace CsPlaywrightXun.src.playwright.Tests.UI.example
{
    public class ExampleTestData
    {
        public string TestName { get; set; }
        public string SearchTerm { get; set; }
        public int ExpectedMinResults { get; set; }
        public string BaseUrl { get; set; }
    }
}
```

### 5. 编写测试用例

创建文件 `src/playwright/Tests/UI/example/ExampleTests.cs`：

```csharp
using Xunit;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Pages.UI.example;
using CsPlaywrightXun.src.playwright.Flows.example;

namespace CsPlaywrightXun.src.playwright.Tests.UI.example
{
    [UITest]
    [TestCategory(TestCategory.PageObject)]
    [TestPriority(TestPriority.Medium)]
    public class ExampleTests : IClassFixture<BrowserFixture>
    {
        private readonly BrowserFixture _fixture;
        private readonly ExamplePage _examplePage;
        private readonly SearchFlow _searchFlow;
        private readonly ILogger _logger;
        
        public ExampleTests(BrowserFixture fixture)
        {
            _fixture = fixture;
            _logger = _fixture.Logger;
            _examplePage = new ExamplePage(_fixture.Page, _logger);
            _searchFlow = new SearchFlow(_examplePage, _logger);
        }
        
        [Theory]
        [CsvData("src/config/date/UI/example_search_data.csv")]
        public async Task SearchFunctionality_WithValidTerm_ShouldReturnResults(ExampleTestData data)
        {
            // Arrange - 准备阶段
            _logger.LogInformation($"开始执行测试：{data.TestName}");
            
            await _examplePage.NavigateAsync(data.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Act - 执行阶段
            var parameters = new Dictionary<string, object>
            {
                ["searchTerm"] = data.SearchTerm,
                ["expectedMinResults"] = data.ExpectedMinResults
            };
            
            await _searchFlow.ExecuteAsync(parameters);
            
            // Assert - 断言阶段
            var resultCount = await _examplePage.GetSearchResultCountAsync();
            
            Assert.True(resultCount >= data.ExpectedMinResults, 
                $"期望至少 {data.ExpectedMinResults} 个结果，实际得到 {resultCount} 个");
            
            _logger.LogInformation($"测试完成：{data.TestName}，结果数量：{resultCount}");
        }
        
        [Fact]
        [TestTag("Smoke")]
        public async Task HomePage_ShouldLoadSuccessfully()
        {
            // Arrange
            var baseUrl = "https://www.baidu.com";
            
            // Act
            await _examplePage.NavigateAsync(baseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Assert
            var isLoaded = await _examplePage.IsLoadedAsync();
            Assert.True(isLoaded, "页面应该成功加载");
            
            var title = await _examplePage.GetTitleAsync();
            Assert.Contains("百度", title);
        }
    }
}
```

## 🏃‍♂️ 运行测试

### 1. 运行单个测试

```bash
# 运行特定的测试类
dotnet test --filter "FullyQualifiedName~ExampleTests"

# 运行特定的测试方法
dotnet test --filter "FullyQualifiedName~ExampleTests.HomePage_ShouldLoadSuccessfully"
```

### 2. 运行分类测试

```bash
# 运行所有 UI 测试
dotnet test --filter "Type=UI"

# 运行中等优先级测试
dotnet test --filter "Priority=Medium"

# 运行冒烟测试
dotnet test --filter "Tag=Smoke"
```

### 3. 运行所有测试

```bash
# 运行所有测试
dotnet test

# 运行测试并生成详细输出
dotnet test --verbosity normal

# 运行测试并生成 HTML 报告
dotnet test --logger "html;LogFileName=test-results.html"
```

### 4. 调试模式运行

```bash
# 以非无头模式运行（可以看到浏览器）
dotnet test --filter "Type=UI" -- TestRunParameters.Parameter(name="Browser.Headless", value="false")
```

## 📊 查看结果

### 1. 控制台输出

测试运行时，您将在控制台看到：

```
开始测试运行，请稍候...
总共 1 个测试文件与指定模式匹配。

正在启动测试执行，请稍候...
总共发现 3 个测试
  通过!  - 失败:     0, 通过:     3, 跳过:     0, 总计:     3, 持续时间: 15 s
```

### 2. 日志文件

查看详细日志：`src/conclusion/logs/test-{Date}.log`

```
2024-01-04 10:30:15 [INF] 开始执行测试：搜索测试1
2024-01-04 10:30:16 [INF] 开始搜索: playwright
2024-01-04 10:30:18 [INF] 搜索完成
2024-01-04 10:30:18 [INF] 开始执行搜索流程，关键词：playwright
2024-01-04 10:30:19 [INF] 搜索成功，找到 10 个结果
2024-01-04 10:30:19 [INF] 搜索流程执行完成
2024-01-04 10:30:19 [INF] 测试完成：搜索测试1，结果数量：10
```

### 3. 截图文件

如果测试失败，会自动生成截图：`src/conclusion/screenshots/`

### 4. HTML 报告

如果生成了 HTML 报告，可以在浏览器中打开查看详细结果。

## 🔧 配置调整

### 1. 修改浏览器设置

编辑配置文件以调整浏览器行为：

```json
{
  "Browser": {
    "Type": "Chromium",
    "Headless": false,
    "ViewportWidth": 1920,
    "ViewportHeight": 1080,
    "Timeout": 30000
  }
}
```

### 2. 调整日志级别

```json
{
  "Logging": {
    "Level": "Debug"
  }
}
```

### 3. 设置并行执行

```json
{
  "TestExecution": {
    "ParallelExecution": true,
    "MaxParallelism": 4
  }
}
```

## 🎯 下一步

现在您已经成功运行了第一个测试！接下来可以：

1. **学习更多功能**：
   - [API 测试指南](api-testing-guide.md)
   - [数据驱动测试](data-driven-testing.md)
   - [Page Object 模式详解](page-object-guide.md)

2. **探索高级特性**：
   - [测试分类和过滤](TestCategoryGuide.md)
   - [错误恢复机制](error-recovery-guide.md)
   - [报告和分析](reporting-guide.md)

3. **最佳实践**：
   - [代码组织规范](best-practices.md)
   - [性能优化技巧](performance-guide.md)
   - [CI/CD 集成](ci-cd-integration.md)

## ❓ 常见问题

### Q: 测试运行很慢怎么办？

A: 可以尝试以下方法：
- 启用并行执行
- 使用无头模式
- 优化等待时间
- 减少不必要的操作

### Q: 元素定位失败怎么办？

A: 检查以下几点：
- 选择器是否正确
- 页面是否完全加载
- 元素是否在视口内
- 是否需要等待更长时间

### Q: 如何调试测试？

A: 推荐的调试方法：
- 设置 `Headless: false` 查看浏览器行为
- 增加日志输出
- 使用断点调试
- 查看截图文件

### Q: 如何添加新的测试数据？

A: 编辑对应的 CSV 文件，添加新的数据行即可。确保数据格式与模型类匹配。

恭喜！您已经成功完成了快速开始指南。现在可以开始构建更复杂的测试场景了。