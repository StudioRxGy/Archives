using EnterpriseAutomationFramework.Core.Exceptions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// RetryableException 单元测试
/// </summary>
public class RetryableExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetProperties()
    {
        // Arrange
        var message = "Test exception message";
        
        // Act
        var exception = new RetryableException(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.True(exception.IsRetryable);
        Assert.Equal(0, exception.RetryCount);
    }
    
    [Fact]
    public void Constructor_WithMessageAndRetryable_ShouldSetProperties()
    {
        // Arrange
        var message = "Test exception message";
        var isRetryable = false;
        var retryCount = 3;
        
        // Act
        var exception = new RetryableException(message, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new ArgumentException("Inner exception");
        var isRetryable = true;
        var retryCount = 2;
        
        // Act
        var exception = new RetryableException(message, innerException, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void Constructor_WithTestNameAndComponent_ShouldSetProperties()
    {
        // Arrange
        var testName = "TestMethod";
        var component = "TestComponent";
        var message = "Test exception message";
        var isRetryable = false;
        var retryCount = 1;
        
        // Act
        var exception = new RetryableException(testName, component, message, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(testName, exception.TestName);
        Assert.Equal(component, exception.Component);
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void Constructor_WithAllParameters_ShouldSetProperties()
    {
        // Arrange
        var testName = "TestMethod";
        var component = "TestComponent";
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");
        var isRetryable = true;
        var retryCount = 5;
        
        // Act
        var exception = new RetryableException(testName, component, message, innerException, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(testName, exception.TestName);
        Assert.Equal(component, exception.Component);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void CreateRetryable_ShouldCreateRetryableException()
    {
        // Arrange
        var message = "Retryable exception";
        var retryCount = 3;
        
        // Act
        var exception = RetryableException.CreateRetryable(message, retryCount);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.True(exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void CreateRetryable_WithDefaultRetryCount_ShouldCreateRetryableException()
    {
        // Arrange
        var message = "Retryable exception";
        
        // Act
        var exception = RetryableException.CreateRetryable(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.True(exception.IsRetryable);
        Assert.Equal(0, exception.RetryCount);
    }
    
    [Fact]
    public void CreateNonRetryable_ShouldCreateNonRetryableException()
    {
        // Arrange
        var message = "Non-retryable exception";
        
        // Act
        var exception = RetryableException.CreateNonRetryable(message);
        
        // Assert
        Assert.Equal(message, exception.Message);
        Assert.False(exception.IsRetryable);
        Assert.Equal(0, exception.RetryCount);
    }
    
    [Fact]
    public void FromException_ShouldCreateRetryableExceptionFromExistingException()
    {
        // Arrange
        var originalException = new HttpRequestException("Original exception");
        var isRetryable = true;
        var retryCount = 2;
        
        // Act
        var exception = RetryableException.FromException(originalException, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(originalException.Message, exception.Message);
        Assert.Equal(originalException, exception.InnerException);
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void FromException_WithDefaultParameters_ShouldCreateRetryableException()
    {
        // Arrange
        var originalException = new TimeoutException("Timeout occurred");
        
        // Act
        var exception = RetryableException.FromException(originalException);
        
        // Assert
        Assert.Equal(originalException.Message, exception.Message);
        Assert.Equal(originalException, exception.InnerException);
        Assert.True(exception.IsRetryable);
        Assert.Equal(0, exception.RetryCount);
    }
    
    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 5)]
    [InlineData(false, 0)]
    [InlineData(false, 3)]
    public void Constructor_WithDifferentRetryableAndCountValues_ShouldSetCorrectly(bool isRetryable, int retryCount)
    {
        // Arrange
        var message = "Test message";
        
        // Act
        var exception = new RetryableException(message, isRetryable, retryCount);
        
        // Assert
        Assert.Equal(isRetryable, exception.IsRetryable);
        Assert.Equal(retryCount, exception.RetryCount);
    }
    
    [Fact]
    public void RetryableException_ShouldInheritFromTestFrameworkException()
    {
        // Arrange & Act
        var exception = new RetryableException("Test message");
        
        // Assert
        Assert.IsAssignableFrom<TestFrameworkException>(exception);
    }
}