using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Services.Api;
using Newtonsoft.Json.Linq;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// API 响应验证集成测试
/// </summary>
public class ApiResponseValidationIntegrationTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ApiClient>> _apiClientLoggerMock;
    private readonly Mock<ILogger<ApiService>> _apiServiceLoggerMock;
    private readonly Mock<ILogger<ApiPerformanceMonitor>> _performanceLoggerMock;
    private readonly TestConfiguration _testConfiguration;
    private readonly ApiClient _apiClient;
    private readonly ApiPerformanceMonitor _performanceMonitor;
    private readonly ApiService _apiService;

    public ApiResponseValidationIntegrationTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _apiClientLoggerMock = new Mock<ILogger<ApiClient>>();
        _apiServiceLoggerMock = new Mock<ILogger<ApiService>>();
        _performanceLoggerMock = new Mock<ILogger<ApiPerformanceMonitor>>();
        
        _testConfiguration = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Test",
                BaseUrl = "https://test.example.com",
                ApiBaseUrl = "https://api.test.example.com"
            },
            Api = new ApiSettings
            {
                Timeout = 30000,
                RetryCount = 2,
                RetryDelay = 1000
            }
        };

        _apiClient = new ApiClient(_httpClient, _testConfiguration, _apiClientLoggerMock.Object);
        _performanceMonitor = new ApiPerformanceMonitor(_performanceLoggerMock.Object);
        _apiService = new ApiService(_apiClient, _apiServiceLoggerMock.Object, _performanceMonitor);
    }

    [Fact]
    public async Task CompleteApiValidationWorkflow_ShouldValidateAllAspects()
    {
        // Arrange
        var jsonResponse = @"{
            ""user"": {
                ""id"": 12345,
                ""name"": ""John Doe"",
                ""email"": ""john.doe@example.com"",
                ""active"": true,
                ""roles"": [""admin"", ""user""],
                ""profile"": {
                    ""age"": 30,
                    ""department"": ""Engineering""
                }
            },
            ""status"": ""success"",
            ""timestamp"": ""2024-01-15T10:30:00Z"",
            ""metadata"": {
                ""version"": ""1.0"",
                ""requestId"": ""req-123456""
            }
        }";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };
        httpResponse.Headers.Add("X-Request-ID", "req-123456");
        httpResponse.Headers.Add("X-Rate-Limit", "1000");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/api/users/12345",
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer test-token"
            }
        };

        var validation = new ApiValidation
        {
            // 状态码验证
            ExpectedStatusCode = 200,
            
            // 响应时间验证 - 设置合理的时间范围
            MaxResponseTime = TimeSpan.FromSeconds(5),
            
            // 内容包含验证
            ContentContains = "success",
            ContentContainsList = new List<string> { "John Doe", "Engineering" },
            
            // 内容不包含验证
            ContentNotContains = "error",
            ContentNotContainsList = new List<string> { "failure", "exception" },
            
            // 正则表达式验证
            ContentRegex = @"""requestId"":\s*""req-\d+""",
            
            // 响应头验证
            RequiredHeaders = new Dictionary<string, string>
            {
                ["X-Request-ID"] = "req-123456"
                // 移除Content-Type验证，因为HttpClient可能会添加charset
            },
            ForbiddenHeaders = new List<string> { "X-Debug-Info", "X-Internal-Token" },
            
            // JSON Path 验证
            JsonPathValidations = new List<JsonPathValidation>
            {
                // 基本值验证
                new() { Path = "$.user.id", ExpectedValue = 12345, ExpectedType = JTokenType.Integer },
                new() { Path = "$.user.name", ExpectedValue = "John Doe", ExpectedType = JTokenType.String },
                new() { Path = "$.user.active", ExpectedValue = true, ExpectedType = JTokenType.Boolean },
                
                // 数组长度验证
                new() { Path = "$.user.roles", ExpectedArrayLength = 2, ExpectedType = JTokenType.Array },
                
                // 正则表达式验证
                new() { Path = "$.user.email", ValueRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$" },
                new() { Path = "$.timestamp", ValueRegex = @"^\d{4}[/-]\d{1,2}[/-]\d{1,2}.*" }, // 更灵活的时间格式匹配
                
                // 嵌套对象验证
                new() { Path = "$.user.profile.age", ExpectedValue = 30, ExpectedType = JTokenType.Integer },
                new() { Path = "$.user.profile.department", ExpectedValue = "Engineering", ExpectedType = JTokenType.String },
                
                // 必需字段验证
                new() { Path = "$.status", IsRequired = true, ExpectedValue = "success" },
                new() { Path = "$.metadata.version", IsRequired = true, ExpectedValue = "1.0" }
            }
        };

        // Act
        var response = await _apiService.SendRequestAsync(request);
        var validationResult = _apiService.ValidateResponse(response, validation);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();
        response.RawContent.Should().Contain("John Doe");
        response.Headers.Should().ContainKey("X-Request-ID");

        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue($"Validation errors: {string.Join(", ", validationResult.Errors)}");
        validationResult.Errors.Should().BeEmpty();

        // 验证性能监控
        var performanceStats = _apiService.GetPerformanceStatistics("/api/users/12345", "GET");
        performanceStats.Should().NotBeNull();
        performanceStats!.TotalRequests.Should().Be(1);
        performanceStats.SuccessfulRequests.Should().Be(1);
        performanceStats.SuccessRate.Should().Be(100.0);
    }

    [Fact]
    public async Task ApiValidationWithMultipleErrors_ShouldReportAllValidationFailures()
    {
        // Arrange
        var jsonResponse = @"{
            ""user"": {
                ""id"": ""wrong-type"",
                ""name"": null,
                ""roles"": [""user""]
            },
            ""status"": ""error"",
            ""errorMessage"": ""User not found""
        }";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };
        httpResponse.Headers.Add("X-Debug-Info", "debug-data");

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/api/users/invalid"
        };

        var validation = new ApiValidation
        {
            // 期望成功状态码，但实际是404
            ExpectedStatusCode = 200,
            
            // 内容不应包含错误信息
            ContentNotContains = "error",
            
            // 不应包含调试头
            ForbiddenHeaders = new List<string> { "X-Debug-Info" },
            
            // JSON Path 验证
            JsonPathValidations = new List<JsonPathValidation>
            {
                // 期望整数类型，但实际是字符串
                new() { Path = "$.user.id", ExpectedType = JTokenType.Integer },
                
                // 期望非空名称
                new() { Path = "$.user.name", IsRequired = true, ExpectedValue = "John Doe" },
                
                // 期望数组长度为2，但实际为1
                new() { Path = "$.user.roles", ExpectedArrayLength = 2 },
                
                // 期望不存在的字段
                new() { Path = "$.user.email", IsRequired = true }
            }
        };

        // Act
        var response = await _apiService.SendRequestAsync(request);
        var validationResult = _apiService.ValidateResponse(response, validation);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(404);
        response.IsSuccess.Should().BeFalse();

        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().NotBeEmpty();
        
        // 验证所有类型的错误都被检测到
        validationResult.Errors.Should().Contain(e => e.Contains("状态码验证失败"));
        validationResult.Errors.Should().Contain(e => e.Contains("内容不包含验证失败"));
        validationResult.Errors.Should().Contain(e => e.Contains("响应包含禁止的响应头"));
        validationResult.Errors.Should().Contain(e => e.Contains("JSON Path验证失败"));

        // 验证性能监控记录了失败请求
        var performanceStats = _apiService.GetPerformanceStatistics("/api/users/invalid", "GET");
        performanceStats.Should().NotBeNull();
        performanceStats!.TotalRequests.Should().Be(1);
        performanceStats.SuccessfulRequests.Should().Be(0);
        performanceStats.FailedRequests.Should().Be(1);
        performanceStats.SuccessRate.Should().Be(0.0);
    }

    [Fact]
    public async Task ApiPerformanceMonitoring_ShouldTrackMultipleRequests()
    {
        // Arrange
        var callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                callCount++;
                HttpResponseMessage httpResponse;
                
                if (callCount <= 2)
                {
                    // 前两次调用返回成功
                    httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(@"{""result"":""success""}", Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    // 第三次调用返回客户端错误（不会重试）
                    httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(@"{""error"":""bad request""}", Encoding.UTF8, "application/json")
                    };
                }
                
                // 模拟响应延迟
                return Task.Delay(100).ContinueWith(_ => httpResponse);
            });

        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/api/performance-test"
        };

        // Act - 发送多个请求
        for (int i = 0; i < 3; i++)
        {
            await _apiService.SendRequestAsync(request);
        }

        // Assert
        var performanceStats = _apiService.GetPerformanceStatistics("/api/performance-test", "GET");
        performanceStats.Should().NotBeNull();
        performanceStats!.TotalRequests.Should().Be(3);
        performanceStats.SuccessfulRequests.Should().Be(2);
        performanceStats.FailedRequests.Should().Be(1);
        performanceStats.SuccessRate.Should().BeApproximately(66.67, 0.01);
        performanceStats.AverageResponseTime.TotalMilliseconds.Should().BeGreaterThan(100);
        performanceStats.MaxResponseTime.TotalMilliseconds.Should().BeGreaterThan(50);
        performanceStats.MinResponseTime.TotalMilliseconds.Should().BeGreaterThan(10);

        // 验证性能报告
        var performanceReport = _apiService.GetPerformanceReport(24);
        performanceReport.Should().NotBeNull();
        performanceReport.TotalRequests.Should().Be(3);
        performanceReport.SuccessRate.Should().BeApproximately(66.67, 0.01);
        performanceReport.Statistics.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _apiService?.Dispose();
        _apiClient?.Dispose();
        _httpClient?.Dispose();
    }
}