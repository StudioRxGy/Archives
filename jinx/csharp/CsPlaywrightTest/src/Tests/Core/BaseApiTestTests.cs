using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Services.Api;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// BaseApiTest 单元测试
/// </summary>
[UnitTest]
[TestCategory(TestCategory.Core)]
[TestPriority(TestPriority.High)]
public class BaseApiTestTests : IDisposable
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestConfiguration _testConfiguration;
    private readonly TestableBaseApiTest _baseApiTest;

    public BaseApiTestTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _loggerMock = new Mock<ILogger>();
        
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
            },
            Browser = new BrowserSettings(),
            Reporting = new ReportingSettings(),
            Logging = new LoggingSettings()
        };

        _baseApiTest = new TestableBaseApiTest(_apiClientMock.Object, _testConfiguration, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullApiClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TestableBaseApiTest(null!, _testConfiguration, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TestableBaseApiTest(_apiClientMock.Object, null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TestableBaseApiTest(_apiClientMock.Object, _testConfiguration, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var baseApiTest = new TestableBaseApiTest(_apiClientMock.Object, _testConfiguration, _loggerMock.Object);

        // Assert
        baseApiTest.Should().NotBeNull();
        baseApiTest.GetApiClient().Should().Be(_apiClientMock.Object);
        baseApiTest.GetConfiguration().Should().Be(_testConfiguration);
        baseApiTest.GetLogger().Should().Be(_loggerMock.Object);
        baseApiTest.GetApiService().Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteApiTestAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _baseApiTest.ExecuteApiTestAsync<object>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task ExecuteApiTestAsync_WithValidRequest_ShouldExecuteAndLogCorrectly()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _baseApiTest.ExecuteApiTestAsync<object>(request, "TestMethod");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();

        // 验证日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("开始执行API测试: TestMethod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API测试执行完成: TestMethod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteApiTestAsync_WithApiException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var apiException = new ApiException("/test", 500, "Internal server error");
        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(apiException);

        // Act & Assert
        var action = async () => await _baseApiTest.ExecuteApiTestAsync<object>(request, "TestMethod");
        await action.Should().ThrowAsync<ApiException>();

        // 验证错误日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API测试执行失败: TestMethod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteApiTestAsync_NonGeneric_ShouldReturnNonGenericResponse()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _baseApiTest.ExecuteApiTestAsync(request, "TestMethod");

        // Assert
        response.Should().NotBeNull();
        response.Should().BeOfType<ApiResponse>();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiResponse_WithNullResponse_ShouldThrowArgumentNullException()
    {
        // Arrange
        var validation = new ApiValidation();

        // Act & Assert
        var action = () => _baseApiTest.ValidateApiResponse(null!, validation);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void ValidateApiResponse_WithNullValidation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 200 };

        // Act & Assert
        var action = () => _baseApiTest.ValidateApiResponse(response, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("validation");
    }

    [Fact]
    public void ValidateApiResponse_WithValidResponse_ShouldReturnValidResult()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            ResponseTime = TimeSpan.FromSeconds(1)
        };
        var validation = new ApiValidation 
        { 
            ExpectedStatusCode = 200,
            MaxResponseTime = TimeSpan.FromSeconds(5)
        };

        // Act
        var result = _baseApiTest.ValidateApiResponse(response, validation, "TestMethod");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        // 验证成功日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API响应验证通过: TestMethod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateApiResponse_WithInvalidResponse_ShouldReturnInvalidResult()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 404,
            ResponseTime = TimeSpan.FromSeconds(1)
        };
        var validation = new ApiValidation 
        { 
            ExpectedStatusCode = 200
        };

        // Act
        var result = _baseApiTest.ValidateApiResponse(response, validation, "TestMethod");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();

        // 验证警告日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API响应验证失败: TestMethod")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void CreateGetRequest_WithBasicParameters_ShouldCreateCorrectRequest()
    {
        // Act
        var request = _baseApiTest.CreateGetRequest("/test");

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("GET");
        request.Endpoint.Should().Be("/test");
        request.QueryParameters.Should().NotBeNull();
        request.Headers.Should().NotBeNull();
    }

    [Fact]
    public void CreateGetRequest_WithQueryParametersAndHeaders_ShouldCreateCorrectRequest()
    {
        // Arrange
        var queryParams = new Dictionary<string, string> { ["param1"] = "value1" };
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };

        // Act
        var request = _baseApiTest.CreateGetRequest("/test", queryParams, headers);

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("GET");
        request.Endpoint.Should().Be("/test");
        request.QueryParameters.Should().BeEquivalentTo(queryParams);
        request.Headers.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void CreatePostRequest_WithBasicParameters_ShouldCreateCorrectRequest()
    {
        // Arrange
        var body = new { name = "test" };

        // Act
        var request = _baseApiTest.CreatePostRequest("/test", body);

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("POST");
        request.Endpoint.Should().Be("/test");
        request.Body.Should().Be(body);
        request.Headers.Should().NotBeNull();
    }

    [Fact]
    public void CreatePostRequest_WithHeaders_ShouldCreateCorrectRequest()
    {
        // Arrange
        var body = new { name = "test" };
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        // Act
        var request = _baseApiTest.CreatePostRequest("/test", body, headers);

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("POST");
        request.Endpoint.Should().Be("/test");
        request.Body.Should().Be(body);
        request.Headers.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void CreatePutRequest_WithBasicParameters_ShouldCreateCorrectRequest()
    {
        // Arrange
        var body = new { id = 1, name = "updated" };

        // Act
        var request = _baseApiTest.CreatePutRequest("/test/1", body);

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("PUT");
        request.Endpoint.Should().Be("/test/1");
        request.Body.Should().Be(body);
        request.Headers.Should().NotBeNull();
    }

    [Fact]
    public void CreateDeleteRequest_WithBasicParameters_ShouldCreateCorrectRequest()
    {
        // Act
        var request = _baseApiTest.CreateDeleteRequest("/test/1");

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("DELETE");
        request.Endpoint.Should().Be("/test/1");
        request.Headers.Should().NotBeNull();
    }

    [Fact]
    public void CreateDeleteRequest_WithHeaders_ShouldCreateCorrectRequest()
    {
        // Arrange
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };

        // Act
        var request = _baseApiTest.CreateDeleteRequest("/test/1", headers);

        // Assert
        request.Should().NotBeNull();
        request.Method.Should().Be("DELETE");
        request.Endpoint.Should().Be("/test/1");
        request.Headers.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void CreateBasicValidation_WithDefaultParameters_ShouldCreateCorrectValidation()
    {
        // Act
        var validation = _baseApiTest.CreateBasicValidation();

        // Assert
        validation.Should().NotBeNull();
        validation.ExpectedStatusCode.Should().Be(200);
        validation.MaxResponseTime.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CreateBasicValidation_WithCustomParameters_ShouldCreateCorrectValidation()
    {
        // Arrange
        var expectedStatusCode = 201;
        var maxResponseTime = TimeSpan.FromSeconds(10);

        // Act
        var validation = _baseApiTest.CreateBasicValidation(expectedStatusCode, maxResponseTime);

        // Assert
        validation.Should().NotBeNull();
        validation.ExpectedStatusCode.Should().Be(expectedStatusCode);
        validation.MaxResponseTime.Should().Be(maxResponseTime);
    }

    [Fact]
    public void GetCurrentTestName_ShouldReturnTestMethodName()
    {
        // Act
        var testName = _baseApiTest.GetCurrentTestName();

        // Assert
        testName.Should().NotBeNullOrEmpty();
        // The method returns the class name and method name from the stack trace
        testName.Should().Contain("GetCurrentTestName");
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesCorrectly()
    {
        // Arrange
        var disposableApiClient = new Mock<IApiClient>();
        disposableApiClient.As<IDisposable>();
        var baseApiTest = new TestableBaseApiTest(disposableApiClient.Object, _testConfiguration, _loggerMock.Object);

        // Act
        baseApiTest.Dispose();

        // Assert
        // Verify that Dispose was called at least once (it might be called multiple times due to ApiService disposal)
        disposableApiClient.As<IDisposable>().Verify(x => x.Dispose(), Times.AtLeastOnce);
        
        // 验证释放日志记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BaseApiTest 正在释放资源")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _baseApiTest.Dispose();
        var action = () => _baseApiTest.Dispose();
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _baseApiTest?.Dispose();
    }

    /// <summary>
    /// 可测试的 BaseApiTest 实现
    /// 暴露受保护的成员以便进行单元测试
    /// </summary>
    private class TestableBaseApiTest : BaseApiTest
    {
        public TestableBaseApiTest(IApiClient apiClient, TestConfiguration configuration, ILogger logger)
            : base(apiClient, configuration, logger)
        {
        }

        public IApiClient GetApiClient() => ApiClient;
        public TestConfiguration GetConfiguration() => Configuration;
        public ILogger GetLogger() => Logger;
        public ApiService GetApiService() => ApiService;

        public new async Task<ApiResponse<T>> ExecuteApiTestAsync<T>(ApiRequest request, string? testName = null)
        {
            return await base.ExecuteApiTestAsync<T>(request, testName);
        }

        public new async Task<ApiResponse> ExecuteApiTestAsync(ApiRequest request, string? testName = null)
        {
            return await base.ExecuteApiTestAsync(request, testName);
        }

        public new ValidationResult ValidateApiResponse(ApiResponse response, ApiValidation validation, string? testName = null)
        {
            return base.ValidateApiResponse(response, validation, testName);
        }

        public new ApiRequest CreateGetRequest(string endpoint, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null)
        {
            return base.CreateGetRequest(endpoint, queryParameters, headers);
        }

        public new ApiRequest CreatePostRequest(string endpoint, object? body = null, Dictionary<string, string>? headers = null)
        {
            return base.CreatePostRequest(endpoint, body, headers);
        }

        public new ApiRequest CreatePutRequest(string endpoint, object? body = null, Dictionary<string, string>? headers = null)
        {
            return base.CreatePutRequest(endpoint, body, headers);
        }

        public new ApiRequest CreateDeleteRequest(string endpoint, Dictionary<string, string>? headers = null)
        {
            return base.CreateDeleteRequest(endpoint, headers);
        }

        public new ApiValidation CreateBasicValidation(int expectedStatusCode = 200, TimeSpan? maxResponseTime = null)
        {
            return base.CreateBasicValidation(expectedStatusCode, maxResponseTime);
        }

        public new string GetCurrentTestName()
        {
            return base.GetCurrentTestName();
        }
    }
}