# 设计文档

## 概述

本文档描述了企业级 C# + Playwright + xUnit 自动化测试框架的详细设计。该框架采用分层架构，支持 Web UI 和 API 自动化测试，具有高可维护性、稳定性和可扩展性。

## 架构

### 整体架构图

```mermaid
graph TB
    subgraph "测试层 (Test Layer)"
        UT[UI 测试]
        AT[API 测试]
        IT[集成测试]
    end
    
    subgraph "场景层 (Scenario Layer)"
        SC[场景编排]
        FL[业务流程]
    end
    
    subgraph "页面对象层 (Page Object Layer)"
        PO[页面对象]
        PC[页面组件]
        PE[页面元素]
    end
    
    subgraph "服务层 (Service Layer)"
        BS[浏览器服务]
        AS[API 服务]
        DS[数据服务]
        CS[配置服务]
    end
    
    subgraph "基础设施层 (Infrastructure Layer)"
        LF[日志框架]
        RF[报告框架]
        CF[配置文件]
        TU[测试工具]
    end
    
    subgraph "外部配置 (External Config)"
        CSV[CSV 数据文件]
        YAML[YAML 元素文件]
        JSON[JSON 配置文件]
        ENV[环境文件]
    end
    
    UT --> SC
    UT --> PO
    AT --> AS
    SC --> FL
    FL --> PO
    PO --> BS
    PO --> PE
    BS --> LF
    AS --> LF
    DS --> CSV
    PE --> YAML
    CS --> JSON
    CS --> ENV
    
    LF --> RF
```

### 分层架构说明

1. **测试层 (Test Layer)**：包含具体的测试用例实现，负责断言和测试意图表达
2. **场景层 (Scenario Layer)**：处理多流程编排和业务流程抽象
3. **页面对象层 (Page Object Layer)**：封装页面元素和操作
4. **服务层 (Service Layer)**：提供核心业务服务
5. **基础设施层 (Infrastructure Layer)**：提供框架基础功能

## 组件和接口

### 1. 核心接口设计

#### IPageObject 接口
```csharp
/// <summary>
/// 页面对象基础接口
/// </summary>
public interface IPageObject
{
    /// <summary>
    /// 导航到指定URL
    /// </summary>
    /// <param name="url">目标URL</param>
    Task NavigateAsync(string url);
    
    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    /// <returns>页面加载状态</returns>
    Task<bool> IsLoadedAsync();
    
    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    Task WaitForLoadAsync(int timeoutMs = 30000);
}
```

#### ITestFixture 接口
```csharp
/// <summary>
/// 测试固件接口，管理 Playwright 生命周期
/// </summary>
public interface ITestFixture : IAsyncLifetime
{
    /// <summary>
    /// Playwright 实例
    /// </summary>
    IPlaywright Playwright { get; }
    
    /// <summary>
    /// 浏览器实例
    /// </summary>
    IBrowser Browser { get; }
    
    /// <summary>
    /// 浏览器上下文
    /// </summary>
    IBrowserContext Context { get; }
    
    /// <summary>
    /// 页面实例
    /// </summary>
    IPage Page { get; }
    
    /// <summary>
    /// 测试配置
    /// </summary>
    TestConfiguration Configuration { get; }
}
```

#### IApiClient 接口
```csharp
/// <summary>
/// API 客户端接口
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// 发送 GET 请求
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> headers = null);
    
    /// <summary>
    /// 发送 POST 请求
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, object data, Dictionary<string, string> headers = null);
    
    /// <summary>
    /// 发送 PUT 请求
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, object data, Dictionary<string, string> headers = null);
    
    /// <summary>
    /// 发送 DELETE 请求
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string> headers = null);
}
```

#### IFlow 接口
```csharp
/// <summary>
/// 业务流程接口
/// </summary>
public interface IFlow
{
    /// <summary>
    /// 执行业务流程
    /// </summary>
    /// <param name="parameters">流程参数</param>
    Task ExecuteAsync(Dictionary<string, object> parameters = null);
}
```

### 2. 配置管理组件

#### TestConfiguration 类
```csharp
/// <summary>
/// 测试配置类
/// </summary>
public class TestConfiguration
{
    /// <summary>
    /// 环境设置
    /// </summary>
    public EnvironmentSettings Environment { get; set; }
    
    /// <summary>
    /// 浏览器设置
    /// </summary>
    public BrowserSettings Browser { get; set; }
    
    /// <summary>
    /// API 设置
    /// </summary>
    public ApiSettings Api { get; set; }
    
    /// <summary>
    /// 报告设置
    /// </summary>
    public ReportingSettings Reporting { get; set; }
    
    /// <summary>
    /// 日志设置
    /// </summary>
    public LoggingSettings Logging { get; set; }
}
```

#### 环境配置
```csharp
/// <summary>
/// 环境设置
/// </summary>
public class EnvironmentSettings
{
    /// <summary>
    /// 环境名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; }
    
    /// <summary>
    /// API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; }
    
    /// <summary>
    /// 环境变量
    /// </summary>
    public Dictionary<string, string> Variables { get; set; }
}
```

### 3. 数据管理组件

#### CSV数据读取器
```csharp
/// <summary>
/// CSV 数据读取器
/// </summary>
public class CsvDataReader
{
    /// <summary>
    /// 读取强类型数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="filePath">文件路径</param>
    /// <returns>数据集合</returns>
    public IEnumerable<T> ReadData<T>(string filePath) where T : class, new();
    
    /// <summary>
    /// 读取动态数据
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>动态数据集合</returns>
    public IEnumerable<Dictionary<string, object>> ReadDynamicData(string filePath);
}
```

#### YAML元素读取器
```csharp
/// <summary>
/// YAML 元素读取器
/// </summary>
public class YamlElementReader
{
    /// <summary>
    /// 加载页面元素集合
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>页面元素集合</returns>
    public PageElementCollection LoadElements(string filePath);
    
    /// <summary>
    /// 获取指定页面的元素
    /// </summary>
    /// <param name="pageName">页面名称</param>
    /// <param name="elementName">元素名称</param>
    /// <returns>页面元素</returns>
    public PageElement GetElement(string pageName, string elementName);
}
```

### 4. 浏览器服务组件

#### BrowserService 类
```csharp
/// <summary>
/// 浏览器服务
/// </summary>
public class BrowserService : IBrowserService
{
    /// <summary>
    /// 创建页面实例
    /// </summary>
    /// <param name="settings">浏览器设置</param>
    /// <returns>页面实例</returns>
    public async Task<IPage> CreatePageAsync(BrowserSettings settings);
    
    /// <summary>
    /// 截取屏幕截图
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="fileName">文件名</param>
    /// <returns>截图字节数组</returns>
    public async Task<byte[]> TakeScreenshotAsync(IPage page, string fileName);
    
    /// <summary>
    /// 关闭浏览器服务
    /// </summary>
    public async Task CloseAsync();
}
```

### 5. API服务组件

#### ApiService 类
```csharp
/// <summary>
/// API 服务
/// </summary>
public class ApiService : IApiService
{
    /// <summary>
    /// 发送API请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    public async Task<ApiResponse<T>> SendRequestAsync<T>(ApiRequest request);
    
    /// <summary>
    /// 验证API响应
    /// </summary>
    /// <param name="response">HTTP响应</param>
    /// <param name="validation">验证规则</param>
    /// <returns>验证结果</returns>
    public async Task<ApiResponse> ValidateResponseAsync(HttpResponseMessage response, ApiValidation validation);
}
```

## 数据模型

### 1. 页面元素模型

```csharp
/// <summary>
/// 页面元素
/// </summary>
public class PageElement
{
    /// <summary>
    /// 元素名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 元素选择器
    /// </summary>
    public string Selector { get; set; }
    
    /// <summary>
    /// 元素类型
    /// </summary>
    public ElementType Type { get; set; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 元素属性
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; }
}

/// <summary>
/// 元素类型枚举
/// </summary>
public enum ElementType
{
    /// <summary>
    /// 按钮
    /// </summary>
    Button,
    
    /// <summary>
    /// 输入框
    /// </summary>
    Input,
    
    /// <summary>
    /// 链接
    /// </summary>
    Link,
    
    /// <summary>
    /// 文本
    /// </summary>
    Text,
    
    /// <summary>
    /// 下拉框
    /// </summary>
    Dropdown,
    
    /// <summary>
    /// 复选框
    /// </summary>
    Checkbox,
    
    /// <summary>
    /// 单选框
    /// </summary>
    Radio
}
```

### 2. 测试数据模型

```csharp
/// <summary>
/// 测试数据
/// </summary>
public class TestData
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; }
    
    /// <summary>
    /// 测试参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }
    
    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
}
```

### 3. API请求模型

```csharp
/// <summary>
/// API 请求
/// </summary>
public class ApiRequest
{
    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string Method { get; set; }
    
    /// <summary>
    /// 请求端点
    /// </summary>
    public string Endpoint { get; set; }
    
    /// <summary>
    /// 请求体
    /// </summary>
    public object Body { get; set; }
    
    /// <summary>
    /// 请求头
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }
    
    /// <summary>
    /// 查询参数
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; }
}

/// <summary>
/// API 响应
/// </summary>
/// <typeparam name="T">响应数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 响应数据
    /// </summary>
    public T Data { get; set; }
    
    /// <summary>
    /// 原始内容
    /// </summary>
    public string RawContent { get; set; }
    
    /// <summary>
    /// 响应头
    /// </summary>
    public Dictionary<string, string> Headers { get; set; }
    
    /// <summary>
    /// 响应时间
    /// </summary>
    public TimeSpan ResponseTime { get; set; }
}
```

### 4. 测试结果模型

```csharp
/// <summary>
/// 测试结果
/// </summary>
public class TestResult
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; }
    
    /// <summary>
    /// 测试状态
    /// </summary>
    public TestStatus Status { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// 执行时长
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    public string StackTrace { get; set; }
    
    /// <summary>
    /// 截图列表
    /// </summary>
    public List<string> Screenshots { get; set; }
    
    /// <summary>
    /// 测试数据
    /// </summary>
    public Dictionary<string, object> TestData { get; set; }
}

/// <summary>
/// 测试状态枚举
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// 通过
    /// </summary>
    Passed,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 跳过
    /// </summary>
    Skipped,
    
    /// <summary>
    /// 不确定
    /// </summary>
    Inconclusive
}
```

## 错误处理

### 1. 异常处理策略

#### 自定义异常类型
```csharp
/// <summary>
/// 测试框架异常基类
/// </summary>
public class TestFrameworkException : Exception
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; }
    
    /// <summary>
    /// 组件名称
    /// </summary>
    public string Component { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="component">组件名称</param>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public TestFrameworkException(string testName, string component, string message, Exception innerException = null)
        : base(message, innerException)
    {
        TestName = testName;
        Component = component;
    }
}

/// <summary>
/// 元素未找到异常
/// </summary>
public class ElementNotFoundException : TestFrameworkException
{
    /// <summary>
    /// 元素选择器
    /// </summary>
    public string Selector { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="selector">元素选择器</param>
    /// <param name="message">错误消息</param>
    public ElementNotFoundException(string testName, string selector, string message)
        : base(testName, "PageObject", message)
    {
        Selector = selector;
    }
}

/// <summary>
/// API 异常
/// </summary>
public class ApiException : TestFrameworkException
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int StatusCode { get; }
    
    /// <summary>
    /// 请求端点
    /// </summary>
    public string Endpoint { get; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="endpoint">请求端点</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="message">错误消息</param>
    public ApiException(string testName, string endpoint, int statusCode, string message)
        : base(testName, "ApiService", message)
    {
        StatusCode = statusCode;
        Endpoint = endpoint;
    }
}
```

### 2. 重试机制

```csharp
/// <summary>
/// 重试策略
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// 重试间隔时间
    /// </summary>
    public TimeSpan DelayBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// 可重试的异常类型
    /// </summary>
    public List<Type> RetryableExceptions { get; set; }
}
```

### 3. 错误恢复策略

- **页面刷新恢复**：当页面元素不可用时自动刷新页面
- **浏览器重启恢复**：当浏览器崩溃时自动重启浏览器实例
- **API重试恢复**：当API调用失败时根据配置进行重试

## 测试策略

### 1. UI测试策略

#### Page Object模式实现
```csharp
/// <summary>
/// 页面对象基类
/// </summary>
public abstract class BasePage : IPageObject
{
    protected readonly IPage _page;
    protected readonly YamlElementReader _elementReader;
    protected readonly ILogger _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="elementReader">元素读取器</param>
    /// <param name="logger">日志记录器</param>
    protected BasePage(IPage page, YamlElementReader elementReader, ILogger logger)
    {
        _page = page;
        _elementReader = elementReader;
        _logger = logger;
    }
    
    /// <summary>
    /// 导航到指定URL
    /// </summary>
    /// <param name="url">目标URL</param>
    public virtual async Task NavigateAsync(string url)
    {
        await _page.GotoAsync(url);
        await WaitForLoadAsync();
    }
    
    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    /// <returns>页面加载状态</returns>
    public abstract Task<bool> IsLoadedAsync();
    
    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public abstract Task WaitForLoadAsync(int timeoutMs = 30000);
}

/// <summary>
/// 首页页面对象
/// </summary>
public class HomePage : BasePage
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="elementReader">元素读取器</param>
    /// <param name="logger">日志记录器</param>
    public HomePage(IPage page, YamlElementReader elementReader, ILogger logger)
        : base(page, elementReader, logger) { }
    
    /// <summary>
    /// 执行搜索操作
    /// </summary>
    /// <param name="query">搜索关键词</param>
    public async Task SearchAsync(string query)
    {
        var searchBox = _elementReader.GetElement("HomePage", "SearchBox");
        await _page.FillAsync(searchBox.Selector, query);
        
        var searchButton = _elementReader.GetElement("HomePage", "SearchButton");
        await _page.ClickAsync(searchButton.Selector);
    }
    
    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    /// <returns>页面加载状态</returns>
    public override async Task<bool> IsLoadedAsync()
    {
        var searchBox = _elementReader.GetElement("HomePage", "SearchBox");
        return await _page.IsVisibleAsync(searchBox.Selector);
    }
    
    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        var searchBox = _elementReader.GetElement("HomePage", "SearchBox");
        await _page.WaitForSelectorAsync(searchBox.Selector, new() { Timeout = timeoutMs });
    }
}
```

### 2. Flow 模式实现

```csharp
/// <summary>
/// 搜索流程
/// </summary>
public class SearchFlow : IFlow
{
    private readonly HomePage _homePage;
    private readonly ILogger _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="homePage">首页对象</param>
    /// <param name="logger">日志记录器</param>
    public SearchFlow(HomePage homePage, ILogger logger)
    {
        _homePage = homePage;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行搜索流程
    /// </summary>
    /// <param name="parameters">流程参数</param>
    public async Task ExecuteAsync(Dictionary<string, object> parameters = null)
    {
        var searchQuery = parameters?["searchQuery"]?.ToString() ?? "默认搜索";
        
        _logger.LogInformation($"开始执行搜索流程，关键词：{searchQuery}");
        
        // 执行搜索操作
        await _homePage.SearchAsync(searchQuery);
        
        _logger.LogInformation("搜索流程执行完成");
    }
}
```

### 3. 测试用例结构

```csharp
/// <summary>
/// 首页测试类
/// </summary>
[Trait("Type", "UI")]
public class HomePageTests : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;
    private readonly HomePage _homePage;
    private readonly SearchFlow _searchFlow;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">浏览器固件</param>
    public HomePageTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _homePage = new HomePage(_fixture.Page, new YamlElementReader(), _fixture.Logger);
        _searchFlow = new SearchFlow(_homePage, _fixture.Logger);
    }
    
    /// <summary>
    /// 搜索功能测试
    /// </summary>
    /// <param name="data">测试数据</param>
    [Theory]
    [CsvData("TestData/search_data.csv")]
    public async Task SearchFunctionality_ShouldReturnResults(SearchTestData data)
    {
        // Arrange - 准备
        await _homePage.NavigateAsync(data.BaseUrl);
        
        // Act - 执行
        var parameters = new Dictionary<string, object>
        {
            ["searchQuery"] = data.SearchQuery
        };
        await _searchFlow.ExecuteAsync(parameters);
        
        // Assert - 断言
        var results = await _homePage.GetSearchResultsAsync();
        Assert.NotEmpty(results);
        Assert.True(results.Count >= data.ExpectedResultCount);
    }
}
```

### 4. API测试策略

#### API测试基类
```csharp
/// <summary>
/// API 测试基类
/// </summary>
[Trait("Type", "API")]
public abstract class BaseApiTest
{
    protected readonly IApiClient _apiClient;
    protected readonly TestConfiguration _configuration;
    protected readonly ILogger _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="configuration">测试配置</param>
    /// <param name="logger">日志记录器</param>
    protected BaseApiTest(IApiClient apiClient, TestConfiguration configuration, ILogger logger)
    {
        _apiClient = apiClient;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行API测试
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="request">API请求</param>
    /// <returns>API响应</returns>
    protected async Task<ApiResponse<T>> ExecuteApiTestAsync<T>(ApiRequest request)
    {
        try
        {
            var response = await _apiClient.SendRequestAsync<T>(request);
            _logger.LogInformation($"API调用成功: {request.Method} {request.Endpoint}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"API调用失败: {request.Method} {request.Endpoint}");
            throw;
        }
    }
}
```

## 配置文件结构

### 1. 环境配置 (appsettings.{Environment}.json)

```json
{
  "Environment": {
    "Name": "Development",
    "BaseUrl": "https://www.baidu.com",
    "ApiBaseUrl": "https://www.baidu.com/api"
  },
  "Browser": {
    "Type": "Chromium",
    "Headless": false,
    "ViewportWidth": 1920,
    "ViewportHeight": 1080,
    "Timeout": 30000
  },
  "Api": {
    "Timeout": 30000,
    "RetryCount": 3,
    "RetryDelay": 1000
  },
  "Logging": {
    "Level": "Information",
    "FilePath": "Logs/test-{Date}.log"
  },
  "Reporting": {
    "OutputPath": "Reports",
    "Format": "Html",
    "IncludeScreenshots": true
  }
}
```

### 2. 页面元素配置 (Elements/HomePage.yaml)

```yaml
HomePage:
  SearchBox:
    selector: "#kw"
    type: Input
    timeout: 5000
  SearchButton:
    selector: "#su"
    type: Button
    timeout: 5000
  SearchResults:
    selector: ".result"
    type: Text
    timeout: 10000
```

### 3. 测试数据配置 (TestData/search_data.csv)

```csv
TestName,SearchQuery,ExpectedResultCount,Environment
搜索功能测试1,playwright,10,Development
搜索功能测试2,自动化测试,5,Development
搜索功能测试3,C#,15,Development
```

## 日志和报告

### 1. 日志框架集成

使用Serilog进行结构化日志记录：

```csharp
/// <summary>
/// 日志配置
/// </summary>
public static class LoggerConfiguration
{
    /// <summary>
    /// 创建日志记录器
    /// </summary>
    /// <param name="settings">日志设置</param>
    /// <returns>日志记录器实例</returns>
    public static ILogger CreateLogger(LoggingSettings settings)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(settings.Level)
            .WriteTo.Console()
            .WriteTo.File(settings.FilePath, rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341") // 可选的集中日志服务
            .CreateLogger();
    }
}
```

### 2. 报告生成

#### 报告数据模型
```csharp
/// <summary>
/// 测试报告
/// </summary>
public class TestReport
{
    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; }
    
    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; }
    
    /// <summary>
    /// 测试摘要
    /// </summary>
    public TestSummary Summary { get; set; }
    
    /// <summary>
    /// 测试结果列表
    /// </summary>
    public List<TestResult> Results { get; set; }
    
    /// <summary>
    /// 截图列表
    /// </summary>
    public List<string> Screenshots { get; set; }
    
    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }
}

/// <summary>
/// 测试摘要
/// </summary>
public class TestSummary
{
    /// <summary>
    /// 总测试数
    /// </summary>
    public int TotalTests { get; set; }
    
    /// <summary>
    /// 通过测试数
    /// </summary>
    public int PassedTests { get; set; }
    
    /// <summary>
    /// 失败测试数
    /// </summary>
    public int FailedTests { get; set; }
    
    /// <summary>
    /// 跳过测试数
    /// </summary>
    public int SkippedTests { get; set; }
    
    /// <summary>
    /// 总执行时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }
    
    /// <summary>
    /// 通过率
    /// </summary>
    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
}
```

## 项目结构

```
EnterpriseAutomationFramework/
├── src/
│   ├── Framework/                          # 框架核心
│   │   ├── Core/                          # 核心接口和基类
│   │   │   ├── Interfaces/                # 接口定义
│   │   │   ├── Base/                      # 基类实现
│   │   │   └── Exceptions/                # 自定义异常
│   │   ├── PageObjects/                   # 页面对象
│   │   │   ├── Base/                      # 页面对象基类
│   │   │   └── Pages/                     # 具体页面实现
│   │   ├── Flows/                         # 业务流程
│   │   ├── Services/                      # 服务层
│   │   │   ├── Browser/                   # 浏览器服务
│   │   │   ├── Api/                       # API服务
│   │   │   ├── Data/                      # 数据服务
│   │   │   └── Configuration/             # 配置服务
│   │   └── Utilities/                     # 工具类
│   │       ├── Logging/                   # 日志工具
│   │       ├── Reporting/                 # 报告工具
│   │       └── Extensions/                # 扩展方法
│   └── Tests/                             # 测试项目
│       ├── UI/                            # UI测试
│       ├── API/                           # API测试
│       ├── Integration/                   # 集成测试
│       └── Fixtures/                      # 测试固件
├── config/                                # 配置文件
│   ├── environments/                      # 环境配置
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Test.json
│   │   ├── appsettings.Staging.json
│   │   └── appsettings.Production.json
│   ├── elements/                          # 页面元素配置
│   │   ├── HomePage.yaml
│   │   └── SearchPage.yaml
│   └── testdata/                          # 测试数据
│       ├── search_data.csv
│       └── user_data.json
├── reports/                               # 测试报告输出
├── logs/                                  # 日志文件
├── screenshots/                           # 截图文件
└── docs/                                  # 文档
    ├── README.md
    ├── 架构设计.md
    └── 使用指南.md
```

这个设计文档提供了完整的技术架构和实现细节，确保框架具有高可维护性、稳定性和可扩展性。