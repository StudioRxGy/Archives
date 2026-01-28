namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 测试执行结果
/// </summary>
public class TestExecutionResult
{
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
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行的命令
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// 使用的过滤器
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// 总测试数
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// 通过的测试数
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// 失败的测试数
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// 跳过的测试数
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// 测试结果详情
    /// </summary>
    public List<TestCaseResult> TestResults { get; set; } = new();

    /// <summary>
    /// 输出文件路径
    /// </summary>
    public List<string> OutputFiles { get; set; } = new();

    /// <summary>
    /// 额外的元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 通过率
    /// </summary>
    public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;

    /// <summary>
    /// 失败率
    /// </summary>
    public double FailureRate => TotalTests > 0 ? (double)FailedTests / TotalTests * 100 : 0;

    /// <summary>
    /// 获取执行摘要
    /// </summary>
    /// <returns>执行摘要</returns>
    public string GetSummary()
    {
        return $"总计: {TotalTests}, 通过: {PassedTests}, 失败: {FailedTests}, 跳过: {SkippedTests}, " +
               $"通过率: {PassRate:F1}%, 耗时: {Duration.TotalSeconds:F1}s, 状态: {(Success ? "成功" : "失败")}";
    }

    /// <summary>
    /// 获取详细报告
    /// </summary>
    /// <returns>详细报告</returns>
    public string GetDetailedReport()
    {
        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== 测试执行报告 ===");
        report.AppendLine($"开始时间: {StartTime:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"结束时间: {EndTime:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"执行时长: {Duration.TotalSeconds:F1} 秒");
        report.AppendLine($"执行状态: {(Success ? "成功" : "失败")}");
        
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            report.AppendLine($"错误信息: {ErrorMessage}");
        }
        
        report.AppendLine();
        report.AppendLine("=== 测试统计 ===");
        report.AppendLine($"总测试数: {TotalTests}");
        report.AppendLine($"通过测试: {PassedTests} ({PassRate:F1}%)");
        report.AppendLine($"失败测试: {FailedTests} ({FailureRate:F1}%)");
        report.AppendLine($"跳过测试: {SkippedTests}");
        
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            report.AppendLine();
            report.AppendLine("=== 过滤器 ===");
            report.AppendLine($"使用的过滤器: {Filter}");
        }
        
        if (!string.IsNullOrWhiteSpace(Command))
        {
            report.AppendLine();
            report.AppendLine("=== 执行命令 ===");
            report.AppendLine(Command);
        }
        
        if (OutputFiles.Any())
        {
            report.AppendLine();
            report.AppendLine("=== 输出文件 ===");
            foreach (var file in OutputFiles)
            {
                report.AppendLine($"- {file}");
            }
        }
        
        if (TestResults.Any())
        {
            report.AppendLine();
            report.AppendLine("=== 测试结果详情 ===");
            
            var failedTests = TestResults.Where(t => !t.Passed).ToList();
            if (failedTests.Any())
            {
                report.AppendLine("失败的测试:");
                foreach (var test in failedTests)
                {
                    report.AppendLine($"- {test.TestName}: {test.ErrorMessage}");
                }
            }
        }
        
        return report.ToString();
    }

    /// <summary>
    /// 添加测试结果
    /// </summary>
    /// <param name="testResult">测试结果</param>
    public void AddTestResult(TestCaseResult testResult)
    {
        TestResults.Add(testResult);
        
        // 更新统计
        TotalTests = TestResults.Count;
        PassedTests = TestResults.Count(t => t.Passed);
        FailedTests = TestResults.Count(t => !t.Passed && !t.Skipped);
        SkippedTests = TestResults.Count(t => t.Skipped);
    }

    /// <summary>
    /// 添加输出文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public void AddOutputFile(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && !OutputFiles.Contains(filePath))
        {
            OutputFiles.Add(filePath);
        }
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
}

/// <summary>
/// 测试用例结果
/// </summary>
public class TestCaseResult
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
    /// 是否通过
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 是否跳过
    /// </summary>
    public bool Skipped { get; set; }

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
    /// 测试分类
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// 测试标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 获取状态描述
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusDescription()
    {
        if (Skipped) return "跳过";
        return Passed ? "通过" : "失败";
    }
}