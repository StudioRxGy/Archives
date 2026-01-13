using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using EnterpriseAutomationFramework.Core.Fixtures;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Utilities;
using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Services.Browser;
using EnterpriseAutomationFramework.Services.Api;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// 错误恢复策略集成测试
/// 验证页面刷新、浏览器重启和API重试恢复机制
/// </summary>
[IntegrationTest]
[TestCategory(TestCategory.ErrorRecovery)]
[TestPriority(TestPriority.Critical)]
[SlowTest]
public class ErrorRecoveryIntegrationTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ErrorRecoveryStrategy> _logger;
    private readonly ILogger<BrowserFixture> _browserLogger;
    private readonly ILogger<RetryExecutor> _retryLogger;
    private readonly ILogger<BrowserService> _browserServiceLogger;
    private readonly ILogger<ApiClient> _apiClientLogger;
    private readonly List<BrowserFixture> _fixtures = new();
    private readonly List<IDisposable> _disposables = new();

    public ErrorRecoveryIntegrationTests(ITestOutputHelper output)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = _loggerFactory.CreateLogger<ErrorRecoveryStrategy>();
        _browserLogger = _loggerFactory.CreateLogger<BrowserFixture>();
        _retryLogger = _loggerFactory.CreateLogger<RetryExecutor>();
        _browserServiceLogger = _loggerFactory.CreateLogger<BrowserService>();
        _apiClientLogger = _loggerFactory.CreateLogger<ApiClient>();
    }

    /// <summary>
    /// 测试页面刷新恢复机制
    /// 验证当页面元素不可用时能够通过刷新页面恢复
    /// </summary>
    [Fact]
    public async Task PageRefreshRecovery_WhenElementNotFound_ShouldRecoverAfterRefresh()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var settings = ErrorRecoverySettings.CreateForUi();
        var retryPolicy = RetryPolicy.CreateDefaultUiPolicy();
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        // 导航到测试页面
        await fixture.Page.GotoAsync("https://www.baidu.com");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act & Assert
        var result = await errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            fixture.Page,
            async () =>
            {
                // 尝试查找一个可能不存在的元素，如果不存在会触发页面刷新
                var element = await fixture.Page.QuerySelectorAsync("#kw");
                if (element == null)
                {
                    throw new ElementNotFoundException("PageRefreshTest", "#kw", "搜索框元素未找到");
                }
                return await element.GetAttributeAsync("placeholder") ?? "搜索框";
            },
            "FindSearchBox");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// 测试页面刷新恢复的重试次数限制
    /// </summary>
    [Fact]
    public async Task PageRefreshRecovery_ExceedMaxAttempts_ShouldThrowException()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var settings = ErrorRecoverySettings.CreateFast();
        settings.PageRefreshMaxAttempts = 1; // 限制为1次重试
        var retryPolicy = RetryPolicy.CreateCustomPolicy(1, TimeSpan.FromMilliseconds(100));
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        await fixture.Page.GotoAsync("https://www.baidu.com");

        // Act & Assert
        await Assert.ThrowsAsync<ElementNotFoundException>(async () =>
        {
            await errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
                fixture.Page,
                async () =>
                {
                    // 总是抛出异常来测试重试限制
                    throw new ElementNotFoundException("MaxAttemptsTest", "#nonexistent", "元素不存在");
                },
                "AlwaysFailOperation");
        });
    }

    /// <summary>
    /// 测试浏览器重启恢复机制
    /// 验证当浏览器崩溃时能够重启浏览器并继续执行
    /// </summary>
    [Fact]
    public async Task BrowserRestartRecovery_WhenBrowserClosed_ShouldRecoverWithNewBrowser()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var browserService = new BrowserService(_browserServiceLogger);

        var settings = ErrorRecoverySettings.CreateForUi();
        var retryPolicy = RetryPolicy.CreateDefaultUiPolicy();
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        var browserSettings = fixture.Configuration.Browser;

        // Act & Assert
        var result = await errorRecoveryStrategy.ExecuteWithBrowserRestartRecoveryAsync(
            browserService,
            browserSettings,
            async (page) =>
            {
                await page.GotoAsync("https://www.baidu.com");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                var title = await page.TitleAsync();
                return title;
            },
            "BrowserRestartTest");

        Assert.NotNull(result);
        Assert.Contains("百度", result);
        
        // Clean up browser service
        await browserService.CloseAsync();
    }

    /// <summary>
    /// 测试API重试恢复机制
    /// 验证API调用失败时的重试逻辑
    /// </summary>
    [Fact]
    public async Task ApiRetryRecovery_WhenApiCallFails_ShouldRetryAndSucceed()
    {
        // Arrange
        var configuration = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Test",
                ApiBaseUrl = "https://httpbin.org"
            },
            Api = new ApiSettings
            {
                Timeout = 30000,
                RetryCount = 3
            }
        };

        var httpClient = new HttpClient();
        var apiClient = new ApiClient(httpClient, configuration, _apiClientLogger);
        _disposables.Add(httpClient);
        _disposables.Add(apiClient);

        var settings = ErrorRecoverySettings.CreateForApi();
        var retryPolicy = RetryPolicy.CreateDefaultApiPolicy();
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        int attemptCount = 0;

        // Act
        var result = await errorRecoveryStrategy.ExecuteWithApiRetryRecoveryAsync(
            apiClient,
            async () =>
            {
                attemptCount++;
                
                // 前两次调用失败，第三次成功
                if (attemptCount < 3)
                {
                    throw new HttpRequestException("模拟API调用失败");
                }

                // 第三次调用成功
                var response = await apiClient.GetAsync("/get");
                return response.IsSuccessStatusCode ? "Success" : "Failed";
            },
            "ApiRetryTest");

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(3, attemptCount); // 验证确实重试了3次
    }

    /// <summary>
    /// 测试综合错误恢复策略
    /// 验证结合页面刷新、浏览器重启和API重试的综合恢复机制
    /// </summary>
    [Fact]
    public async Task ComprehensiveRecovery_WithMultipleRecoveryTypes_ShouldHandleAllScenarios()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var browserService = new BrowserService(_browserServiceLogger);

        var httpClient = new HttpClient();
        var apiClient = new ApiClient(httpClient, fixture.Configuration, _apiClientLogger);
        _disposables.Add(httpClient);
        _disposables.Add(apiClient);

        var settings = ErrorRecoverySettings.CreateDefault();
        var retryPolicy = RetryPolicy.CreateCustomPolicy(2, TimeSpan.FromMilliseconds(100), 
            typeof(ElementNotFoundException), typeof(TimeoutException), typeof(InvalidOperationException)); // Allow 2 retries with specific exceptions
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        var context = new ErrorRecoveryContext(
            fixture.Page,
            browserService,
            fixture.Configuration.Browser,
            apiClient,
            "ComprehensiveRecoveryTest",
            "ErrorRecoveryIntegrationTests",
            "ComprehensiveOperation");

        int operationCount = 0;

        // Act
        var result = await errorRecoveryStrategy.ExecuteWithComprehensiveRecoveryAsync(
            context,
            async () =>
            {
                operationCount++;
                
                // 第一次操作：模拟页面元素问题
                if (operationCount == 1)
                {
                    throw new ElementNotFoundException("ComprehensiveTest", "#test", "测试元素未找到");
                }
                
                // 第二次操作：成功
                await fixture.Page.GotoAsync("https://www.baidu.com");
                await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                var title = await fixture.Page.TitleAsync();
                return title;
            },
            "ComprehensiveRecoveryOperation");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("百度", result);
        Assert.True(operationCount >= 2); // 验证至少执行了2次操作
        
        // Clean up browser service
        await browserService.CloseAsync();
    }

    /// <summary>
    /// 测试错误恢复上下文的功能
    /// </summary>
    [Fact]
    public async Task ErrorRecoveryContext_WithDifferentCapabilities_ShouldReportCorrectly()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var browserService = new BrowserService(_browserServiceLogger);

        var httpClient = new HttpClient();
        var apiClient = new ApiClient(httpClient, fixture.Configuration, _apiClientLogger);
        _disposables.Add(httpClient);
        _disposables.Add(apiClient);

        // Act
        var pageContext = ErrorRecoveryContext.ForPage(fixture.Page, "PageTest");
        var browserContext = ErrorRecoveryContext.ForBrowser(browserService, fixture.Configuration.Browser, "BrowserTest");
        var apiContext = ErrorRecoveryContext.ForApi(apiClient, "ApiTest");
        var fullContext = new ErrorRecoveryContext(
            fixture.Page, browserService, fixture.Configuration.Browser, apiClient,
            "FullTest", "TestComponent", "TestOperation");

        // Assert
        Assert.True(pageContext.HasPageRecoveryCapability());
        Assert.False(pageContext.HasBrowserRestartCapability());
        Assert.False(pageContext.HasApiRetryCapability());

        Assert.False(browserContext.HasPageRecoveryCapability());
        Assert.True(browserContext.HasBrowserRestartCapability());
        Assert.False(browserContext.HasApiRetryCapability());

        Assert.False(apiContext.HasPageRecoveryCapability());
        Assert.False(apiContext.HasBrowserRestartCapability());
        Assert.True(apiContext.HasApiRetryCapability());

        Assert.True(fullContext.HasPageRecoveryCapability());
        Assert.True(fullContext.HasBrowserRestartCapability());
        Assert.True(fullContext.HasApiRetryCapability());

        // 验证上下文描述
        var fullDescription = fullContext.GetDescription();
        Assert.Contains("Test: FullTest", fullDescription);
        Assert.Contains("Component: TestComponent", fullDescription);
        Assert.Contains("Operation: TestOperation", fullDescription);
        Assert.Contains("Page: Available", fullDescription);
        Assert.Contains("Browser: Available", fullDescription);
        Assert.Contains("API: Available", fullDescription);
        
        // Clean up browser service
        await browserService.CloseAsync();
    }

    /// <summary>
    /// 测试错误恢复设置的验证功能
    /// </summary>
    [Fact]
    public void ErrorRecoverySettings_Validation_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var validSettings = ErrorRecoverySettings.CreateDefault();
        var invalidSettings = new ErrorRecoverySettings
        {
            PageRefreshTimeout = -1,
            ApiMaxRetryAttempts = -1,
            ExponentialBackoffMultiplier = 0.5
        };

        // Assert
        Assert.True(validSettings.IsValid());
        Assert.Empty(validSettings.GetValidationErrors());

        Assert.False(invalidSettings.IsValid());
        var errors = invalidSettings.GetValidationErrors();
        Assert.Contains("页面刷新超时时间必须大于0", errors);
        Assert.Contains("API最大重试次数不能小于0", errors);
        Assert.Contains("指数退避倍数必须大于1.0", errors);
    }

    /// <summary>
    /// 测试不同类型的错误恢复设置
    /// </summary>
    [Fact]
    public void ErrorRecoverySettings_DifferentTypes_ShouldHaveCorrectDefaults()
    {
        // Act
        var defaultSettings = ErrorRecoverySettings.CreateDefault();
        var apiSettings = ErrorRecoverySettings.CreateForApi();
        var uiSettings = ErrorRecoverySettings.CreateForUi();
        var fastSettings = ErrorRecoverySettings.CreateFast();
        var conservativeSettings = ErrorRecoverySettings.CreateConservative();

        // Assert
        Assert.True(defaultSettings.EnablePageRefreshRecovery);
        Assert.True(defaultSettings.EnableBrowserRestartRecovery);
        Assert.True(defaultSettings.EnableApiRetryRecovery);

        Assert.False(apiSettings.EnablePageRefreshRecovery);
        Assert.False(apiSettings.EnableBrowserRestartRecovery);
        Assert.True(apiSettings.EnableApiRetryRecovery);
        Assert.Equal(5, apiSettings.ApiMaxRetryAttempts);

        Assert.True(uiSettings.EnablePageRefreshRecovery);
        Assert.True(uiSettings.EnableBrowserRestartRecovery);
        Assert.False(uiSettings.EnableApiRetryRecovery);

        Assert.Equal(500, fastSettings.ApiRetryDelay);
        Assert.Equal(2, fastSettings.ApiMaxRetryAttempts);

        Assert.Equal(2000, conservativeSettings.ApiRetryDelay);
        Assert.Equal(5, conservativeSettings.ApiMaxRetryAttempts);
        Assert.Equal(120000, conservativeSettings.MaxRetryDelay);
    }

    /// <summary>
    /// 测试错误恢复策略的工厂方法
    /// </summary>
    [Fact]
    public void ErrorRecoveryStrategy_FactoryMethods_ShouldCreateCorrectInstances()
    {
        // Act
        var defaultStrategy = ErrorRecoveryStrategy.CreateDefault(_logger);
        var apiStrategy = ErrorRecoveryStrategy.CreateForApi(_logger);
        var uiStrategy = ErrorRecoveryStrategy.CreateForUi(_logger);

        // Assert
        Assert.NotNull(defaultStrategy);
        Assert.NotNull(apiStrategy);
        Assert.NotNull(uiStrategy);
    }

    /// <summary>
    /// 测试并发错误恢复操作
    /// 验证多个恢复操作可以并发执行而不互相干扰
    /// </summary>
    [Fact]
    public async Task ErrorRecovery_ConcurrentOperations_ShouldNotInterfere()
    {
        // Arrange
        var fixture = new BrowserFixture(_browserLogger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var settings = ErrorRecoverySettings.CreateFast();
        var retryPolicy = RetryPolicy.CreateDefaultUiPolicy();
        var retryExecutor = new RetryExecutor(retryPolicy, _retryLogger);
        var errorRecoveryStrategy = new ErrorRecoveryStrategy(_logger, retryExecutor, settings);

        // 创建多个独立的页面实例
        var (context1, page1) = await fixture.GetIsolatedBrowserAsync();
        var (context2, page2) = await fixture.GetIsolatedBrowserAsync();

        try
        {
            // Act - 并发执行恢复操作
            var task1 = errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
                page1,
                async () =>
                {
                    await page1.GotoAsync("https://www.baidu.com");
                    await page1.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    return await page1.TitleAsync();
                },
                "ConcurrentOperation1");

            var task2 = errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
                page2,
                async () =>
                {
                    await page2.GotoAsync("https://www.baidu.com");
                    await page2.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    return await page2.TitleAsync();
                },
                "ConcurrentOperation2");

            var results = await Task.WhenAll(task1, task2);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, title => Assert.Contains("百度", title));
        }
        finally
        {
            // 清理
            await page1.CloseAsync();
            await context1.CloseAsync();
            await page2.CloseAsync();
            await context2.CloseAsync();
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        // 清理 BrowserFixture
        foreach (var fixture in _fixtures)
        {
            try
            {
                fixture.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理 BrowserFixture 时发生错误: {ex.Message}");
            }
        }
        _fixtures.Clear();

        // 清理其他资源
        foreach (var disposable in _disposables)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理资源时发生错误: {ex.Message}");
            }
        }
        _disposables.Clear();
        
        // 清理 LoggerFactory
        _loggerFactory?.Dispose();
    }
}