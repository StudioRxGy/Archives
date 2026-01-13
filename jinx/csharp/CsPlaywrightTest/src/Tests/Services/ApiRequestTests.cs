using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Services.Api;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// ApiRequest 单元测试
/// </summary>
public class ApiRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new ApiRequest();

        // Assert
        request.Method.Should().Be("GET");
        request.Endpoint.Should().Be(string.Empty);
        request.Body.Should().BeNull();
        request.Headers.Should().NotBeNull().And.BeEmpty();
        request.QueryParameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void BuildEndpointWithQuery_WithNoQueryParameters_ShouldReturnOriginalEndpoint()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test"
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Be("/api/test");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithEmptyQueryParameters_ShouldReturnOriginalEndpoint()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>()
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Be("/api/test");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithSingleQueryParameter_ShouldAppendParameter()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param1"] = "value1"
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Be("/api/test?param1=value1");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithMultipleQueryParameters_ShouldAppendAllParameters()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param1"] = "value1",
                ["param2"] = "value2",
                ["param3"] = "value3"
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Contain("param1=value1");
        result.Should().Contain("param2=value2");
        result.Should().Contain("param3=value3");
        result.Should().StartWith("/api/test?");
        result.Should().Contain("&");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithExistingQueryInEndpoint_ShouldAppendWithAmpersand()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test?existing=param",
            QueryParameters = new Dictionary<string, string>
            {
                ["new"] = "value"
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Be("/api/test?existing=param&new=value");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithSpecialCharacters_ShouldUrlEncodeValues()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param with spaces"] = "value with spaces",
                ["param&special"] = "value&special=chars"
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Contain("param%20with%20spaces=value%20with%20spaces");
        result.Should().Contain("param%26special=value%26special%3Dchars");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithNullValue_ShouldHandleNullValue()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param1"] = "value1",
                ["param2"] = null!
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Contain("param1=value1");
        result.Should().Contain("param2=");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithEmptyKey_ShouldSkipEmptyKey()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param1"] = "value1",
                [""] = "empty_key_value",
                [" "] = "whitespace_key_value"
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Contain("param1=value1");
        result.Should().NotContain("empty_key_value");
        result.Should().NotContain("whitespace_key_value");
    }

    [Fact]
    public void BuildEndpointWithQuery_WithEmptyValue_ShouldIncludeEmptyValue()
    {
        // Arrange
        var request = new ApiRequest
        {
            Endpoint = "/api/test",
            QueryParameters = new Dictionary<string, string>
            {
                ["param1"] = "value1",
                ["param2"] = ""
            }
        };

        // Act
        var result = request.BuildEndpointWithQuery();

        // Assert
        result.Should().Contain("param1=value1");
        result.Should().Contain("param2=");
    }
}

/// <summary>
/// ApiResponse 单元测试
/// </summary>
public class ApiResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new ApiResponse<string>();

        // Assert
        response.StatusCode.Should().Be(0);
        response.Data.Should().BeNull();
        response.RawContent.Should().Be(string.Empty);
        response.Headers.Should().NotBeNull().And.BeEmpty();
        response.ResponseTime.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, true)]
    [InlineData(204, true)]
    [InlineData(299, true)]
    [InlineData(199, false)]
    [InlineData(300, false)]
    [InlineData(400, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public void IsSuccess_ShouldReturnCorrectValue(int statusCode, bool expectedIsSuccess)
    {
        // Arrange
        var response = new ApiResponse<string>
        {
            StatusCode = statusCode
        };

        // Act
        var isSuccess = response.IsSuccess;

        // Assert
        isSuccess.Should().Be(expectedIsSuccess);
    }

    [Fact]
    public void ApiResponse_NonGeneric_ShouldInheritFromGeneric()
    {
        // Act
        var response = new ApiResponse();

        // Assert
        response.Should().BeAssignableTo<ApiResponse<object>>();
        response.StatusCode.Should().Be(0);
        response.Data.Should().BeNull();
        response.RawContent.Should().Be(string.Empty);
        response.Headers.Should().NotBeNull().And.BeEmpty();
        response.ResponseTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ApiResponse_WithData_ShouldStoreData()
    {
        // Arrange
        var testData = new { id = 1, name = "test" };

        // Act
        var response = new ApiResponse<object>
        {
            StatusCode = 200,
            Data = testData,
            RawContent = "{\"id\":1,\"name\":\"test\"}",
            ResponseTime = TimeSpan.FromMilliseconds(150)
        };

        // Assert
        response.StatusCode.Should().Be(200);
        response.Data.Should().Be(testData);
        response.RawContent.Should().Be("{\"id\":1,\"name\":\"test\"}");
        response.ResponseTime.Should().Be(TimeSpan.FromMilliseconds(150));
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ApiResponse_WithHeaders_ShouldStoreHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Custom-Header"] = "custom-value"
        };

        // Act
        var response = new ApiResponse<string>
        {
            Headers = headers
        };

        // Assert
        response.Headers.Should().BeEquivalentTo(headers);
        response.Headers["Content-Type"].Should().Be("application/json");
        response.Headers["X-Custom-Header"].Should().Be("custom-value");
    }
}