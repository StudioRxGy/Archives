# API 参考文档

## 概述

本文档提供了企业级 C# + Playwright + xUnit 自动化测试框架的完整 API 参考。包括所有核心接口、基类、服务类和工具类的详细说明。

## 📚 目录

- [核心接口](#核心接口)
- [基类](#基类)
- [服务类](#服务类)
- [数据模型](#数据模型)
- [异常类](#异常类)
- [工具类](#工具类)
- [属性和标记](#属性和标记)

## 🔌 核心接口

### IPageObject

页面对象基础接口，定义了页面操作的基本契约。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Interfaces
{
    public interface IPageObject
    {
        /// <summary>
        /// 导航到指定URL
        /// </summary>
        /// <param name="url">目标URL</param>
        /// <returns>异步任务</returns>
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
        /// <returns>异步任务</returns>
        Task WaitForLoadAsync(int timeoutMs = 30000);
    }
}
```

**使用示例：**

```csharp
public class MyPage : IPageObject
{
    public async Task NavigateAsync(string url)
    {
        await _page.GotoAsync(url);
    }
    
    public async Task<bool> IsLoadedAsync()
    {
        return await _page.IsVisibleAsync("#main-content");
    }
    
    public async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await _page.WaitForSelectorAsync("#main-content", new() { Timeout = timeoutMs });
    }
}
```

### ITestFixture

测试固件接口，管理 Playwright 生命周期。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Interfaces
{
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
        
        /// <summary>
        /// 日志记录器
        /// </summary>
        ILogger Logger { get; }
    }
}
```

### IApiClient

API 客户端接口，提供 HTTP 请求功能。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Interfaces
{
    public interface IApiClient
    {
        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        /// <param name="endpoint">请求端点</param>
        /// <param name="headers">请求头</param>
        /// <returns>HTTP 响应</returns>
        Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> headers = null);
        
        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        /// <param name="endpoint">请求端点</param>
        /// <param name="data">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>HTTP 响应</returns>
        Task<HttpResponseMessage> PostAsync(string endpoint, object data, Dictionary<string, string> headers = null);
        
        /// <summary>
        /// 发送 PUT 请求
        /// </summary>
        /// <param name="endpoint">请求端点</param>
        /// <param name="data">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <returns>HTTP 响应</returns>
        Task<HttpResponseMessage> PutAsync(string endpoint, object data, Dictionary<string, string> headers = null);
        
        /// <summary>
        /// 发送 DELETE 请求
        /// </summary>
        /// <param name="endpoint">请求端点</param>
        /// <param name="headers">请求头</param>
        /// <returns>HTTP 响应</returns>
        Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string> headers = null);
    }
}
```

### IFlow

业务流程接口，定义业务流程的执行契约。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Interfaces
{
    public interface IFlow
    {
        /// <summary>
        /// 执行业务流程
        /// </summary>
        /// <param name="parameters">流程参数</param>
        /// <returns>异步任务</returns>
        Task ExecuteAsync(Dictionary<string, object> parameters = null);
    }
}
```

## 🏗️ 基类

### BasePageObjectWithPlaywright

Playwright 页面对象基类，提供丰富的页面操作方法。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Base
{
    public abstract class BasePageObjectWithPlaywright : IPageObject
    {
        protected readonly IPage _page;
        protected readonly ILogger Logger;
        protected readonly YamlElementReader _elementReader;
        
        // 统计属性
        public int PassCount { get; private set; }
        public int FailCount { get; private set; }
        
        protected BasePageObjectWithPlaywright(IPage page, ILogger logger, YamlElementReader elementReader = null)
        {
            _page = page;
            Logger = logger;
            _elementReader = elementReader;
        }
        
        // 导航方法
        public virtual async Task NavigateAsync(string url) { }
        public virtual async Task RefreshAsync() { }
        public virtual string GetCurrentUrl() { }
        public virtual async Task CloseAsync() { }
        
        // 元素等待方法
        public virtual async Task WaitForElementAsync(string selector, int timeoutMs = 30000) { }
        public virtual async Task<bool> IsElementExistAsync(string selector, int timeoutMs = 5000) { }
        public virtual async Task SleepAsync(int seconds) { }
        
        // 输入方法
        public virtual async Task TypeAsync(string selector, string text) { }
        public virtual async Task ClearAndTypeAsync(string selector, string text) { }
        public virtual async Task TypeAndEnterAsync(string selector, string text, int delayMs = 100) { }
        
        // 点击方法
        public virtual async Task ClickAsync(string selector) { }
        public virtual async Task RightClickAsync(string selector) { }
        public virtual async Task DoubleClickAsync(string selector) { }
        public virtual async Task ClickLinkTextAsync(string linkText) { }
        
        // 鼠标操作方法
        public virtual async Task HoverAsync(string selector) { }
        public virtual async Task DragAndDropAsync(string sourceSelector, string targetSelector) { }
        
        // 获取信息方法
        public virtual async Task<string> GetTextAsync(string selector) { }
        public virtual async Task<string> GetAttributeAsync(string selector, string attributeName) { }
        public virtual async Task<string> GetTitleAsync() { }
        public virtual string GetUrl() { }
        
        // JavaScript 执行方法
        public virtual async Task<object> ExecuteJavaScriptAsync(string script) { }
        public virtual async Task ClickByJavaScriptAsync(string selector) { }
        public virtual async Task ScrollToAsync(int x, int y) { }
        
        // 截图方法
        public virtual async Task<byte[]> TakeScreenshotAsync(string fileName = null) { }
        
        // 断言方法
        public virtual async Task<string> AssertEqualAsync(object actual, object expected) { }
        public virtual async Task<string> AssertNotEqualAsync(object actual, object expected) { }
        public virtual async Task<string> IsTextInElementAsync(string selector, string expectedText) { }
        public virtual async Task<string> IsTitleEqualAsync(string expectedTitle) { }
        public virtual async Task<string> IsTitleContainsAsync(string expectedText) { }
        
        // 统计方法
        public int GetPassCount() => PassCount;
        public int GetFailCount() => FailCount;
        public void ResetCounts() { PassCount = 0; FailCount = 0; }
        
        // 抽象方法（子类必须实现）
        public abstract Task<bool> IsLoadedAsync();
        public abstract Task WaitForLoadAsync(int timeoutMs = 30000);
    }
}
```

**使用示例：**

```csharp
public class LoginPage : BasePageObjectWithPlaywright
{
    private const string UsernameSelector = "#username";
    private const string PasswordSelector = "#password";
    private const string LoginButtonSelector = "#login-btn";
    
    public LoginPage(IPage page, ILogger logger) : base(page, logger) { }
    
    public async Task LoginAsync(string username, string password)
    {
        await TypeAsync(UsernameSelector, username);
        await TypeAsync(PasswordSelector, password);
        await ClickAsync(LoginButtonSelector);
    }
    
    public override async Task<bool> IsLoadedAsync()
    {
        return await IsElementExistAsync(LoginButtonSelector);
    }
    
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await WaitForElementAsync(LoginButtonSelector, timeoutMs);
    }
}
```

### BaseFlow

业务流程基类，提供流程执行的基础功能。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Base
{
    public abstract class BaseFlow : IFlow
    {
        protected readonly ILogger Logger;
        
        protected BaseFlow(ILogger logger)
        {
            Logger = logger;
        }
        
        /// <summary>
        /// 执行业务流程（抽象方法，子类必须实现）
        /// </summary>
        /// <param name="parameters">流程参数</param>
        /// <returns>异步任务</returns>
        public abstract Task ExecuteAsync(Dictionary<string, object> parameters = null);
        
        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="parameters">参数字典</param>
        /// <param name="requiredKeys">必需的参数键</param>
        protected virtual void ValidateParameters(Dictionary<string, object> parameters, params string[] requiredKeys)
        {
            if (parameters == null && requiredKeys.Length > 0)
            {
                throw new ArgumentNullException(nameof(parameters), "参数不能为空");
            }
            
            foreach (var key in requiredKeys)
            {
                if (!parameters.ContainsKey(key))
                {
                    throw new ArgumentException($"缺少必需参数: {key}");
                }
            }
        }
        
        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameters">参数字典</param>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        protected virtual T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue = default)
        {
            if (parameters?.ContainsKey(key) == true)
            {
                return (T)Convert.ChangeType(parameters[key], typeof(T));
            }
            return defaultValue;
        }
    }
}
```

### BaseApiTest

API 测试基类，提供 API 测试的基础功能。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Base
{
    public abstract class BaseApiTest
    {
        protected readonly IApiClient _apiClient;
        protected readonly TestConfiguration _configuration;
        protected readonly ILogger _logger;
        
        protected BaseApiTest(IApiClient apiClient, TestConfiguration configuration, ILogger logger)
        {
            _apiClient = apiClient;
            _configuration = configuration;
            _logger = logger;
        }
        
        /// <summary>
        /// 执行 API 测试
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="request">API 请求</param>
        /// <returns>API 响应</returns>
        protected async Task<ApiResponse<T>> ExecuteApiTestAsync<T>(ApiRequest request)
        {
            try
            {
                _logger.LogInformation($"发送 API 请求: {request.Method} {request.Endpoint}");
                
                var stopwatch = Stopwatch.StartNew();
                var response = await SendRequestAsync<T>(request);
                stopwatch.Stop();
                
                response.ResponseTime = stopwatch.Elapsed;
                
                _logger.LogInformation($"API 请求完成: {request.Method} {request.Endpoint}, " +
                                     $"状态码: {response.StatusCode}, 耗时: {response.ResponseTime.TotalMilliseconds}ms");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API 请求失败: {request.Method} {request.Endpoint}");
                throw;
            }
        }
        
        /// <summary>
        /// 发送 API 请求
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="request">API 请求</param>
        /// <returns>API 响应</returns>
        protected abstract Task<ApiResponse<T>> SendRequestAsync<T>(ApiRequest request);
        
        /// <summary>
        /// 验证响应状态码
        /// </summary>
        /// <param name="response">API 响应</param>
        /// <param name="expectedStatusCode">期望的状态码</param>
        protected void AssertStatusCode<T>(ApiResponse<T> response, int expectedStatusCode)
        {
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }
        
        /// <summary>
        /// 验证响应时间
        /// </summary>
        /// <param name="response">API 响应</param>
        /// <param name="maxResponseTimeMs">最大响应时间（毫秒）</param>
        protected void AssertResponseTime<T>(ApiResponse<T> response, int maxResponseTimeMs)
        {
            Assert.True(response.ResponseTime.TotalMilliseconds <= maxResponseTimeMs,
                $"响应时间 {response.ResponseTime.TotalMilliseconds}ms 超过了最大限制 {maxResponseTimeMs}ms");
        }
    }
}
```

## 🔧 服务类

### BrowserService

浏览器服务类，管理 Playwright 浏览器实例。

```csharp
namespace CsPlaywrightXun.src.playwright.Services.Browser
{
    public class BrowserService : IBrowserService
    {
        private readonly ILogger _logger;
        private IPlaywright _playwright;
        private IBrowser _browser;
        
        public BrowserService(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// 初始化浏览器服务
        /// </summary>
        /// <param name="settings">浏览器设置</param>
        public async Task InitializeAsync(BrowserSettings settings)
        {
            _playwright = await Playwright.CreateAsync();
            
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = settings.Headless,
                SlowMo = settings.SlowMo,
                Timeout = settings.Timeout
            };
            
            _browser = settings.Type.ToLower() switch
            {
                "chromium" => await _playwright.Chromium.LaunchAsync(launchOptions),
                "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
                "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
                _ => await _playwright.Chromium.LaunchAsync(launchOptions)
            };
            
            _logger.LogInformation($"浏览器服务已初始化: {settings.Type}");
        }
        
        /// <summary>
        /// 创建浏览器上下文
        /// </summary>
        /// <param name="settings">浏览器设置</param>
        /// <returns>浏览器上下文</returns>
        public async Task<IBrowserContext> CreateContextAsync(BrowserSettings settings)
        {
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = settings.ViewportWidth,
                    Height = settings.ViewportHeight
                },
                Locale = settings.Locale,
                TimezoneId = settings.TimezoneId
            };
            
            var context = await _browser.NewContextAsync(contextOptions);
            
            _logger.LogInformation("浏览器上下文已创建");
            
            return context;
        }
        
        /// <summary>
        /// 创建页面实例
        /// </summary>
        /// <param name="context">浏览器上下文</param>
        /// <returns>页面实例</returns>
        public async Task<IPage> CreatePageAsync(IBrowserContext context)
        {
            var page = await context.NewPageAsync();
            
            _logger.LogInformation("页面实例已创建");
            
            return page;
        }
        
        /// <summary>
        /// 截取屏幕截图
        /// </summary>
        /// <param name="page">页面实例</param>
        /// <param name="fileName">文件名</param>
        /// <returns>截图字节数组</returns>
        public async Task<byte[]> TakeScreenshotAsync(IPage page, string fileName)
        {
            var screenshotPath = Path.Combine("src/conclusion/screenshots", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath));
            
            var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });
            
            _logger.LogInformation($"截图已保存: {screenshotPath}");
            
            return screenshot;
        }
        
        /// <summary>
        /// 关闭浏览器服务
        /// </summary>
        public async Task CloseAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }
            
            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
            
            _logger.LogInformation("浏览器服务已关闭");
        }
    }
}
```

### ApiClient

API 客户端类，提供 HTTP 请求功能。

```csharp
namespace CsPlaywrightXun.src.playwright.Services.Api
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly TestConfiguration _configuration;
        
        public ApiClient(HttpClient httpClient, ILogger logger, TestConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            
            // 设置基础配置
            _httpClient.BaseAddress = new Uri(_configuration.Environment.ApiBaseUrl);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(_configuration.Api.Timeout);
        }
        
        /// <summary>
        /// 发送 GET 请求
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync(HttpMethod.Get, endpoint, null, headers);
        }
        
        /// <summary>
        /// 发送 POST 请求
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync(HttpMethod.Post, endpoint, data, headers);
        }
        
        /// <summary>
        /// 发送 PUT 请求
        /// </summary>
        public async Task<HttpResponseMessage> PutAsync(string endpoint, object data, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync(HttpMethod.Put, endpoint, data, headers);
        }
        
        /// <summary>
        /// 发送 DELETE 请求
        /// </summary>
        public async Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string> headers = null)
        {
            return await SendRequestAsync(HttpMethod.Delete, endpoint, null, headers);
        }
        
        /// <summary>
        /// 发送 HTTP 请求
        /// </summary>
        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, object data, Dictionary<string, string> headers)
        {
            var request = new HttpRequestMessage(method, endpoint);
            
            // 添加请求头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            
            // 添加请求体
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            
            _logger.LogInformation($"发送 {method} 请求到 {endpoint}");
            
            var response = await _httpClient.SendAsync(request);
            
            _logger.LogInformation($"收到响应: {response.StatusCode}");
            
            return response;
        }
    }
}
```

### CsvDataReader

CSV 数据读取器，支持强类型和动态数据读取。

```csharp
namespace CsPlaywrightXun.src.playwright.Services.Data
{
    public class CsvDataReader
    {
        private readonly ILogger _logger;
        
        public CsvDataReader(ILogger logger = null)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// 读取强类型数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>数据集合</returns>
        public IEnumerable<T> ReadData<T>(string filePath) where T : class, new()
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV 文件不存在: {filePath}");
            }
            
            var results = new List<T>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length == 0)
            {
                _logger?.LogWarning($"CSV 文件为空: {filePath}");
                return results;
            }
            
            var headers = lines[0].Split(',');
            var properties = typeof(T).GetProperties();
            
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var item = new T();
                
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    var property = properties.FirstOrDefault(p => 
                        p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));
                    
                    if (property != null && property.CanWrite)
                    {
                        var value = Convert.ChangeType(values[j], property.PropertyType);
                        property.SetValue(item, value);
                    }
                }
                
                results.Add(item);
            }
            
            _logger?.LogInformation($"从 CSV 文件读取了 {results.Count} 条数据: {filePath}");
            
            return results;
        }
        
        /// <summary>
        /// 读取动态数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>动态数据集合</returns>
        public IEnumerable<Dictionary<string, object>> ReadDynamicData(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV 文件不存在: {filePath}");
            }
            
            var results = new List<Dictionary<string, object>>();
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length == 0)
            {
                _logger?.LogWarning($"CSV 文件为空: {filePath}");
                return results;
            }
            
            var headers = lines[0].Split(',');
            
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var item = new Dictionary<string, object>();
                
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    item[headers[j]] = values[j];
                }
                
                results.Add(item);
            }
            
            _logger?.LogInformation($"从 CSV 文件读取了 {results.Count} 条动态数据: {filePath}");
            
            return results;
        }
    }
}
```

## 📊 数据模型

### TestConfiguration

测试配置类，包含所有配置信息。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Configuration
{
    public class TestConfiguration
    {
        /// <summary>
        /// 环境设置
        /// </summary>
        public EnvironmentSettings Environment { get; set; } = new();
        
        /// <summary>
        /// 浏览器设置
        /// </summary>
        public BrowserSettings Browser { get; set; } = new();
        
        /// <summary>
        /// API 设置
        /// </summary>
        public ApiSettings Api { get; set; } = new();
        
        /// <summary>
        /// 报告设置
        /// </summary>
        public ReportingSettings Reporting { get; set; } = new();
        
        /// <summary>
        /// 日志设置
        /// </summary>
        public LoggingSettings Logging { get; set; } = new();
        
        /// <summary>
        /// 测试执行设置
        /// </summary>
        public TestExecutionSettings TestExecution { get; set; } = new();
    }
    
    public class EnvironmentSettings
    {
        public string Name { get; set; } = "Development";
        public string BaseUrl { get; set; } = "https://localhost";
        public string ApiBaseUrl { get; set; } = "https://localhost/api";
        public Dictionary<string, string> Variables { get; set; } = new();
    }
    
    public class BrowserSettings
    {
        public string Type { get; set; } = "Chromium";
        public bool Headless { get; set; } = true;
        public int ViewportWidth { get; set; } = 1920;
        public int ViewportHeight { get; set; } = 1080;
        public int Timeout { get; set; } = 30000;
        public int SlowMo { get; set; } = 0;
        public string Locale { get; set; } = "zh-CN";
        public string TimezoneId { get; set; } = "Asia/Shanghai";
    }
    
    public class ApiSettings
    {
        public int Timeout { get; set; } = 30000;
        public int RetryCount { get; set; } = 3;
        public int RetryDelay { get; set; } = 1000;
        public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    }
    
    public class ReportingSettings
    {
        public string OutputPath { get; set; } = "src/conclusion/reports";
        public string Format { get; set; } = "Html";
        public bool IncludeScreenshots { get; set; } = true;
        public bool GenerateAllureReport { get; set; } = false;
    }
    
    public class LoggingSettings
    {
        public string Level { get; set; } = "Information";
        public string FilePath { get; set; } = "src/conclusion/logs/test-{Date}.log";
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = true;
    }
}
```

### ApiRequest 和 ApiResponse

API 请求和响应模型。

```csharp
namespace CsPlaywrightXun.src.playwright.Services.Api
{
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
        public Dictionary<string, string> Headers { get; set; } = new();
        
        /// <summary>
        /// 查询参数
        /// </summary>
        public Dictionary<string, string> QueryParameters { get; set; } = new();
        
        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }
    
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
        public Dictionary<string, string> Headers { get; set; } = new();
        
        /// <summary>
        /// 响应时间
        /// </summary>
        public TimeSpan ResponseTime { get; set; }
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
    }
}
```

### TestResult 和 TestReport

测试结果和报告模型。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Models
{
    public class TestResult
    {
        public string TestName { get; set; }
        public string TestClass { get; set; }
        public TestStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public List<string> Screenshots { get; set; } = new();
        public Dictionary<string, object> TestData { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public TestPriority Priority { get; set; }
        public string Environment { get; set; }
    }
    
    public class TestReport
    {
        public DateTime GeneratedAt { get; set; }
        public string Environment { get; set; }
        public TestSummary Summary { get; set; }
        public List<TestResult> Results { get; set; } = new();
        public List<string> Screenshots { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
    }
    
    public class TestSummary
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
        public Dictionary<string, int> TestsByType { get; set; } = new();
        public Dictionary<string, int> TestsByPriority { get; set; } = new();
    }
    
    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Inconclusive
    }
}
```

## ⚠️ 异常类

### 自定义异常类型

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Exceptions
{
    /// <summary>
    /// 测试框架异常基类
    /// </summary>
    public class TestFrameworkException : Exception
    {
        public string TestName { get; }
        public string Component { get; }
        
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
        public string Selector { get; }
        
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
        public int StatusCode { get; }
        public string Endpoint { get; }
        
        public ApiException(string testName, string endpoint, int statusCode, string message)
            : base(testName, "ApiService", message)
        {
            StatusCode = statusCode;
            Endpoint = endpoint;
        }
    }
    
    /// <summary>
    /// CSV 数据异常
    /// </summary>
    public class CsvDataException : TestFrameworkException
    {
        public string FilePath { get; }
        
        public CsvDataException(string testName, string filePath, string message)
            : base(testName, "CsvDataReader", message)
        {
            FilePath = filePath;
        }
    }
    
    /// <summary>
    /// YAML 数据异常
    /// </summary>
    public class YamlDataException : TestFrameworkException
    {
        public string FilePath { get; }
        
        public YamlDataException(string testName, string filePath, string message)
            : base(testName, "YamlElementReader", message)
        {
            FilePath = filePath;
        }
    }
    
    /// <summary>
    /// 可重试异常
    /// </summary>
    public class RetryableException : TestFrameworkException
    {
        public int AttemptNumber { get; }
        public int MaxAttempts { get; }
        
        public RetryableException(string testName, string component, int attemptNumber, int maxAttempts, string message)
            : base(testName, component, message)
        {
            AttemptNumber = attemptNumber;
            MaxAttempts = maxAttempts;
        }
    }
}
```

## 🛠️ 工具类

### TestFilter

测试过滤器工具类，用于生成测试过滤表达式。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Utilities
{
    public static class TestFilter
    {
        // 基本过滤器
        public static string ByType(TestType type) => $"Type={type}";
        public static string ByCategory(TestCategory category) => $"Category={category}";
        public static string ByPriority(TestPriority priority) => $"Priority={priority}";
        public static string BySpeed(string speed) => $"Speed={speed}";
        public static string BySuite(string suite) => $"Suite={suite}";
        public static string ByTag(string tag) => $"Tag={tag}";
        public static string ByEnvironment(string environment) => $"Environment={environment}";
        
        // 多条件过滤器
        public static string ByTypes(params TestType[] types) => 
            $"({string.Join("|", types.Select(t => $"Type={t}"))})";
        
        public static string ByCategories(params TestCategory[] categories) => 
            $"({string.Join("|", categories.Select(c => $"Category={c}"))})";
        
        public static string ByPriorities(params TestPriority[] priorities) => 
            $"({string.Join("|", priorities.Select(p => $"Priority={p}"))})";
        
        // 逻辑组合
        public static string And(params string[] filters) => 
            $"({string.Join("&", filters)})";
        
        public static string Or(params string[] filters) => 
            $"({string.Join("|", filters)})";
        
        public static string Not(string filter) => $"!{filter}";
        
        // 预定义过滤器
        public static string UITestsOnly => ByType(TestType.UI);
        public static string APITestsOnly => ByType(TestType.API);
        public static string IntegrationTestsOnly => ByType(TestType.Integration);
        public static string UnitTestsOnly => ByType(TestType.Unit);
        public static string E2ETestsOnly => ByType(TestType.E2E);
        
        public static string UIAndAPITests => ByTypes(TestType.UI, TestType.API);
        public static string FastTestsOnly => BySpeed("Fast");
        public static string SlowTestsOnly => BySpeed("Slow");
        public static string SmokeTestsOnly => BySuite("Smoke");
        public static string RegressionTestsOnly => BySuite("Regression");
        
        public static string CriticalTestsOnly => ByPriority(TestPriority.Critical);
        public static string HighPriorityTestsOnly => ByPriority(TestPriority.High);
        
        // 命令生成
        public static string GenerateTestCommand(string filter, string projectPath = null)
        {
            var project = string.IsNullOrEmpty(projectPath) ? "" : $"\"{projectPath}\" ";
            return $"dotnet test {project}--filter \"{filter}\"";
        }
        
        public static string GenerateVerboseTestCommand(string filter, string projectPath = null)
        {
            var project = string.IsNullOrEmpty(projectPath) ? "" : $"\"{projectPath}\" ";
            return $"dotnet test {project}--filter \"{filter}\" --verbosity normal --logger console";
        }
    }
}
```

### RetryExecutor

重试执行器，提供可配置的重试机制。

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Utilities
{
    public class RetryExecutor
    {
        private readonly RetryPolicy _policy;
        private readonly ILogger _logger;
        
        public RetryExecutor(RetryPolicy policy, ILogger logger)
        {
            _policy = policy;
            _logger = logger;
        }
        
        /// <summary>
        /// 执行带重试的操作
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>异步任务</returns>
        public async Task ExecuteAsync(Func<Task> operation, string operationName = "操作")
        {
            var attempt = 1;
            
            while (attempt <= _policy.MaxAttempts)
            {
                try
                {
                    _logger.LogInformation($"执行 {operationName}，第 {attempt} 次尝试");
                    
                    await operation();
                    
                    _logger.LogInformation($"{operationName} 执行成功");
                    return;
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    _logger.LogWarning($"{operationName} 第 {attempt} 次尝试失败: {ex.Message}");
                    
                    if (attempt < _policy.MaxAttempts)
                    {
                        _logger.LogInformation($"等待 {_policy.DelayBetweenAttempts.TotalMilliseconds}ms 后重试");
                        await Task.Delay(_policy.DelayBetweenAttempts);
                    }
                    
                    attempt++;
                }
            }
            
            _logger.LogError($"{operationName} 在 {_policy.MaxAttempts} 次尝试后仍然失败");
            throw new RetryableException("", "RetryExecutor", attempt - 1, _policy.MaxAttempts, 
                $"{operationName} 重试失败");
        }
        
        /// <summary>
        /// 执行带重试的操作（有返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>操作结果</returns>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "操作")
        {
            var attempt = 1;
            
            while (attempt <= _policy.MaxAttempts)
            {
                try
                {
                    _logger.LogInformation($"执行 {operationName}，第 {attempt} 次尝试");
                    
                    var result = await operation();
                    
                    _logger.LogInformation($"{operationName} 执行成功");
                    return result;
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    _logger.LogWarning($"{operationName} 第 {attempt} 次尝试失败: {ex.Message}");
                    
                    if (attempt < _policy.MaxAttempts)
                    {
                        _logger.LogInformation($"等待 {_policy.DelayBetweenAttempts.TotalMilliseconds}ms 后重试");
                        await Task.Delay(_policy.DelayBetweenAttempts);
                    }
                    
                    attempt++;
                }
            }
            
            _logger.LogError($"{operationName} 在 {_policy.MaxAttempts} 次尝试后仍然失败");
            throw new RetryableException("", "RetryExecutor", attempt - 1, _policy.MaxAttempts, 
                $"{operationName} 重试失败");
        }
        
        /// <summary>
        /// 判断是否应该重试
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="attempt">当前尝试次数</param>
        /// <returns>是否应该重试</returns>
        private bool ShouldRetry(Exception exception, int attempt)
        {
            if (attempt >= _policy.MaxAttempts)
                return false;
            
            if (_policy.RetryableExceptions == null || _policy.RetryableExceptions.Count == 0)
                return true;
            
            return _policy.RetryableExceptions.Any(type => type.IsAssignableFrom(exception.GetType()));
        }
    }
    
    public class RetryPolicy
    {
        public int MaxAttempts { get; set; } = 3;
        public TimeSpan DelayBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);
        public List<Type> RetryableExceptions { get; set; } = new();
        
        public static RetryPolicy Default => new()
        {
            MaxAttempts = 3,
            DelayBetweenAttempts = TimeSpan.FromSeconds(1),
            RetryableExceptions = new List<Type>
            {
                typeof(ElementNotFoundException),
                typeof(TimeoutException),
                typeof(HttpRequestException)
            }
        };
    }
}
```

## 🏷️ 属性和标记

### 测试分类属性

```csharp
namespace CsPlaywrightXun.src.playwright.Core.Attributes
{
    // 测试类型属性
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestTypeAttribute : Attribute, ITraitAttribute
    {
        public TestType Type { get; }
        
        public TestTypeAttribute(TestType type)
        {
            Type = type;
        }
        
        public string Key => "Type";
        public string Value => Type.ToString();
    }
    
    // 便捷属性
    public class UITestAttribute : TestTypeAttribute
    {
        public UITestAttribute() : base(TestType.UI) { }
    }
    
    public class APITestAttribute : TestTypeAttribute
    {
        public APITestAttribute() : base(TestType.API) { }
    }
    
    public class IntegrationTestAttribute : TestTypeAttribute
    {
        public IntegrationTestAttribute() : base(TestType.Integration) { }
    }
    
    public class UnitTestAttribute : TestTypeAttribute
    {
        public UnitTestAttribute() : base(TestType.Unit) { }
    }
    
    public class E2ETestAttribute : TestTypeAttribute
    {
        public E2ETestAttribute() : base(TestType.E2E) { }
    }
    
    // 测试分类属性
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestCategoryAttribute : Attribute, ITraitAttribute
    {
        public TestCategory Category { get; }
        
        public TestCategoryAttribute(TestCategory category)
        {
            Category = category;
        }
        
        public string Key => "Category";
        public string Value => Category.ToString();
    }
    
    // 测试优先级属性
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TestPriorityAttribute : Attribute, ITraitAttribute
    {
        public TestPriority Priority { get; }
        
        public TestPriorityAttribute(TestPriority priority)
        {
            Priority = priority;
        }
        
        public string Key => "Priority";
        public string Value => Priority.ToString();
    }
    
    // 其他便捷属性
    public class FastTestAttribute : Attribute, ITraitAttribute
    {
        public string Key => "Speed";
        public string Value => "Fast";
    }
    
    public class SlowTestAttribute : Attribute, ITraitAttribute
    {
        public string Key => "Speed";
        public string Value => "Slow";
    }
    
    public class SmokeTestAttribute : Attribute, ITraitAttribute
    {
        public string Key => "Suite";
        public string Value => "Smoke";
    }
    
    public class RegressionTestAttribute : Attribute, ITraitAttribute
    {
        public string Key => "Suite";
        public string Value => "Regression";
    }
    
    // 枚举定义
    public enum TestType
    {
        UI,
        API,
        Integration,
        Unit,
        E2E
    }
    
    public enum TestCategory
    {
        PageObject,
        Flow,
        ApiClient,
        DataProvider,
        ErrorRecovery,
        Configuration,
        Reporting
    }
    
    public enum TestPriority
    {
        Critical,
        High,
        Medium,
        Low
    }
}
```

### 数据属性

```csharp
namespace CsPlaywrightXun.src.playwright.Services.Data
{
    /// <summary>
    /// CSV 数据属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CsvDataAttribute : DataAttribute
    {
        private readonly string _filePath;
        
        public CsvDataAttribute(string filePath)
        {
            _filePath = filePath;
        }
        
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var parameterType = testMethod.GetParameters().FirstOrDefault()?.ParameterType;
            
            if (parameterType == null)
                yield break;
            
            var reader = new CsvDataReader();
            var data = reader.ReadData(parameterType, _filePath);
            
            foreach (var item in data)
            {
                yield return new object[] { item };
            }
        }
    }
    
    /// <summary>
    /// JSON 数据属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class JsonDataAttribute : DataAttribute
    {
        private readonly string _filePath;
        
        public JsonDataAttribute(string filePath)
        {
            _filePath = filePath;
        }
        
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var parameterType = testMethod.GetParameters().FirstOrDefault()?.ParameterType;
            
            if (parameterType == null)
                yield break;
            
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize(json, typeof(IEnumerable<>).MakeGenericType(parameterType));
            
            foreach (var item in (IEnumerable)data)
            {
                yield return new object[] { item };
            }
        }
    }
}
```

## 📝 使用示例

### 完整的测试类示例

```csharp
[UITest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.High)]
public class ComprehensiveExampleTests : IClassFixture<BrowserFixture>
{
    private readonly BrowserFixture _fixture;
    private readonly ExamplePage _page;
    private readonly ExampleFlow _flow;
    
    public ComprehensiveExampleTests(BrowserFixture fixture)
    {
        _fixture = fixture;
        _page = new ExamplePage(_fixture.Page, _fixture.Logger);
        _flow = new ExampleFlow(_page, _fixture.Logger);
    }
    
    [Theory]
    [CsvData("TestData/example_data.csv")]
    [TestTag("DataDriven")]
    public async Task DataDrivenTest_ShouldWork(ExampleTestData data)
    {
        // 使用数据驱动测试
        await _page.NavigateAsync(data.BaseUrl);
        await _flow.ExecuteAsync(new Dictionary<string, object>
        {
            ["parameter"] = data.Parameter
        });
        
        var result = await _page.GetResultAsync();
        Assert.Equal(data.ExpectedResult, result);
    }
    
    [Fact]
    [SmokeTest]
    [FastTest]
    public async Task SmokeTest_ShouldPass()
    {
        // 冒烟测试
        await _page.NavigateAsync("https://example.com");
        var isLoaded = await _page.IsLoadedAsync();
        Assert.True(isLoaded);
    }
    
    [Fact]
    [TestEnvironment("Production")]
    [TestTag("Critical")]
    public async Task ProductionTest_ShouldWork()
    {
        // 生产环境测试
        await _page.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _page.PerformCriticalOperation();
        
        var result = await _page.AssertEqualAsync(
            await _page.GetStatusAsync(), 
            "Success"
        );
        Assert.Equal("pass", result);
    }
}
```

这个 API 参考文档提供了框架的完整接口说明，帮助开发者快速理解和使用框架的各种功能。每个接口和类都包含了详细的说明和使用示例。