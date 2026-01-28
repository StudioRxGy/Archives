using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Tests.API.Models;
using System.Text.Json;

namespace CsPlaywrightXun.src.playwright.Tests.API.baidu;

/// <summary>
/// 高级 API 测试类
/// 演示复杂的 API 测试场景，包括链式调用、状态管理和高级验证
/// </summary>
[APITest]
[Trait("Type", "API")]
[Trait("Category", "Advanced")]
public class AdvancedApiTests : BaseApiTest, IClassFixture<ApiTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">API 测试固件</param>
    /// <param name="output">测试输出助手</param>
    public AdvancedApiTests(ApiTestFixture fixture, ITestOutputHelper output)
        : base(fixture.ApiClient, fixture.Configuration, fixture.Logger)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        Logger.LogInformation("AdvancedApiTests 初始化完成");
    }

    /// <summary>
    /// API 链式调用测试
    /// 演示如何进行多个相关的 API 调用并验证它们之间的关系
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("TestType", "Integration")]
    public async Task ApiChaining_MultipleRelatedCalls_ShouldMaintainConsistency()
    {
        _output.WriteLine("开始 API 链式调用测试");

        // 第一步：执行初始搜索
        var initialRequest = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "API链式测试",
            ["ie"] = "utf-8"
        });

        var initialResponse = await ExecuteApiTestAsync<string>(initialRequest, "初始搜索");
        Assert.True(initialResponse.IsSuccess, "初始搜索失败");
        
        _output.WriteLine($"初始搜索完成 - 状态码: {initialResponse.StatusCode}, " +
                         $"响应时间: {initialResponse.ResponseTime.TotalMilliseconds}ms");

        // 第二步：基于初始结果执行相关搜索
        var relatedRequest = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "API测试框架",
            ["ie"] = "utf-8"
        });

        var relatedResponse = await ExecuteApiTestAsync<string>(relatedRequest, "相关搜索");
        Assert.True(relatedResponse.IsSuccess, "相关搜索失败");
        
        _output.WriteLine($"相关搜索完成 - 状态码: {relatedResponse.StatusCode}, " +
                         $"响应时间: {relatedResponse.ResponseTime.TotalMilliseconds}ms");

        // 第三步：验证两次调用的一致性
        Assert.True(initialResponse.Headers.ContainsKey("Content-Type"), "初始响应缺少 Content-Type 头");
        Assert.True(relatedResponse.Headers.ContainsKey("Content-Type"), "相关响应缺少 Content-Type 头");
        
        // 验证响应格式一致性
        Assert.Equal(initialResponse.Headers["Content-Type"], relatedResponse.Headers["Content-Type"]);
        
        // 验证响应时间在合理范围内
        var timeDifference = Math.Abs(initialResponse.ResponseTime.TotalMilliseconds - 
                                    relatedResponse.ResponseTime.TotalMilliseconds);
        Assert.True(timeDifference < 10000, $"两次调用响应时间差异过大: {timeDifference}ms");

        _output.WriteLine("API 链式调用测试完成 - 所有验证通过");
    }

    /// <summary>
    /// API 批量操作测试
    /// 演示如何高效地执行大量 API 调用并进行批量验证
    /// </summary>
    [Theory]
    [InlineData(10, "小批量测试")]
    [InlineData(50, "中批量测试")]
    [InlineData(100, "大批量测试")]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Load")]
    public async Task ApiBatch_BulkOperations_ShouldHandleHighVolume(int batchSize, string testDescription)
    {
        _output.WriteLine($"开始 {testDescription} - 批量大小: {batchSize}");

        // 生成批量请求
        var requests = Enumerable.Range(1, batchSize)
            .Select(i => CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = $"批量测试{i}",
                ["ie"] = "utf-8"
            }))
            .ToList();

        var startTime = DateTime.UtcNow;
        var semaphore = new SemaphoreSlim(10, 10); // 限制并发数为 10

        // 执行批量请求
        var tasks = requests.Select(async (request, index) =>
        {
            await semaphore.WaitAsync();
            try
            {
                var response = await ExecuteApiTestAsync<string>(request, $"{testDescription}_{index + 1}");
                return new BatchResult
                {
                    Index = index + 1,
                    Response = response,
                    IsSuccess = response.IsSuccess,
                    ResponseTime = response.ResponseTime
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;
        var totalTime = endTime - startTime;

        // 分析结果
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);
        var averageResponseTime = results.Average(r => r.ResponseTime.TotalMilliseconds);
        var maxResponseTime = results.Max(r => r.ResponseTime.TotalMilliseconds);
        var minResponseTime = results.Min(r => r.ResponseTime.TotalMilliseconds);
        var successRate = (double)successCount / batchSize * 100;

        // 输出统计信息
        _output.WriteLine($"{testDescription} 完成:");
        _output.WriteLine($"  总耗时: {totalTime.TotalSeconds:F2}s");
        _output.WriteLine($"  成功请求: {successCount}/{batchSize}");
        _output.WriteLine($"  失败请求: {failureCount}");
        _output.WriteLine($"  成功率: {successRate:F2}%");
        _output.WriteLine($"  平均响应时间: {averageResponseTime:F2}ms");
        _output.WriteLine($"  最大响应时间: {maxResponseTime:F2}ms");
        _output.WriteLine($"  最小响应时间: {minResponseTime:F2}ms");
        _output.WriteLine($"  吞吐量: {batchSize / totalTime.TotalSeconds:F2} 请求/秒");

        // 断言
        Assert.True(successRate >= 95, $"成功率过低: {successRate:F2}%");
        Assert.True(averageResponseTime < 10000, $"平均响应时间过长: {averageResponseTime:F2}ms");
        
        // 检查是否有异常的响应时间
        var outliers = results.Where(r => r.ResponseTime.TotalMilliseconds > averageResponseTime * 3).ToList();
        if (outliers.Any())
        {
            _output.WriteLine($"发现 {outliers.Count} 个响应时间异常的请求:");
            foreach (var outlier in outliers.Take(5)) // 只显示前5个
            {
                _output.WriteLine($"  请求 {outlier.Index}: {outlier.ResponseTime.TotalMilliseconds:F2}ms");
            }
        }
    }

    /// <summary>
    /// API 状态码覆盖测试
    /// 验证 API 在各种情况下返回正确的状态码
    /// </summary>
    [Theory]
    [InlineData("正常查询", "playwright", 200)]
    [InlineData("空查询", "", 200)]
    [InlineData("特殊字符查询", "!@#$%^&*()", 200)]
    [InlineData("超长查询", null, 200)] // null 将被替换为超长字符串
    [Trait("Priority", "High")]
    [Trait("TestType", "Validation")]
    public async Task ApiStatusCodes_VariousScenarios_ShouldReturnCorrectStatusCodes(
        string scenario, string? query, int expectedStatusCode)
    {
        // 处理特殊情况
        if (query == null) // 超长查询
        {
            query = new string('测', 2000);
        }

        _output.WriteLine($"测试场景: {scenario}");
        _output.WriteLine($"查询参数: {(query.Length > 50 ? query[..50] + "..." : query)}");

        // 创建请求
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = query,
            ["ie"] = "utf-8"
        });

        // 执行请求
        var response = await ExecuteApiTestAsync<string>(request, scenario);

        // 验证状态码
        Assert.Equal(expectedStatusCode, response.StatusCode);
        
        // 验证响应基本属性
        Assert.NotNull(response.RawContent);
        Assert.True(response.ResponseTime > TimeSpan.Zero, "响应时间应该大于0");
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(30), "响应时间不应超过30秒");

        _output.WriteLine($"场景 '{scenario}' 测试通过 - 状态码: {response.StatusCode}, " +
                         $"响应时间: {response.ResponseTime.TotalMilliseconds:F2}ms");
    }

    /// <summary>
    /// API 响应内容深度验证测试
    /// 使用复杂的验证规则验证响应内容
    /// </summary>
    [Theory]
    [MemberData(nameof(GetValidationRuleData))]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Validation")]
    public async Task ApiContentValidation_ComplexRules_ShouldPassAllValidations(ApiValidationRuleData validationRule)
    {
        _output.WriteLine($"执行复杂验证测试: {validationRule.TestName}");
        _output.WriteLine($"描述: {validationRule.Description}");

        // 创建测试请求
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "复杂验证测试",
            ["ie"] = "utf-8"
        });

        // 执行请求
        var response = await ExecuteApiTestAsync<string>(request, validationRule.TestName);

        // 应用验证规则
        var validation = validationRule.ToApiValidation();
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation, validationRule.TestName);

        // 输出验证详情
        if (validationResult.IsValid)
        {
            _output.WriteLine($"验证通过: {validationRule.TestName}");
        }
        else
        {
            _output.WriteLine($"验证失败: {validationRule.TestName}");
            foreach (var error in validationResult.Errors)
            {
                _output.WriteLine($"  错误: {error}");
            }
        }

        // 断言验证结果
        Assert.True(validationResult.IsValid, 
            $"复杂验证失败 - {validationRule.TestName}: {string.Join("; ", validationResult.Errors)}");
    }

    /// <summary>
    /// API 并发安全测试
    /// 验证 API 在高并发情况下的稳定性和数据一致性
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("TestType", "Concurrency")]
    public async Task ApiConcurrency_HighConcurrentLoad_ShouldMaintainStability()
    {
        const int concurrentUsers = 20;
        const int requestsPerUser = 5;
        const int totalRequests = concurrentUsers * requestsPerUser;

        _output.WriteLine($"开始并发安全测试 - 并发用户: {concurrentUsers}, 每用户请求: {requestsPerUser}");

        var allTasks = new List<Task<ConcurrencyResult>>();
        var startTime = DateTime.UtcNow;

        // 创建并发任务
        for (int user = 1; user <= concurrentUsers; user++)
        {
            var userId = user;
            var userTasks = Enumerable.Range(1, requestsPerUser).Select(async requestId =>
            {
                var request = CreateGetRequest("/s", new Dictionary<string, string>
                {
                    ["wd"] = $"并发测试_用户{userId}_请求{requestId}",
                    ["ie"] = "utf-8"
                });

                var requestStart = DateTime.UtcNow;
                var response = await ExecuteApiTestAsync<string>(request, $"并发测试_U{userId}_R{requestId}");
                var requestEnd = DateTime.UtcNow;

                return new ConcurrencyResult
                {
                    UserId = userId,
                    RequestId = requestId,
                    Response = response,
                    StartTime = requestStart,
                    EndTime = requestEnd,
                    IsSuccess = response.IsSuccess
                };
            });

            allTasks.AddRange(userTasks);
        }

        // 执行所有并发请求
        var results = await Task.WhenAll(allTasks);
        var endTime = DateTime.UtcNow;
        var totalTime = endTime - startTime;

        // 分析并发结果
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);
        var successRate = (double)successCount / totalRequests * 100;
        var averageResponseTime = results.Average(r => r.Response.ResponseTime.TotalMilliseconds);
        var maxResponseTime = results.Max(r => r.Response.ResponseTime.TotalMilliseconds);
        var throughput = totalRequests / totalTime.TotalSeconds;

        // 检查时间重叠（并发性验证）
        var overlappingRequests = 0;
        for (int i = 0; i < results.Length; i++)
        {
            for (int j = i + 1; j < results.Length; j++)
            {
                if (results[i].StartTime < results[j].EndTime && results[j].StartTime < results[i].EndTime)
                {
                    overlappingRequests++;
                }
            }
        }

        // 输出并发测试结果
        _output.WriteLine("并发安全测试完成:");
        _output.WriteLine($"  总请求数: {totalRequests}");
        _output.WriteLine($"  成功请求: {successCount}");
        _output.WriteLine($"  失败请求: {failureCount}");
        _output.WriteLine($"  成功率: {successRate:F2}%");
        _output.WriteLine($"  总耗时: {totalTime.TotalSeconds:F2}s");
        _output.WriteLine($"  平均响应时间: {averageResponseTime:F2}ms");
        _output.WriteLine($"  最大响应时间: {maxResponseTime:F2}ms");
        _output.WriteLine($"  吞吐量: {throughput:F2} 请求/秒");
        _output.WriteLine($"  重叠请求对数: {overlappingRequests}");

        // 并发安全断言
        Assert.True(successRate >= 90, $"并发测试成功率过低: {successRate:F2}%");
        Assert.True(averageResponseTime < 15000, $"并发测试平均响应时间过长: {averageResponseTime:F2}ms");
        Assert.True(overlappingRequests > 0, "没有检测到真正的并发执行");

        // 检查是否有明显的性能退化
        var responseTimesByUser = results.GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                AverageResponseTime = g.Average(r => r.Response.ResponseTime.TotalMilliseconds),
                RequestCount = g.Count()
            })
            .ToList();

        var maxUserAvgTime = responseTimesByUser.Max(u => u.AverageResponseTime);
        var minUserAvgTime = responseTimesByUser.Min(u => u.AverageResponseTime);
        var timeVariance = maxUserAvgTime - minUserAvgTime;

        _output.WriteLine($"用户间响应时间差异: {timeVariance:F2}ms (最大: {maxUserAvgTime:F2}ms, 最小: {minUserAvgTime:F2}ms)");
        
        // 响应时间差异不应过大（表明系统在并发下保持稳定）
        Assert.True(timeVariance < averageResponseTime * 2, 
            $"用户间响应时间差异过大，可能存在并发问题: {timeVariance:F2}ms");
    }

    /// <summary>
    /// API 错误恢复测试
    /// 验证 API 客户端的错误恢复和重试机制
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Reliability")]
    public async Task ApiErrorRecovery_RetryMechanism_ShouldRecoverFromTransientFailures()
    {
        _output.WriteLine("开始 API 错误恢复测试");

        // 创建一个可能导致间歇性失败的请求（使用非常短的超时）
        var customApiSettings = new Core.Configuration.ApiSettings
        {
            Timeout = 1000,    // 1秒超时，可能导致超时
            RetryCount = 3,    // 重试3次
            RetryDelay = 500   // 重试间隔500ms
        };

        var customApiClient = _fixture.CreateCustomApiClient(customApiSettings);
        var customApiService = new ApiService(customApiClient, 
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ApiService>());

        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/s",
            QueryParameters = new Dictionary<string, string>
            {
                ["wd"] = "错误恢复测试",
                ["ie"] = "utf-8"
            }
        };

        var startTime = DateTime.UtcNow;
        ApiResponse<string>? response = null;
        Exception? lastException = null;

        try
        {
            response = await customApiService.SendRequestAsync<string>(request);
        }
        catch (Exception ex)
        {
            lastException = ex;
        }

        var endTime = DateTime.UtcNow;
        var totalTime = endTime - startTime;

        _output.WriteLine($"错误恢复测试完成 - 总耗时: {totalTime.TotalMilliseconds:F2}ms");

        if (response != null)
        {
            _output.WriteLine($"请求最终成功 - 状态码: {response.StatusCode}, 响应时间: {response.ResponseTime.TotalMilliseconds:F2}ms");
            Assert.True(response.IsSuccess, "重试后请求应该成功");
        }
        else
        {
            _output.WriteLine($"请求最终失败 - 异常: {lastException?.Message}");
            // 即使失败，也应该在合理的时间内完成（表明重试机制正常工作）
            Assert.True(totalTime.TotalSeconds >= 2, "重试机制应该至少尝试几次");
            Assert.True(totalTime.TotalSeconds <= 10, "重试不应该花费过长时间");
        }
    }

    /// <summary>
    /// 获取验证规则数据
    /// </summary>
    /// <returns>验证规则数据集合</returns>
    public static IEnumerable<object[]> GetValidationRuleData()
    {
        var filePath = PathConfiguration.GetTestDataPath("api_validation_rules.json", "API");
        
        if (!File.Exists(filePath))
        {
            return GetDefaultValidationRuleData();
        }

        try
        {
            var jsonContent = File.ReadAllText(filePath);
            var validationRules = JsonSerializer.Deserialize<List<ApiValidationRuleData>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (validationRules != null && validationRules.Any())
            {
                return validationRules.Select(rule => new object[] { rule });
            }
        }
        catch (Exception)
        {
            // 如果解析失败，返回默认验证规则
        }
        
        return GetDefaultValidationRuleData();
    }

    /// <summary>
    /// 获取默认验证规则数据
    /// </summary>
    /// <returns>默认验证规则数据集合</returns>
    private static IEnumerable<object[]> GetDefaultValidationRuleData()
    {
        yield return new object[]
        {
            new ApiValidationRuleData
            {
                TestName = "基础响应验证",
                ExpectedStatusCode = 200,
                MaxResponseTimeMs = 5000,
                Description = "基础的API响应验证规则"
            }
        };
    }
}

/// <summary>
/// 批量测试结果
/// </summary>
public class BatchResult
{
    public int Index { get; set; }
    public ApiResponse<string> Response { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// 并发测试结果
/// </summary>
public class ConcurrencyResult
{
    public int UserId { get; set; }
    public int RequestId { get; set; }
    public ApiResponse<string> Response { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsSuccess { get; set; }
}