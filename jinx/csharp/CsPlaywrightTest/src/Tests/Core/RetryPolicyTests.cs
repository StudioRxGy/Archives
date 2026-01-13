using System.Net;
using System.Net.Sockets;
using EnterpriseAutomationFramework.Core.Utilities;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// RetryPolicy 单元测试
/// </summary>
public class RetryPolicyTests
{
    [Fact]
    public void RetryPolicy_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var policy = new RetryPolicy();
        
        // Assert
        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.DelayBetweenAttempts);
        Assert.False(policy.UseExponentialBackoff);
        Assert.Equal(2.0, policy.ExponentialBackoffMultiplier);
        Assert.Equal(TimeSpan.FromMinutes(1), policy.MaxDelay);
        Assert.NotEmpty(policy.RetryableExceptions);
        Assert.NotEmpty(policy.RetryableStatusCodes);
    }
    
    [Fact]
    public void RetryPolicy_DefaultConstructor_ShouldIncludeDefaultRetryableExceptions()
    {
        // Act
        var policy = new RetryPolicy();
        
        // Assert
        Assert.Contains(typeof(HttpRequestException), policy.RetryableExceptions);
        Assert.Contains(typeof(TaskCanceledException), policy.RetryableExceptions);
        Assert.Contains(typeof(TimeoutException), policy.RetryableExceptions);
        Assert.Contains(typeof(SocketException), policy.RetryableExceptions);
    }
    
    [Fact]
    public void RetryPolicy_DefaultConstructor_ShouldIncludeDefaultRetryableStatusCodes()
    {
        // Act
        var policy = new RetryPolicy();
        
        // Assert
        Assert.Contains(HttpStatusCode.RequestTimeout, policy.RetryableStatusCodes);
        Assert.Contains(HttpStatusCode.InternalServerError, policy.RetryableStatusCodes);
        Assert.Contains(HttpStatusCode.BadGateway, policy.RetryableStatusCodes);
        Assert.Contains(HttpStatusCode.ServiceUnavailable, policy.RetryableStatusCodes);
        Assert.Contains(HttpStatusCode.GatewayTimeout, policy.RetryableStatusCodes);
        Assert.Contains(HttpStatusCode.TooManyRequests, policy.RetryableStatusCodes);
    }
    
    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TaskCanceledException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(ArgumentException), false)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void ShouldRetry_WithDifferentExceptionTypes_ShouldReturnExpectedResult(Type exceptionType, bool expectedResult)
    {
        // Arrange
        var policy = new RetryPolicy();
        Exception exception;
        
        // Handle special cases for exception construction
        if (exceptionType == typeof(SocketException))
        {
            exception = new SocketException();
        }
        else
        {
            exception = (Exception)Activator.CreateInstance(exceptionType, "Test exception")!;
        }
        
        // Act
        var result = policy.ShouldRetry(exception);
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public void ShouldRetry_WithSocketException_ShouldReturnTrue()
    {
        // Arrange
        var policy = new RetryPolicy();
        var exception = new SocketException();
        
        // Act
        var result = policy.ShouldRetry(exception);
        
        // Assert
        Assert.True(result);
    }
    
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void ShouldRetry_WithDifferentStatusCodes_ShouldReturnExpectedResult(HttpStatusCode statusCode, bool expectedResult)
    {
        // Arrange
        var policy = new RetryPolicy();
        
        // Act
        var result = policy.ShouldRetry(statusCode);
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public void ShouldRetry_WithInnerException_ShouldCheckInnerException()
    {
        // Arrange
        var policy = new RetryPolicy();
        var innerException = new HttpRequestException("Inner exception");
        var outerException = new InvalidOperationException("Outer exception", innerException);
        
        // Act
        var result = policy.ShouldRetry(outerException);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void ShouldRetry_WithCustomRetryCondition_ShouldUseCustomCondition()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            RetryCondition = ex => ex.Message.Contains("retry")
        };
        var retryableException = new Exception("This should retry");
        var nonRetryableException = new Exception("This should not");
        
        // Act & Assert
        Assert.True(policy.ShouldRetry(retryableException));
        Assert.False(policy.ShouldRetry(nonRetryableException));
    }
    
    [Theory]
    [InlineData(0, 1000)] // First attempt
    [InlineData(1, 1000)] // Second attempt (no exponential backoff)
    [InlineData(2, 1000)] // Third attempt (no exponential backoff)
    public void CalculateDelay_WithoutExponentialBackoff_ShouldReturnFixedDelay(int attemptNumber, int expectedDelayMs)
    {
        // Arrange
        var policy = new RetryPolicy
        {
            DelayBetweenAttempts = TimeSpan.FromMilliseconds(1000),
            UseExponentialBackoff = false
        };
        
        // Act
        var delay = policy.CalculateDelay(attemptNumber);
        
        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(expectedDelayMs), delay);
    }
    
    [Theory]
    [InlineData(0, 1000)] // First attempt: 1000ms
    [InlineData(1, 2000)] // Second attempt: 1000 * 2^1 = 2000ms
    [InlineData(2, 4000)] // Third attempt: 1000 * 2^2 = 4000ms
    [InlineData(3, 8000)] // Fourth attempt: 1000 * 2^3 = 8000ms
    public void CalculateDelay_WithExponentialBackoff_ShouldReturnExponentialDelay(int attemptNumber, int expectedDelayMs)
    {
        // Arrange
        var policy = new RetryPolicy
        {
            DelayBetweenAttempts = TimeSpan.FromMilliseconds(1000),
            UseExponentialBackoff = true,
            ExponentialBackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromMinutes(1)
        };
        
        // Act
        var delay = policy.CalculateDelay(attemptNumber);
        
        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(expectedDelayMs), delay);
    }
    
    [Fact]
    public void CalculateDelay_WithExponentialBackoffExceedingMaxDelay_ShouldReturnMaxDelay()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            DelayBetweenAttempts = TimeSpan.FromSeconds(10),
            UseExponentialBackoff = true,
            ExponentialBackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(15)
        };
        
        // Act
        var delay = policy.CalculateDelay(2); // 10 * 2^2 = 40 seconds, should be capped at 15
        
        // Assert
        Assert.Equal(TimeSpan.FromSeconds(15), delay);
    }
    
    [Fact]
    public void CreateDefaultApiPolicy_ShouldReturnCorrectConfiguration()
    {
        // Act
        var policy = RetryPolicy.CreateDefaultApiPolicy();
        
        // Assert
        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.DelayBetweenAttempts);
        Assert.True(policy.UseExponentialBackoff);
        Assert.Equal(2.0, policy.ExponentialBackoffMultiplier);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
    }
    
    [Fact]
    public void CreateDefaultUiPolicy_ShouldReturnCorrectConfiguration()
    {
        // Act
        var policy = RetryPolicy.CreateDefaultUiPolicy();
        
        // Assert
        Assert.Equal(2, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.DelayBetweenAttempts);
        Assert.False(policy.UseExponentialBackoff);
        Assert.Contains(typeof(TimeoutException), policy.RetryableExceptions);
        Assert.Contains(typeof(InvalidOperationException), policy.RetryableExceptions);
    }
    
    [Fact]
    public void CreateCustomPolicy_ShouldReturnCorrectConfiguration()
    {
        // Arrange
        var maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(3);
        var retryableExceptions = new[] { typeof(HttpRequestException), typeof(TimeoutException) };
        
        // Act
        var policy = RetryPolicy.CreateCustomPolicy(maxAttempts, delay, retryableExceptions);
        
        // Assert
        Assert.Equal(maxAttempts, policy.MaxAttempts);
        Assert.Equal(delay, policy.DelayBetweenAttempts);
        Assert.Equal(retryableExceptions.Length, policy.RetryableExceptions.Count);
        Assert.Contains(typeof(HttpRequestException), policy.RetryableExceptions);
        Assert.Contains(typeof(TimeoutException), policy.RetryableExceptions);
    }
    
    [Fact]
    public void RetryPolicy_Properties_ShouldBeSettable()
    {
        // Arrange
        var policy = new RetryPolicy();
        
        // Act
        policy.MaxAttempts = 5;
        policy.DelayBetweenAttempts = TimeSpan.FromSeconds(2);
        policy.UseExponentialBackoff = true;
        policy.ExponentialBackoffMultiplier = 1.5;
        policy.MaxDelay = TimeSpan.FromSeconds(30);
        
        // Assert
        Assert.Equal(5, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.DelayBetweenAttempts);
        Assert.True(policy.UseExponentialBackoff);
        Assert.Equal(1.5, policy.ExponentialBackoffMultiplier);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
    }
}