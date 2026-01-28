namespace EnterpriseAutomationFramework.Core.Models;

/// <summary>
/// 测试结果
/// </summary>
public class TestResult
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// 测试类名
    /// </summary>
    public string? TestClass { get; set; }

    /// <summary>
    /// 测试方法名
    /// </summary>
    public string? TestMethod { get; set; }

    /// <summary>
    /// 测试状态
    /// </summary>
    public TestStatus Status { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 执行时长
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// 测试输出
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// 截图列表
    /// </summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>
    /// 测试数据
    /// </summary>
    public Dictionary<string, object> TestData { get; set; } = new();

    /// <summary>
    /// 测试分类
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 测试标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 测试元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 添加截图
    /// </summary>
    /// <param name="screenshotPath">截图路径</param>
    public void AddScreenshot(string screenshotPath)
    {
        if (!string.IsNullOrWhiteSpace(screenshotPath) && !Screenshots.Contains(screenshotPath))
        {
            Screenshots.Add(screenshotPath);
        }
    }

    /// <summary>
    /// 添加测试数据
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddTestData(string key, object value)
    {
        TestData[key] = value;
    }

    /// <summary>
    /// 添加元数据
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    /// <summary>
    /// 获取元数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>元数据值</returns>
    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取状态描述
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusDescription()
    {
        return Status switch
        {
            TestStatus.Passed => "通过",
            TestStatus.Failed => "失败",
            TestStatus.Skipped => "跳过",
            TestStatus.Inconclusive => "不确定",
            _ => "未知"
        };
    }

    /// <summary>
    /// 是否为失败状态
    /// </summary>
    /// <returns>是否失败</returns>
    public bool IsFailed()
    {
        return Status == TestStatus.Failed;
    }

    /// <summary>
    /// 是否为通过状态
    /// </summary>
    /// <returns>是否通过</returns>
    public bool IsPassed()
    {
        return Status == TestStatus.Passed;
    }

    /// <summary>
    /// 是否为跳过状态
    /// </summary>
    /// <returns>是否跳过</returns>
    public bool IsSkipped()
    {
        return Status == TestStatus.Skipped;
    }
}

/// <summary>
/// 测试状态枚举
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// 通过
    /// </summary>
    Passed,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 跳过
    /// </summary>
    Skipped,

    /// <summary>
    /// 不确定
    /// </summary>
    Inconclusive
}