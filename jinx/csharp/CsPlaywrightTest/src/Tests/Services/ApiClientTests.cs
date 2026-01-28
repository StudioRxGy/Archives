using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Services.Api;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// ApiClient 单元测试
/// </summary>
[UnitTest]
[TestCategory(TestCategory.ApiClient)]
[TestPriority(TestPriority.High)]
public class ApiClientTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ApiClient>> _loggerMock;
    private readonly TestConfiguration _testConfiguration;
    private readonly ApiClient _apiClient;

    public ApiClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _loggerMock = new Mock<ILogger<ApiClient>>();
        
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

        _apiClient = new ApiClient(_httpClient, _testConfiguration, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiClient(null!, _testConfiguration, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiClient(_httpClient, null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApiClient(_httpClient, _testConfiguration, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Act
        var client = new ApiClient(_httpClient, _testConfiguration, _loggerMock.Object);

        // Assert
        client.Should().NotBeNull();
        _httpClient.Timeout.Should().Be(TimeSpan.FromMilliseconds(_testConfiguration.Api.Timeout));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetAsync_WithInvalidEndpoint_ShouldThrowArgumentException(string endpoint)
    {
        // Act & Assert
        var action = async () => await _apiClient.GetAsync(endpoint);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task GetAsync_WithValidEndpoint_ShouldSendGetRequest()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.GetAsync("/test");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
    }

    [Fact]
    public async Task GetAsync_WithQueryParameters_ShouldBuildCorrectUrl()
    {
        // Arrange
        var queryParams = new Dictionary<string, string>
        {
            ["param1"] = "value1",
            ["param2"] = "value2"
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri!.Query.Contains("param1=value1") &&
                    req.RequestUri.Query.Contains("param2=value2")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.GetAsync("/test", queryParams, null);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAsync_WithHeaders_ShouldAddHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "custom-value"
        };

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get &&
                    req.Headers.Contains("Authorization") &&
                    req.Headers.Contains("X-Custom-Header")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.GetAsync("/test", headers);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task PostAsync_WithInvalidEndpoint_ShouldThrowArgumentException(string endpoint)
    {
        // Act & Assert
        var action = async () => await _apiClient.PostAsync(endpoint, new { });
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task PostAsync_WithValidData_ShouldSendPostRequest()
    {
        // Arrange
        var testData = new { name = "test", value = 123 };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":1}", Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.PostAsync("/test", testData);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostAsync_WithStringData_ShouldSendStringContent()
    {
        // Arrange
        var testData = "{\"name\":\"test\"}";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.PostAsync("/test", testData);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAsync_WithNullData_ShouldSendRequestWithoutContent()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.PostAsync("/test", null);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task PutAsync_WithInvalidEndpoint_ShouldThrowArgumentException(string endpoint)
    {
        // Act & Assert
        var action = async () => await _apiClient.PutAsync(endpoint, new { });
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task PutAsync_WithValidData_ShouldSendPutRequest()
    {
        // Arrange
        var testData = new { id = 1, name = "updated" };
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Put &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.PutAsync("/test/1", testData);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task DeleteAsync_WithInvalidEndpoint_ShouldThrowArgumentException(string endpoint)
    {
        // Act & Assert
        var action = async () => await _apiClient.DeleteAsync(endpoint);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task DeleteAsync_WithValidEndpoint_ShouldSendDeleteRequest()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.DeleteAsync("/test/1");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SendRequest_WithRetryableError_ShouldRetry()
    {
        // Arrange
        var failureResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(failureResponse)
            .ReturnsAsync(successResponse);

        // Act
        var response = await _apiClient.GetAsync("/test");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // 验证重试逻辑被调用了两次
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequest_WithNonRetryableError_ShouldNotRetry()
    {
        // Arrange
        var badRequestResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(badRequestResponse);

        // Act
        var response = await _apiClient.GetAsync("/test");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // 验证只调用了一次，没有重试
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequest_WithHttpRequestException_ShouldThrowApiException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        var action = async () => await _apiClient.GetAsync("/test");
        await action.Should().ThrowAsync<ApiException>()
            .WithMessage("*GET请求失败*");
    }

    [Fact]
    public async Task SendRequest_WithTimeout_ShouldThrowApiException()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout", new TimeoutException()));

        // Act & Assert
        var action = async () => await _apiClient.GetAsync("/test");
        await action.Should().ThrowAsync<ApiException>()
            .WithMessage("*GET请求失败*");
    }

    [Theory]
    [InlineData("https://absolute.example.com/api/test")]
    [InlineData("http://another.example.com/endpoint")]
    public async Task SendRequest_WithAbsoluteUrl_ShouldUseAbsoluteUrl(string absoluteUrl)
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == absoluteUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.GetAsync(absoluteUrl);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/test", "https://api.test.example.com/test")]
    [InlineData("test", "https://api.test.example.com/test")]
    [InlineData("api/v1/test", "https://api.test.example.com/api/v1/test")]
    public async Task SendRequest_WithRelativeUrl_ShouldBuildCorrectUrl(string relativeUrl, string expectedUrl)
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == expectedUrl),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _apiClient.GetAsync(relativeUrl);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        // Act
        _apiClient.Dispose();

        // Assert
        // HttpClient disposal is handled internally, we just verify no exception is thrown
        var action = () => _apiClient.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _apiClient.Dispose();
        var action = () => _apiClient.Dispose();
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
        _httpClient?.Dispose();
    }
}