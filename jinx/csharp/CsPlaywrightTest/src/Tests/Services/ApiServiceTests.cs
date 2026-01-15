using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Services.Api;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// ApiService 单元测试
/// </summary>
public class ApiServiceTests : IDisposable
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<ILogger<ApiService>> _loggerMock;
    private readonly Mock<IApiPerformanceMonitor> _performanceMonitorMock;
    private readonly ApiService _apiService;

    public ApiServiceTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _loggerMock = new Mock<ILogger<ApiService>>();
        _performanceMonitorMock = new Mock<IApiPerformanceMonitor>();
        _apiService = new ApiService(_apiClientMock.Object, _loggerMock.Object, _performanceMonitorMock.Object);
    }

    [Fact]
    public void Constructor_WithNullApiClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiService(null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiService(_apiClientMock.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var service = new ApiService(_apiClientMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SendRequestAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _apiService.SendRequestAsync<object>(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SendRequestAsync_WithGetRequest_ShouldCallGetAsync()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test",
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();
        response.RawContent.Should().Contain("success");
        
        _apiClientMock.Verify(x => x.GetAsync(request.BuildEndpointWithQuery(), request.Headers), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_WithPostRequest_ShouldCallPostAsync()
    {
        // Arrange
        var testData = new { name = "test", value = 123 };
        var request = new ApiRequest
        {
            Method = "POST",
            Endpoint = "/test",
            Body = testData,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":1}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(201);
        response.IsSuccess.Should().BeTrue();
        
        _apiClientMock.Verify(x => x.PostAsync(request.Endpoint, request.Body, request.Headers), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_WithPutRequest_ShouldCallPutAsync()
    {
        // Arrange
        var testData = new { id = 1, name = "updated" };
        var request = new ApiRequest
        {
            Method = "PUT",
            Endpoint = "/test/1",
            Body = testData
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":1,\"name\":\"updated\"}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();
        
        _apiClientMock.Verify(x => x.PutAsync(request.Endpoint, request.Body, request.Headers), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_WithDeleteRequest_ShouldCallDeleteAsync()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "DELETE",
            Endpoint = "/test/1"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _apiClientMock.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(204);
        response.IsSuccess.Should().BeTrue();
        
        _apiClientMock.Verify(x => x.DeleteAsync(request.Endpoint, request.Headers), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_WithUnsupportedMethod_ShouldThrowApiException()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "PATCH",
            Endpoint = "/test"
        };

        // Act & Assert
        var action = async () => await _apiService.SendRequestAsync<object>(request);
        await action.Should().ThrowAsync<ApiException>()
            .WithMessage("*不支持的HTTP方法: PATCH*");
    }

    [Fact]
    public async Task SendRequestAsync_WithStringResponseType_ShouldReturnStringData()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var responseContent = "Plain text response";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "text/plain")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<string>(request);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().Be(responseContent);
        response.RawContent.Should().Be(responseContent);
    }

    [Fact]
    public async Task SendRequestAsync_WithJsonResponse_ShouldDeserializeData()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var testObject = new { id = 1, name = "test", active = true };
        var jsonContent = System.Text.Json.JsonSerializer.Serialize(testObject);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<TestResponseModel>(request);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.Name.Should().Be("test");
        response.Data.Active.Should().BeTrue();
        response.RawContent.Should().Be(jsonContent);
    }

    // Helper class for testing JSON deserialization
    public class TestResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    [Fact]
    public async Task SendRequestAsync_WithInvalidJson_ShouldNotThrowButLogWarning()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var invalidJson = "{ invalid json }";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().BeNull(); // 反序列化失败时应该为null
        response.RawContent.Should().Be(invalidJson);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendRequestAsync_WithApiClientException_ShouldThrowApiException()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var originalException = new ApiException("/test", 500, "Internal server error");
        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(originalException);

        // Act & Assert
        var action = async () => await _apiService.SendRequestAsync<object>(request);
        await action.Should().ThrowAsync<ApiException>()
            .WithMessage("*API请求失败*");
    }

    [Fact]
    public async Task SendRequestAsync_WithHttpResponseHeaders_ShouldExtractHeaders()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        httpResponse.Headers.Add("X-Custom-Header", "custom-value");
        httpResponse.Headers.Add("X-Rate-Limit", "100");

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        response.Headers.Should().ContainKey("X-Custom-Header");
        response.Headers.Should().ContainKey("X-Rate-Limit");
        response.Headers["X-Custom-Header"].Should().Be("custom-value");
        response.Headers["X-Rate-Limit"].Should().Be("100");
    }

    [Fact]
    public async Task SendRequestAsync_NonGeneric_ShouldReturnNonGenericResponse()
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
        var response = await _apiService.SendRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Should().BeOfType<ApiResponse>();
        response.StatusCode.Should().Be(200);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateResponse_WithNullResponse_ShouldThrowArgumentNullException()
    {
        // Arrange
        var validation = new ApiValidation();

        // Act & Assert
        var action = () => _apiService.ValidateResponse(null!, validation);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("response");
    }

    [Fact]
    public void ValidateResponse_WithNullValidation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 200 };

        // Act & Assert
        var action = () => _apiService.ValidateResponse(response, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("validation");
    }

    [Fact]
    public void ValidateResponse_WithMatchingStatusCode_ShouldReturnValid()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 200 };
        var validation = new ApiValidation { ExpectedStatusCode = 200 };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithNonMatchingStatusCode_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 404 };
        var validation = new ApiValidation { ExpectedStatusCode = 200 };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("状态码验证失败");
    }

    [Fact]
    public void ValidateResponse_WithExceededResponseTime_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            ResponseTime = TimeSpan.FromSeconds(5) 
        };
        var validation = new ApiValidation 
        { 
            MaxResponseTime = TimeSpan.FromSeconds(2) 
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("响应时间验证失败");
    }

    [Fact]
    public void ValidateResponse_WithContentContains_ShouldValidateContent()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This is a test response with success message" 
        };
        var validation = new ApiValidation 
        { 
            ContentContains = "success" 
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithContentNotContains_ShouldValidateContent()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This is a test response with success message" 
        };
        var validation = new ApiValidation 
        { 
            ContentNotContains = "error" 
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithRequiredHeaders_ShouldValidateHeaders()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Custom-Header"] = "custom-value"
            }
        };
        var validation = new ApiValidation 
        { 
            RequiredHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Custom-Header"] = "custom-value"
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithMissingRequiredHeaders_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
        var validation = new ApiValidation 
        { 
            RequiredHeaders = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Authorization"] = "Bearer token"
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("缺少必需的响应头");
    }

    [Fact]
    public void Dispose_ShouldDisposeApiClient()
    {
        // Arrange
        var disposableApiClient = new Mock<IApiClient>();
        disposableApiClient.As<IDisposable>();
        var service = new ApiService(disposableApiClient.Object, _loggerMock.Object);

        // Act
        service.Dispose();

        // Assert
        disposableApiClient.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _apiService.Dispose();
        var action = () => _apiService.Dispose();
        action.Should().NotThrow();
    }

    #region Enhanced Validation Tests

    [Fact]
    public void ValidateResponse_WithMultipleExpectedStatusCodes_ShouldValidateCorrectly()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 201 };
        var validation = new ApiValidation 
        { 
            ExpectedStatusCodes = new List<int> { 200, 201, 202 } 
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithNonMatchingStatusCodes_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse { StatusCode = 404 };
        var validation = new ApiValidation 
        { 
            ExpectedStatusCodes = new List<int> { 200, 201, 202 } 
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("状态码验证失败");
        result.Errors[0].Should().Contain("[200, 201, 202]");
    }

    [Fact]
    public void ValidateResponse_WithMinResponseTime_ShouldValidateCorrectly()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            ResponseTime = TimeSpan.FromSeconds(2) 
        };
        var validation = new ApiValidation 
        { 
            MinResponseTime = TimeSpan.FromSeconds(1),
            MaxResponseTime = TimeSpan.FromSeconds(5)
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithTooFastResponse_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            ResponseTime = TimeSpan.FromMilliseconds(500) 
        };
        var validation = new ApiValidation 
        { 
            MinResponseTime = TimeSpan.FromSeconds(1)
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("响应时间验证失败");
        result.Errors[0].Should().Contain("期望大于");
    }

    [Fact]
    public void ValidateResponse_WithContentContainsList_ShouldValidateAllItems()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This response contains success and data keywords" 
        };
        var validation = new ApiValidation 
        { 
            ContentContainsList = new List<string> { "success", "data", "response" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithMissingContentFromList_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This response contains success keyword" 
        };
        var validation = new ApiValidation 
        { 
            ContentContainsList = new List<string> { "success", "missing", "response" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("内容包含验证失败");
        result.Errors[0].Should().Contain("missing");
    }

    [Fact]
    public void ValidateResponse_WithContentNotContainsList_ShouldValidateAllItems()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This response contains success message" 
        };
        var validation = new ApiValidation 
        { 
            ContentNotContainsList = new List<string> { "error", "failure", "exception" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithForbiddenContentFromList_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "This response contains an error message" 
        };
        var validation = new ApiValidation 
        { 
            ContentNotContainsList = new List<string> { "error", "failure", "exception" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("内容不包含验证失败");
        result.Errors[0].Should().Contain("error");
    }

    [Fact]
    public void ValidateResponse_WithValidRegex_ShouldValidateCorrectly()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "User ID: 12345, Status: Active" 
        };
        var validation = new ApiValidation 
        { 
            ContentRegex = @"User ID: \d+, Status: \w+"
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithNonMatchingRegex_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "User Name: John, Status: Active" 
        };
        var validation = new ApiValidation 
        { 
            ContentRegex = @"User ID: \d+, Status: \w+"
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("内容正则表达式验证失败");
    }

    [Fact]
    public void ValidateResponse_WithInvalidRegex_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200, 
            RawContent = "Some content" 
        };
        var validation = new ApiValidation 
        { 
            ContentRegex = @"[invalid regex pattern"
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("正则表达式格式错误");
    }

    [Fact]
    public void ValidateResponse_WithForbiddenHeaders_ShouldValidateCorrectly()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Custom-Header"] = "custom-value"
            }
        };
        var validation = new ApiValidation 
        { 
            ForbiddenHeaders = new List<string> { "X-Debug-Info", "X-Internal-Token" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithPresentForbiddenHeaders_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Debug-Info"] = "debug-data"
            }
        };
        var validation = new ApiValidation 
        { 
            ForbiddenHeaders = new List<string> { "X-Debug-Info", "X-Internal-Token" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("响应包含禁止的响应头");
        result.Errors[0].Should().Contain("X-Debug-Info");
    }

    [Fact]
    public void ValidateResponse_WithValidJsonPath_ShouldValidateCorrectly()
    {
        // Arrange
        var jsonResponse = @"{
            ""user"": {
                ""id"": 123,
                ""name"": ""John Doe"",
                ""active"": true,
                ""roles"": [""admin"", ""user""]
            },
            ""status"": ""success""
        }";
        
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = jsonResponse
        };
        
        var validation = new ApiValidation 
        { 
            JsonPathValidations = new List<JsonPathValidation>
            {
                new() { Path = "$.user.id", ExpectedValue = 123, ExpectedType = Newtonsoft.Json.Linq.JTokenType.Integer },
                new() { Path = "$.user.name", ExpectedValue = "John Doe", ExpectedType = Newtonsoft.Json.Linq.JTokenType.String },
                new() { Path = "$.user.active", ExpectedValue = true, ExpectedType = Newtonsoft.Json.Linq.JTokenType.Boolean },
                new() { Path = "$.user.roles", ExpectedArrayLength = 2, ExpectedType = Newtonsoft.Json.Linq.JTokenType.Array },
                new() { Path = "$.status", ValueRegex = @"^(success|failure)$" }
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithInvalidJsonPathValue_ShouldReturnInvalid()
    {
        // Arrange
        var jsonResponse = @"{
            ""user"": {
                ""id"": 456,
                ""name"": ""Jane Doe""
            }
        }";
        
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = jsonResponse
        };
        
        var validation = new ApiValidation 
        { 
            JsonPathValidations = new List<JsonPathValidation>
            {
                new() { Path = "$.user.id", ExpectedValue = 123 },
                new() { Path = "$.user.name", ExpectedValue = "John Doe" }
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Contains("$.user.id") && e.Contains("值不匹配"));
        result.Errors.Should().Contain(e => e.Contains("$.user.name") && e.Contains("值不匹配"));
    }

    [Fact]
    public void ValidateResponse_WithMissingRequiredJsonPath_ShouldReturnInvalid()
    {
        // Arrange
        var jsonResponse = @"{
            ""user"": {
                ""id"": 123
            }
        }";
        
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = jsonResponse
        };
        
        var validation = new ApiValidation 
        { 
            JsonPathValidations = new List<JsonPathValidation>
            {
                new() { Path = "$.user.name", IsRequired = true },
                new() { Path = "$.user.email", IsRequired = false }
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("$.user.name");
        result.Errors[0].Should().Contain("不存在");
    }

    [Fact]
    public void ValidateResponse_WithInvalidJson_ShouldReturnInvalidForJsonPath()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = "{ invalid json }"
        };
        
        var validation = new ApiValidation 
        { 
            JsonPathValidations = new List<JsonPathValidation>
            {
                new() { Path = "$.user.id", ExpectedValue = 123 }
            }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("不是有效的JSON格式");
    }

    [Fact]
    public void ValidateResponse_WithJsonSchema_ShouldValidateJsonFormat()
    {
        // Arrange
        var jsonResponse = @"{""id"": 123, ""name"": ""test""}";
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = jsonResponse
        };
        
        var validation = new ApiValidation 
        { 
            JsonSchema = "basic-schema" // 基础实现只验证JSON格式
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateResponse_WithInvalidJsonForSchema_ShouldReturnInvalid()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 200,
            RawContent = "{ invalid json }"
        };
        
        var validation = new ApiValidation 
        { 
            JsonSchema = "basic-schema"
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("JSON Schema验证失败");
    }

    [Fact]
    public void ValidateResponse_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var response = new ApiResponse 
        { 
            StatusCode = 404,
            ResponseTime = TimeSpan.FromSeconds(10),
            RawContent = "Error: Not found with failure message",
            Headers = new Dictionary<string, string>()
        };
        
        var validation = new ApiValidation 
        { 
            ExpectedStatusCode = 200,
            MaxResponseTime = TimeSpan.FromSeconds(5),
            ContentNotContains = "Error",
            RequiredHeaders = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
        };

        // Act
        var result = _apiService.ValidateResponse(response, validation);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.Contains("状态码验证失败"));
        result.Errors.Should().Contain(e => e.Contains("响应时间验证失败"));
        result.Errors.Should().Contain(e => e.Contains("内容不包含验证失败"));
        result.Errors.Should().Contain(e => e.Contains("响应头验证失败"));
    }

    #endregion

    #region Performance Monitoring Tests

    [Fact]
    public async Task SendRequestAsync_WithPerformanceMonitor_ShouldRecordMetrics()
    {
        // Arrange
        var request = new ApiRequest
        {
            Method = "GET",
            Endpoint = "/test",
            Body = new { test = "data" }
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _apiClientMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(httpResponse);

        // Act
        var response = await _apiService.SendRequestAsync<object>(request);

        // Assert
        response.Should().NotBeNull();
        _performanceMonitorMock.Verify(x => x.RecordMetric(
            request.Endpoint,
            request.Method,
            It.IsAny<TimeSpan>(),
            200,
            It.IsAny<long>(),
            It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public void GetPerformanceStatistics_ShouldCallPerformanceMonitor()
    {
        // Arrange
        var endpoint = "/test";
        var method = "GET";
        var expectedStats = new ApiPerformanceStatistics { EndpointKey = "GET /test" };
        
        _performanceMonitorMock.Setup(x => x.GetStatistics(endpoint, method))
            .Returns(expectedStats);

        // Act
        var result = _apiService.GetPerformanceStatistics(endpoint, method);

        // Assert
        result.Should().Be(expectedStats);
        _performanceMonitorMock.Verify(x => x.GetStatistics(endpoint, method), Times.Once);
    }

    [Fact]
    public void GetAllPerformanceStatistics_ShouldCallPerformanceMonitor()
    {
        // Arrange
        var expectedStats = new List<ApiPerformanceStatistics>
        {
            new() { EndpointKey = "GET /test1" },
            new() { EndpointKey = "POST /test2" }
        };
        
        _performanceMonitorMock.Setup(x => x.GetAllStatistics())
            .Returns(expectedStats);

        // Act
        var result = _apiService.GetAllPerformanceStatistics();

        // Assert
        result.Should().BeEquivalentTo(expectedStats);
        _performanceMonitorMock.Verify(x => x.GetAllStatistics(), Times.Once);
    }

    [Fact]
    public void GetPerformanceReport_ShouldCallPerformanceMonitor()
    {
        // Arrange
        var timeRange = 12;
        var expectedReport = new ApiPerformanceReport 
        { 
            GeneratedAt = DateTime.UtcNow,
            TimeRangeHours = timeRange,
            TotalRequests = 100
        };
        
        _performanceMonitorMock.Setup(x => x.GetPerformanceReport(timeRange))
            .Returns(expectedReport);

        // Act
        var result = _apiService.GetPerformanceReport(timeRange);

        // Assert
        result.Should().Be(expectedReport);
        _performanceMonitorMock.Verify(x => x.GetPerformanceReport(timeRange), Times.Once);
    }

    [Fact]
    public void ClearPerformanceMetrics_ShouldCallPerformanceMonitor()
    {
        // Arrange
        var endpoint = "/test";
        var method = "GET";

        // Act
        _apiService.ClearPerformanceMetrics(endpoint, method);

        // Assert
        _performanceMonitorMock.Verify(x => x.ClearMetrics(endpoint, method), Times.Once);
    }

    [Fact]
    public void ClearAllPerformanceMetrics_ShouldCallPerformanceMonitor()
    {
        // Act
        _apiService.ClearAllPerformanceMetrics();

        // Assert
        _performanceMonitorMock.Verify(x => x.ClearAllMetrics(), Times.Once);
    }

    #endregion

    public void Dispose()
    {
        _apiService?.Dispose();
    }
}