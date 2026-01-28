using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using EnterpriseAutomationFramework.Core.Fixtures;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Services.Browser;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// BrowserFixture 集成测试
/// 验证浏览器固件的并行执行和测试隔离功能
/// </summary>
[IntegrationTest]
[TestCategory(TestCategory.Fixture)]
[TestPriority(TestPriority.High)]
[SlowTest]
public class BrowserFixtureIntegrationTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly List<BrowserFixture> _fixtures = new();

    public BrowserFixtureIntegrationTests(ITestOutputHelper output)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<BrowserFixture>();
    }

    /// <summary>
    /// 测试 BrowserFixture 基本初始化功能
    /// </summary>
    [Fact]
    public async Task BrowserFixture_Initialize_ShouldCreateAllComponents()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);

        // Act
        await fixture.InitializeAsync();

        // Assert
        Assert.NotNull(fixture.Configuration);
        Assert.NotNull(fixture.Playwright);
        Assert.NotNull(fixture.Browser);
        Assert.NotNull(fixture.Context);
        Assert.NotNull(fixture.Page);
        
        // 验证配置是否正确加载
        Assert.Equal("Development", fixture.Configuration.Environment.Name);
        Assert.True(fixture.Browser.IsConnected);
        
        // 验证页面是否可用
        Assert.False(fixture.Page.IsClosed);
    }

    /// <summary>
    /// 测试多个 BrowserFixture 实例的并行初始化
    /// 验证并行执行支持
    /// </summary>
    [Fact]
    public async Task MultipleBrowserFixtures_ParallelInitialization_ShouldSucceed()
    {
        // Arrange
        const int fixtureCount = 3;
        var fixtures = new List<BrowserFixture>();
        var tasks = new List<Task>();

        for (int i = 0; i < fixtureCount; i++)
        {
            var fixture = new BrowserFixture(_logger, "Development");
            fixtures.Add(fixture);
            _fixtures.Add(fixture);
        }

        // Act - 并行初始化所有固件
        foreach (var fixture in fixtures)
        {
            tasks.Add(fixture.InitializeAsync());
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var fixture in fixtures)
        {
            Assert.NotNull(fixture.Configuration);
            Assert.NotNull(fixture.Playwright);
            Assert.NotNull(fixture.Browser);
            Assert.NotNull(fixture.Context);
            Assert.NotNull(fixture.Page);
            Assert.True(fixture.Browser.IsConnected);
            Assert.False(fixture.Page.IsClosed);
        }

        // 验证每个固件都有独立的上下文
        var contexts = fixtures.Select(f => f.Context).ToList();
        var uniqueContexts = contexts.Distinct().ToList();
        Assert.Equal(fixtureCount, uniqueContexts.Count);
    }

    /// <summary>
    /// 测试浏览器上下文隔离
    /// 验证每个测试用例使用独立的 BrowserContext
    /// </summary>
    [Fact]
    public async Task BrowserFixture_CreateNewContext_ShouldProvideIsolation()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        // Act
        var originalContext = fixture.Context;
        var newContext1 = await fixture.CreateNewContextAsync();
        var newContext2 = await fixture.CreateNewContextAsync();

        // Assert
        Assert.NotNull(originalContext);
        Assert.NotNull(newContext1);
        Assert.NotNull(newContext2);
        
        // 验证所有上下文都是不同的实例
        Assert.NotEqual(originalContext, newContext1);
        Assert.NotEqual(originalContext, newContext2);
        Assert.NotEqual(newContext1, newContext2);

        // 清理新创建的上下文
        await newContext1.CloseAsync();
        await newContext2.CloseAsync();
    }

    /// <summary>
    /// 测试隔离的浏览器实例创建
    /// </summary>
    [Fact]
    public async Task BrowserFixture_GetIsolatedBrowser_ShouldCreateSeparateInstances()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        // Act
        var (context1, page1) = await fixture.GetIsolatedBrowserAsync();
        var (context2, page2) = await fixture.GetIsolatedBrowserAsync();

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(page1);
        Assert.NotNull(context2);
        Assert.NotNull(page2);
        
        // 验证上下文和页面都是独立的
        Assert.NotEqual(context1, context2);
        Assert.NotEqual(page1, page2);
        Assert.NotEqual(fixture.Context, context1);
        Assert.NotEqual(fixture.Context, context2);
        Assert.NotEqual(fixture.Page, page1);
        Assert.NotEqual(fixture.Page, page2);

        // 清理隔离的实例
        await page1.CloseAsync();
        await context1.CloseAsync();
        await page2.CloseAsync();
        await context2.CloseAsync();
    }

    /// <summary>
    /// 测试页面导航和基本操作
    /// </summary>
    [Fact]
    public async Task BrowserFixture_PageNavigation_ShouldWork()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        // Act
        await fixture.Page.GotoAsync("https://www.baidu.com");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await fixture.Page.TitleAsync();
        Assert.Contains("百度", title);
        
        var url = fixture.Page.Url;
        Assert.Contains("baidu.com", url);
    }

    /// <summary>
    /// 测试截图功能
    /// </summary>
    [Fact]
    public async Task BrowserFixture_TakeScreenshot_ShouldWork()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        // Act
        await fixture.Page.GotoAsync("https://www.baidu.com");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var screenshot = await fixture.TakeScreenshotAsync("test-screenshot.png");

        // Assert
        Assert.NotNull(screenshot);
        Assert.True(screenshot.Length > 0);
    }

    /// <summary>
    /// 测试并行页面操作的隔离性
    /// </summary>
    [Fact]
    public async Task BrowserFixture_ParallelPageOperations_ShouldBeIsolated()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);
        await fixture.InitializeAsync();

        var (context1, page1) = await fixture.GetIsolatedBrowserAsync();
        var (context2, page2) = await fixture.GetIsolatedBrowserAsync();

        try
        {
            // Act - 并行执行不同的页面操作
            var task1 = NavigateAndGetTitle(page1, "https://www.baidu.com");
            var task2 = NavigateAndGetTitle(page2, "https://www.baidu.com");

            var results = await Task.WhenAll(task1, task2);

            // Assert
            Assert.Equal(2, results.Length);
            Assert.All(results, title => Assert.Contains("百度", title));
            
            // 验证页面状态是独立的 - 检查页面实例而不是URL
            Assert.NotEqual(page1, page2);
            Assert.NotEqual(context1, context2);
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
    /// 测试配置加载失败时的默认配置回退
    /// </summary>
    [Fact]
    public async Task BrowserFixture_InvalidEnvironment_ShouldUseDefaultConfiguration()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "NonExistentEnvironment");
        _fixtures.Add(fixture);

        // Act
        await fixture.InitializeAsync();

        // Assert
        Assert.NotNull(fixture.Configuration);
        Assert.Equal("NonExistentEnvironment", fixture.Configuration.Environment.Name);
        Assert.Equal("https://www.baidu.com", fixture.Configuration.Environment.BaseUrl);
        Assert.Equal("Chromium", fixture.Configuration.Browser.Type);
    }

    /// <summary>
    /// 测试重复初始化的幂等性
    /// </summary>
    [Fact]
    public async Task BrowserFixture_MultipleInitialize_ShouldBeIdempotent()
    {
        // Arrange
        var fixture = new BrowserFixture(_logger, "Development");
        _fixtures.Add(fixture);

        // Act
        await fixture.InitializeAsync();
        var firstPlaywright = fixture.Playwright;
        var firstBrowser = fixture.Browser;
        var firstContext = fixture.Context;
        var firstPage = fixture.Page;

        await fixture.InitializeAsync(); // 第二次初始化

        // Assert
        Assert.Same(firstPlaywright, fixture.Playwright);
        Assert.Same(firstBrowser, fixture.Browser);
        Assert.Same(firstContext, fixture.Context);
        Assert.Same(firstPage, fixture.Page);
    }

    /// <summary>
    /// 辅助方法：导航到页面并获取标题
    /// </summary>
    private async Task<string> NavigateAndGetTitle(IPage page, string url)
    {
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return await page.TitleAsync();
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        foreach (var fixture in _fixtures)
        {
            try
            {
                fixture.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                // 记录清理错误但不抛出异常
                Console.WriteLine($"清理 BrowserFixture 时发生错误: {ex.Message}");
            }
        }
        _fixtures.Clear();
    }
}