using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Tests.Flows;

/// <summary>
/// BaseFlow 基类单元测试
/// </summary>
public class BaseFlowTests
{
    private readonly Mock<ITestFixture> _mockTestFixture;
    private readonly Mock<ILogger> _mockLogger;
    private readonly TestFlow _testFlow;

    public BaseFlowTests()
    {
        _mockTestFixture = new Mock<ITestFixture>();
        _mockLogger = new Mock<ILogger>();
        _testFlow = new TestFlow(_mockTestFixture.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act & Assert
        Assert.NotNull(_testFlow);
        Assert.Equal("TestFlow", _testFlow.GetFlowName());
        Assert.Empty(_testFlow.GetExecutedSteps());
    }

    [Fact]
    public void Constructor_WithNullTestFixture_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFlow(null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFlow(_mockTestFixture.Object, null!));
    }

    [Fact]
    public async Task ExecuteStepAsync_WithValidStep_ShouldExecuteSuccessfully()
    {
        // Arrange
        var stepName = "测试步骤";
        var stepExecuted = false;

        // Act
        await _testFlow.ExecuteStepAsyncPublic(stepName, () =>
        {
            stepExecuted = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(stepExecuted);
        Assert.Contains(stepName, _testFlow.GetExecutedSteps());
    }

    [Fact]
    public async Task ExecuteStepAsync_WithNullStepName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _testFlow.ExecuteStepAsyncPublic(null!, () => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecuteStepAsync_WithEmptyStepName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _testFlow.ExecuteStepAsyncPublic("", () => Task.CompletedTask));
    }

    [Fact]
    public async Task ExecuteStepAsync_WithNullStepAction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _testFlow.ExecuteStepAsyncPublic("测试步骤", null!));
    }

    [Fact]
    public async Task ExecuteStepAsync_WhenStepThrowsException_ShouldWrapInTestFrameworkException()
    {
        // Arrange
        var stepName = "失败步骤";
        var originalException = new InvalidOperationException("原始错误");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TestFrameworkException>(() =>
            _testFlow.ExecuteStepAsyncPublic(stepName, () => throw originalException));

        Assert.Equal("TestFlow", exception.TestName);
        Assert.Equal("Flow", exception.Component);
        Assert.Contains(stepName, exception.Message);
        Assert.Equal(originalException, exception.InnerException);
    }

    [Fact]
    public void ValidateStep_WithTrueCondition_ShouldPass()
    {
        // Arrange
        var stepName = "验证步骤";

        // Act & Assert (should not throw)
        _testFlow.ValidateStepPublic(stepName, true, "错误消息");
    }

    [Fact]
    public void ValidateStep_WithFalseCondition_ShouldThrowTestFrameworkException()
    {
        // Arrange
        var stepName = "验证步骤";
        var errorMessage = "验证失败";

        // Act & Assert
        var exception = Assert.Throws<TestFrameworkException>(() =>
            _testFlow.ValidateStepPublic(stepName, false, errorMessage));

        Assert.Equal("TestFlow", exception.TestName);
        Assert.Equal("Flow", exception.Component);
        Assert.Contains(stepName, exception.Message);
        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public void ValidateStep_WithNullStepName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _testFlow.ValidateStepPublic(null!, true, "错误消息"));
    }

    [Fact]
    public void ValidateRequiredParameter_WithValidParameters_ShouldPass()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "testParam", "testValue" } };

        // Act & Assert (should not throw)
        _testFlow.ValidateRequiredParameterPublic(parameters, "testParam");
    }

    [Fact]
    public void ValidateRequiredParameter_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _testFlow.ValidateRequiredParameterPublic(null, "testParam"));

        Assert.Contains("TestFlow", exception.Message);
    }

    [Fact]
    public void ValidateRequiredParameter_WithMissingParameter_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _testFlow.ValidateRequiredParameterPublic(parameters, "missingParam"));

        Assert.Contains("TestFlow", exception.Message);
        Assert.Contains("missingParam", exception.Message);
    }

    [Fact]
    public void ValidateRequiredParameter_WithNullParameterValue_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "testParam", null! } };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _testFlow.ValidateRequiredParameterPublic(parameters, "testParam"));

        Assert.Contains("testParam", exception.Message);
        Assert.Contains("不能为 null", exception.Message);
    }

    [Fact]
    public void ValidateStringParameter_WithValidString_ShouldPass()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "stringParam", "validValue" } };

        // Act & Assert (should not throw)
        _testFlow.ValidateStringParameterPublic(parameters, "stringParam");
    }

    [Fact]
    public void ValidateStringParameter_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "stringParam", "" } };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _testFlow.ValidateStringParameterPublic(parameters, "stringParam"));

        Assert.Contains("stringParam", exception.Message);
        Assert.Contains("不能为空字符串", exception.Message);
    }

    [Fact]
    public void ValidateStringParameter_WithWhitespaceString_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "stringParam", "   " } };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _testFlow.ValidateStringParameterPublic(parameters, "stringParam"));

        Assert.Contains("stringParam", exception.Message);
        Assert.Contains("不能为空字符串", exception.Message);
    }

    [Fact]
    public void StartFlowExecution_ShouldClearExecutedSteps()
    {
        // Arrange
        _testFlow.AddExecutedStep("previousStep");

        // Act
        _testFlow.StartFlowExecutionPublic();

        // Assert
        Assert.Empty(_testFlow.GetExecutedSteps());
    }

    /// <summary>
    /// 测试用的 Flow 实现类
    /// </summary>
    private class TestFlow : BaseFlow
    {
        public TestFlow(ITestFixture testFixture, ILogger logger) : base(testFixture, logger) { }

        public override Task ExecuteAsync(Dictionary<string, object>? parameters = null)
        {
            return Task.CompletedTask;
        }

        // 公开受保护的方法用于测试
        public string GetFlowName() => FlowName;
        public IReadOnlyList<string> GetExecutedSteps() => ExecutedSteps;
        public Task ExecuteStepAsyncPublic(string stepName, Func<Task> stepAction) => ExecuteStepAsync(stepName, stepAction);
        public void ValidateStepPublic(string stepName, bool condition, string errorMessage) => ValidateStep(stepName, condition, errorMessage);
        public void ValidateRequiredParameterPublic(Dictionary<string, object>? parameters, string parameterName) => ValidateRequiredParameter(parameters, parameterName);
        public void ValidateStringParameterPublic(Dictionary<string, object> parameters, string parameterName) => ValidateStringParameter(parameters, parameterName);
        public void StartFlowExecutionPublic() => StartFlowExecution();
        public void EndFlowExecutionPublic() => EndFlowExecution();
        public void LogFlowExecutionFailurePublic(Exception ex) => LogFlowExecutionFailure(ex);

        // 用于测试的辅助方法
        public void AddExecutedStep(string stepName)
        {
            // 通过反射访问私有字段来模拟已执行的步骤
            var field = typeof(BaseFlow).GetField("_executedSteps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var steps = (List<string>)field!.GetValue(this)!;
            steps.Add(stepName);
        }
    }
}