using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.UI.baidu;

/// <summary>
/// 完整的首页UI测试类
/// 演示数据驱动测试、参数化、断言和测试验证逻辑
/// 验证测试隔离和并行执行
/// 需求: 1.1, 1.2, 4.5, 11.1, 11.2
/// </summary>
[Trait("Type", "UI")]
[Trait("Category", "HomePage")]
[Trait("Priority", "High")]
public class ComprehensiveHomePageTests : IClassFixture<BrowserFixture>, IAsyncLifetime
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
    public ComprehensiveHomePageTests(BrowserFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<ComprehensiveHomePageTests>();
    }

    /// <summary>
    /// 测试初始化 - 确保每个测试用例使用独立的 BrowserContext
    /// </summary>
    public async Task InitializeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在初始化测试环境...");
        
        await _fixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _fixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_fixture, _logger);
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 测试环境初始化完成 - Context: {_isolatedContext.GetHashCode()}, Page: {_isolatedPage.GetHashCode()}");
    }

    /// <summary>
    /// 测试清理 - 确保测试隔离
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在清理测试环境...");
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 已关闭隔离的浏览器上下文: {_isolatedContext.GetHashCode()}");
        }
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 测试环境清理完成");
    }

    #region 基础功能测试

    /// <summary>
    /// 测试首页加载功能 - 验证页面基本元素
    /// </summary>
    [Fact]
    [Trait("TestType", "Smoke")]
    public async Task HomePage_LoadAndValidateElements_ShouldSucceed()
    {
        _output.WriteLine("开始测试首页加载和元素验证");
        
        // Arrange & Act
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Assert - 验证页面加载状态
        var isLoaded = await _homePage.IsLoadedAsync();
        Assert.True(isLoaded, "首页应该成功加载");
        
        // Assert - 验证搜索框可用性
        var isSearchBoxAvailable = await _homePage.IsSearchBoxAvailableAsync();
        Assert.True(isSearchBoxAvailable, "搜索框应该可用");
        
        // Assert - 验证搜索按钮可用性
        var isSearchButtonAvailable = await _homePage.IsSearchButtonAvailableAsync();
        Assert.True(isSearchButtonAvailable, "搜索按钮应该可用");
        
        // Assert - 验证搜索框占位符文本
        var placeholder = await _homePage.GetSearchBoxPlaceholderAsync();
        Assert.False(string.IsNullOrEmpty(placeholder), "搜索框应该有占位符文本");
        
        // Assert - 验证页面URL
        var currentUrl = _isolatedPage!.Url;
        Assert.Contains("baidu.com", currentUrl);
        
        _output.WriteLine($"首页加载验证通过 - URL: {currentUrl}, 占位符: {placeholder}");
    }

    /// <summary>
    /// 测试搜索框基本操作
    /// </summary>
    [Fact]
    [Trait("TestType", "Functional")]
    public async Task SearchBox_BasicOperations_ShouldWork()
    {
        _output.WriteLine("开始测试搜索框基本操作");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        const string testInput = "测试输入内容";
        
        // Act - 输入文本
        await _homePage.SearchAsync(testInput);
        
        // 等待页面跳转
        await Task.Delay(2000);
        
        // Assert - 验证URL包含搜索参数
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("wd="), "URL应该包含搜索参数");
        Assert.True(currentUrl.Contains("测试输入内容") || currentUrl.Contains(Uri.EscapeDataString("测试输入内容")), 
            "URL应该包含搜索关键词");
        
        _output.WriteLine($"搜索框基本操作测试通过 - 搜索URL: {currentUrl}");
    }

    #endregion

    #region 数据驱动测试 - CSV

    /// <summary>
    /// 测试数据模型
    /// </summary>
    public class SearchTestData
    {
        public string TestName { get; set; } = string.Empty;
        public string SearchQuery { get; set; } = string.Empty;
        public int ExpectedResultCount { get; set; }
        public string Environment { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// 数据驱动搜索测试 - 使用CSV数据源
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [CsvData("CsPlaywrightXun/src/config/date/UI/valid_test_data.csv")]
    [Trait("TestType", "DataDriven")]
    [Trait("DataSource", "CSV")]
    public async Task Search_WithCsvData_ShouldReturnResults(Dictionary<string, object> testData)
    {
        // 跳过禁用的测试
        if (testData.ContainsKey("IsEnabled") && !Convert.ToBoolean(testData["IsEnabled"]))
        {
            _output.WriteLine($"跳过禁用的测试: {testData["TestName"]}");
            return;
        }
        
        var testName = testData["TestName"].ToString()!;
        var searchQuery = testData["SearchQuery"].ToString()!;
        var expectedResultCount = Convert.ToInt32(testData["ExpectedResultCount"]);
        
        _output.WriteLine($"开始执行CSV数据驱动测试: {testName}");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 使用SearchFlow执行搜索
        var parameters = new Dictionary<string, object>
        {
            ["searchQuery"] = searchQuery,
            ["validateResults"] = true,
            ["expectedMinResults"] = expectedResultCount,
            ["captureResults"] = true
        };
        
        await _searchFlow!.ExecuteAsync(parameters);
        
        // Assert - 验证搜索结果
        var actualResultCount = await _homePage.GetSearchResultCountAsync();
        Assert.True(actualResultCount >= expectedResultCount, 
            $"搜索结果数量不足，期望至少 {expectedResultCount} 个，实际 {actualResultCount} 个");
        
        // Assert - 验证搜索结果内容
        var searchResults = await _homePage.GetSearchResultsAsync();
        Assert.NotEmpty(searchResults);
        Assert.True(searchResults.Count >= expectedResultCount, 
            $"搜索结果列表数量不足，期望至少 {expectedResultCount} 个，实际 {searchResults.Count} 个");
        
        _output.WriteLine($"CSV数据驱动测试通过: {testName} - 搜索词: {searchQuery}, 结果数: {actualResultCount}");
    }

    #endregion

    #region 数据驱动测试 - JSON

    /// <summary>
    /// 数据驱动搜索测试 - 使用JSON数据源
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [JsonData("CsPlaywrightXun/src/config/date/UI/search_test_data.json")]
    [Trait("TestType", "DataDriven")]
    [Trait("DataSource", "JSON")]
    public async Task Search_WithJsonData_ShouldReturnResults(Dictionary<string, object> testData)
    {
        // 跳过禁用的测试
        if (testData.ContainsKey("isEnabled") && !Convert.ToBoolean(testData["isEnabled"]))
        {
            _output.WriteLine($"跳过禁用的测试: {testData["testName"]}");
            return;
        }
        
        var testName = testData["testName"].ToString()!;
        var searchQuery = testData["searchQuery"].ToString()!;
        var expectedResultCount = Convert.ToInt32(testData["expectedResultCount"]);
        
        _output.WriteLine($"开始执行JSON数据驱动测试: {testName}");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 执行搜索
        await _homePage.SearchAsync(searchQuery);
        await _homePage.WaitForSearchResultsAsync();
        
        // Assert - 验证搜索结果
        var actualResultCount = await _homePage.GetSearchResultCountAsync();
        Assert.True(actualResultCount >= expectedResultCount, 
            $"搜索结果数量不足，期望至少 {expectedResultCount} 个，实际 {actualResultCount} 个");
        
        // Assert - 验证页面状态
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("wd="), "应该导航到搜索结果页面");
        
        _output.WriteLine($"JSON数据驱动测试通过: {testName} - 搜索词: {searchQuery}, 结果数: {actualResultCount}");
    }

    #endregion

    #region 参数化测试

    /// <summary>
    /// 参数化搜索测试 - 使用内联数据
    /// </summary>
    /// <param name="searchQuery">搜索关键词</param>
    /// <param name="expectedMinResults">期望的最少结果数</param>
    [Theory]
    [InlineData("Playwright", 5)]
    [InlineData("自动化测试", 3)]
    [InlineData("C# 编程", 8)]
    [InlineData("软件测试", 10)]
    [Trait("TestType", "Parameterized")]
    public async Task Search_WithInlineData_ShouldReturnResults(string searchQuery, int expectedMinResults)
    {
        _output.WriteLine($"开始执行参数化测试 - 搜索词: {searchQuery}, 期望结果数: {expectedMinResults}");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 使用SearchFlow执行搜索
        await _searchFlow!.ExecuteSearchWithValidationAsync(searchQuery, expectedMinResults);
        
        // Assert - 验证搜索结果
        var actualResultCount = await _homePage.GetSearchResultCountAsync();
        Assert.True(actualResultCount >= expectedMinResults, 
            $"搜索结果数量不足，期望至少 {expectedMinResults} 个，实际 {actualResultCount} 个");
        
        // Assert - 验证搜索结果质量
        var searchResults = await _homePage.GetSearchResultsAsync();
        Assert.NotEmpty(searchResults);
        
        // 验证搜索结果中包含相关内容（简单的相关性检查）
        var relevantResults = searchResults.Where(result => 
            result.ToLower().Contains(searchQuery.ToLower().Split(' ')[0])).Count();
        
        Assert.True(relevantResults > 0, $"搜索结果应该包含与 '{searchQuery}' 相关的内容");
        
        _output.WriteLine($"参数化测试通过 - 搜索词: {searchQuery}, 结果数: {actualResultCount}, 相关结果: {relevantResults}");
    }

    #endregion

    #region 并行执行和测试隔离验证

    /// <summary>
    /// 并行测试1 - 验证测试隔离
    /// </summary>
    [Fact]
    [Trait("TestType", "Parallel")]
    [Trait("Execution", "Parallel")]
    public async Task ParallelTest1_SearchFunctionality_ShouldBeIsolated()
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        _output.WriteLine($"[{testId}] 开始执行并行测试1");
        
        const string searchQuery = "并行测试1_Playwright";
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act
        await _homePage.SearchAsync(searchQuery);
        await Task.Delay(1000); // 模拟处理时间
        
        // Assert - 验证测试隔离
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("并行测试1") || currentUrl.Contains(Uri.EscapeDataString("并行测试1")), 
            "并行测试1应该包含正确的搜索参数");
        
        // 验证上下文隔离
        var contextId = _isolatedContext!.GetHashCode();
        var pageId = _isolatedPage.GetHashCode();
        
        _output.WriteLine($"[{testId}] 并行测试1完成 - Context: {contextId}, Page: {pageId}, URL: {currentUrl}");
    }

    /// <summary>
    /// 并行测试2 - 验证测试隔离
    /// </summary>
    [Fact]
    [Trait("TestType", "Parallel")]
    [Trait("Execution", "Parallel")]
    public async Task ParallelTest2_SearchFunctionality_ShouldBeIsolated()
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        _output.WriteLine($"[{testId}] 开始执行并行测试2");
        
        const string searchQuery = "并行测试2_自动化";
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act
        await _homePage.SearchAsync(searchQuery);
        await Task.Delay(1500); // 不同的处理时间以验证隔离
        
        // Assert - 验证测试隔离
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("并行测试2") || currentUrl.Contains(Uri.EscapeDataString("并行测试2")), 
            "并行测试2应该包含正确的搜索参数");
        
        // 验证上下文隔离
        var contextId = _isolatedContext!.GetHashCode();
        var pageId = _isolatedPage.GetHashCode();
        
        _output.WriteLine($"[{testId}] 并行测试2完成 - Context: {contextId}, Page: {pageId}, URL: {currentUrl}");
    }

    /// <summary>
    /// 并行测试3 - 验证测试隔离
    /// </summary>
    [Fact]
    [Trait("TestType", "Parallel")]
    [Trait("Execution", "Parallel")]
    public async Task ParallelTest3_SearchFunctionality_ShouldBeIsolated()
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        _output.WriteLine($"[{testId}] 开始执行并行测试3");
        
        const string searchQuery = "并行测试3_测试框架";
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act
        await _homePage.SearchAsync(searchQuery);
        await Task.Delay(800); // 不同的处理时间以验证隔离
        
        // Assert - 验证测试隔离
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("并行测试3") || currentUrl.Contains(Uri.EscapeDataString("并行测试3")), 
            "并行测试3应该包含正确的搜索参数");
        
        // 验证上下文隔离
        var contextId = _isolatedContext!.GetHashCode();
        var pageId = _isolatedPage.GetHashCode();
        
        _output.WriteLine($"[{testId}] 并行测试3完成 - Context: {contextId}, Page: {pageId}, URL: {currentUrl}");
    }

    #endregion

    #region Flow集成测试

    /// <summary>
    /// 测试SearchFlow完整功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "Flow")]
    public async Task SearchFlow_CompleteWorkflow_ShouldExecuteSuccessfully()
    {
        _output.WriteLine("开始测试SearchFlow完整工作流程");
        
        const string searchQuery = "企业级自动化测试框架";
        const int expectedMinResults = 5;
        
        // Act - 执行完整的搜索流程
        await _searchFlow!.ExecuteSearchWithValidationAsync(searchQuery, expectedMinResults);
        
        // Assert - 验证流程执行结果
        var actualResultCount = await _homePage!.GetSearchResultCountAsync();
        Assert.True(actualResultCount >= expectedMinResults, 
            $"SearchFlow执行后，搜索结果数量应该至少为 {expectedMinResults}，实际为 {actualResultCount}");
        
        // Assert - 验证页面状态
        var currentUrl = _fixture.Page.Url;
        Assert.True(currentUrl.Contains("wd="), "SearchFlow应该导航到搜索结果页面");
        
        // Assert - 验证搜索结果内容
        var searchResults = await _homePage.GetSearchResultsAsync();
        Assert.NotEmpty(searchResults);
        Assert.True(searchResults.Count >= expectedMinResults, 
            $"搜索结果列表应该包含至少 {expectedMinResults} 个结果");
        
        _output.WriteLine($"SearchFlow完整工作流程测试通过 - 搜索词: {searchQuery}, 结果数: {actualResultCount}");
    }

    /// <summary>
    /// 测试SearchFlow错误处理
    /// </summary>
    [Fact]
    [Trait("TestType", "ErrorHandling")]
    [Trait("Component", "Flow")]
    public async Task SearchFlow_WithInvalidInput_ShouldHandleGracefully()
    {
        _output.WriteLine("开始测试SearchFlow错误处理");
        
        // Test with empty search query
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _searchFlow!.ExecuteSimpleSearchAsync("");
        });
        
        Assert.Contains("searchQuery", exception.Message);
        
        _output.WriteLine("SearchFlow错误处理测试通过 - 空搜索词正确抛出异常");
    }

    #endregion

    #region 截图和报告功能测试

    /// <summary>
    /// 测试截图功能
    /// </summary>
    [Fact]
    [Trait("TestType", "Reporting")]
    [Trait("Category", "Screenshot")]
    public async Task Screenshot_Functionality_ShouldCapturePageState()
    {
        _output.WriteLine("开始测试截图功能");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 执行搜索以改变页面状态
        await _homePage.SearchAsync("截图测试");
        await Task.Delay(2000);
        
        // Act - 截图
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"comprehensive_test_screenshot_{timestamp}";
        
        var screenshotBytes = await _fixture.TakeScreenshotAsync(_isolatedPage!, fileName);
        
        // Assert
        Assert.NotNull(screenshotBytes);
        Assert.True(screenshotBytes.Length > 0, "截图应该包含数据");
        Assert.True(screenshotBytes.Length > 10000, "截图文件大小应该合理（大于10KB）");
        
        _output.WriteLine($"截图功能测试通过 - 文件名: {fileName}, 大小: {screenshotBytes.Length} 字节");
    }

    #endregion

    #region 性能和稳定性测试

    /// <summary>
    /// 测试连续搜索操作的稳定性
    /// </summary>
    [Fact]
    [Trait("TestType", "Stability")]
    [Trait("Category", "Performance")]
    public async Task Search_ContinuousOperations_ShouldBeStable()
    {
        _output.WriteLine("开始测试连续搜索操作的稳定性");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        var searchQueries = new[] { "稳定性测试1", "稳定性测试2", "稳定性测试3" };
        var results = new List<int>();
        
        // Act - 执行连续搜索
        foreach (var query in searchQueries)
        {
            _output.WriteLine($"执行搜索: {query}");
            
            await _homePage.SearchAsync(query);
            await _homePage.WaitForSearchResultsAsync();
            
            var resultCount = await _homePage.GetSearchResultCountAsync();
            results.Add(resultCount);
            
            // 返回首页准备下一次搜索
            await _homePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            
            await Task.Delay(1000); // 避免请求过于频繁
        }
        
        // Assert
        Assert.All(results, count => Assert.True(count > 0, "每次搜索都应该返回结果"));
        Assert.Equal(searchQueries.Length, results.Count);
        
        _output.WriteLine($"连续搜索稳定性测试通过 - 执行了 {results.Count} 次搜索，结果数: [{string.Join(", ", results)}]");
    }

    #endregion
}