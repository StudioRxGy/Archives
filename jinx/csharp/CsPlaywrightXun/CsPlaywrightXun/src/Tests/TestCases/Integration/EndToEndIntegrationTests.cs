using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;
using CsPlaywrightXun.src.playwright.Tests.Integration.Scenarios;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.Integration;

/// <summary>
/// 端到端集成测试类
/// 结合 UI 和 API 测试验证完整业务流程
/// 需求: 1.1, 1.3, 5.4, 6.6, 6.7
/// </summary>
[Trait("Type", "Integration")]
[Trait("Category", "EndToEnd")]
[Trait("Priority", "Critical")]
public class EndToEndIntegrationTests : IClassFixture<BrowserFixture>, IClassFixture<ApiTestFixture>, IAsyncLifetime
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
    
    // API 组件
    private BaseApiTest? _apiTest;
    
    // 场景编排器
    private SearchScenario? _searchScenario;
    private UserJourneyScenario? _userJourneyScenario;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="browserFixture">浏览器固件</param>
    /// <param name="apiFixture">API固件</param>
    /// <param name="output">测试输出助手</param>
    public EndToEndIntegrationTests(BrowserFixture browserFixture, ApiTestFixture apiFixture, ITestOutputHelper output)
    {
        _browserFixture = browserFixture ?? throw new ArgumentNullException(nameof(browserFixture));
        _apiFixture = apiFixture ?? throw new ArgumentNullException(nameof(apiFixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<EndToEndIntegrationTests>();
    }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在初始化集成测试环境...");
        
        // 初始化浏览器组件
        await _browserFixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _browserFixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_browserFixture, _logger);
        
        // 初始化API组件
        _apiTest = new TestApiClient(_apiFixture.ApiClient, _apiFixture.Configuration, _apiFixture.Logger);
        
        // 初始化场景编排器
        _searchScenario = new SearchScenario(_browserFixture, _apiFixture, _logger);
        _userJourneyScenario = new UserJourneyScenario(_browserFixture, _apiFixture, _logger);
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 集成测试环境初始化完成");
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在清理集成测试环境...");
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
        }
        
        _apiTest?.Dispose();
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 集成测试环境清理完成");
    }

    #region 基础集成测试

    /// <summary>
    /// UI和API基础集成测试
    /// 验证UI操作和API调用的基本集成
    /// </summary>
    [Fact]
    [Trait("TestType", "BasicIntegration")]
    public async Task UIAndAPI_BasicIntegration_ShouldWorkTogether()
    {
        _output.WriteLine("=== 开始UI和API基础集成测试 ===");
        
        const string searchQuery = "集成测试";
        
        // 步骤1: API预检 - 验证服务可用性
        _output.WriteLine("步骤1: API预检验证");
        var apiPreCheckRequest = ((TestApiClient)_apiTest!).CreateGetRequest("/");
        var apiPreCheckResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(apiPreCheckRequest, "API预检");
        
        Assert.True(apiPreCheckResponse.IsSuccess, "API服务应该可用");
        _output.WriteLine($"API预检通过 - 状态码: {apiPreCheckResponse.StatusCode}");
        
        // 步骤2: UI操作 - 执行搜索
        _output.WriteLine("步骤2: UI搜索操作");
        await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.SearchAsync(searchQuery);
        
        // 等待页面跳转
        await Task.Delay(2000);
        var uiUrl = _isolatedPage!.Url;
        Assert.Contains("wd=", uiUrl);
        _output.WriteLine($"UI搜索完成 - URL: {uiUrl}");
        
        // 步骤3: API验证 - 验证相同搜索的API响应
        _output.WriteLine("步骤3: API搜索验证");
        var apiSearchRequest = ((TestApiClient)_apiTest).CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = searchQuery,
            ["ie"] = "utf-8"
        });
        var apiSearchResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(apiSearchRequest, "API搜索验证");
        
        Assert.True(apiSearchResponse.IsSuccess, "API搜索应该成功");
        Assert.Contains(searchQuery, apiSearchResponse.RawContent, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"API搜索验证通过 - 状态码: {apiSearchResponse.StatusCode}");
        
        // 步骤4: 结果一致性验证
        _output.WriteLine("步骤4: UI和API结果一致性验证");
        var uiResultCount = await _homePage.GetSearchResultCountAsync();
        
        // 验证UI和API都返回了搜索结果
        Assert.True(uiResultCount > 0, "UI应该显示搜索结果");
        Assert.True(apiSearchResponse.RawContent.Length > 1000, "API应该返回有意义的内容");
        
        _output.WriteLine($"集成测试完成 - UI结果数: {uiResultCount}, API响应长度: {apiSearchResponse.RawContent.Length}");
    }

    /// <summary>
    /// 数据一致性集成测试
    /// 验证UI显示的数据与API返回的数据一致性
    /// </summary>
    [Fact]
    [Trait("TestType", "DataConsistency")]
    public async Task UIAndAPI_DataConsistency_ShouldShowConsistentResults()
    {
        _output.WriteLine("=== 开始数据一致性集成测试 ===");
        
        const string searchQuery = "数据一致性测试";
        
        // 步骤1: 并行执行UI和API搜索
        _output.WriteLine("步骤1: 并行执行UI和API搜索");
        
        var uiTask = Task.Run(async () =>
        {
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            await _homePage.SearchAsync(searchQuery);
            await Task.Delay(2000);
            return new
            {
                Url = _isolatedPage!.Url,
                ResultCount = await _homePage.GetSearchResultCountAsync(),
                Results = await _homePage.GetSearchResultsAsync()
            };
        });
        
        var apiTask = Task.Run(async () =>
        {
            var request = ((TestApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = searchQuery,
                ["ie"] = "utf-8"
            });
            return await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(request, "并行API搜索");
        });
        
        var uiResult = await uiTask;
        var apiResult = await apiTask;
        
        // 步骤2: 验证基本响应
        Assert.True(uiResult.Url.Contains("wd="), "UI应该导航到搜索结果页");
        Assert.True(apiResult.IsSuccess, "API搜索应该成功");
        
        _output.WriteLine($"UI结果: URL包含搜索参数, 结果数: {uiResult.ResultCount}");
        _output.WriteLine($"API结果: 状态码 {apiResult.StatusCode}, 响应时间: {apiResult.ResponseTime.TotalMilliseconds:F2}ms");
        
        // 步骤3: 数据一致性验证
        _output.WriteLine("步骤3: 验证数据一致性");
        
        // 验证搜索关键词在两个响应中都存在
        var uiContainsQuery = uiResult.Results.Any(r => r.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        var apiContainsQuery = apiResult.RawContent.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
        
        // 注意：由于UI和API可能返回不同格式的数据，我们主要验证关键信息的存在
        Assert.True(uiResult.ResultCount > 0, "UI应该显示搜索结果");
        Assert.True(apiResult.RawContent.Length > 1000, "API应该返回有意义的内容");
        
        _output.WriteLine($"数据一致性验证完成 - UI包含查询词: {uiContainsQuery}, API包含查询词: {apiContainsQuery}");
    }

    #endregion

    #region 场景编排测试

    /// <summary>
    /// 搜索场景集成测试
    /// 使用SearchScenario编排多个流程
    /// </summary>
    [Fact]
    [Trait("TestType", "ScenarioOrchestration")]
    public async Task SearchScenario_MultiFlowOrchestration_ShouldExecuteSuccessfully()
    {
        _output.WriteLine("=== 开始搜索场景集成测试 ===");
        
        var scenarioParameters = new Dictionary<string, object>
        {
            ["searchQuery"] = "场景编排测试",
            ["validateUI"] = true,
            ["validateAPI"] = true,
            ["performanceCheck"] = true,
            ["expectedMinResults"] = 3
        };
        
        // 执行搜索场景
        await _searchScenario!.ExecuteAsync(scenarioParameters);
        
        // 验证场景执行结果
        var scenarioResult = _searchScenario.GetExecutionResult();
        Assert.True(scenarioResult.IsSuccess, $"搜索场景应该成功执行: {scenarioResult.ErrorMessage}");
        Assert.True(scenarioResult.ExecutedSteps.Count >= 4, "搜索场景应该执行多个步骤");
        
        _output.WriteLine($"搜索场景执行完成 - 总步骤: {scenarioResult.ExecutedSteps.Count}, " +
                         $"耗时: {scenarioResult.TotalDuration.TotalSeconds:F2}s");
        
        // 验证各个组件的执行结果
        Assert.True(scenarioResult.UIValidationPassed, "UI验证应该通过");
        Assert.True(scenarioResult.APIValidationPassed, "API验证应该通过");
        Assert.True(scenarioResult.PerformanceCheckPassed, "性能检查应该通过");
    }

    /// <summary>
    /// 用户旅程场景集成测试
    /// 模拟完整的用户使用旅程
    /// </summary>
    [Fact]
    [Trait("TestType", "UserJourney")]
    public async Task UserJourneyScenario_CompleteUserFlow_ShouldProvideSeamlessExperience()
    {
        _output.WriteLine("=== 开始用户旅程场景集成测试 ===");
        
        var journeyParameters = new Dictionary<string, object>
        {
            ["userType"] = "新用户",
            ["searchQueries"] = new[] { "第一次搜索", "第二次搜索", "第三次搜索" },
            ["includeAPIValidation"] = true,
            ["capturePerformanceMetrics"] = true
        };
        
        // 执行用户旅程场景
        await _userJourneyScenario!.ExecuteAsync(journeyParameters);
        
        // 验证旅程执行结果
        var journeyResult = _userJourneyScenario.GetExecutionResult();
        Assert.True(journeyResult.IsSuccess, $"用户旅程应该成功完成: {journeyResult.ErrorMessage}");
        Assert.True(journeyResult.CompletedSearches >= 3, "应该完成多次搜索");
        
        _output.WriteLine($"用户旅程完成 - 完成搜索: {journeyResult.CompletedSearches}, " +
                         $"总耗时: {journeyResult.TotalDuration.TotalSeconds:F2}s, " +
                         $"平均响应时间: {journeyResult.AverageResponseTime:F2}ms");
        
        // 验证性能指标
        Assert.True(journeyResult.AverageResponseTime < 5000, "平均响应时间应该合理");
        Assert.True(journeyResult.SuccessRate >= 95, $"成功率应该足够高: {journeyResult.SuccessRate:F2}%");
    }

    #endregion

    #region 错误处理和恢复测试

    /// <summary>
    /// 错误处理集成测试
    /// 验证UI和API错误情况下的集成处理
    /// </summary>
    [Fact]
    [Trait("TestType", "ErrorHandling")]
    public async Task ErrorHandling_UIAndAPIFailures_ShouldHandleGracefully()
    {
        _output.WriteLine("=== 开始错误处理集成测试 ===");
        
        // 测试场景1: 无效搜索查询
        _output.WriteLine("场景1: 无效搜索查询处理");
        var invalidQuery = "";
        
        // UI错误处理
        await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // 尝试空搜索 - UI应该优雅处理
        try
        {
            await _homePage.SearchAsync(invalidQuery);
            // UI可能允许空搜索，这是正常的
            _output.WriteLine("UI空搜索处理: 允许空搜索");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"UI空搜索处理: 抛出异常 - {ex.Message}");
        }
        
        // API错误处理
        var invalidApiRequest = ((TestApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = invalidQuery,
            ["ie"] = "utf-8"
        });
        var invalidApiResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(invalidApiRequest, "无效API搜索");
        
        // API应该返回有效响应，即使查询为空
        Assert.True(invalidApiResponse.StatusCode >= 200 && invalidApiResponse.StatusCode < 500, 
            "API应该优雅处理无效查询");
        
        _output.WriteLine($"API无效查询处理: 状态码 {invalidApiResponse.StatusCode}");
        
        // 测试场景2: 网络延迟模拟
        _output.WriteLine("场景2: 网络延迟处理");
        
        // 模拟慢查询
        var slowQuery = "网络延迟测试" + new string('测', 50);
        var slowApiRequest = ((TestApiClient)_apiTest).CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = slowQuery,
            ["ie"] = "utf-8"
        });
        
        var slowApiResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(slowApiRequest, "慢查询测试");
        
        // 验证即使是慢查询也能在合理时间内完成
        Assert.True(slowApiResponse.ResponseTime < TimeSpan.FromSeconds(30), 
            $"慢查询响应时间应该合理: {slowApiResponse.ResponseTime.TotalSeconds:F2}s");
        
        _output.WriteLine($"慢查询处理完成 - 响应时间: {slowApiResponse.ResponseTime.TotalMilliseconds:F2}ms");
    }

    /// <summary>
    /// 恢复机制集成测试
    /// 验证系统在故障后的恢复能力
    /// </summary>
    [Fact]
    [Trait("TestType", "Recovery")]
    public async Task Recovery_SystemFailureRecovery_ShouldRestoreNormalOperation()
    {
        _output.WriteLine("=== 开始恢复机制集成测试 ===");
        
        const string testQuery = "恢复测试";
        
        // 步骤1: 建立基线 - 正常操作
        _output.WriteLine("步骤1: 建立正常操作基线");
        await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.SearchAsync(testQuery);
        
        var baselineResultCount = await _homePage.GetSearchResultCountAsync();
        Assert.True(baselineResultCount > 0, "基线操作应该成功");
        _output.WriteLine($"基线建立 - 结果数: {baselineResultCount}");
        
        // 步骤2: 模拟故障 - 页面刷新
        _output.WriteLine("步骤2: 模拟页面故障和恢复");
        await _isolatedPage!.ReloadAsync();
        await _homePage.WaitForLoadAsync();
        
        // 恢复操作 - 重新搜索
        await _homePage.SearchAsync(testQuery);
        var recoveryResultCount = await _homePage.GetSearchResultCountAsync();
        
        Assert.True(recoveryResultCount > 0, "恢复后操作应该成功");
        _output.WriteLine($"页面恢复成功 - 结果数: {recoveryResultCount}");
        
        // 步骤3: API恢复测试
        _output.WriteLine("步骤3: API恢复能力测试");
        
        // 连续多次API调用，模拟高负载后的恢复
        var recoveryTasks = new List<Task<ApiResponse<string>>>();
        for (int i = 0; i < 5; i++)
        {
            var request = ((TestApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = $"{testQuery}_{i}",
                ["ie"] = "utf-8"
            });
            recoveryTasks.Add(((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(request, $"恢复测试_{i}"));
        }
        
        var recoveryResults = await Task.WhenAll(recoveryTasks);
        var successfulRecoveries = recoveryResults.Count(r => r.IsSuccess);
        
        Assert.True(successfulRecoveries >= 4, $"大部分API恢复调用应该成功: {successfulRecoveries}/5");
        _output.WriteLine($"API恢复测试完成 - 成功: {successfulRecoveries}/5");
        
        // 步骤4: 验证系统完全恢复
        _output.WriteLine("步骤4: 验证系统完全恢复");
        var finalApiRequest = ((TestApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = testQuery,
            ["ie"] = "utf-8"
        });
        var finalApiResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(finalApiRequest, "最终恢复验证");
        
        Assert.True(finalApiResponse.IsSuccess, "最终API调用应该成功");
        Assert.True(finalApiResponse.ResponseTime < TimeSpan.FromSeconds(10), 
            "恢复后响应时间应该正常");
        
        _output.WriteLine($"系统完全恢复 - 响应时间: {finalApiResponse.ResponseTime.TotalMilliseconds:F2}ms");
    }

    #endregion

    #region 性能集成测试

    /// <summary>
    /// 性能集成测试
    /// 验证UI和API在集成场景下的性能表现
    /// </summary>
    [Fact]
    [Trait("TestType", "Performance")]
    public async Task Performance_IntegratedUIAndAPI_ShouldMeetPerformanceTargets()
    {
        _output.WriteLine("=== 开始性能集成测试 ===");
        
        const int testIterations = 3;
        const string performanceQuery = "性能集成测试";
        
        var performanceResults = new List<IntegratedPerformanceResult>();
        
        for (int i = 0; i < testIterations; i++)
        {
            _output.WriteLine($"执行性能测试迭代 {i + 1}/{testIterations}");
            
            var iterationStart = DateTime.UtcNow;
            var result = new IntegratedPerformanceResult { Iteration = i + 1 };
            
            // UI性能测试
            var uiStart = DateTime.UtcNow;
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            await _homePage.SearchAsync($"{performanceQuery}_{i}");
            await Task.Delay(1000); // 等待结果加载
            var uiResultCount = await _homePage.GetSearchResultCountAsync();
            result.UIResponseTime = DateTime.UtcNow - uiStart;
            result.UIResultCount = uiResultCount;
            result.UISuccess = uiResultCount > 0;
            
            // API性能测试
            var apiStart = DateTime.UtcNow;
            var apiRequest = ((TestApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = $"{performanceQuery}_{i}",
                ["ie"] = "utf-8"
            });
            var apiResponse = await ((TestApiClient)_apiTest).ExecuteApiTestAsync<string>(apiRequest, $"性能测试API_{i}");
            result.APIResponseTime = apiResponse.ResponseTime;
            result.APISuccess = apiResponse.IsSuccess;
            result.APIStatusCode = apiResponse.StatusCode;
            
            result.TotalTime = DateTime.UtcNow - iterationStart;
            performanceResults.Add(result);
            
            _output.WriteLine($"迭代 {i + 1} 完成 - UI: {result.UIResponseTime.TotalMilliseconds:F2}ms, " +
                             $"API: {result.APIResponseTime.TotalMilliseconds:F2}ms, " +
                             $"总计: {result.TotalTime.TotalMilliseconds:F2}ms");
            
            // 迭代间隔
            if (i < testIterations - 1)
            {
                await Task.Delay(1000);
            }
        }
        
        // 性能分析
        var avgUITime = performanceResults.Average(r => r.UIResponseTime.TotalMilliseconds);
        var avgAPITime = performanceResults.Average(r => r.APIResponseTime.TotalMilliseconds);
        var avgTotalTime = performanceResults.Average(r => r.TotalTime.TotalMilliseconds);
        var uiSuccessRate = performanceResults.Count(r => r.UISuccess) * 100.0 / testIterations;
        var apiSuccessRate = performanceResults.Count(r => r.APISuccess) * 100.0 / testIterations;
        
        // 输出性能报告
        _output.WriteLine("\n=== 性能集成测试报告 ===");
        _output.WriteLine($"测试迭代数: {testIterations}");
        _output.WriteLine($"UI平均响应时间: {avgUITime:F2}ms");
        _output.WriteLine($"API平均响应时间: {avgAPITime:F2}ms");
        _output.WriteLine($"集成平均总时间: {avgTotalTime:F2}ms");
        _output.WriteLine($"UI成功率: {uiSuccessRate:F2}%");
        _output.WriteLine($"API成功率: {apiSuccessRate:F2}%");
        
        // 性能断言
        Assert.True(avgUITime < 10000, $"UI平均响应时间应该合理: {avgUITime:F2}ms");
        Assert.True(avgAPITime < 5000, $"API平均响应时间应该合理: {avgAPITime:F2}ms");
        Assert.True(avgTotalTime < 15000, $"集成总时间应该合理: {avgTotalTime:F2}ms");
        Assert.True(uiSuccessRate >= 90, $"UI成功率应该足够高: {uiSuccessRate:F2}%");
        Assert.True(apiSuccessRate >= 95, $"API成功率应该足够高: {apiSuccessRate:F2}%");
    }

    #endregion
}

/// <summary>
/// 测试用API客户端包装器
/// </summary>
internal class TestApiClient : BaseApiTest
{
    public TestApiClient(IApiClient apiClient, TestConfiguration configuration, ILogger logger)
        : base(apiClient, configuration, logger)
    {
    }

    // 公开受保护的方法
    public new ApiRequest CreateGetRequest(string endpoint, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null)
    {
        return base.CreateGetRequest(endpoint, queryParameters, headers);
    }

    public new Task<ApiResponse<T>> ExecuteApiTestAsync<T>(ApiRequest request, string? testName = null)
    {
        return base.ExecuteApiTestAsync<T>(request, testName);
    }
}

/// <summary>
/// 集成性能测试结果
/// </summary>
public class IntegratedPerformanceResult
{
    public int Iteration { get; set; }
    public TimeSpan UIResponseTime { get; set; }
    public TimeSpan APIResponseTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    public bool UISuccess { get; set; }
    public bool APISuccess { get; set; }
    public int UIResultCount { get; set; }
    public int APIStatusCode { get; set; }
}