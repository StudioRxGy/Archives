using EnterpriseAutomationFramework.Core.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// RetryExecutor 单元测试
/// </summary>
public class RetryExecutorTests
{
    private readonly Mock<ILogger<RetryExecutor>> _mockLogger;
    private readonly RetryPolicy _testPolicy;
    private readonly RetryExecutor _executor;
    
    public RetryExecutorTests()
    {
        _mockLogger = new Mock<ILogger<RetryExecutor>>();
        _testPolicy = new RetryPolicy
        {
            MaxAttempts = 2,
            DelayBetweenAttempts = TimeSpan.FromMilliseconds(10),
            UseExponentialBackoff = false
        };
        _executor = new RetryExecutor(_testPolicy, _mockLogger.Object);
    }
    
    [Fact]
    public void Constructor_WithNullPolicy_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RetryExecutor(null!, _mockLogger.Object));
    }
    
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RetryExecutor(_testPolicy, null!));
    }
    
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));
        
        // Act
        var result = await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperationAfterRetry_ShouldReturnResult()
    {
        // Arrange
        var callCount = 0;
        var expectedResult = "success";
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new HttpRequestException("First attempt fails");
            }
            return Task.FromResult(expectedResult);
        });
        
        // Act
        var result = await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(2, callCount);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithNonRetryableException_ShouldThrowImmediately()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new ArgumentException("Non-retryable exception");
        });
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _executor.ExecuteAsync(operation, "TestOperation"));
        Assert.Equal(1, callCount);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithRetryableExceptionExceedingMaxAttempts_ShouldThrowAfterAllAttempts()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new HttpRequestException("Always fails");
        });
        
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _executor.ExecuteAsync(operation, "TestOperation"));
        Assert.Equal(3, callCount); // MaxAttempts + 1
    }
    
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Func<Task<string>>? nullOperation = null;
        await Assert.ThrowsAsync<ArgumentNullException>(() => _executor.ExecuteAsync(nullOperation!, "TestOperation"));
    }
    
    [Fact]
    public async Task ExecuteAsync_VoidOperation_ShouldExecuteSuccessfully()
    {
        // Arrange
        var executed = false;
        Func<Task> operation = () =>
        {
            executed = true;
            return Task.CompletedTask;
        };
        
        // Act
        await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        Assert.True(executed);
    }
    
    [Fact]
    public async Task ExecuteAsync_SyncOperation_ShouldExecuteSuccessfully()
    {
        // Arrange
        var expectedResult = 42;
        var operation = new Func<int>(() => expectedResult);
        
        // Act
        var result = await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Fact]
    public async Task ExecuteAsync_SyncVoidOperation_ShouldExecuteSuccessfully()
    {
        // Arrange
        var executed = false;
        var operation = new Action(() => executed = true);
        
        // Act
        await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        Assert.True(executed);
    }
    
    [Fact]
    public void Create_ShouldReturnRetryExecutor()
    {
        // Act
        var executor = RetryExecutor.Create(_testPolicy, _mockLogger.Object);
        
        // Assert
        Assert.NotNull(executor);
        Assert.IsType<RetryExecutor>(executor);
    }
    
    [Fact]
    public void CreateForApi_ShouldReturnRetryExecutorWithApiPolicy()
    {
        // Act
        var executor = RetryExecutor.CreateForApi(_mockLogger.Object);
        
        // Assert
        Assert.NotNull(executor);
        Assert.IsType<RetryExecutor>(executor);
    }
    
    [Fact]
    public void CreateForUi_ShouldReturnRetryExecutorWithUiPolicy()
    {
        // Act
        var executor = RetryExecutor.CreateForUi(_mockLogger.Object);
        
        // Assert
        Assert.NotNull(executor);
        Assert.IsType<RetryExecutor>(executor);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldLogDebugForEachAttempt()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount <= 2)
            {
                throw new HttpRequestException("Retry attempt");
            }
            return Task.FromResult("success");
        });
        
        // Act
        await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("执行操作 'TestOperation' - 尝试")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }
    
    [Fact]
    public async Task ExecuteAsync_WithRetrySuccess_ShouldLogInformation()
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
        await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("操作 'TestOperation' 在第 2 次尝试后成功")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task ExecuteAsync_WithRetryFailure_ShouldLogWarning()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount <= 2)
            {
                throw new HttpRequestException("Retry attempt");
            }
            return Task.FromResult("success");
        });
        
        // Act
        await _executor.ExecuteAsync(operation, "TestOperation");
        
        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("操作 'TestOperation' 失败，将在")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }
    
    [Fact]
    public async Task ExecuteAsync_WithFinalFailure_ShouldLogError()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => throw new HttpRequestException("Always fails"));
        
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _executor.ExecuteAsync(operation, "TestOperation"));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("操作 'TestOperation' 最终失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}