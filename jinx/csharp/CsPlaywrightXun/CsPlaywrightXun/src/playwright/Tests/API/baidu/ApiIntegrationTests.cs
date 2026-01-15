using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Tests.API.Models;

namespace CsPlaywrightXun.src.playwright.Tests.API.baidu;

/// <summary>
/// API 集成测试类
/// 演示端到端的 API 测试场景，包括完整的业务流程验证
/// </summary>
[APITest]
[Trait("Type", "Integration")]
[Trait("Category", "EndToEnd")]
public class ApiIntegrationTests : BaseApiTest, IClassFixture<ApiTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">API 测试固件</param>
    /// <param name="output">测试输出助手</param>
    public ApiIntegrationTests(ApiTestFixture fixture, ITestOutputHelper output)
        : base(fixture.ApiClient, fixture.Configuration, fixture.Logger)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        Logger.LogInformation("ApiIntegrationTests 初始化完成");
    }

    /// <summary>
    /// 完整搜索流程集成测试
    /// 模拟用户完整的搜索体验，从初始查询到结果获取
    /// </summary>
    [Fact]
    [Trait("Priority", "Critical")]
    [Trait("TestType", "E2E")]
    public async Task CompleteSearchWorkflow_EndToEnd_ShouldProvideConsistentExperience()
    {
        _output.WriteLine("=== 开始完整搜索流程集成测试 ===");

        var testSession = new TestSession
        {
            SessionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.UtcNow,
            TestName = "完整搜索流程"
        };

        try
        {
            // 第一步：首页访问验证
            _output.WriteLine("步骤 1: 验证首页可访问性");
            var homePageRequest = CreateGetRequest("/");
            var homePageResponse = await ExecuteApiTestAsync<string>(homePageRequest, "首页访问");
            
            Assert.True(homePageResponse.IsSuccess, "首页应该可以正常访问");
            testSession.AddStep("首页访问", homePageResponse.IsSuccess, homePageResponse.ResponseTime);
            _output.WriteLine($"首页访问成功 - 响应时间: {homePageResponse.ResponseTime.TotalMilliseconds:F2}ms");

            // 第二步：搜索建议获取（模拟自动完成）
            _output.WriteLine("步骤 2: 获取搜索建议");
            var suggestionRequest = CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = "API",
                ["ie"] = "utf-8"
            });
            
            var suggestionResponse = await ExecuteApiTestAsync<string>(suggestionRequest, "搜索建议");
            Assert.True(suggestionResponse.IsSuccess, "搜索建议应该正常返回");
            testSession.AddStep("搜索建议", suggestionResponse.IsSuccess, suggestionResponse.ResponseTime);
            _output.WriteLine($"搜索建议获取成功 - 响应时间: {suggestionResponse.ResponseTime.TotalMilliseconds:F2}ms");

            // 第三步：执行主要搜索
            _output.WriteLine("步骤 3: 执行主要搜索");
            var mainSearchRequest = CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = "API自动化测试框架",
                ["ie"] = "utf-8"
            });

            var mainSearchResponse = await ExecuteApiTestAsync<string>(mainSearchRequest, "主要搜索");
            Assert.True(mainSearchResponse.IsSuccess, "主要搜索应该成功");
            Assert.Contains("API", mainSearchResponse.RawContent, StringComparison.OrdinalIgnoreCase);
            testSession.AddStep("主要搜索", mainSearchResponse.IsSuccess, mainSearchResponse.ResponseTime);
            _output.WriteLine($"主要搜索完成 - 响应时间: {mainSearchResponse.ResponseTime.TotalMilliseconds:F2}ms");

            // 第四步：相关搜索验证
            _output.WriteLine("步骤 4: 验证相关搜索");
            var relatedSearches = new[]
            {
                "API测试工具",
                "自动化测试",
                "测试框架对比"
            };

            var relatedResults = new List<(string Query, ApiResponse<string> Response)>();
            foreach (var query in relatedSearches)
            {
                var relatedRequest = CreateGetRequest("/s", new Dictionary<string, string>
                {
                    ["wd"] = query,
                    ["ie"] = "utf-8"
                });

                var relatedResponse = await ExecuteApiTestAsync<string>(relatedRequest, $"相关搜索_{query}");
                relatedResults.Add((query, relatedResponse));
                testSession.AddStep($"相关搜索_{query}", relatedResponse.IsSuccess, relatedResponse.ResponseTime);
            }

            // 验证所有相关搜索都成功
            Assert.All(relatedResults, result =>
            {
                Assert.True(result.Response.IsSuccess, $"相关搜索失败: {result.Query}");
            });

            var avgRelatedResponseTime = relatedResults.Average(r => r.Response.ResponseTime.TotalMilliseconds);
            _output.WriteLine($"相关搜索完成 - 平均响应时间: {avgRelatedResponseTime:F2}ms");

            // 第五步：搜索结果一致性验证
            _output.WriteLine("步骤 5: 验证搜索结果一致性");
            var consistencyRequest = CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = "API自动化测试框架",
                ["ie"] = "utf-8"
            });

            var consistencyResponse = await ExecuteApiTestAsync<string>(consistencyRequest, "一致性验证");
            Assert.True(consistencyResponse.IsSuccess, "一致性验证搜索应该成功");
            
            // 验证两次相同搜索的结果应该基本一致
            Assert.Equal(mainSearchResponse.StatusCode, consistencyResponse.StatusCode);
            testSession.AddStep("一致性验证", consistencyResponse.IsSuccess, consistencyResponse.ResponseTime);
            _output.WriteLine($"一致性验证完成 - 响应时间: {consistencyResponse.ResponseTime.TotalMilliseconds:F2}ms");

            testSession.EndTime = DateTime.UtcNow;
            testSession.IsSuccess = true;

            // 输出完整的测试会话报告
            _output.WriteLine("\n=== 完整搜索流程测试报告 ===");
            _output.WriteLine($"会话ID: {testSession.SessionId}");
            _output.WriteLine($"测试名称: {testSession.TestName}");
            _output.WriteLine($"开始时间: {testSession.StartTime:yyyy-MM-dd HH:mm:ss}");
            _output.WriteLine($"结束时间: {testSession.EndTime:yyyy-MM-dd HH:mm:ss}");
            _output.WriteLine($"总耗时: {testSession.TotalDuration.TotalSeconds:F2}s");
            _output.WriteLine($"总步骤数: {testSession.Steps.Count}");
            _output.WriteLine($"成功步骤: {testSession.SuccessfulSteps}");
            _output.WriteLine($"失败步骤: {testSession.FailedSteps}");
            _output.WriteLine($"成功率: {testSession.SuccessRate:F2}%");
            _output.WriteLine($"平均步骤响应时间: {testSession.AverageResponseTime:F2}ms");

            _output.WriteLine("\n步骤详情:");
            foreach (var step in testSession.Steps)
            {
                _output.WriteLine($"  {step.StepName}: {(step.IsSuccess ? "成功" : "失败")} " +
                                $"({step.ResponseTime.TotalMilliseconds:F2}ms)");
            }

            Assert.True(testSession.SuccessRate >= 100, $"集成测试成功率不足: {testSession.SuccessRate:F2}%");
        }
        catch (Exception ex)
        {
            testSession.EndTime = DateTime.UtcNow;
            testSession.IsSuccess = false;
            testSession.ErrorMessage = ex.Message;

            _output.WriteLine($"\n=== 集成测试失败 ===");
            _output.WriteLine($"错误信息: {ex.Message}");
            _output.WriteLine($"已完成步骤: {testSession.Steps.Count}");
            
            throw;
        }
    }

    /// <summary>
    /// API 性能基准测试
    /// 建立 API 性能基准，用于后续性能回归测试
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Benchmark")]
    public async Task ApiPerformanceBenchmark_EstablishBaseline_ShouldMeetPerformanceTargets()
    {
        _output.WriteLine("=== 开始 API 性能基准测试 ===");

        var benchmarkScenarios = new[]
        {
            new BenchmarkScenario
            {
                Name = "简单查询",
                Query = "test",
                ExpectedMaxResponseTime = TimeSpan.FromSeconds(3),
                Iterations = 10
            },
            new BenchmarkScenario
            {
                Name = "复杂查询",
                Query = "API自动化测试框架 C# Playwright xUnit",
                ExpectedMaxResponseTime = TimeSpan.FromSeconds(5),
                Iterations = 10
            },
            new BenchmarkScenario
            {
                Name = "中文查询",
                Query = "中文搜索测试查询",
                ExpectedMaxResponseTime = TimeSpan.FromSeconds(4),
                Iterations = 10
            },
            new BenchmarkScenario
            {
                Name = "特殊字符查询",
                Query = "API+测试&框架|性能",
                ExpectedMaxResponseTime = TimeSpan.FromSeconds(4),
                Iterations = 10
            }
        };

        var benchmarkResults = new List<BenchmarkResult>();

        foreach (var scenario in benchmarkScenarios)
        {
            _output.WriteLine($"\n执行基准测试: {scenario.Name}");
            
            var responseTimes = new List<TimeSpan>();
            var successCount = 0;

            for (int i = 0; i < scenario.Iterations; i++)
            {
                var request = CreateGetRequest("/s", new Dictionary<string, string>
                {
                    ["wd"] = scenario.Query,
                    ["ie"] = "utf-8"
                });

                var response = await ExecuteApiTestAsync<string>(request, $"{scenario.Name}_迭代{i + 1}");
                
                responseTimes.Add(response.ResponseTime);
                if (response.IsSuccess)
                {
                    successCount++;
                }

                // 在迭代之间添加小延迟，避免过于频繁的请求
                await Task.Delay(100);
            }

            var result = new BenchmarkResult
            {
                ScenarioName = scenario.Name,
                Query = scenario.Query,
                Iterations = scenario.Iterations,
                SuccessCount = successCount,
                FailureCount = scenario.Iterations - successCount,
                SuccessRate = (double)successCount / scenario.Iterations * 100,
                AverageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds)),
                MinResponseTime = responseTimes.Min(),
                MaxResponseTime = responseTimes.Max(),
                MedianResponseTime = CalculateMedian(responseTimes),
                P95ResponseTime = CalculatePercentile(responseTimes, 95),
                P99ResponseTime = CalculatePercentile(responseTimes, 99),
                ExpectedMaxResponseTime = scenario.ExpectedMaxResponseTime
            };

            benchmarkResults.Add(result);

            // 输出单个场景结果
            _output.WriteLine($"  成功率: {result.SuccessRate:F2}%");
            _output.WriteLine($"  平均响应时间: {result.AverageResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  最小响应时间: {result.MinResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  最大响应时间: {result.MaxResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  中位数响应时间: {result.MedianResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  95百分位响应时间: {result.P95ResponseTime.TotalMilliseconds:F2}ms");
            _output.WriteLine($"  99百分位响应时间: {result.P99ResponseTime.TotalMilliseconds:F2}ms");

            // 验证性能目标
            Assert.True(result.SuccessRate >= 95, 
                $"场景 '{scenario.Name}' 成功率不足: {result.SuccessRate:F2}%");
            Assert.True(result.P95ResponseTime <= scenario.ExpectedMaxResponseTime,
                $"场景 '{scenario.Name}' 95百分位响应时间超标: {result.P95ResponseTime.TotalMilliseconds:F2}ms > {scenario.ExpectedMaxResponseTime.TotalMilliseconds}ms");
        }

        // 输出综合基准报告
        _output.WriteLine("\n=== API 性能基准报告 ===");
        var overallSuccessRate = benchmarkResults.Average(r => r.SuccessRate);
        var overallAvgResponseTime = TimeSpan.FromMilliseconds(
            benchmarkResults.Average(r => r.AverageResponseTime.TotalMilliseconds));

        _output.WriteLine($"总体成功率: {overallSuccessRate:F2}%");
        _output.WriteLine($"总体平均响应时间: {overallAvgResponseTime.TotalMilliseconds:F2}ms");

        // 识别性能最好和最差的场景
        var bestScenario = benchmarkResults.OrderBy(r => r.AverageResponseTime).First();
        var worstScenario = benchmarkResults.OrderByDescending(r => r.AverageResponseTime).First();

        _output.WriteLine($"性能最佳场景: {bestScenario.ScenarioName} ({bestScenario.AverageResponseTime.TotalMilliseconds:F2}ms)");
        _output.WriteLine($"性能最差场景: {worstScenario.ScenarioName} ({worstScenario.AverageResponseTime.TotalMilliseconds:F2}ms)");

        // 基准测试断言
        Assert.True(overallSuccessRate >= 95, $"总体成功率不足: {overallSuccessRate:F2}%");
        Assert.True(overallAvgResponseTime.TotalSeconds <= 10, 
            $"总体平均响应时间过长: {overallAvgResponseTime.TotalSeconds:F2}s");
    }

    /// <summary>
    /// API 可靠性测试
    /// 长时间运行测试，验证 API 的稳定性和可靠性
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("TestType", "Reliability")]
    public async Task ApiReliability_LongRunning_ShouldMaintainStability()
    {
        _output.WriteLine("=== 开始 API 可靠性测试 ===");

        const int testDurationMinutes = 2; // 2分钟的可靠性测试
        const int requestIntervalMs = 5000; // 每5秒一个请求
        
        var endTime = DateTime.UtcNow.AddMinutes(testDurationMinutes);
        var reliabilityResults = new List<ReliabilityTestResult>();
        var requestCount = 0;

        _output.WriteLine($"测试将运行 {testDurationMinutes} 分钟，每 {requestIntervalMs}ms 发送一个请求");

        while (DateTime.UtcNow < endTime)
        {
            requestCount++;
            var requestStart = DateTime.UtcNow;

            try
            {
                var request = CreateGetRequest("/s", new Dictionary<string, string>
                {
                    ["wd"] = $"可靠性测试_{requestCount}",
                    ["ie"] = "utf-8"
                });

                var response = await ExecuteApiTestAsync<string>(request, $"可靠性测试_{requestCount}");
                
                reliabilityResults.Add(new ReliabilityTestResult
                {
                    RequestNumber = requestCount,
                    Timestamp = requestStart,
                    IsSuccess = response.IsSuccess,
                    StatusCode = response.StatusCode,
                    ResponseTime = response.ResponseTime,
                    ErrorMessage = response.IsSuccess ? null : $"状态码: {response.StatusCode}"
                });

                if (requestCount % 10 == 0) // 每10个请求输出一次进度
                {
                    var currentSuccessRate = reliabilityResults.Count(r => r.IsSuccess) * 100.0 / reliabilityResults.Count;
                    _output.WriteLine($"进度: {requestCount} 请求完成，当前成功率: {currentSuccessRate:F2}%");
                }
            }
            catch (Exception ex)
            {
                reliabilityResults.Add(new ReliabilityTestResult
                {
                    RequestNumber = requestCount,
                    Timestamp = requestStart,
                    IsSuccess = false,
                    StatusCode = 0,
                    ResponseTime = TimeSpan.Zero,
                    ErrorMessage = ex.Message
                });
            }

            // 等待下一个请求间隔
            var nextRequestTime = requestStart.AddMilliseconds(requestIntervalMs);
            var waitTime = nextRequestTime - DateTime.UtcNow;
            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime);
            }
        }

        // 分析可靠性测试结果
        var totalRequests = reliabilityResults.Count;
        var successfulRequests = reliabilityResults.Count(r => r.IsSuccess);
        var failedRequests = totalRequests - successfulRequests;
        var successRate = (double)successfulRequests / totalRequests * 100;
        var averageResponseTime = reliabilityResults.Where(r => r.IsSuccess)
                                                  .Average(r => r.ResponseTime.TotalMilliseconds);

        // 检查连续失败
        var maxConsecutiveFailures = CalculateMaxConsecutiveFailures(reliabilityResults);
        
        // 检查响应时间趋势
        var responseTimeTrend = AnalyzeResponseTimeTrend(reliabilityResults);

        // 输出可靠性测试报告
        _output.WriteLine("\n=== API 可靠性测试报告 ===");
        _output.WriteLine($"测试持续时间: {testDurationMinutes} 分钟");
        _output.WriteLine($"总请求数: {totalRequests}");
        _output.WriteLine($"成功请求数: {successfulRequests}");
        _output.WriteLine($"失败请求数: {failedRequests}");
        _output.WriteLine($"成功率: {successRate:F2}%");
        _output.WriteLine($"平均响应时间: {averageResponseTime:F2}ms");
        _output.WriteLine($"最大连续失败次数: {maxConsecutiveFailures}");
        _output.WriteLine($"响应时间趋势: {responseTimeTrend}");

        if (failedRequests > 0)
        {
            _output.WriteLine("\n失败请求详情:");
            var failedResults = reliabilityResults.Where(r => !r.IsSuccess).Take(5);
            foreach (var failure in failedResults)
            {
                _output.WriteLine($"  请求 {failure.RequestNumber} ({failure.Timestamp:HH:mm:ss}): {failure.ErrorMessage}");
            }
        }

        // 可靠性断言
        Assert.True(successRate >= 95, $"可靠性测试成功率不足: {successRate:F2}%");
        Assert.True(maxConsecutiveFailures <= 3, $"连续失败次数过多: {maxConsecutiveFailures}");
        Assert.True(averageResponseTime <= 10000, $"平均响应时间过长: {averageResponseTime:F2}ms");
    }

    /// <summary>
    /// 计算中位数
    /// </summary>
    private TimeSpan CalculateMedian(List<TimeSpan> values)
    {
        var sorted = values.OrderBy(v => v.TotalMilliseconds).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
        {
            var mid1 = sorted[count / 2 - 1].TotalMilliseconds;
            var mid2 = sorted[count / 2].TotalMilliseconds;
            return TimeSpan.FromMilliseconds((mid1 + mid2) / 2);
        }
        else
        {
            return sorted[count / 2];
        }
    }

    /// <summary>
    /// 计算百分位数
    /// </summary>
    private TimeSpan CalculatePercentile(List<TimeSpan> values, double percentile)
    {
        var sorted = values.OrderBy(v => v.TotalMilliseconds).ToList();
        var index = (percentile / 100.0) * (sorted.Count - 1);
        
        if (index == Math.Floor(index))
        {
            return sorted[(int)index];
        }
        else
        {
            var lower = sorted[(int)Math.Floor(index)].TotalMilliseconds;
            var upper = sorted[(int)Math.Ceiling(index)].TotalMilliseconds;
            var interpolated = lower + (upper - lower) * (index - Math.Floor(index));
            return TimeSpan.FromMilliseconds(interpolated);
        }
    }

    /// <summary>
    /// 计算最大连续失败次数
    /// </summary>
    private int CalculateMaxConsecutiveFailures(List<ReliabilityTestResult> results)
    {
        int maxConsecutive = 0;
        int currentConsecutive = 0;

        foreach (var result in results)
        {
            if (!result.IsSuccess)
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }

    /// <summary>
    /// 分析响应时间趋势
    /// </summary>
    private string AnalyzeResponseTimeTrend(List<ReliabilityTestResult> results)
    {
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        if (successfulResults.Count < 10)
        {
            return "数据不足";
        }

        var firstHalf = successfulResults.Take(successfulResults.Count / 2)
                                       .Average(r => r.ResponseTime.TotalMilliseconds);
        var secondHalf = successfulResults.Skip(successfulResults.Count / 2)
                                        .Average(r => r.ResponseTime.TotalMilliseconds);

        var difference = secondHalf - firstHalf;
        var percentageChange = (difference / firstHalf) * 100;

        if (Math.Abs(percentageChange) < 5)
        {
            return "稳定";
        }
        else if (percentageChange > 0)
        {
            return $"上升 ({percentageChange:F1}%)";
        }
        else
        {
            return $"下降 ({Math.Abs(percentageChange):F1}%)";
        }
    }
}

/// <summary>
/// 测试会话
/// </summary>
public class TestSession
{
    public string SessionId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<TestStep> Steps { get; set; } = new();

    public TimeSpan TotalDuration => EndTime - StartTime;
    public int SuccessfulSteps => Steps.Count(s => s.IsSuccess);
    public int FailedSteps => Steps.Count(s => !s.IsSuccess);
    public double SuccessRate => Steps.Count > 0 ? (double)SuccessfulSteps / Steps.Count * 100 : 0;
    public double AverageResponseTime => Steps.Count > 0 ? Steps.Average(s => s.ResponseTime.TotalMilliseconds) : 0;

    public void AddStep(string stepName, bool isSuccess, TimeSpan responseTime)
    {
        Steps.Add(new TestStep
        {
            StepName = stepName,
            IsSuccess = isSuccess,
            ResponseTime = responseTime,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// 测试步骤
/// </summary>
public class TestStep
{
    public string StepName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 基准测试场景
/// </summary>
public class BenchmarkScenario
{
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public TimeSpan ExpectedMaxResponseTime { get; set; }
    public int Iterations { get; set; }
}

/// <summary>
/// 基准测试结果
/// </summary>
public class BenchmarkResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public TimeSpan ExpectedMaxResponseTime { get; set; }
}

/// <summary>
/// 可靠性测试结果
/// </summary>
public class ReliabilityTestResult
{
    public int RequestNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}