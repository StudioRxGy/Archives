using Microsoft.Extensions.Logging;
using Serilog;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright;

namespace CsPlaywrightXun.src.playwright.Core.Logging;

/// <summary>
/// 测试上下文日志记录器
/// </summary>
public class TestContextLogger : IDisposable
{
    private readonly IDisposable _logContext;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly string _testName;
    private readonly DateTime _startTime;
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testClass">测试类名</param>
    /// <param name="testMethod">测试方法名</param>
    /// <param name="logger">日志记录器</param>
    public TestContextLogger(string testName, string? testClass = null, string? testMethod = null, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _testName = testName;
        _startTime = DateTime.UtcNow;
        _logger = logger ?? Framework.GetLogger<TestContextLogger>();

        // 配置测试上下文
        _logContext = SerilogConfiguration.ConfigureTestContext(testName, testClass, testMethod);

        // 记录测试开始
        _logger.LogInformation("测试开始: {TestName}", testName);
    }

    /// <summary>
    /// 记录测试步骤
    /// </summary>
    /// <param name="stepName">步骤名称</param>
    /// <param name="description">步骤描述</param>
    public void LogStep(string stepName, string? description = null)
    {
        var message = string.IsNullOrEmpty(description) 
            ? "执行步骤: {StepName}" 
            : "执行步骤: {StepName} - {Description}";
        
        _logger.LogInformation(message, stepName, description);
    }

    /// <summary>
    /// 记录测试数据
    /// </summary>
    /// <param name="dataName">数据名称</param>
    /// <param name="dataValue">数据值</param>
    public void LogTestData(string dataName, object? dataValue)
    {
        _logger.LogDebug("测试数据: {DataName} = {DataValue}", dataName, dataValue);
    }

    /// <summary>
    /// 记录测试断言
    /// </summary>
    /// <param name="assertion">断言描述</param>
    /// <param name="result">断言结果</param>
    public void LogAssertion(string assertion, bool result)
    {
        if (result)
        {
            _logger.LogInformation("断言通过: {Assertion}", assertion);
        }
        else
        {
            _logger.LogError("断言失败: {Assertion}", assertion);
        }
    }

    /// <summary>
    /// 记录测试错误
    /// </summary>
    /// <param name="exception">异常信息</param>
    /// <param name="message">错误消息</param>
    public void LogError(Exception exception, string? message = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogError(exception, "测试执行出错");
        }
        else
        {
            _logger.LogError(exception, "测试执行出错: {Message}", message);
        }
    }

    /// <summary>
    /// 记录测试警告
    /// </summary>
    /// <param name="message">警告消息</param>
    public void LogWarning(string message)
    {
        _logger.LogWarning("测试警告: {Message}", message);
    }

    /// <summary>
    /// 记录测试完成
    /// </summary>
    /// <param name="success">是否成功</param>
    /// <param name="message">完成消息</param>
    public void LogTestComplete(bool success, string? message = null)
    {
        var duration = DateTime.UtcNow - _startTime;
        
        if (success)
        {
            var successMessage = string.IsNullOrEmpty(message) 
                ? "测试完成: {TestName}，耗时: {Duration}ms" 
                : "测试完成: {TestName}，耗时: {Duration}ms，结果: {Message}";
            
            _logger.LogInformation(successMessage, _testName, duration.TotalMilliseconds, message);
        }
        else
        {
            var failureMessage = string.IsNullOrEmpty(message) 
                ? "测试失败: {TestName}，耗时: {Duration}ms" 
                : "测试失败: {TestName}，耗时: {Duration}ms，原因: {Message}";
            
            _logger.LogError(failureMessage, _testName, duration.TotalMilliseconds, message);
        }
    }

    /// <summary>
    /// 记录性能指标
    /// </summary>
    /// <param name="metricName">指标名称</param>
    /// <param name="value">指标值</param>
    /// <param name="unit">单位</param>
    public void LogPerformanceMetric(string metricName, double value, string unit = "ms")
    {
        _logger.LogInformation("性能指标: {MetricName} = {Value} {Unit}", metricName, value, unit);
    }

    /// <summary>
    /// 记录截图信息
    /// </summary>
    /// <param name="screenshotPath">截图路径</param>
    /// <param name="description">截图描述</param>
    public void LogScreenshot(string screenshotPath, string? description = null)
    {
        var message = string.IsNullOrEmpty(description) 
            ? "截图保存: {ScreenshotPath}" 
            : "截图保存: {ScreenshotPath} - {Description}";
        
        _logger.LogInformation(message, screenshotPath, description);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _logContext?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// 测试上下文日志记录器扩展方法
/// </summary>
public static class TestContextLoggerExtensions
{
    /// <summary>
    /// 创建测试上下文日志记录器
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="testClass">测试类名</param>
    /// <param name="testMethod">测试方法名</param>
    /// <returns>测试上下文日志记录器</returns>
    public static TestContextLogger CreateTestLogger(string testName, string? testClass = null, string? testMethod = null)
    {
        return new TestContextLogger(testName, testClass, testMethod);
    }

    /// <summary>
    /// 从测试类型创建测试上下文日志记录器
    /// </summary>
    /// <param name="testType">测试类型</param>
    /// <param name="testMethodName">测试方法名</param>
    /// <returns>测试上下文日志记录器</returns>
    public static TestContextLogger CreateTestLogger(Type testType, string testMethodName)
    {
        var testName = $"{testType.Name}.{testMethodName}";
        return new TestContextLogger(testName, testType.Name, testMethodName);
    }
}