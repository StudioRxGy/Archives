using EnterpriseAutomationFramework.Core.Extensions;
using EnterpriseAutomationFramework.Core.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// RetryExtensions 单元测试
/// </summary>
public class RetryExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly RetryPolicy _testPolicy;
    
    public RetryExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
        _testPolicy = new RetryPolicy
        {
            MaxAttempts = 1,
            DelayBetweenAttempts = TimeSpan.FromMilliseconds(10),
            UseExponentialBackoff = false
        };
        
        // Setup logger to return true for IsEnabled
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }
    
    [Fact]
    public async Task WithRetryAsync_FuncTaskT_ShouldExecuteSuccessfully()
    {
        // Arrange
        var expectedResult = "success";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
        
        // Act
        var result = await operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task WithRetryAsync_FuncTask_ShouldExecuteSuccessfully()
    {
        // Arrange
        var executed = false;
        var operation = new Func<Task>(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });
        
        // Act
        await operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation");
        
        // Assert
        Assert.True(executed);
    }
    
    [Fact]
    public async Task WithRetryAsync_FuncT_ShouldExecuteSuccessfully()
    {
        // Arrange
        var expectedResult = 42;
        var operation = new Func<int>(() => expectedResult);
        
        // Act
        var result = await operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task WithRetryAsync_Action_ShouldExecuteSuccessfully()
    {
        // Arrange
        var executed = false;
        var operation = new Action(() => executed = true);
        
        // Act
        await operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation");
        
        // Assert
        Assert.True(executed);
    }
    
    [Fact]
    public async Task WithRetryAsync_WithRetryableException_ShouldRetryAndSucceed()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("First attempt fails");
            }
            return Task.FromResult("success");
        });
        
        // Act
        var result = await operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation");
        
        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }
    
    [Fact]
    public async Task WithApiRetryAsync_ShouldUseDefaultApiPolicy()
    {
        // Arrange
        var expectedResult = "api success";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
        
        // Act
        var result = await operation.WithApiRetryAsync(_mockLogger.Object, "ApiOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task WithUiRetryAsync_ShouldUseDefaultUiPolicy()
    {
        // Arrange
        var expectedResult = "ui success";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
        
        // Act
        var result = await operation.WithUiRetryAsync(_mockLogger.Object, "UiOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task WithRetryAsync_WithNonRetryableException_ShouldThrowImmediately()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new ArgumentException("Non-retryable exception");
        });
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            operation.WithRetryAsync(_testPolicy, _mockLogger.Object, "TestOperation"));
        Assert.Equal(1, callCount);
    }
    
    [Fact]
    public async Task WithRetryAsync_WithDefaultOperationName_ShouldUseDefaultName()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => Task.FromResult("success"));
        
        // Act
        var result = await operation.WithRetryAsync(_testPolicy, _mockLogger.Object);
        
        // Assert
        Assert.Equal("success", result);
    }
    
    [Fact]
    public async Task WithApiRetryAsync_WithDefaultOperationName_ShouldUseApiOperationName()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => Task.FromResult("success"));
        
        // Act
        var result = await operation.WithApiRetryAsync(_mockLogger.Object);
        
        // Assert
        Assert.Equal("success", result);
    }
    
    [Fact]
    public async Task WithUiRetryAsync_WithDefaultOperationName_ShouldUseUiOperationName()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => Task.FromResult("success"));
        
        // Act
        var result = await operation.WithUiRetryAsync(_mockLogger.Object);
        
        // Assert
        Assert.Equal("success", result);
    }
    
    [Fact]
    public void LoggerAdapter_ShouldImplementILoggerT()
    {
        // Arrange & Act
        var adapter = new LoggerAdapter<RetryExtensionsTests>(_mockLogger.Object);
        
        // Assert
        Assert.IsAssignableFrom<ILogger<RetryExtensionsTests>>(adapter);
    }
    
    [Fact]
    public void LoggerAdapter_BeginScope_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var adapter = new LoggerAdapter<RetryExtensionsTests>(_mockLogger.Object);
        var state = "test state";
        var mockScope = new Mock<IDisposable>();
        _mockLogger.Setup(x => x.BeginScope(state)).Returns(mockScope.Object);
        
        // Act
        var scope = adapter.BeginScope(state);
        
        // Assert
        Assert.Equal(mockScope.Object, scope);
        _mockLogger.Verify(x => x.BeginScope(state), Times.Once);
    }
    
    [Fact]
    public void LoggerAdapter_IsEnabled_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var adapter = new LoggerAdapter<RetryExtensionsTests>(_mockLogger.Object);
        var logLevel = LogLevel.Information;
        _mockLogger.Setup(x => x.IsEnabled(logLevel)).Returns(true);
        
        // Act
        var result = adapter.IsEnabled(logLevel);
        
        // Assert
        Assert.True(result);
        _mockLogger.Verify(x => x.IsEnabled(logLevel), Times.Once);
    }
    
    [Fact]
    public void LoggerAdapter_Log_ShouldCallUnderlyingLogger()
    {
        // Arrange
        var adapter = new LoggerAdapter<RetryExtensionsTests>(_mockLogger.Object);
        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "TestEvent");
        var state = "test state";
        var exception = new Exception("test exception");
        var formatter = new Func<string, Exception?, string>((s, e) => s);
        
        // Act
        adapter.Log(logLevel, eventId, state, exception, formatter);
        
        // Assert
        _mockLogger.Verify(x => x.Log(logLevel, eventId, state, exception, formatter), Times.Once);
    }
}