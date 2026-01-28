using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.UI.baidu;

/// <summary>
/// 简化的首页UI测试类
/// 演示基本的UI测试用例，使用HomePage和SearchFlow
/// 验证测试隔离和并行执行
/// </summary>
[Trait("Type", "UI")]
[Trait("Category", "HomePage")]
public class SimpleHomePageTests : IClassFixture<BrowserFixture>, IAsyncLifetime
{
    private readonly BrowserFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    private IBrowserContext? _isolatedContext;
    private IPage? _isolatedPage;
    private HomePage? _homePage;
    private SearchFlow? _searchFlow;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">浏览器固件</param>
    /// <param name="output">测试输出助手</param>
    public SimpleHomePageTests(BrowserFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<SimpleHomePageTests>();
    }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        _output.WriteLine("正在初始化测试环境...");
        
        await _fixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _fixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_fixture, _logger);
        
        _output.WriteLine("测试环境初始化完成");
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine("正在清理测试环境...");
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
        }
        
        _output.WriteLine("测试环境清理完成");
    }

    /// <summary>
    /// 测试首页加载功能
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    public async Task HomePage_ShouldLoadSuccessfully()
    {
        _output.WriteLine("开始测试首页加载功能");
        
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        var isLoaded = await _homePage.IsLoadedAsync();
        Assert.True(isLoaded, "首页应该成功加载");
        
        var isSearchBoxAvailable = await _homePage.IsSearchBoxAvailableAsync();
        Assert.True(isSearchBoxAvailable, "搜索框应该可用");
        
        var isSearchButtonAvailable = await _homePage.IsSearchButtonAvailableAsync();
        Assert.True(isSearchButtonAvailable, "搜索按钮应该可用");
        
        _output.WriteLine("首页加载功能测试通过");
    }

    /// <summary>
    /// 测试搜索基本功能
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    public async Task Search_BasicFunctionality_ShouldWork()
    {
        _output.WriteLine("开始测试搜索基本功能");
        
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        const string testQuery = "测试搜索内容";
        await _homePage.SearchAsync(testQuery);
        
        await Task.Delay(2000);
        
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("wd="), "URL应该包含搜索参数");
        
        _output.WriteLine("搜索基本功能测试通过");
    }

    /// <summary>
    /// 测试SearchFlow简单搜索
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("Component", "Flow")]
    public async Task SearchFlow_SimpleSearch_ShouldWork()
    {
        _output.WriteLine("开始测试SearchFlow简单搜索");
        
        const string searchQuery = "Playwright自动化测试";
        await _searchFlow!.ExecuteSimpleSearchAsync(searchQuery);
        
        var currentUrl = _fixture.Page.Url;
        Assert.True(currentUrl.Contains("wd="), "搜索流程应该导航到搜索结果页面");
        
        _output.WriteLine("SearchFlow简单搜索测试通过");
    }

    /// <summary>
    /// 并行测试1 - 验证测试隔离
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("Execution", "Parallel")]
    public async Task ParallelTest1_ShouldExecuteIndependently()
    {
        _output.WriteLine("开始执行并行测试1");
        
        const string searchQuery = "并行测试1";
        
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.SearchAsync(searchQuery);
        
        await Task.Delay(1000);
        
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("并行测试1"), "并行测试1应该包含正确的搜索参数");
        
        _output.WriteLine("并行测试1执行完成");
    }

    /// <summary>
    /// 并行测试2 - 验证测试隔离
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("Execution", "Parallel")]
    public async Task ParallelTest2_ShouldExecuteIndependently()
    {
        _output.WriteLine("开始执行并行测试2");
        
        const string searchQuery = "并行测试2";
        
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.SearchAsync(searchQuery);
        
        await Task.Delay(1500);
        
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("并行测试2"), "并行测试2应该包含正确的搜索参数");
        
        _output.WriteLine("并行测试2执行完成");
    }

    /// <summary>
    /// 测试截图功能
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("Category", "Reporting")]
    public async Task Screenshot_ShouldCapturePageState()
    {
        _output.WriteLine("开始测试截图功能");
        
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"screenshot_test_{timestamp}";
        
        var screenshotBytes = await _fixture.TakeScreenshotAsync(_isolatedPage!, fileName);
        
        Assert.NotNull(screenshotBytes);
        Assert.True(screenshotBytes.Length > 0, "截图应该包含数据");
        
        _output.WriteLine($"截图功能测试通过，截图大小: {screenshotBytes.Length} 字节");
    }
}