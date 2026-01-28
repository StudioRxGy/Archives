namespace CsPlaywrightXun.src.playwright.Core.Interfaces;

/// <summary>
/// 报告生成器接口
/// </summary>
public interface IReporter
{
    /// <summary>
    /// 开始测试报告
    /// </summary>
    /// <param name="testName">测试名称</param>
    Task StartTestAsync(string testName);
    
    /// <summary>
    /// 结束测试报告
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="result">测试结果</param>
    Task EndTestAsync(string testName, TestResult result);
    
    /// <summary>
    /// 记录测试步骤
    /// </summary>
    /// <param name="stepName">步骤名称</param>
    /// <param name="description">步骤描述</param>
    /// <param name="status">步骤状态</param>
    Task LogStepAsync(string stepName, string description, StepStatus status);
    
    /// <summary>
    /// 添加截图
    /// </summary>
    /// <param name="screenshotPath">截图路径</param>
    /// <param name="description">截图描述</param>
    Task AddScreenshotAsync(string screenshotPath, string description);
    
    /// <summary>
    /// 生成最终报告
    /// </summary>
    Task GenerateReportAsync();
}

/// <summary>
/// 测试结果枚举
/// </summary>
public enum TestResult
{
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// 步骤状态枚举
/// </summary>
public enum StepStatus
{
    Passed,
    Failed,
    Warning,
    Info
}