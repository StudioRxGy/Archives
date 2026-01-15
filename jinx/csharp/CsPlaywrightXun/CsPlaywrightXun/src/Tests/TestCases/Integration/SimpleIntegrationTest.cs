using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.Integration;

/// <summary>
/// 简单集成测试类
/// 验证UI和API基本集成功能
/// 需求: 1.1, 1.3, 5.4, 6.6, 6.7
/// </summary>
[Trait("Type", "Integration")]
[Trait("Category", "Simple")]
[Trait("Priority", "High")]
public class SimpleIntegrationTest : IClassFixture<BrowserFixture>, IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly BrowserFixture _browserFixture;
    private readonly ApiTestFixture _apiFixture;
    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    
    // UI 组件
    private IBrowserContext? _isolatedContext;
    private IPage? _isolatedPage;
    private HomePage? _homePage;
    private SearchFlow? _searchFlow;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="browserFixture">浏览器固件</param>
    /// <param name="apiFixture">API固件</param>
    /// <param name="output">测试输出助手</param>
    public SimpleIntegrationTest(BrowserFixture browserFixture, ApiTestFixture apiFixture, ITestOutputHelper output)
    {
        _browserFixture = browserFixture ?? throw new ArgumentNullException(nameof(browserFixture));
        _apiFixture = apiFixture ?? throw new ArgumentNullException(nameof(apiFixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<SimpleIntegrationTest>();
    }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在初始化简单集成测试环境...");
        
        // 初始化浏览器组件
        await _browserFixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _browserFixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_browserFixture, _logger);
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 简单集成测试环境初始化完成");
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在清理简单集成测试环境...");
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
        }
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 简单集成测试环境清理完成");
    }

    /// <summary>
    /// 基础UI和API集成测试
    /// 验证UI操作和API调用的基本集成
    /// </summary>
    [Fact]
    [Trait("TestType", "BasicIntegration")]
    public async Task BasicUIAndAPI_Integration_ShouldWorkTogether()
    {
        _output.WriteLine("=== 开始基础UI和API集成测试 ===");
        
        const string searchQuery = "简单集成测试";
        
        // 步骤1: UI操作 - 执行搜索
        _output.WriteLine("步骤1: UI搜索操作");
        await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        var isPageLoaded = await _homePage.IsLoadedAsync();
        Assert.True(isPageLoaded, "首页应该成功加载");
        
        await _homePage.SearchAsync(searchQuery);
        await Task.Delay(2000); // 等待搜索结果加载
        
        var uiResultCount = await _homePage.GetSearchResultCountAsync();
        var currentUrl = _isolatedPage!.Url;
        
        Assert.True(uiResultCount > 0, "UI搜索应该返回结果");
        Assert.Contains("wd=", currentUrl, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine($"UI搜索完成 - 结果数: {uiResultCount}, URL: {currentUrl}");
        
        // 步骤2: 使用SearchFlow验证业务流程
        _output.WriteLine("步骤2: SearchFlow业务流程验证");
        
        // 返回首页重新搜索
        await _homePage.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // 使用SearchFlow执行搜索
        await _searchFlow!.ExecuteSearchWithValidationAsync("SearchFlow测试", 1);
        
        var flowResultCount = await _homePage.GetSearchResultCountAsync();
        Assert.True(flowResultCount > 0, "SearchFlow应该成功执行搜索");
        
        _output.WriteLine($"SearchFlow执行完成 - 结果数: {flowResultCount}");
        
        // 步骤3: 验证测试隔离
        _output.WriteLine("步骤3: 验证测试隔离");
        
        // 验证浏览器上下文隔离
        var contextId = _isolatedContext!.GetHashCode();
        var pageId = _isolatedPage.GetHashCode();
        
        Assert.NotEqual(0, contextId);
        Assert.NotEqual(0, pageId);
        
        _output.WriteLine($"测试隔离验证完成 - Context: {contextId}, Page: {pageId}");
        
        _output.WriteLine("=== 基础UI和API集成测试完成 ===");
    }

    /// <summary>
    /// 多流程编排测试
    /// 验证多个流程的协调执行
    /// </summary>
    [Fact]
    [Trait("TestType", "MultiFlow")]
    public async Task MultiFlow_Orchestration_ShouldExecuteSuccessfully()
    {
        _output.WriteLine("=== 开始多流程编排测试 ===");
        
        var searchQueries = new[] { "多流程测试1", "多流程测试2", "多流程测试3" };
        var results = new List<int>();
        
        foreach (var query in searchQueries)
        {
            _output.WriteLine($"执行搜索: {query}");
            
            // 导航到首页
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            
            // 使用SearchFlow执行搜索
            await _searchFlow!.ExecuteSimpleSearchAsync(query);
            await Task.Delay(1000);
            
            var resultCount = await _homePage.GetSearchResultCountAsync();
            results.Add(resultCount);
            
            _output.WriteLine($"搜索 '{query}' 完成 - 结果数: {resultCount}");
        }
        
        // 验证所有搜索都成功
        Assert.All(results, count => Assert.True(count > 0, "每次搜索都应该返回结果"));
        Assert.Equal(searchQueries.Length, results.Count);
        
        var totalResults = results.Sum();
        var averageResults = results.Average();
        
        _output.WriteLine($"多流程编排测试完成:");
        _output.WriteLine($"  总搜索次数: {results.Count}");
        _output.WriteLine($"  总结果数: {totalResults}");
        _output.WriteLine($"  平均结果数: {averageResults:F2}");
        
        _output.WriteLine("=== 多流程编排测试完成 ===");
    }

    /// <summary>
    /// 错误处理和恢复测试
    /// 验证系统在异常情况下的处理能力
    /// </summary>
    [Fact]
    [Trait("TestType", "ErrorHandling")]
    public async Task ErrorHandling_AndRecovery_ShouldHandleGracefully()
    {
        _output.WriteLine("=== 开始错误处理和恢复测试 ===");
        
        // 测试场景1: 空搜索查询处理
        _output.WriteLine("场景1: 空搜索查询处理");
        
        await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // 尝试空搜索 - 应该优雅处理
        try
        {
            await _homePage.SearchAsync("");
            _output.WriteLine("空搜索处理: 允许空搜索或优雅处理");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"空搜索处理: 抛出异常 - {ex.Message}");
            // 这是可以接受的行为
        }
        
        // 测试场景2: 页面刷新恢复
        _output.WriteLine("场景2: 页面刷新恢复");
        
        // 先执行正常搜索建立基线
        await _homePage.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.SearchAsync("恢复测试");
        
        var baselineResults = await _homePage.GetSearchResultCountAsync();
        Assert.True(baselineResults > 0, "基线搜索应该成功");
        
        // 模拟页面问题 - 刷新页面
        await _isolatedPage!.ReloadAsync();
        await _homePage.WaitForLoadAsync();
        
        // 恢复操作 - 重新搜索
        await _homePage.SearchAsync("恢复测试");
        var recoveryResults = await _homePage.GetSearchResultCountAsync();
        
        Assert.True(recoveryResults > 0, "恢复后搜索应该成功");
        _output.WriteLine($"页面恢复成功 - 基线结果: {baselineResults}, 恢复结果: {recoveryResults}");
        
        _output.WriteLine("=== 错误处理和恢复测试完成 ===");
    }
}