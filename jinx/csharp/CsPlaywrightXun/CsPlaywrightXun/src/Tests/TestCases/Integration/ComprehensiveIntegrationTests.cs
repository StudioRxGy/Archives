using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Models;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Services.Reporting;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;
using CsPlaywrightXun.src.playwright.Tests.Integration.Scenarios;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.Integration;

/// <summary>
/// 综合集成测试类
/// 验证完整的测试执行和报告生成流程
/// 需求: 1.1, 1.3, 5.4, 6.6, 6.7
/// </summary>
[Trait("Type", "Integration")]
[Trait("Category", "Comprehensive")]
[Trait("Priority", "Critical")]
public class ComprehensiveIntegrationTests : IClassFixture<BrowserFixture>, IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private readonly BrowserFixture _browserFixture;
    private readonly ApiTestFixture _apiFixture;
    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    
    // 测试组件
    private IBrowserContext? _isolatedContext;
    private IPage? _isolatedPage;
    private HomePage? _homePage;
    private SearchFlow? _searchFlow;
    private BaseApiTest? _apiTest;
    
    // 场景编排器
    private SearchScenario? _searchScenario;
    private UserJourneyScenario? _userJourneyScenario;
    
    // 测试执行跟踪
    private readonly List<TestResult> _testResults = new();
    private DateTime _testSuiteStartTime;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="browserFixture">浏览器固件</param>
    /// <param name="apiFixture">API固件</param>
    /// <param name="output">测试输出助手</param>
    public ComprehensiveIntegrationTests(BrowserFixture browserFixture, ApiTestFixture apiFixture, ITestOutputHelper output)
    {
        _browserFixture = browserFixture ?? throw new ArgumentNullException(nameof(browserFixture));
        _apiFixture = apiFixture ?? throw new ArgumentNullException(nameof(apiFixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<ComprehensiveIntegrationTests>();
    }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        _testSuiteStartTime = DateTime.UtcNow;
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在初始化综合集成测试环境...");
        
        // 初始化浏览器组件
        await _browserFixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _browserFixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_browserFixture, _logger);
        
        // 初始化API组件
        _apiTest = new ComprehensiveApiClient(_apiFixture.ApiClient, _apiFixture.Configuration, _apiFixture.Logger);
        
        // 初始化场景编排器
        _searchScenario = new SearchScenario(_browserFixture, _apiFixture, _logger);
        _userJourneyScenario = new UserJourneyScenario(_browserFixture, _apiFixture, _logger);
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 综合集成测试环境初始化完成");
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在清理综合集成测试环境...");
        
        // 生成最终测试报告
        await GenerateFinalTestReportAsync();
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
        }
        
        _apiTest?.Dispose();
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 综合集成测试环境清理完成");
    }

    #region 完整业务流程集成测试

    /// <summary>
    /// 完整业务流程集成测试
    /// 验证从用户访问到搜索完成的完整业务流程
    /// </summary>
    [Fact]
    [Trait("TestType", "CompleteBusinessFlow")]
    public async Task CompleteBusinessFlow_EndToEndIntegration_ShouldExecuteSuccessfully()
    {
        var testResult = CreateTestResult("完整业务流程集成测试");
        
        try
        {
            _output.WriteLine("=== 开始完整业务流程集成测试 ===");
            
            const string businessScenario = "企业用户搜索自动化测试解决方案";
            
            // 阶段1: 用户访问和初始化
            _output.WriteLine("阶段1: 用户访问和初始化");
            await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
            await _homePage.WaitForLoadAsync();
            
            var isPageLoaded = await _homePage.IsLoadedAsync();
            Assert.True(isPageLoaded, "首页应该成功加载");
            
            // 阶段2: 用户界面交互
            _output.WriteLine("阶段2: 用户界面交互");
            var searchBoxAvailable = await _homePage.IsSearchBoxAvailableAsync();
            var searchButtonAvailable = await _homePage.IsSearchButtonAvailableAsync();
            
            Assert.True(searchBoxAvailable, "搜索框应该可用");
            Assert.True(searchButtonAvailable, "搜索按钮应该可用");
            
            // 阶段3: 业务操作执行
            _output.WriteLine("阶段3: 业务操作执行");
            await _homePage.SearchAsync(businessScenario);
            await Task.Delay(2000); // 等待搜索结果加载
            
            var uiResultCount = await _homePage.GetSearchResultCountAsync();
            var uiResults = await _homePage.GetSearchResultsAsync();
            
            Assert.True(uiResultCount > 0, "UI搜索应该返回结果");
            Assert.NotEmpty(uiResults);
            
            // 阶段4: API验证和数据一致性
            _output.WriteLine("阶段4: API验证和数据一致性");
            var apiRequest = ((ComprehensiveApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = businessScenario,
                ["ie"] = "utf-8"
            });
            
            var apiResponse = await ((ComprehensiveApiClient)_apiTest).ExecuteApiTestAsync<string>(apiRequest, "业务流程API验证");
            
            Assert.True(apiResponse.IsSuccess, "API搜索应该成功");
            Assert.Contains("自动化", apiResponse.RawContent, StringComparison.OrdinalIgnoreCase);
            
            // 阶段5: 性能和质量验证
            _output.WriteLine("阶段5: 性能和质量验证");
            Assert.True(apiResponse.ResponseTime < TimeSpan.FromSeconds(10), 
                $"API响应时间应该合理: {apiResponse.ResponseTime.TotalMilliseconds:F2}ms");
            
            // 阶段6: 业务结果验证
            _output.WriteLine("阶段6: 业务结果验证");
            var businessRelevantResults = uiResults.Count(r => 
                r.Contains("自动化", StringComparison.OrdinalIgnoreCase) || 
                r.Contains("测试", StringComparison.OrdinalIgnoreCase));
            
            Assert.True(businessRelevantResults > 0, "搜索结果应该与业务场景相关");
            
            // 记录业务流程指标
            var businessMetrics = new Dictionary<string, object>
            {
                ["UIResultCount"] = uiResultCount,
                ["APIResponseTime"] = apiResponse.ResponseTime.TotalMilliseconds,
                ["BusinessRelevantResults"] = businessRelevantResults,
                ["APIContentLength"] = apiResponse.RawContent.Length,
                ["SearchQuery"] = businessScenario
            };
            
            testResult.TestData = businessMetrics;
            testResult.Status = TestStatus.Passed;
            
            _output.WriteLine($"完整业务流程测试成功:");
            _output.WriteLine($"  UI结果数: {uiResultCount}");
            _output.WriteLine($"  API响应时间: {apiResponse.ResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  业务相关结果: {businessRelevantResults}");
            _output.WriteLine($"  API内容长度: {apiResponse.RawContent.Length}");
        }
        catch (Exception ex)
        {
            testResult.Status = TestStatus.Failed;
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            
            // 截图记录失败状态
            if (_isolatedPage != null)
            {
                var screenshot = await _browserFixture.TakeScreenshotAsync(_isolatedPage, "complete_business_flow_failure");
                testResult.Screenshots.Add($"complete_business_flow_failure_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            
            throw;
        }
        finally
        {
            testResult.EndTime = DateTime.UtcNow;
            testResult.Duration = testResult.EndTime - testResult.StartTime;
            _testResults.Add(testResult);
        }
    }

    /// <summary>
    /// 多场景编排集成测试
    /// 验证多个场景的协调执行
    /// </summary>
    [Fact]
    [Trait("TestType", "MultiScenarioOrchestration")]
    public async Task MultiScenarioOrchestration_CoordinatedExecution_ShouldHandleComplexWorkflows()
    {
        var testResult = CreateTestResult("多场景编排集成测试");
        
        try
        {
            _output.WriteLine("=== 开始多场景编排集成测试 ===");
            
            // 场景1: 搜索场景
            _output.WriteLine("执行场景1: 搜索场景编排");
            var searchScenarioParams = new Dictionary<string, object>
            {
                ["searchQuery"] = "多场景编排测试",
                ["validateUI"] = true,
                ["validateAPI"] = true,
                ["performanceCheck"] = true,
                ["expectedMinResults"] = 2
            };
            
            await _searchScenario!.ExecuteAsync(searchScenarioParams);
            var searchResult = _searchScenario.GetExecutionResult();
            
            Assert.True(searchResult.IsSuccess, $"搜索场景应该成功: {searchResult.ErrorMessage}");
            
            // 场景2: 用户旅程场景
            _output.WriteLine("执行场景2: 用户旅程场景编排");
            var journeyScenarioParams = new Dictionary<string, object>
            {
                ["userType"] = "经验用户",
                ["searchQueries"] = new[] { "第一次搜索", "第二次搜索" },
                ["includeAPIValidation"] = true,
                ["capturePerformanceMetrics"] = true
            };
            
            await _userJourneyScenario!.ExecuteAsync(journeyScenarioParams);
            var journeyResult = _userJourneyScenario.GetExecutionResult();
            
            Assert.True(journeyResult.IsSuccess, $"用户旅程场景应该成功: {journeyResult.ErrorMessage}");
            
            // 场景协调验证
            _output.WriteLine("验证场景协调结果");
            
            // 验证搜索场景结果
            Assert.True(searchResult.UIValidationPassed, "搜索场景UI验证应该通过");
            Assert.True(searchResult.APIValidationPassed, "搜索场景API验证应该通过");
            Assert.True(searchResult.PerformanceCheckPassed, "搜索场景性能检查应该通过");
            
            // 验证用户旅程结果
            Assert.True(journeyResult.CompletedSearches >= 2, "用户旅程应该完成多次搜索");
            Assert.True(journeyResult.SuccessRate >= 80, $"用户旅程成功率应该足够高: {journeyResult.SuccessRate:F2}%");
            
            // 记录多场景指标
            var multiScenarioMetrics = new Dictionary<string, object>
            {
                ["SearchScenarioSteps"] = searchResult.ExecutedSteps.Count,
                ["SearchScenarioDuration"] = searchResult.TotalDuration.TotalMilliseconds,
                ["JourneyScenarioSearches"] = journeyResult.CompletedSearches,
                ["JourneyScenarioDuration"] = journeyResult.TotalDuration.TotalMilliseconds,
                ["JourneySuccessRate"] = journeyResult.SuccessRate,
                ["JourneyAverageResponseTime"] = journeyResult.AverageResponseTime,
                ["TotalScenariosExecuted"] = 2
            };
            
            testResult.TestData = multiScenarioMetrics;
            testResult.Status = TestStatus.Passed;
            
            _output.WriteLine($"多场景编排测试成功:");
            _output.WriteLine($"  搜索场景步骤数: {searchResult.ExecutedSteps.Count}");
            _output.WriteLine($"  搜索场景耗时: {searchResult.TotalDuration.TotalSeconds:F2}s");
            _output.WriteLine($"  旅程场景搜索数: {journeyResult.CompletedSearches}");
            _output.WriteLine($"  旅程场景耗时: {journeyResult.TotalDuration.TotalSeconds:F2}s");
            _output.WriteLine($"  旅程成功率: {journeyResult.SuccessRate:F2}%");
        }
        catch (Exception ex)
        {
            testResult.Status = TestStatus.Failed;
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            testResult.EndTime = DateTime.UtcNow;
            testResult.Duration = testResult.EndTime - testResult.StartTime;
            _testResults.Add(testResult);
        }
    }

    /// <summary>
    /// 端到端性能集成测试
    /// 验证整个系统在负载下的性能表现
    /// </summary>
    [Fact]
    [Trait("TestType", "EndToEndPerformance")]
    public async Task EndToEndPerformance_SystemLoadTesting_ShouldMaintainPerformanceStandards()
    {
        var testResult = CreateTestResult("端到端性能集成测试");
        
        try
        {
            _output.WriteLine("=== 开始端到端性能集成测试 ===");
            
            const int performanceIterations = 5;
            const double maxAverageResponseTime = 8000; // 8秒
            const double minSuccessRate = 90; // 90%
            
            var performanceResults = new List<PerformanceTestIteration>();
            
            for (int i = 0; i < performanceIterations; i++)
            {
                _output.WriteLine($"执行性能测试迭代 {i + 1}/{performanceIterations}");
                
                var iteration = new PerformanceTestIteration
                {
                    IterationNumber = i + 1,
                    StartTime = DateTime.UtcNow
                };
                
                try
                {
                    // UI性能测试
                    var uiStart = DateTime.UtcNow;
                    await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
                    await _homePage.WaitForLoadAsync();
                    await _homePage.SearchAsync($"性能测试迭代{i + 1}");
                    await Task.Delay(1000);
                    var uiResultCount = await _homePage.GetSearchResultCountAsync();
                    iteration.UIResponseTime = DateTime.UtcNow - uiStart;
                    iteration.UISuccess = uiResultCount > 0;
                    iteration.UIResultCount = uiResultCount;
                    
                    // API性能测试
                    var apiRequest = ((ComprehensiveApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
                    {
                        ["wd"] = $"性能测试迭代{i + 1}",
                        ["ie"] = "utf-8"
                    });
                    
                    var apiResponse = await ((ComprehensiveApiClient)_apiTest).ExecuteApiTestAsync<string>(apiRequest, $"性能测试API_{i + 1}");
                    iteration.APIResponseTime = apiResponse.ResponseTime;
                    iteration.APISuccess = apiResponse.IsSuccess;
                    iteration.APIStatusCode = apiResponse.StatusCode;
                    
                    // 综合性能指标
                    iteration.TotalResponseTime = iteration.UIResponseTime + iteration.APIResponseTime;
                    iteration.OverallSuccess = iteration.UISuccess && iteration.APISuccess;
                    
                    _output.WriteLine($"  迭代 {i + 1}: UI={iteration.UIResponseTime.TotalMilliseconds:F2}ms, " +
                                     $"API={iteration.APIResponseTime.TotalMilliseconds:F2}ms, " +
                                     $"总计={iteration.TotalResponseTime.TotalMilliseconds:F2}ms, " +
                                     $"成功={iteration.OverallSuccess}");
                }
                catch (Exception ex)
                {
                    iteration.OverallSuccess = false;
                    iteration.ErrorMessage = ex.Message;
                    _output.WriteLine($"  迭代 {i + 1} 失败: {ex.Message}");
                }
                finally
                {
                    iteration.EndTime = DateTime.UtcNow;
                    performanceResults.Add(iteration);
                }
                
                // 迭代间隔
                if (i < performanceIterations - 1)
                {
                    await Task.Delay(2000);
                }
            }
            
            // 性能分析
            var successfulIterations = performanceResults.Where(r => r.OverallSuccess).ToList();
            var successRate = (double)successfulIterations.Count / performanceResults.Count * 100;
            
            var avgUITime = successfulIterations.Any() ? successfulIterations.Average(r => r.UIResponseTime.TotalMilliseconds) : 0;
            var avgAPITime = successfulIterations.Any() ? successfulIterations.Average(r => r.APIResponseTime.TotalMilliseconds) : 0;
            var avgTotalTime = successfulIterations.Any() ? successfulIterations.Average(r => r.TotalResponseTime.TotalMilliseconds) : 0;
            
            // 性能验证
            Assert.True(successRate >= minSuccessRate, $"成功率不足: {successRate:F2}% < {minSuccessRate}%");
            Assert.True(avgTotalTime <= maxAverageResponseTime, $"平均响应时间过长: {avgTotalTime:F2}ms > {maxAverageResponseTime}ms");
            
            // 记录性能指标
            var performanceMetrics = new Dictionary<string, object>
            {
                ["TotalIterations"] = performanceIterations,
                ["SuccessfulIterations"] = successfulIterations.Count,
                ["SuccessRate"] = successRate,
                ["AverageUITime"] = avgUITime,
                ["AverageAPITime"] = avgAPITime,
                ["AverageTotalTime"] = avgTotalTime,
                ["MaxAllowedResponseTime"] = maxAverageResponseTime,
                ["MinRequiredSuccessRate"] = minSuccessRate
            };
            
            testResult.TestData = performanceMetrics;
            testResult.Status = TestStatus.Passed;
            
            _output.WriteLine($"端到端性能测试完成:");
            _output.WriteLine($"  总迭代数: {performanceIterations}");
            _output.WriteLine($"  成功迭代数: {successfulIterations.Count}");
            _output.WriteLine($"  成功率: {successRate:F2}%");
            _output.WriteLine($"  平均UI时间: {avgUITime:F2}ms");
            _output.WriteLine($"  平均API时间: {avgAPITime:F2}ms");
            _output.WriteLine($"  平均总时间: {avgTotalTime:F2}ms");
        }
        catch (Exception ex)
        {
            testResult.Status = TestStatus.Failed;
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            testResult.EndTime = DateTime.UtcNow;
            testResult.Duration = testResult.EndTime - testResult.StartTime;
            _testResults.Add(testResult);
        }
    }

    #endregion

    #region 报告生成和验证

    /// <summary>
    /// 测试报告生成集成测试
    /// 验证完整的测试执行和报告生成流程
    /// </summary>
    [Fact]
    [Trait("TestType", "ReportGeneration")]
    public async Task TestReportGeneration_CompleteReportingFlow_ShouldGenerateComprehensiveReports()
    {
        var testResult = CreateTestResult("测试报告生成集成测试");
        
        try
        {
            _output.WriteLine("=== 开始测试报告生成集成测试 ===");
            
            // 执行一系列测试操作以生成报告数据
            _output.WriteLine("步骤1: 执行测试操作生成报告数据");
            
            // 模拟多个测试场景
            var reportTestScenarios = new[]
            {
                "报告生成测试场景1",
                "报告生成测试场景2",
                "报告生成测试场景3"
            };
            
            var scenarioResults = new List<Dictionary<string, object>>();
            
            foreach (var scenario in reportTestScenarios)
            {
                var scenarioStart = DateTime.UtcNow;
                
                try
                {
                    // UI测试
                    await _homePage!.NavigateAsync(_browserFixture.Configuration.Environment.BaseUrl);
                    await _homePage.WaitForLoadAsync();
                    await _homePage.SearchAsync(scenario);
                    await Task.Delay(1000);
                    var uiResults = await _homePage.GetSearchResultCountAsync();
                    
                    // API测试
                    var apiRequest = ((ComprehensiveApiClient)_apiTest!).CreateGetRequest("/s", new Dictionary<string, string>
                    {
                        ["wd"] = scenario,
                        ["ie"] = "utf-8"
                    });
                    var apiResponse = await ((ComprehensiveApiClient)_apiTest).ExecuteApiTestAsync<string>(apiRequest, $"报告测试_{scenario}");
                    
                    var scenarioResult = new Dictionary<string, object>
                    {
                        ["ScenarioName"] = scenario,
                        ["UIResultCount"] = uiResults,
                        ["APISuccess"] = apiResponse.IsSuccess,
                        ["APIResponseTime"] = apiResponse.ResponseTime.TotalMilliseconds,
                        ["Duration"] = (DateTime.UtcNow - scenarioStart).TotalMilliseconds,
                        ["Success"] = uiResults > 0 && apiResponse.IsSuccess
                    };
                    
                    scenarioResults.Add(scenarioResult);
                    
                    _output.WriteLine($"  场景 '{scenario}' 完成: UI结果={uiResults}, API成功={apiResponse.IsSuccess}");
                }
                catch (Exception ex)
                {
                    var scenarioResult = new Dictionary<string, object>
                    {
                        ["ScenarioName"] = scenario,
                        ["Success"] = false,
                        ["Error"] = ex.Message,
                        ["Duration"] = (DateTime.UtcNow - scenarioStart).TotalMilliseconds
                    };
                    
                    scenarioResults.Add(scenarioResult);
                    _output.WriteLine($"  场景 '{scenario}' 失败: {ex.Message}");
                }
            }
            
            // 步骤2: 生成测试报告
            _output.WriteLine("步骤2: 生成综合测试报告");
            
            var testReport = new TestReport
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                Environment = _browserFixture.Configuration.Environment.Name,
                Summary = new TestSummary
                {
                    TotalTests = scenarioResults.Count,
                    PassedTests = scenarioResults.Count(r => r.ContainsKey("Success") && Convert.ToBoolean(r["Success"])),
                    FailedTests = scenarioResults.Count(r => !r.ContainsKey("Success") || !Convert.ToBoolean(r["Success"])),
                    SkippedTests = 0,
                    TotalDuration = TimeSpan.FromMilliseconds(scenarioResults.Sum(r => r.ContainsKey("Duration") ? Convert.ToDouble(r["Duration"]) : 0))
                },
                Results = scenarioResults.Select(r => new TestResult
                {
                    TestName = r.ContainsKey("ScenarioName") ? r["ScenarioName"].ToString()! : "Unknown",
                    Status = r.ContainsKey("Success") && Convert.ToBoolean(r["Success"]) ? TestStatus.Passed : TestStatus.Failed,
                    StartTime = DateTime.UtcNow.AddMilliseconds(r.ContainsKey("Duration") ? -Convert.ToDouble(r["Duration"]) : 0),
                    EndTime = DateTime.UtcNow,
                    Duration = TimeSpan.FromMilliseconds(r.ContainsKey("Duration") ? Convert.ToDouble(r["Duration"]) : 0),
                    ErrorMessage = r.ContainsKey("Error") ? r["Error"]?.ToString() : null,
                    TestData = r
                }).ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["TestSuite"] = "ComprehensiveIntegrationTests",
                    ["TestType"] = "ReportGeneration",
                    ["BrowserType"] = "Chromium",
                    ["Environment"] = _browserFixture.Configuration.Environment.Name
                }
            };
            
            // 步骤3: 生成HTML报告 - 简化版本
            _output.WriteLine("步骤3: 生成简化报告");
            
            var reportOutputPath = PathConfiguration.GetOutputPath("integration_test_reports", "TestReports");
            PathConfiguration.EnsureDirectoryExists(reportOutputPath);
            
            var reportFileName = $"integration_test_report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var htmlReportPath = Path.Combine(reportOutputPath, reportFileName);
            
            // 生成简单的HTML报告内容
            var htmlContent = GenerateSimpleHtmlReport(testReport);
            await File.WriteAllTextAsync(htmlReportPath, htmlContent);
            
            Assert.True(File.Exists(htmlReportPath), "HTML报告文件应该生成成功");
            
            var htmlFileContent = await File.ReadAllTextAsync(htmlReportPath);
            Assert.Contains("测试报告", htmlFileContent);
            Assert.Contains(testReport.ReportId, htmlFileContent);
            
            // 步骤4: 验证报告内容
            _output.WriteLine("步骤4: 验证报告内容完整性");
            
            Assert.True(testReport.Summary.TotalTests > 0, "报告应该包含测试数据");
            Assert.True(testReport.Results.Any(), "报告应该包含测试结果");
            Assert.NotNull(testReport.Metadata);
            Assert.True(testReport.Metadata.Any(), "报告应该包含元数据");
            
            // 记录报告生成指标
            var reportMetrics = new Dictionary<string, object>
            {
                ["ReportId"] = testReport.ReportId,
                ["TotalScenarios"] = scenarioResults.Count,
                ["SuccessfulScenarios"] = scenarioResults.Count(r => r.ContainsKey("Success") && Convert.ToBoolean(r["Success"])),
                ["ReportFilePath"] = htmlReportPath,
                ["ReportFileSize"] = new FileInfo(htmlReportPath).Length,
                ["ReportGenerationTime"] = DateTime.UtcNow
            };
            
            testResult.TestData = reportMetrics;
            testResult.Status = TestStatus.Passed;
            
            _output.WriteLine($"测试报告生成完成:");
            _output.WriteLine($"  报告ID: {testReport.ReportId}");
            _output.WriteLine($"  总场景数: {scenarioResults.Count}");
            _output.WriteLine($"  成功场景数: {scenarioResults.Count(r => r.ContainsKey("Success") && Convert.ToBoolean(r["Success"]))}");
            _output.WriteLine($"  报告文件: {htmlReportPath}");
            _output.WriteLine($"  文件大小: {new FileInfo(htmlReportPath).Length} 字节");
        }
        catch (Exception ex)
        {
            testResult.Status = TestStatus.Failed;
            testResult.ErrorMessage = ex.Message;
            testResult.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            testResult.EndTime = DateTime.UtcNow;
            testResult.Duration = testResult.EndTime - testResult.StartTime;
            _testResults.Add(testResult);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 生成简单的HTML报告
    /// </summary>
    private string GenerateSimpleHtmlReport(TestReport testReport)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>测试报告 - {testReport.ReportId}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 10px; border-radius: 5px; }}
        .summary {{ margin: 20px 0; }}
        .test-result {{ margin: 10px 0; padding: 10px; border: 1px solid #ddd; border-radius: 3px; }}
        .passed {{ background-color: #d4edda; }}
        .failed {{ background-color: #f8d7da; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>测试报告</h1>
        <p>报告ID: {testReport.ReportId}</p>
        <p>生成时间: {testReport.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>
        <p>环境: {testReport.Environment}</p>
    </div>
    
    <div class='summary'>
        <h2>测试摘要</h2>
        <p>总测试数: {testReport.Summary.TotalTests}</p>
        <p>通过: {testReport.Summary.PassedTests}</p>
        <p>失败: {testReport.Summary.FailedTests}</p>
        <p>跳过: {testReport.Summary.SkippedTests}</p>
        <p>通过率: {testReport.Summary.PassRate:F2}%</p>
        <p>总耗时: {testReport.Summary.TotalDuration.TotalSeconds:F2}s</p>
    </div>
    
    <div class='results'>
        <h2>测试结果</h2>";

        foreach (var result in testReport.Results)
        {
            var cssClass = result.Status == TestStatus.Passed ? "passed" : "failed";
            html += $@"
        <div class='test-result {cssClass}'>
            <h3>{result.TestName}</h3>
            <p>状态: {result.Status}</p>
            <p>耗时: {result.Duration.TotalMilliseconds:F2}ms</p>";
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                html += $"<p>错误: {result.ErrorMessage}</p>";
            }
            
            html += "</div>";
        }

        html += @"
    </div>
</body>
</html>";

        return html;
    }

    /// <summary>
    /// 创建测试结果对象
    /// </summary>
    private TestResult CreateTestResult(string testName)
    {
        return new TestResult
        {
            TestName = testName,
            StartTime = DateTime.UtcNow,
            Status = TestStatus.Inconclusive,
            Screenshots = new List<string>(),
            TestData = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// 生成最终测试报告
    /// </summary>
    private async Task GenerateFinalTestReportAsync()
    {
        try
        {
            if (!_testResults.Any())
            {
                _output.WriteLine("没有测试结果需要生成报告");
                return;
            }
            
            _output.WriteLine("=== 生成最终综合测试报告 ===");
            
            var finalReport = new TestReport
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                Environment = _browserFixture.Configuration.Environment.Name,
                Summary = new TestSummary
                {
                    TotalTests = _testResults.Count,
                    PassedTests = _testResults.Count(r => r.Status == TestStatus.Passed),
                    FailedTests = _testResults.Count(r => r.Status == TestStatus.Failed),
                    SkippedTests = _testResults.Count(r => r.Status == TestStatus.Skipped),
                    TotalDuration = TimeSpan.FromMilliseconds(_testResults.Sum(r => r.Duration.TotalMilliseconds))
                },
                Results = _testResults,
                Metadata = new Dictionary<string, object>
                {
                    ["TestSuite"] = "ComprehensiveIntegrationTests",
                    ["TestSuiteStartTime"] = _testSuiteStartTime,
                    ["TestSuiteEndTime"] = DateTime.UtcNow,
                    ["TestSuiteDuration"] = (DateTime.UtcNow - _testSuiteStartTime).TotalSeconds,
                    ["BrowserType"] = "Chromium",
                    ["Environment"] = _browserFixture.Configuration.Environment.Name,
                    ["TestFramework"] = "xUnit + Playwright"
                }
            };
            
            var reportOutputPath = PathConfiguration.GetOutputPath("final_integration_reports", Path.Combine("TestReports", "Integration"));
            PathConfiguration.EnsureDirectoryExists(reportOutputPath);
            
            var reportFileName = $"final_integration_report_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            var htmlReportPath = Path.Combine(reportOutputPath, reportFileName);
            
            // 生成简单的HTML报告
            var htmlContent = GenerateSimpleHtmlReport(finalReport);
            await File.WriteAllTextAsync(htmlReportPath, htmlContent);
            
            _output.WriteLine($"最终测试报告已生成: {htmlReportPath}");
            _output.WriteLine($"测试摘要:");
            _output.WriteLine($"  总测试数: {finalReport.Summary.TotalTests}");
            _output.WriteLine($"  通过: {finalReport.Summary.PassedTests}");
            _output.WriteLine($"  失败: {finalReport.Summary.FailedTests}");
            _output.WriteLine($"  跳过: {finalReport.Summary.SkippedTests}");
            _output.WriteLine($"  通过率: {finalReport.Summary.PassRate:F2}%");
            _output.WriteLine($"  总耗时: {finalReport.Summary.TotalDuration.TotalSeconds:F2}s");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"生成最终测试报告时发生错误: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// 综合测试专用API客户端
/// </summary>
internal class ComprehensiveApiClient : BaseApiTest
{
    public ComprehensiveApiClient(Core.Interfaces.IApiClient apiClient, Core.Configuration.TestConfiguration configuration, ILogger logger)
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
/// 性能测试迭代结果
/// </summary>
public class PerformanceTestIteration
{
    public int IterationNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan UIResponseTime { get; set; }
    public TimeSpan APIResponseTime { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public bool UISuccess { get; set; }
    public bool APISuccess { get; set; }
    public bool OverallSuccess { get; set; }
    public int UIResultCount { get; set; }
    public int APIStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}