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
/// 用户旅程场景编排器
/// 模拟完整的用户使用旅程，包括多次搜索、页面导航和API交互
/// 需求: 1.1, 1.3, 5.4, 6.6, 6.7
/// </summary>
public class UserJourneyScenario : BaseScenario
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
    public UserJourneyScenario(BrowserFixture browserFixture, ApiTestFixture apiFixture, ILogger logger)
        : base(browserFixture, apiFixture, logger)
    {
    }

    /// <summary>
    /// 执行用户旅程场景
    /// </summary>
    /// <param name="parameters">场景参数，应包含 "userType" 和 "searchQueries" 键，可选包含 "includeAPIValidation"、"capturePerformanceMetrics" 键</param>
    public override async Task ExecuteAsync(Dictionary<string, object>? parameters = null)
    {
        StartScenarioExecution();

        try
        {
            // 验证参数
            ValidateRequiredParameter(parameters!, "userType");
            ValidateRequiredParameter(parameters, "searchQueries");
            
            var userType = parameters!["userType"].ToString()!;
            var searchQueries = (string[])parameters["searchQueries"];
            var includeAPIValidation = parameters.ContainsKey("includeAPIValidation") && Convert.ToBoolean(parameters["includeAPIValidation"]);
            var capturePerformanceMetrics = parameters.ContainsKey("capturePerformanceMetrics") && Convert.ToBoolean(parameters["capturePerformanceMetrics"]);
            
            _logger.LogInformation($"[{ScenarioName}] 用户旅程参数 - 用户类型: {userType}, 搜索数量: {searchQueries.Length}, API验证: {includeAPIValidation}, 性能监控: {capturePerformanceMetrics}");

            // 步骤1: 初始化用户会话
            var userSession = await ExecuteScenarioStepAsync("初始化用户会话", async () =>
            {
                return await InitializeUserSessionAsync(userType);
            });

            // 步骤2: 用户首次访问
            await ExecuteScenarioStepAsync("用户首次访问", async () =>
            {
                await ExecuteFirstVisitFlowAsync(userSession);
            });

            // 步骤3: 执行搜索旅程
            var searchResults = await ExecuteScenarioStepAsync("执行搜索旅程", async () =>
            {
                return await ExecuteSearchJourneyFlowAsync(userSession, searchQueries, includeAPIValidation, capturePerformanceMetrics);
            });

            // 步骤4: 用户行为分析
            var behaviorAnalysis = await ExecuteScenarioStepAsync("用户行为分析", async () =>
            {
                return await AnalyzeUserBehaviorFlowAsync(userSession, searchResults);
            });

            // 步骤5: 会话总结和清理
            await ExecuteScenarioStepAsync("会话总结和清理", async () =>
            {
                await FinalizeUserSessionAsync(userSession, behaviorAnalysis);
            });

            // 更新执行结果
            var executionResult = GetExecutionResult();
            executionResult.CompletedSearches = searchResults.Count;
            executionResult.AverageResponseTime = searchResults.Count > 0 ? searchResults.Average(r => r.ResponseTime.TotalMilliseconds) : 0;
            executionResult.SuccessRate = searchResults.Count > 0 ? searchResults.Count(r => r.IsSuccess) * 100.0 / searchResults.Count : 0;
            executionResult.ExtendedProperties["UserType"] = userType;
            executionResult.ExtendedProperties["TotalSearches"] = searchQueries.Length;
            executionResult.ExtendedProperties["BehaviorAnalysis"] = behaviorAnalysis;

            EndScenarioExecution();
        }
        catch (Exception ex)
        {
            LogScenarioExecutionFailure(ex);
            throw;
        }
        finally
        {
            await CleanupUserSessionAsync();
        }
    }

    /// <summary>
    /// 初始化用户会话
    /// </summary>
    private async Task<UserSession> InitializeUserSessionAsync(string userType)
    {
        // 初始化浏览器组件
        await _browserFixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _browserFixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_browserFixture, _logger);
        
        // 初始化API组件
        _apiTest = new UserJourneyApiClient(_apiFixture.ApiClient, _apiFixture.Configuration, _apiFixture.Logger);
        
        var userSession = new UserSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UserType = userType,
            StartTime = DateTime.UtcNow,
            BrowserContext = _isolatedContext,
            Page = _isolatedPage
        };
        
        _logger.LogInformation($"[{ScenarioName}] 用户会话初始化完成 - 会话ID: {userSession.SessionId}, 用户类型: {userType}");
        
        return userSession;
    }

    /// <summary>
    /// 执行首次访问流程
    /// </summary>
    private async Task ExecuteFirstVisitFlowAsync(UserSession userSession)
    {
        var visitStart = DateTime.UtcNow;
        
        try
        {
            // 导航到首页
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            
            // 记录首次访问信息
            userSession.FirstVisitTime = DateTime.UtcNow;
            userSession.FirstVisitDuration = DateTime.UtcNow - visitStart;
            userSession.LandingPageUrl = _isolatedPage!.Url;
            
            // 模拟新用户的探索行为
            if (userSession.UserType == "新用户")
            {
                // 新用户可能会查看页面元素
                var isSearchBoxAvailable = await _homePage.IsSearchBoxAvailableAsync();
                var placeholder = await _homePage.GetSearchBoxPlaceholderAsync();
                
                userSession.ExplorationActions.Add($"检查搜索框可用性: {isSearchBoxAvailable}");
                userSession.ExplorationActions.Add($"查看占位符文本: {placeholder}");
                
                // 新用户可能会停留更长时间
                await Task.Delay(2000);
            }
            else
            {
                // 老用户直接进入搜索
                await Task.Delay(500);
            }
            
            _logger.LogInformation($"[{ScenarioName}] 首次访问完成 - 用户类型: {userSession.UserType}, 耗时: {userSession.FirstVisitDuration.TotalMilliseconds:F2}ms");
        }
        catch (Exception ex)
        {
            userSession.Errors.Add($"首次访问失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 执行搜索旅程流程
    /// </summary>
    private async Task<List<SearchJourneyResult>> ExecuteSearchJourneyFlowAsync(
        UserSession userSession, 
        string[] searchQueries, 
        bool includeAPIValidation, 
        bool capturePerformanceMetrics)
    {
        var searchResults = new List<SearchJourneyResult>();
        
        for (int i = 0; i < searchQueries.Length; i++)
        {
            var query = searchQueries[i];
            var searchStart = DateTime.UtcNow;
            
            _logger.LogInformation($"[{ScenarioName}] 执行搜索 {i + 1}/{searchQueries.Length}: {query}");
            
            var searchResult = new SearchJourneyResult
            {
                SearchIndex = i + 1,
                SearchQuery = query,
                StartTime = searchStart
            };
            
            try
            {
                // 如果不是第一次搜索，需要返回首页
                if (i > 0)
                {
                    await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
                    await _homePage.WaitForLoadAsync();
                    
                    // 模拟用户在搜索间的停顿
                    var pauseTime = userSession.UserType == "新用户" ? 3000 : 1000;
                    await Task.Delay(pauseTime);
                }
                
                // 执行UI搜索
                await _searchFlow!.ExecuteSearchWithValidationAsync(query, 1);
                
                // 收集UI结果
                searchResult.UIResultCount = await _homePage!.GetSearchResultCountAsync();
                searchResult.UIResults = await _homePage.GetSearchResultsAsync();
                searchResult.ResultPageUrl = _isolatedPage!.Url;
                searchResult.UISuccess = searchResult.UIResultCount > 0;
                
                // API验证（如果需要）
                if (includeAPIValidation)
                {
                    var apiRequest = ((UserJourneyApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
                    {
                        ["wd"] = query,
                        ["ie"] = "utf-8"
                    });
                    
                    var apiResponse = await ((UserJourneyApiClient)_apiTest).ExecuteApiTestAsync<string>(apiRequest, $"旅程API搜索_{i + 1}");
                    
                    searchResult.APISuccess = apiResponse.IsSuccess;
                    searchResult.APIStatusCode = apiResponse.StatusCode;
                    searchResult.APIResponseTime = apiResponse.ResponseTime;
                    searchResult.APIContentLength = apiResponse.RawContent?.Length ?? 0;
                }
                
                // 性能指标（如果需要）
                if (capturePerformanceMetrics)
                {
                    searchResult.PerformanceMetrics = new Dictionary<string, object>
                    {
                        ["UIResultCount"] = searchResult.UIResultCount,
                        ["APIResponseTime"] = searchResult.APIResponseTime.TotalMilliseconds,
                        ["APIContentLength"] = searchResult.APIContentLength,
                        ["SearchIndex"] = searchResult.SearchIndex
                    };
                }
                
                searchResult.IsSuccess = searchResult.UISuccess && (!includeAPIValidation || searchResult.APISuccess);
                searchResult.EndTime = DateTime.UtcNow;
                searchResult.ResponseTime = searchResult.EndTime - searchStart;
                
                userSession.SearchHistory.Add(searchResult);
                
                _logger.LogInformation($"[{ScenarioName}] 搜索 {i + 1} 完成 - UI结果: {searchResult.UIResultCount}, 成功: {searchResult.IsSuccess}, 耗时: {searchResult.ResponseTime.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                searchResult.IsSuccess = false;
                searchResult.ErrorMessage = ex.Message;
                searchResult.EndTime = DateTime.UtcNow;
                searchResult.ResponseTime = searchResult.EndTime - searchStart;
                
                userSession.Errors.Add($"搜索 {i + 1} 失败: {ex.Message}");
                _logger.LogError(ex, $"[{ScenarioName}] 搜索 {i + 1} 失败");
            }
            
            searchResults.Add(searchResult);
        }
        
        return searchResults;
    }

    /// <summary>
    /// 分析用户行为流程
    /// </summary>
    private async Task<UserBehaviorAnalysis> AnalyzeUserBehaviorFlowAsync(UserSession userSession, List<SearchJourneyResult> searchResults)
    {
        var analysis = new UserBehaviorAnalysis
        {
            SessionId = userSession.SessionId,
            UserType = userSession.UserType,
            AnalysisTime = DateTime.UtcNow
        };
        
        // 基本统计
        analysis.TotalSearches = searchResults.Count;
        analysis.SuccessfulSearches = searchResults.Count(r => r.IsSuccess);
        analysis.FailedSearches = analysis.TotalSearches - analysis.SuccessfulSearches;
        analysis.SuccessRate = analysis.TotalSearches > 0 ? (double)analysis.SuccessfulSearches / analysis.TotalSearches * 100 : 0;
        
        // 时间分析
        var successfulSearches = searchResults.Where(r => r.IsSuccess).ToList();
        if (successfulSearches.Any())
        {
            analysis.AverageSearchTime = successfulSearches.Average(r => r.ResponseTime.TotalMilliseconds);
            analysis.MinSearchTime = successfulSearches.Min(r => r.ResponseTime.TotalMilliseconds);
            analysis.MaxSearchTime = successfulSearches.Max(r => r.ResponseTime.TotalMilliseconds);
        }
        
        // 结果质量分析
        if (successfulSearches.Any())
        {
            analysis.AverageResultCount = successfulSearches.Average(r => r.UIResultCount);
            analysis.TotalResultsFound = successfulSearches.Sum(r => r.UIResultCount);
        }
        
        // 用户行为模式
        analysis.BehaviorPatterns = AnalyzeBehaviorPatterns(userSession, searchResults);
        
        // 性能趋势
        if (successfulSearches.Count > 1)
        {
            var firstHalf = successfulSearches.Take(successfulSearches.Count / 2).Average(r => r.ResponseTime.TotalMilliseconds);
            var secondHalf = successfulSearches.Skip(successfulSearches.Count / 2).Average(r => r.ResponseTime.TotalMilliseconds);
            
            analysis.PerformanceTrend = secondHalf > firstHalf ? "下降" : secondHalf < firstHalf ? "提升" : "稳定";
            analysis.PerformanceTrendPercentage = firstHalf > 0 ? Math.Abs((secondHalf - firstHalf) / firstHalf * 100) : 0;
        }
        else
        {
            analysis.PerformanceTrend = "数据不足";
        }
        
        // 会话总时长
        userSession.EndTime = DateTime.UtcNow;
        analysis.TotalSessionTime = (userSession.EndTime - userSession.StartTime).TotalSeconds;
        
        _logger.LogInformation($"[{ScenarioName}] 用户行为分析完成 - 成功率: {analysis.SuccessRate:F2}%, 平均搜索时间: {analysis.AverageSearchTime:F2}ms, 性能趋势: {analysis.PerformanceTrend}");
        
        await Task.CompletedTask; // 占位符，表示异步操作完成
        return analysis;
    }

    /// <summary>
    /// 分析行为模式
    /// </summary>
    private List<string> AnalyzeBehaviorPatterns(UserSession userSession, List<SearchJourneyResult> searchResults)
    {
        var patterns = new List<string>();
        
        // 搜索频率模式
        if (searchResults.Count >= 3)
        {
            var intervals = new List<double>();
            for (int i = 1; i < searchResults.Count; i++)
            {
                intervals.Add((searchResults[i].StartTime - searchResults[i - 1].StartTime).TotalSeconds);
            }
            
            var avgInterval = intervals.Average();
            if (avgInterval < 10)
            {
                patterns.Add("快速连续搜索模式");
            }
            else if (avgInterval > 30)
            {
                patterns.Add("深度思考搜索模式");
            }
            else
            {
                patterns.Add("正常搜索节奏模式");
            }
        }
        
        // 搜索成功率模式
        var successRate = searchResults.Count > 0 ? searchResults.Count(r => r.IsSuccess) * 100.0 / searchResults.Count : 0;
        if (successRate >= 90)
        {
            patterns.Add("高成功率用户");
        }
        else if (successRate >= 70)
        {
            patterns.Add("中等成功率用户");
        }
        else
        {
            patterns.Add("低成功率用户");
        }
        
        // 用户类型特定模式
        if (userSession.UserType == "新用户")
        {
            if (userSession.ExplorationActions.Count > 2)
            {
                patterns.Add("探索型新用户");
            }
            else
            {
                patterns.Add("目标明确型新用户");
            }
        }
        
        // 错误恢复模式
        if (userSession.Errors.Count > 0 && successRate > 50)
        {
            patterns.Add("错误恢复能力强");
        }
        
        return patterns;
    }

    /// <summary>
    /// 完成用户会话
    /// </summary>
    private async Task FinalizeUserSessionAsync(UserSession userSession, UserBehaviorAnalysis behaviorAnalysis)
    {
        userSession.EndTime = DateTime.UtcNow;
        userSession.TotalDuration = userSession.EndTime - userSession.StartTime;
        userSession.BehaviorAnalysis = behaviorAnalysis;
        
        // 记录会话摘要
        _logger.LogInformation($"[{ScenarioName}] 用户会话完成摘要:");
        _logger.LogInformation($"  会话ID: {userSession.SessionId}");
        _logger.LogInformation($"  用户类型: {userSession.UserType}");
        _logger.LogInformation($"  会话时长: {userSession.TotalDuration.TotalSeconds:F2}s");
        _logger.LogInformation($"  搜索次数: {userSession.SearchHistory.Count}");
        _logger.LogInformation($"  成功率: {behaviorAnalysis.SuccessRate:F2}%");
        _logger.LogInformation($"  平均搜索时间: {behaviorAnalysis.AverageSearchTime:F2}ms");
        _logger.LogInformation($"  行为模式: {string.Join(", ", behaviorAnalysis.BehaviorPatterns)}");
        
        if (userSession.Errors.Any())
        {
            _logger.LogWarning($"  会话错误数: {userSession.Errors.Count}");
        }
        
        await Task.CompletedTask; // 占位符，表示异步操作完成
    }

    /// <summary>
    /// 清理用户会话
    /// </summary>
    private async Task CleanupUserSessionAsync()
    {
        try
        {
            if (_isolatedContext != null)
            {
                await _isolatedContext.CloseAsync();
            }
            
            _apiTest?.Dispose();
            
            _logger.LogInformation($"[{ScenarioName}] 用户会话清理完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[{ScenarioName}] 用户会话清理时发生警告");
        }
    }
}

/// <summary>
/// 旅程专用API客户端
/// </summary>
internal class UserJourneyApiClient : BaseApiTest
{
    public UserJourneyApiClient(Core.Interfaces.IApiClient apiClient, Core.Configuration.TestConfiguration configuration, ILogger logger)
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
/// 用户会话
/// </summary>
public class UserSession
{
    public string SessionId { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime FirstVisitTime { get; set; }
    public TimeSpan FirstVisitDuration { get; set; }
    public string LandingPageUrl { get; set; } = string.Empty;
    public List<string> ExplorationActions { get; set; } = new();
    public List<SearchJourneyResult> SearchHistory { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public UserBehaviorAnalysis? BehaviorAnalysis { get; set; }
    public IBrowserContext? BrowserContext { get; set; }
    public IPage? Page { get; set; }
}

/// <summary>
/// 搜索旅程结果
/// </summary>
public class SearchJourneyResult
{
    public int SearchIndex { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    // UI结果
    public bool UISuccess { get; set; }
    public int UIResultCount { get; set; }
    public List<string> UIResults { get; set; } = new();
    public string ResultPageUrl { get; set; } = string.Empty;
    
    // API结果
    public bool APISuccess { get; set; }
    public int APIStatusCode { get; set; }
    public TimeSpan APIResponseTime { get; set; }
    public int APIContentLength { get; set; }
    
    // 性能指标
    public Dictionary<string, object>? PerformanceMetrics { get; set; }
}

/// <summary>
/// 用户行为分析
/// </summary>
public class UserBehaviorAnalysis
{
    public string SessionId { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public DateTime AnalysisTime { get; set; }
    
    // 基本统计
    public int TotalSearches { get; set; }
    public int SuccessfulSearches { get; set; }
    public int FailedSearches { get; set; }
    public double SuccessRate { get; set; }
    
    // 时间分析
    public double AverageSearchTime { get; set; }
    public double MinSearchTime { get; set; }
    public double MaxSearchTime { get; set; }
    public double TotalSessionTime { get; set; }
    
    // 结果质量
    public double AverageResultCount { get; set; }
    public int TotalResultsFound { get; set; }
    
    // 行为模式
    public List<string> BehaviorPatterns { get; set; } = new();
    
    // 性能趋势
    public string PerformanceTrend { get; set; } = string.Empty;
    public double PerformanceTrendPercentage { get; set; }
}