using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Api;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Tests.API.Models;
using CsPlaywrightXun.src.playwright.Tests.API.baidu;
using System.Text.Json;

namespace CsPlaywrightXun.src.playwright.Tests.API;

/// <summary>
/// 百度 API 测试类
/// 演示完整的 API 测试功能，包括数据驱动测试、响应验证和性能监控
/// </summary>
[APITest]
[Trait("Type", "API")]
[Trait("Category", "BaiduAPI")]
public class BaiduApiTests : BaseApiTest, IClassFixture<ApiTestFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly ApiTestFixture _fixture;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">API 测试固件</param>
    /// <param name="output">测试输出助手</param>
    public BaiduApiTests(ApiTestFixture fixture, ITestOutputHelper output)
        : base(fixture.ApiClient, fixture.Configuration, fixture.Logger)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        Logger.LogInformation("BaiduApiTests 初始化完成");
    }

    /// <summary>
    /// 基础搜索 API 测试
    /// 验证搜索 API 的基本功能
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("TestType", "Smoke")]
    public async Task SearchApi_BasicFunctionality_ShouldReturnValidResponse()
    {
        // Arrange
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "playwright",
            ["ie"] = "utf-8"
        });

        var validation = CreateBasicValidation(200, TimeSpan.FromSeconds(10));
        validation.ContentContainsList = new List<string> { "playwright" };

        // Act
        var response = await ExecuteApiTestAsync<string>(request, "基础搜索API测试");

        // Assert
        Assert.True(response.IsSuccess, $"API 调用失败，状态码: {response.StatusCode}");
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(10), 
            $"响应时间过长: {response.ResponseTime.TotalMilliseconds}ms");
        Assert.NotNull(response.RawContent);
        Assert.NotEmpty(response.RawContent);

        // 验证响应内容
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation);
        Assert.True(validationResult.IsValid, 
            $"响应验证失败: {string.Join("; ", validationResult.Errors)}");

        _output.WriteLine($"测试完成 - 状态码: {response.StatusCode}, 响应时间: {response.ResponseTime.TotalMilliseconds}ms");
    }

    /// <summary>
    /// 数据驱动的搜索 API 测试
    /// 使用 JSON 数据文件驱动多个测试场景
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [MemberData(nameof(GetApiTestData))]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "DataDriven")]
    public async Task SearchApi_DataDriven_ShouldPassAllScenarios(ApiTestData testData)
    {
        // 跳过禁用的测试
        if (!testData.Enabled)
        {
            _output.WriteLine($"跳过禁用的测试: {testData.TestName}");
            return;
        }

        // 验证测试数据
        var (isValid, errors) = testData.Validate();
        Assert.True(isValid, $"测试数据无效: {string.Join("; ", errors)}");

        // Arrange
        var request = testData.ToApiRequest();
        var validation = testData.ToApiValidation();

        _output.WriteLine($"执行测试: {testData.TestName}");
        _output.WriteLine($"描述: {testData.Description}");
        _output.WriteLine($"端点: {request.Method} {request.Endpoint}");

        // Act
        var response = await ExecuteApiTestAsync<string>(request, testData.TestName);

        // Assert
        Assert.True(response.IsSuccess, 
            $"API 调用失败 - 测试: {testData.TestName}, 状态码: {response.StatusCode}");

        // 验证响应
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation, testData.TestName);
        Assert.True(validationResult.IsValid, 
            $"响应验证失败 - 测试: {testData.TestName}, 错误: {string.Join("; ", validationResult.Errors)}");

        _output.WriteLine($"测试通过 - {testData.TestName}: 状态码 {response.StatusCode}, " +
                         $"响应时间 {response.ResponseTime.TotalMilliseconds}ms");
    }

    /// <summary>
    /// API 性能测试
    /// 验证 API 在并发请求下的性能表现
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Performance")]
    public async Task SearchApi_Performance_ShouldMeetPerformanceRequirements()
    {
        // Arrange
        const int concurrentRequests = 5;
        const int maxResponseTimeMs = 5000;
        
        var requests = Enumerable.Range(1, concurrentRequests)
            .Select(i => CreateGetRequest("/s", new Dictionary<string, string>
            {
                ["wd"] = $"性能测试{i}",
                ["ie"] = "utf-8"
            }))
            .ToList();

        var validation = CreateBasicValidation(200, TimeSpan.FromMilliseconds(maxResponseTimeMs));

        _output.WriteLine($"开始性能测试 - 并发请求数: {concurrentRequests}");

        // Act
        var startTime = DateTime.UtcNow;
        var tasks = requests.Select(async (request, index) =>
        {
            var response = await ExecuteApiTestAsync<string>(request, $"性能测试_{index + 1}");
            return new { Index = index + 1, Response = response };
        });

        var results = await Task.WhenAll(tasks);
        var totalTime = DateTime.UtcNow - startTime;

        // Assert
        Assert.All(results, result =>
        {
            Assert.True(result.Response.IsSuccess, 
                $"请求 {result.Index} 失败，状态码: {result.Response.StatusCode}");
            Assert.True(result.Response.ResponseTime.TotalMilliseconds < maxResponseTimeMs,
                $"请求 {result.Index} 响应时间过长: {result.Response.ResponseTime.TotalMilliseconds}ms");
        });

        // 计算性能指标
        var responseTimes = results.Select(r => r.Response.ResponseTime.TotalMilliseconds).ToList();
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();

        _output.WriteLine($"性能测试完成:");
        _output.WriteLine($"  总耗时: {totalTime.TotalMilliseconds}ms");
        _output.WriteLine($"  平均响应时间: {averageResponseTime:F2}ms");
        _output.WriteLine($"  最大响应时间: {maxResponseTime:F2}ms");
        _output.WriteLine($"  最小响应时间: {minResponseTime:F2}ms");

        // 性能断言
        Assert.True(averageResponseTime < maxResponseTimeMs, 
            $"平均响应时间超标: {averageResponseTime:F2}ms > {maxResponseTimeMs}ms");
    }

    /// <summary>
    /// API 错误处理测试
    /// 验证 API 对无效请求的错误处理能力
    /// </summary>
    [Theory]
    [InlineData("", "空查询参数测试")]
    [InlineData("超长查询参数测试", "超长查询参数测试")]
    [InlineData("<script>alert('xss')</script>", "XSS 攻击测试")]
    [InlineData("' OR '1'='1", "SQL 注入测试")]
    [Trait("Priority", "High")]
    [Trait("TestType", "Security")]
    public async Task SearchApi_ErrorHandling_ShouldHandleInvalidInputsGracefully(string query, string testDescription)
    {
        // 处理特殊测试场景
        if (testDescription == "超长查询参数测试")
        {
            query = "非常长的查询参数" + new string('测', 1000);
        }

        // Arrange
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = query,
            ["ie"] = "utf-8"
        });

        _output.WriteLine($"执行错误处理测试: {testDescription}");
        _output.WriteLine($"查询参数: {query}");

        // Act
        var response = await ExecuteApiTestAsync<string>(request, testDescription);

        // Assert - 应该返回有效响应，不应该崩溃
        Assert.True(response.StatusCode >= 200 && response.StatusCode < 500,
            $"服务器错误 - {testDescription}: 状态码 {response.StatusCode}");
        
        Assert.NotNull(response.RawContent);
        
        // 验证响应时间合理
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(30),
            $"响应时间过长 - {testDescription}: {response.ResponseTime.TotalMilliseconds}ms");

        _output.WriteLine($"错误处理测试通过 - {testDescription}: 状态码 {response.StatusCode}");
    }

    /// <summary>
    /// API 响应头验证测试
    /// 验证 API 返回的响应头是否符合预期
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("TestType", "Validation")]
    public async Task SearchApi_ResponseHeaders_ShouldContainRequiredHeaders()
    {
        // Arrange
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "响应头测试",
            ["ie"] = "utf-8"
        });

        var validation = CreateBasicValidation(200);
        validation.RequiredHeaders = new Dictionary<string, string>
        {
            ["Content-Type"] = "text/html"
        };

        // Act
        var response = await ExecuteApiTestAsync<string>(request, "响应头验证测试");

        // Assert
        Assert.True(response.IsSuccess, $"API 调用失败，状态码: {response.StatusCode}");
        
        // 验证响应头
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation);
        Assert.True(validationResult.IsValid, 
            $"响应头验证失败: {string.Join("; ", validationResult.Errors)}");

        // 输出响应头信息
        _output.WriteLine("响应头信息:");
        foreach (var header in response.Headers)
        {
            _output.WriteLine($"  {header.Key}: {header.Value}");
        }
    }

    /// <summary>
    /// API 内容验证测试
    /// 使用正则表达式验证响应内容格式
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Validation")]
    public async Task SearchApi_ContentValidation_ShouldMatchExpectedFormat()
    {
        // Arrange
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "内容验证测试",
            ["ie"] = "utf-8"
        });

        var validation = CreateBasicValidation(200);
        validation.ContentRegex = @".*百度.*";
        validation.ContentNotContainsList = new List<string> { "error", "404", "500" };

        // Act
        var response = await ExecuteApiTestAsync<string>(request, "内容验证测试");

        // Assert
        Assert.True(response.IsSuccess, $"API 调用失败，状态码: {response.StatusCode}");
        
        // 验证内容格式
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation);
        Assert.True(validationResult.IsValid, 
            $"内容验证失败: {string.Join("; ", validationResult.Errors)}");

        _output.WriteLine($"内容验证通过 - 响应长度: {response.RawContent.Length} 字符");
    }

    /// <summary>
    /// API 超时测试
    /// 验证 API 客户端的超时处理机制
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("TestType", "Reliability")]
    public async Task SearchApi_Timeout_ShouldHandleTimeoutGracefully()
    {
        // Arrange - 使用一个可能较慢的查询
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "超时测试" + new string('测', 100),
            ["ie"] = "utf-8"
        });

        var validation = CreateBasicValidation(200, TimeSpan.FromSeconds(30));

        // Act & Assert
        var response = await ExecuteApiTestAsync<string>(request, "超时测试");
        
        // 即使是慢查询，也应该在合理时间内返回
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(30),
            $"响应时间过长: {response.ResponseTime.TotalMilliseconds}ms");

        _output.WriteLine($"超时测试完成 - 响应时间: {response.ResponseTime.TotalMilliseconds}ms");
    }

    /// <summary>
    /// 获取 API 性能报告
    /// 展示如何获取和分析 API 性能数据
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("TestType", "Reporting")]
    public async Task GetApiPerformanceReport_ShouldProvideDetailedMetrics()
    {
        // Arrange - 先执行几个请求以生成性能数据
        var requests = new[]
        {
            CreateGetRequest("/s", new Dictionary<string, string> { ["wd"] = "性能报告1", ["ie"] = "utf-8" }),
            CreateGetRequest("/s", new Dictionary<string, string> { ["wd"] = "性能报告2", ["ie"] = "utf-8" }),
            CreateGetRequest("/s", new Dictionary<string, string> { ["wd"] = "性能报告3", ["ie"] = "utf-8" })
        };

        // Act - 执行请求
        foreach (var request in requests)
        {
            await ExecuteApiTestAsync<string>(request, "性能报告数据生成");
        }

        // 获取性能报告
        var performanceReport = ApiService.GetPerformanceReport(1); // 最近1小时的数据

        // Assert
        Assert.NotNull(performanceReport);
        Assert.True(performanceReport.TotalRequests >= 3, 
            $"性能报告中的请求数量不足: {performanceReport.TotalRequests}");

        // 输出性能报告
        _output.WriteLine("=== API 性能报告 ===");
        _output.WriteLine($"报告生成时间: {performanceReport.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        _output.WriteLine($"时间范围: {performanceReport.TimeRangeHours} 小时");
        _output.WriteLine($"总请求数: {performanceReport.TotalRequests}");
        _output.WriteLine($"平均响应时间: {performanceReport.AverageResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"最大响应时间: {performanceReport.MaxResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"最小响应时间: {performanceReport.MinResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"成功率: {performanceReport.SuccessRate:F2}%");

        if (performanceReport.Statistics.Any())
        {
            _output.WriteLine("\n=== 各端点详细统计 ===");
            foreach (var stat in performanceReport.Statistics)
            {
                _output.WriteLine($"  {stat.EndpointKey}");
                _output.WriteLine($"  总请求数: {stat.TotalRequests}");
                _output.WriteLine($"  成功请求数: {stat.SuccessfulRequests}");
                _output.WriteLine($"  失败请求数: {stat.FailedRequests}");
                _output.WriteLine($"  成功率: {stat.SuccessRate:F2}%");
                _output.WriteLine($"  平均响应时间: {stat.AverageResponseTime.TotalMilliseconds:F2}ms");
                _output.WriteLine($"  95百分位响应时间: {stat.P95ResponseTime.TotalMilliseconds:F2}ms");
                _output.WriteLine($"  99百分位响应时间: {stat.P99ResponseTime.TotalMilliseconds:F2}ms");
                _output.WriteLine("");
            }
        }
    }

    /// <summary>
    /// 获取 API 测试数据
    /// </summary>
    /// <returns>测试数据集合</returns>
    public static IEnumerable<object[]> GetApiTestData()
    {
        var filePath = "src/config/date/API/api_test_data.json";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
        
        if (!File.Exists(fullPath))
        {
            // 如果文件不存在，返回默认测试数据
            return GetDefaultApiTestData();
        }

        try
        {
            var jsonContent = File.ReadAllText(fullPath);
            var testDataList = JsonSerializer.Deserialize<List<ApiTestData>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (testDataList != null && testDataList.Any())
            {
                return testDataList.Select(testData => new object[] { testData });
            }
        }
        catch (Exception)
        {
            // 如果解析失败，返回默认测试数据
        }
        
        return GetDefaultApiTestData();
    }

    /// <summary>
    /// 获取默认 API 测试数据
    /// </summary>
    /// <returns>默认测试数据集合</returns>
    private static IEnumerable<object[]> GetDefaultApiTestData()
    {
        yield return new object[]
        {
            new ApiTestData
            {
                TestName = "默认搜索测试",
                Endpoint = "/s",
                Method = "GET",
                QueryParameters = new Dictionary<string, string>
                {
                    ["wd"] = "playwright",
                    ["ie"] = "utf-8"
                },
                ExpectedStatusCode = 200,
                MaxResponseTimeMs = 5000,
                Description = "默认的搜索API测试"
            }
        };
    }
}