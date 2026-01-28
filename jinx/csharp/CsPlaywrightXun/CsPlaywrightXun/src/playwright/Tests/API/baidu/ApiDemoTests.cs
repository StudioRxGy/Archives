using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Api;

namespace CsPlaywrightXun.src.playwright.Tests.API.baidu;

/// <summary>
/// API 演示测试类
/// 展示 API 测试框架的核心功能，使用更宽松的验证条件
/// </summary>
[APITest]
[Trait("Type", "API")]
[Trait("Category", "Demo")]
public class ApiDemoTests : BaseApiTest, IClassFixture<ApiTestFixture>
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">API 测试固件</param>
    /// <param name="output">测试输出助手</param>
    public ApiDemoTests(ApiTestFixture fixture, ITestOutputHelper output)
        : base(fixture.ApiClient, fixture.Configuration, fixture.Logger)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        Logger.LogInformation("ApiDemoTests 初始化完成");
    }

    /// <summary>
    /// 演示基础 API 调用功能
    /// 验证 API 客户端能够成功发送请求并接收响应
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("TestType", "Demo")]
    public async Task ApiDemo_BasicRequest_ShouldSucceed()
    {
        _output.WriteLine("=== API 基础功能演示 ===");

        // Arrange
        var request = CreateGetRequest("/s", new Dictionary<string, string>
        {
            ["wd"] = "API测试演示",
            ["ie"] = "utf-8"
        });

        // Act
        var response = await ExecuteApiTestAsync<string>(request, "API基础功能演示");

        // Assert - 验证基础功能
        Assert.True(response.IsSuccess, $"API 调用应该成功，实际状态码: {response.StatusCode}");
        Assert.True(response.ResponseTime > TimeSpan.Zero, "响应时间应该大于0");
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(30), "响应时间应该在30秒内");
        Assert.NotNull(response.RawContent);
        Assert.NotEmpty(response.RawContent);
        Assert.True(response.RawContent.Length > 100, "响应内容应该有实际内容");

        // 输出演示信息
        _output.WriteLine($"✅ API 调用成功");
        _output.WriteLine($"✅ 状态码: {response.StatusCode}");
        _output.WriteLine($"✅ 响应时间: {response.ResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"✅ 响应内容长度: {response.RawContent.Length} 字符");
        _output.WriteLine($"✅ 响应头数量: {response.Headers.Count}");

        _output.WriteLine("\n=== 响应头信息 ===");
        foreach (var header in response.Headers.Take(5)) // 只显示前5个响应头
        {
            _output.WriteLine($"  {header.Key}: {header.Value}");
        }
    }

    /// <summary>
    /// 演示 API 响应验证功能
    /// 展示如何使用验证规则验证 API 响应
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Demo")]
    public async Task ApiDemo_ResponseValidation_ShouldWork()
    {
        _output.WriteLine("=== API 响应验证演示 ===");

        // Arrange
        var request = CreateGetRequest("/", new Dictionary<string, string>());

        var validation = new ApiValidation
        {
            ExpectedStatusCode = 200,
            MaxResponseTime = TimeSpan.FromSeconds(10),
            RequiredHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "text/html"
            },
            ContentContainsList = new List<string> { "百度" } // 使用中文内容验证
        };

        // Act
        var response = await ExecuteApiTestAsync<string>(request, "响应验证演示");

        // 执行验证
        var validationResult = ValidateApiResponse(new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = response.Data,
            RawContent = response.RawContent,
            ResponseTime = response.ResponseTime,
            Headers = response.Headers
        }, validation);

        // Assert
        Assert.True(response.IsSuccess, "API 调用应该成功");
        Assert.True(validationResult.IsValid, 
            $"响应验证应该通过，错误: {string.Join("; ", validationResult.Errors)}");

        // 输出演示信息
        _output.WriteLine($"✅ API 调用成功，状态码: {response.StatusCode}");
        _output.WriteLine($"✅ 响应验证通过");
        _output.WriteLine($"✅ 响应时间: {response.ResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"✅ 内容验证: 包含预期内容");
        _output.WriteLine($"✅ 响应头验证: 包含必需的响应头");
    }

    /// <summary>
    /// 演示 API 性能监控功能
    /// 展示如何监控和分析 API 性能
    /// </summary>
    [Fact]
    [Trait("Priority", "Medium")]
    [Trait("TestType", "Demo")]
    public async Task ApiDemo_PerformanceMonitoring_ShouldWork()
    {
        _output.WriteLine("=== API 性能监控演示 ===");

        // 执行多个请求以生成性能数据
        var requests = new[]
        {
            CreateGetRequest("/", new Dictionary<string, string>()),
            CreateGetRequest("/s", new Dictionary<string, string> { ["wd"] = "性能测试1" }),
            CreateGetRequest("/s", new Dictionary<string, string> { ["wd"] = "性能测试2" })
        };

        var responseTimes = new List<TimeSpan>();

        foreach (var (request, index) in requests.Select((r, i) => (r, i)))
        {
            var response = await ExecuteApiTestAsync<string>(request, $"性能监控演示_{index + 1}");
            responseTimes.Add(response.ResponseTime);
            
            Assert.True(response.IsSuccess, $"请求 {index + 1} 应该成功");
            
            // 在请求之间添加小延迟
            await Task.Delay(200);
        }

        // 分析性能数据
        var averageResponseTime = responseTimes.Average(t => t.TotalMilliseconds);
        var maxResponseTime = responseTimes.Max(t => t.TotalMilliseconds);
        var minResponseTime = responseTimes.Min(t => t.TotalMilliseconds);

        // 输出性能分析结果
        _output.WriteLine($"✅ 完成 {requests.Length} 个 API 请求");
        _output.WriteLine($"✅ 平均响应时间: {averageResponseTime:F2}ms");
        _output.WriteLine($"✅ 最大响应时间: {maxResponseTime:F2}ms");
        _output.WriteLine($"✅ 最小响应时间: {minResponseTime:F2}ms");

        // 获取性能报告
        var performanceReport = ApiService.GetPerformanceReport(1);
        if (performanceReport.TotalRequests > 0)
        {
            _output.WriteLine($"✅ 性能报告生成成功");
            _output.WriteLine($"  总请求数: {performanceReport.TotalRequests}");
            _output.WriteLine($"  成功率: {performanceReport.SuccessRate:F2}%");
        }

        // 性能断言
        Assert.True(averageResponseTime < 10000, $"平均响应时间应该在10秒内: {averageResponseTime:F2}ms");
        Assert.True(performanceReport.TotalRequests >= requests.Length, "性能报告应该记录所有请求");
    }

    /// <summary>
    /// 演示 API 错误处理功能
    /// 展示框架如何处理各种错误情况
    /// </summary>
    [Fact]
    [Trait("Priority", "Low")]
    [Trait("TestType", "Demo")]
    public async Task ApiDemo_ErrorHandling_ShouldWork()
    {
        _output.WriteLine("=== API 错误处理演示 ===");

        // 测试不存在的端点
        var invalidRequest = CreateGetRequest("/nonexistent-endpoint-12345");

        var response = await ExecuteApiTestAsync<string>(invalidRequest, "错误处理演示");

        // 验证错误处理
        _output.WriteLine($"✅ 错误请求处理完成");
        _output.WriteLine($"  状态码: {response.StatusCode}");
        _output.WriteLine($"  响应时间: {response.ResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  响应内容长度: {response.RawContent.Length}");

        // 即使是错误请求，也应该有合理的响应
        Assert.True(response.ResponseTime > TimeSpan.Zero, "即使错误请求也应该有响应时间");
        Assert.True(response.ResponseTime < TimeSpan.FromSeconds(30), "错误请求响应时间应该合理");
        Assert.NotNull(response.RawContent);

        _output.WriteLine("✅ 错误处理机制工作正常");
    }

    /// <summary>
    /// 演示完整的 API 测试工作流
    /// 展示从请求创建到结果验证的完整流程
    /// </summary>
    [Fact]
    [Trait("Priority", "High")]
    [Trait("TestType", "Demo")]
    public async Task ApiDemo_CompleteWorkflow_ShouldWork()
    {
        _output.WriteLine("=== 完整 API 测试工作流演示 ===");

        var workflowSteps = new List<(string Step, bool Success, TimeSpan Duration)>();
        var overallStart = DateTime.UtcNow;

        try
        {
            // 步骤 1: 创建请求
            _output.WriteLine("步骤 1: 创建 API 请求");
            var stepStart = DateTime.UtcNow;
            
            var request = CreateGetRequest("/", new Dictionary<string, string>());
            Assert.NotNull(request);
            Assert.Equal("GET", request.Method);
            Assert.Equal("/", request.Endpoint);
            
            workflowSteps.Add(("创建请求", true, DateTime.UtcNow - stepStart));
            _output.WriteLine("✅ 请求创建成功");

            // 步骤 2: 执行请求
            _output.WriteLine("步骤 2: 执行 API 请求");
            stepStart = DateTime.UtcNow;
            
            var response = await ExecuteApiTestAsync<string>(request, "完整工作流演示");
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            
            workflowSteps.Add(("执行请求", true, DateTime.UtcNow - stepStart));
            _output.WriteLine($"✅ 请求执行成功，状态码: {response.StatusCode}");

            // 步骤 3: 验证响应
            _output.WriteLine("步骤 3: 验证 API 响应");
            stepStart = DateTime.UtcNow;
            
            var validation = CreateBasicValidation(200, TimeSpan.FromSeconds(10));
            var validationResult = ValidateApiResponse(new ApiResponse
            {
                StatusCode = response.StatusCode,
                Data = response.Data,
                RawContent = response.RawContent,
                ResponseTime = response.ResponseTime,
                Headers = response.Headers
            }, validation);
            
            workflowSteps.Add(("验证响应", validationResult.IsValid, DateTime.UtcNow - stepStart));
            _output.WriteLine($"✅ 响应验证完成，结果: {(validationResult.IsValid ? "通过" : "失败")}");

            // 步骤 4: 生成报告
            _output.WriteLine("步骤 4: 生成性能报告");
            stepStart = DateTime.UtcNow;
            
            var performanceReport = ApiService.GetPerformanceReport(1);
            Assert.NotNull(performanceReport);
            
            workflowSteps.Add(("生成报告", true, DateTime.UtcNow - stepStart));
            _output.WriteLine("✅ 性能报告生成成功");

            var overallDuration = DateTime.UtcNow - overallStart;

            // 输出工作流总结
            _output.WriteLine("\n=== 工作流执行总结 ===");
            _output.WriteLine($"总耗时: {overallDuration.TotalMilliseconds:F2}ms");
            _output.WriteLine($"成功步骤: {workflowSteps.Count(s => s.Success)}/{workflowSteps.Count}");
            
            _output.WriteLine("\n步骤详情:");
            foreach (var (step, success, duration) in workflowSteps)
            {
                _output.WriteLine($"  {step}: {(success ? "✅" : "❌")} ({duration.TotalMilliseconds:F2}ms)");
            }

            // 最终断言
            Assert.True(workflowSteps.All(s => s.Success), "所有工作流步骤都应该成功");
            Assert.True(overallDuration < TimeSpan.FromSeconds(30), "整个工作流应该在30秒内完成");

            _output.WriteLine("\n🎉 完整 API 测试工作流演示成功！");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ 工作流执行失败: {ex.Message}");
            throw;
        }
    }
}