namespace CsPlaywrightXun.src.playwright.Core.Models;

/// <summary>
/// 测试摘要
/// </summary>
public class TestSummary
{
    /// <summary>
    /// 总测试数
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// 通过测试数
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// 失败测试数
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// 跳过测试数
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// 不确定测试数
    /// </summary>
    public int InconclusiveTests { get; set; }

    /// <summary>
    /// 总执行时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 平均执行时长
    /// </summary>
    public TimeSpan AverageDuration { get; set; }

    /// <summary>
    /// 最快测试时长
    /// </summary>
    public TimeSpan FastestTest { get; set; }

    /// <summary>
    /// 最慢测试时长
    /// </summary>
    public TimeSpan SlowestTest { get; set; }

    /// <summary>
    /// 通过率
    /// </summary>
    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

    /// <summary>
    /// 失败率
    /// </summary>
    public double FailureRate => TotalTests > 0 ? (double)FailedTests / TotalTests * 100 : 0;

    /// <summary>
    /// 跳过率
    /// </summary>
    public double SkipRate => TotalTests > 0 ? (double)SkippedTests / TotalTests * 100 : 0;

    /// <summary>
    /// 测试执行效率（测试数/小时）
    /// </summary>
    public double TestsPerHour => TotalDuration.TotalHours > 0 ? TotalTests / TotalDuration.TotalHours : 0;

    /// <summary>
    /// 从测试结果列表创建摘要
    /// </summary>
    /// <param name="testResults">测试结果列表</param>
    /// <returns>测试摘要</returns>
    public static TestSummary FromTestResults(IEnumerable<TestResult> testResults)
    {
        var results = testResults.ToList();
        var summary = new TestSummary();

        if (!results.Any())
        {
            return summary;
        }

        summary.TotalTests = results.Count;
        summary.PassedTests = results.Count(r => r.Status == TestStatus.Passed);
        summary.FailedTests = results.Count(r => r.Status == TestStatus.Failed);
        summary.SkippedTests = results.Count(r => r.Status == TestStatus.Skipped);
        summary.InconclusiveTests = results.Count(r => r.Status == TestStatus.Inconclusive);

        var durations = results.Select(r => r.Duration).ToList();
        summary.TotalDuration = TimeSpan.FromTicks(durations.Sum(d => d.Ticks));
        summary.AverageDuration = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
        summary.FastestTest = durations.Min();
        summary.SlowestTest = durations.Max();

        return summary;
    }

    /// <summary>
    /// 获取摘要文本
    /// </summary>
    /// <returns>摘要文本</returns>
    public string GetSummaryText()
    {
        return $"总计: {TotalTests}, 通过: {PassedTests}, 失败: {FailedTests}, 跳过: {SkippedTests}, " +
               $"通过率: {PassRate:F1}%, 总耗时: {TotalDuration.TotalSeconds:F1}s";
    }

    /// <summary>
    /// 获取详细统计信息
    /// </summary>
    /// <returns>详细统计信息</returns>
    public Dictionary<string, object> GetDetailedStatistics()
    {
        return new Dictionary<string, object>
        {
            ["TotalTests"] = TotalTests,
            ["PassedTests"] = PassedTests,
            ["FailedTests"] = FailedTests,
            ["SkippedTests"] = SkippedTests,
            ["InconclusiveTests"] = InconclusiveTests,
            ["PassRate"] = Math.Round(PassRate, 2),
            ["FailureRate"] = Math.Round(FailureRate, 2),
            ["SkipRate"] = Math.Round(SkipRate, 2),
            ["TotalDurationSeconds"] = Math.Round(TotalDuration.TotalSeconds, 2),
            ["AverageDurationSeconds"] = Math.Round(AverageDuration.TotalSeconds, 2),
            ["FastestTestSeconds"] = Math.Round(FastestTest.TotalSeconds, 2),
            ["SlowestTestSeconds"] = Math.Round(SlowestTest.TotalSeconds, 2),
            ["TestsPerHour"] = Math.Round(TestsPerHour, 2)
        };
    }

    /// <summary>
    /// 检查是否有失败的测试
    /// </summary>
    /// <returns>是否有失败的测试</returns>
    public bool HasFailures()
    {
        return FailedTests > 0;
    }

    /// <summary>
    /// 检查是否所有测试都通过
    /// </summary>
    /// <returns>是否所有测试都通过</returns>
    public bool AllTestsPassed()
    {
        return TotalTests > 0 && PassedTests == TotalTests;
    }

    /// <summary>
    /// 获取成功状态
    /// </summary>
    /// <returns>成功状态</returns>
    public bool IsSuccessful()
    {
        return !HasFailures();
    }
}