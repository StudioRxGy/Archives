using EnterpriseAutomationFramework.Core.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// 测试上下文日志记录器测试类
/// </summary>
public class TestContextLoggerTests : IDisposable
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly List<TestContextLogger> _loggers;

    public TestContextLoggerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _loggers = new List<TestContextLogger>();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var testName = "TestMethod_ShouldDoSomething";
        var testClass = "TestClass";
        var testMethod = "TestMethod";

        // Act
        var logger = new TestContextLogger(testName, testClass, testMethod, _mockLogger.Object);
        _loggers.Add(logger);

        // Assert
        logger.Should().NotBeNull();
        
        // Verify that test start was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("测试开始")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithMinimalParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var testName = "SimpleTest";

        // Act
        var logger = new TestContextLogger(testName, logger: _mockLogger.Object);
        _loggers.Add(logger);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void LogStep_WithStepName_ShouldLogInformation()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var stepName = "Navigate to homepage";

        // Act
        logger.LogStep(stepName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("执行步骤") && v.ToString()!.Contains(stepName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogStep_WithStepNameAndDescription_ShouldLogInformationWithDescription()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var stepName = "Click button";
        var description = "Click the submit button";

        // Act
        logger.LogStep(stepName, description);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("执行步骤") && 
                                              v.ToString()!.Contains(stepName) && 
                                              v.ToString()!.Contains(description)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogTestData_WithDataNameAndValue_ShouldLogDebug()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var dataName = "Username";
        var dataValue = "testuser@example.com";

        // Act
        logger.LogTestData(dataName, dataValue);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("测试数据") && 
                                              v.ToString()!.Contains(dataName) && 
                                              v.ToString()!.Contains(dataValue.ToString()!)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogAssertion_WithResult_ShouldLogAppropriateLevel(bool result, LogLevel expectedLevel)
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var assertion = "Element should be visible";

        // Act
        logger.LogAssertion(assertion, result);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                expectedLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("断言") && v.ToString()!.Contains(assertion)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithException_ShouldLogError()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.LogError(exception);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("测试执行出错")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithExceptionAndMessage_ShouldLogErrorWithMessage()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var exception = new InvalidOperationException("Test exception");
        var message = "Custom error message";

        // Act
        logger.LogError(exception, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("测试执行出错") && v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarning_WithMessage_ShouldLogWarning()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var message = "This is a warning";

        // Act
        logger.LogWarning(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("测试警告") && v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, LogLevel.Information)]
    [InlineData(false, LogLevel.Error)]
    public void LogTestComplete_WithSuccess_ShouldLogAppropriateLevel(bool success, LogLevel expectedLevel)
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);

        // Act
        logger.LogTestComplete(success);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                expectedLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(success ? "测试完成" : "测试失败") && 
                                              v.ToString()!.Contains("耗时")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformanceMetric_WithMetric_ShouldLogInformation()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var metricName = "ResponseTime";
        var value = 250.5;
        var unit = "ms";

        // Act
        logger.LogPerformanceMetric(metricName, value, unit);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("性能指标") && 
                                              v.ToString()!.Contains(metricName) && 
                                              v.ToString()!.Contains(value.ToString()) && 
                                              v.ToString()!.Contains(unit)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogScreenshot_WithPath_ShouldLogInformation()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var screenshotPath = "/path/to/screenshot.png";

        // Act
        logger.LogScreenshot(screenshotPath);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("截图保存") && v.ToString()!.Contains(screenshotPath)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogScreenshot_WithPathAndDescription_ShouldLogInformationWithDescription()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);
        _loggers.Add(logger);
        var screenshotPath = "/path/to/screenshot.png";
        var description = "Error state screenshot";

        // Act
        logger.LogScreenshot(screenshotPath, description);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("截图保存") && 
                                              v.ToString()!.Contains(screenshotPath) && 
                                              v.ToString()!.Contains(description)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var logger = new TestContextLogger("TestMethod", logger: _mockLogger.Object);

        // Act & Assert
        var action = () => logger.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void CreateTestLogger_WithParameters_ShouldReturnValidLogger()
    {
        // Arrange
        var testName = "TestMethod";
        var testClass = "TestClass";
        var testMethod = "TestMethod";

        // Act
        var logger = TestContextLoggerExtensions.CreateTestLogger(testName, testClass, testMethod);
        _loggers.Add(logger);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<TestContextLogger>();
    }

    [Fact]
    public void CreateTestLogger_WithType_ShouldReturnValidLogger()
    {
        // Arrange
        var testType = typeof(TestContextLoggerTests);
        var testMethodName = "TestMethod";

        // Act
        var logger = TestContextLoggerExtensions.CreateTestLogger(testType, testMethodName);
        _loggers.Add(logger);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<TestContextLogger>();
    }

    public void Dispose()
    {
        foreach (var logger in _loggers)
        {
            logger?.Dispose();
        }
        _loggers.Clear();
    }
}