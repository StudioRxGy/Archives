using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.Integration.Scenarios;

/// <summary>
/// 搜索场景编排器
/// 编排UI搜索流程和API验证流程，实现完整的搜索业务场景
/// 需求: 1.3, 5.4, 6.6, 6.7
/// </summary>
public class SearchScenario : BaseScenario
{
    private IBrowserContext? _isolatedContext;
    private IPage? _isolatedPage;
    private HomePage? _homePage;
    private SearchFlow? _searchFlow;
    private BaseApiTest? _apiTest;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="browserFixture">浏览器固件</param>
    /// <param name="apiFixture">API固件</param>
    /// <param name="logger">日志记录器</param>
    public SearchScenario(BrowserFixture browserFixture, ApiTestFixture apiFixture, ILogger logger)
        : base(browserFixture, apiFixture, logger)
    {
    }

    /// <summary>
    /// 执行搜索场景
    /// </summary>
    /// <param name="parameters">场景参数，应包含 "searchQuery" 键，可选包含 "validateUI"、"validateAPI"、"performanceCheck"、"expectedMinResults" 键</param>
    public override async Task ExecuteAsync(Dictionary<string, object>? parameters = null)
    {
        StartScenarioExecution();

        try
        {
            // 验证参数
            ValidateStringParameter(parameters!, "searchQuery");
            
            var searchQuery = parameters!["searchQuery"].ToString()!;
            var validateUI = parameters.ContainsKey("validateUI") && Convert.ToBoolean(parameters["validateUI"]);
            var validateAPI = parameters.ContainsKey("validateAPI") && Convert.ToBoolean(parameters["validateAPI"]);
            var performanceCheck = parameters.ContainsKey("performanceCheck") && Convert.ToBoolean(parameters["performanceCheck"]);
            var expectedMinResults = parameters.ContainsKey("expectedMinResults") ? Convert.ToInt32(parameters["expectedMinResults"]) : 1;
            
            _logger.LogInformation($"[{ScenarioName}] 搜索场景参数 - 查询: {searchQuery}, UI验证: {validateUI}, API验证: {validateAPI}, 性能检查: {performanceCheck}");

            // 步骤1: 初始化组件
            await ExecuteScenarioStepAsync("初始化UI和API组件", async () =>
            {
                await InitializeComponentsAsync();
            });

            // 步骤2: 执行UI搜索流程
            var uiResults = await ExecuteScenarioStepAsync("执行UI搜索流程", async () =>
            {
                return await ExecuteUISearchFlowAsync(searchQuery, validateUI, expectedMinResults);
            });

            // 步骤3: 执行API验证流程（如果需要）
            var apiResults = new APISearchResults();
            if (validateAPI)
            {
                apiResults = await ExecuteScenarioStepAsync("执行API验证流程", async () =>
                {
                    return await ExecuteAPIValidationFlowAsync(searchQuery, performanceCheck);
                });
            }

            // 步骤4: 性能检查流程（如果需要）
            var performanceResults = new PerformanceCheckResults();
            if (performanceCheck)
            {
                performanceResults = await ExecuteScenarioStepAsync("执行性能检查流程", async () =>
                {
                    return await ExecutePerformanceCheckFlowAsync(searchQuery);
                });
            }

            // 步骤5: 结果整合和验证
            await ExecuteScenarioStepAsync("整合和验证结果", async () =>
            {
                await IntegrateAndValidateResultsAsync(uiResults, apiResults, performanceResults, validateUI, validateAPI, performanceCheck);
            });

            // 更新执行结果
            var executionResult = GetExecutionResult();
            executionResult.UIValidationPassed = !validateUI || uiResults.IsSuccess;
            executionResult.APIValidationPassed = !validateAPI || apiResults.IsSuccess;
            executionResult.PerformanceCheckPassed = !performanceCheck || performanceResults.IsSuccess;
            executionResult.CompletedSearches = 1;
            executionResult.AverageResponseTime = CalculateAverageResponseTime(uiResults, apiResults, performanceResults);
            executionResult.SuccessRate = CalculateSuccessRate(uiResults, apiResults, performanceResults, validateUI, validateAPI, performanceCheck);

            EndScenarioExecution();
        }
        catch (Exception ex)
        {
            LogScenarioExecutionFailure(ex);
            throw;
        }
        finally
        {
            await CleanupComponentsAsync();
        }
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    private async Task InitializeComponentsAsync()
    {
        // 初始化浏览器组件
        await _browserFixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _browserFixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_browserFixture, _logger);
        
        // 初始化API组件
        _apiTest = new SearchScenarioApiClient(_apiFixture.ApiClient, _apiFixture.Configuration, _apiFixture.Logger);
        
        _logger.LogInformation($"[{ScenarioName}] 组件初始化完成");
    }

    /// <summary>
    /// 执行UI搜索流程
    /// </summary>
    private async Task<UISearchResults> ExecuteUISearchFlowAsync(string searchQuery, bool validateResults, int expectedMinResults)
    {
        var results = new UISearchResults();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // 导航到首页
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            
            // 执行搜索
            if (validateResults)
            {
                await _searchFlow!.ExecuteSearchWithValidationAsync(searchQuery, expectedMinResults);
            }
            else
            {
                await _searchFlow!.ExecuteSimpleSearchAsync(searchQuery);
            }
            
            // 收集结果
            if (validateResults)
            {
                results.ResultCount = await _homePage.GetSearchResultCountAsync();
                results.Results = await _homePage.GetSearchResultsAsync();
                results.CurrentUrl = _isolatedPage!.Url;
            }
            
            results.IsSuccess = true;
            results.ResponseTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation($"[{ScenarioName}] UI搜索流程完成 - 结果数: {results.ResultCount}, 响应时间: {results.ResponseTime.TotalMilliseconds:F2}ms");
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.ErrorMessage = ex.Message;
            results.ResponseTime = DateTime.UtcNow - startTime;
            _logger.LogError(ex, $"[{ScenarioName}] UI搜索流程失败");
        }
        
        return results;
    }

    /// <summary>
    /// 执行API验证流程
    /// </summary>
    private async Task<APISearchResults> ExecuteAPIValidationFlowAsync(string searchQuery, bool includePerformanceCheck)
    {
        var results = new APISearchResults();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // 执行API搜索
            var request = ((SearchScenarioApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = searchQuery,
                ["ie"] = "utf-8"
            });
            
            var response = await ((SearchScenarioApiClient)_apiTest).ExecuteApiTestAsync<string>(request, "场景API搜索");
            
            results.IsSuccess = response.IsSuccess;
            results.StatusCode = response.StatusCode;
            results.ResponseTime = response.ResponseTime;
            results.ContentLength = response.RawContent?.Length ?? 0;
            results.ContainsSearchQuery = response.RawContent?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ?? false;
            
            if (includePerformanceCheck)
            {
                results.PerformanceMetrics = new Dictionary<string, object>
                {
                    ["ResponseTime"] = response.ResponseTime.TotalMilliseconds,
                    ["ContentLength"] = results.ContentLength,
                    ["StatusCode"] = results.StatusCode
                };
            }
            
            _logger.LogInformation($"[{ScenarioName}] API验证流程完成 - 状态码: {results.StatusCode}, 响应时间: {results.ResponseTime.TotalMilliseconds:F2}ms");
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.ErrorMessage = ex.Message;
            results.ResponseTime = DateTime.UtcNow - startTime;
            _logger.LogError(ex, $"[{ScenarioName}] API验证流程失败");
        }
        
        return results;
    }

    /// <summary>
    /// 执行性能检查流程
    /// </summary>
    private async Task<PerformanceCheckResults> ExecutePerformanceCheckFlowAsync(string searchQuery)
    {
        var results = new PerformanceCheckResults();
        var startTime = DateTime.UtcNow;
        
        try
        {
            // 执行多次API调用以获取性能基线
            var performanceRequests = new List<Task<Services.Api.ApiResponse<string>>>();
            for (int i = 0; i < 3; i++)
            {
                var request = ((SearchScenarioApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
                {
                    ["wd"] = $"{searchQuery}_perf_{i}",
                    ["ie"] = "utf-8"
                });
                performanceRequests.Add(((SearchScenarioApiClient)_apiTest).ExecuteApiTestAsync<string>(request, $"性能检查_{i}"));
            }
            
            var responses = await Task.WhenAll(performanceRequests);
            
            // 分析性能数据
            var responseTimes = responses.Select(r => r.ResponseTime.TotalMilliseconds).ToList();
            results.AverageResponseTime = responseTimes.Average();
            results.MinResponseTime = responseTimes.Min();
            results.MaxResponseTime = responseTimes.Max();
            results.SuccessfulRequests = responses.Count(r => r.IsSuccess);
            results.TotalRequests = responses.Length;
            results.SuccessRate = (double)results.SuccessfulRequests / results.TotalRequests * 100;
            
            // 性能检查通过条件
            results.IsSuccess = results.AverageResponseTime < 5000 && results.SuccessRate >= 90;
            results.TotalTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation($"[{ScenarioName}] 性能检查流程完成 - 平均响应时间: {results.AverageResponseTime:F2}ms, 成功率: {results.SuccessRate:F2}%");
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.ErrorMessage = ex.Message;
            results.TotalTime = DateTime.UtcNow - startTime;
            _logger.LogError(ex, $"[{ScenarioName}] 性能检查流程失败");
        }
        
        return results;
    }

    /// <summary>
    /// 整合和验证结果
    /// </summary>
    private async Task IntegrateAndValidateResultsAsync(
        UISearchResults uiResults, 
        APISearchResults apiResults, 
        PerformanceCheckResults performanceResults,
        bool validateUI, 
        bool validateAPI, 
        bool performanceCheck)
    {
        _logger.LogInformation($"[{ScenarioName}] 开始整合验证结果");
        
        // 记录详细结果
        _logger.LogInformation($"[{ScenarioName}] UI结果: 成功={uiResults.IsSuccess}, 结果数={uiResults.ResultCount}, 响应时间={uiResults.ResponseTime.TotalMilliseconds:F2}ms");
        
        if (validateAPI)
        {
            _logger.LogInformation($"[{ScenarioName}] API结果: 成功={apiResults.IsSuccess}, 状态码={apiResults.StatusCode}, 响应时间={apiResults.ResponseTime.TotalMilliseconds:F2}ms");
        }
        
        if (performanceCheck)
        {
            _logger.LogInformation($"[{ScenarioName}] 性能结果: 成功={performanceResults.IsSuccess}, 平均响应时间={performanceResults.AverageResponseTime:F2}ms, 成功率={performanceResults.SuccessRate:F2}%");
        }
        
        // 这里不包含断言，只是记录和整合结果
        // 断言逻辑由调用方（测试方法）处理
        
        await Task.CompletedTask; // 占位符，表示异步操作完成
    }

    /// <summary>
    /// 计算平均响应时间
    /// </summary>
    private double CalculateAverageResponseTime(UISearchResults uiResults, APISearchResults apiResults, PerformanceCheckResults performanceResults)
    {
        var times = new List<double>();
        
        if (uiResults.IsSuccess)
            times.Add(uiResults.ResponseTime.TotalMilliseconds);
        
        if (apiResults.IsSuccess)
            times.Add(apiResults.ResponseTime.TotalMilliseconds);
        
        if (performanceResults.IsSuccess)
            times.Add(performanceResults.AverageResponseTime);
        
        return times.Count > 0 ? times.Average() : 0;
    }

    /// <summary>
    /// 计算成功率
    /// </summary>
    private double CalculateSuccessRate(UISearchResults uiResults, APISearchResults apiResults, PerformanceCheckResults performanceResults, bool validateUI, bool validateAPI, bool performanceCheck)
    {
        var totalChecks = 0;
        var successfulChecks = 0;
        
        if (validateUI)
        {
            totalChecks++;
            if (uiResults.IsSuccess) successfulChecks++;
        }
        
        if (validateAPI)
        {
            totalChecks++;
            if (apiResults.IsSuccess) successfulChecks++;
        }
        
        if (performanceCheck)
        {
            totalChecks++;
            if (performanceResults.IsSuccess) successfulChecks++;
        }
        
        return totalChecks > 0 ? (double)successfulChecks / totalChecks * 100 : 100;
    }

    /// <summary>
    /// 清理组件
    /// </summary>
    private async Task CleanupComponentsAsync()
    {
        try
        {
            if (_isolatedContext != null)
            {
                await _isolatedContext.CloseAsync();
            }
            
            _apiTest?.Dispose();
            
            _logger.LogInformation($"[{ScenarioName}] 组件清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[{ScenarioName}] 组件清理时发生警告");
        }
    }
}

/// <summary>
/// 场景专用API客户端
/// </summary>
internal class SearchScenarioApiClient : BaseApiTest
{
    public SearchScenarioApiClient(Core.Interfaces.IApiClient apiClient, Core.Configuration.TestConfiguration configuration, ILogger logger)
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
/// UI搜索结果
/// </summary>
public class UISearchResults
{
    public bool IsSuccess { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int ResultCount { get; set; }
    public List<string> Results { get; set; } = new();
    public string CurrentUrl { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// API搜索结果
/// </summary>
public class APISearchResults
{
    public bool IsSuccess { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int StatusCode { get; set; }
    public int ContentLength { get; set; }
    public bool ContainsSearchQuery { get; set; }
    public Dictionary<string, object>? PerformanceMetrics { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 性能检查结果
/// </summary>
public class PerformanceCheckResults
{
    public bool IsSuccess { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double AverageResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public int SuccessfulRequests { get; set; }
    public int TotalRequests { get; set; }
    public double SuccessRate { get; set; }
    public string? ErrorMessage { get; set; }
}