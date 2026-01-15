namespace EnterpriseAutomationFramework.Core.Models;

/// <summary>
/// 测试报告
/// </summary>
public class TestReport
{
    /// <summary>
    /// 报告ID
    /// </summary>
    public string ReportId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 报告名称
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 测试开始时间
    /// </summary>
    public DateTime TestStartTime { get; set; }

    /// <summary>
    /// 测试结束时间
    /// </summary>
    public DateTime TestEndTime { get; set; }

    /// <summary>
    /// 环境名称
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 测试摘要
    /// </summary>
    public TestSummary Summary { get; set; } = new();

    /// <summary>
    /// 测试结果列表
    /// </summary>
    public List<TestResult> Results { get; set; } = new();

    /// <summary>
    /// 截图列表
    /// </summary>
    public List<string> Screenshots { get; set; } = new();

    /// <summary>
    /// 输出文件列表
    /// </summary>
    public List<string> OutputFiles { get; set; } = new();

    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 测试配置信息
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// 系统信息
    /// </summary>
    public Dictionary<string, object> SystemInfo { get; set; } = new();

    /// <summary>
    /// 报告版本
    /// </summary>
    public string ReportVersion { get; set; } = "1.0";

    /// <summary>
    /// 添加测试结果
    /// </summary>
    /// <param name="testResult">测试结果</param>
    public void AddTestResult(TestResult testResult)
    {
        if (testResult == null) return;

        Results.Add(testResult);
        
        // 收集截图
        foreach (var screenshot in testResult.Screenshots)
        {
            AddScreenshot(screenshot);
        }

        // 更新摘要
        RefreshSummary();
    }

    /// <summary>
    /// 添加多个测试结果
    /// </summary>
    /// <param name="testResults">测试结果列表</param>
    public void AddTestResults(IEnumerable<TestResult> testResults)
    {
        foreach (var result in testResults)
        {
            AddTestResult(result);
        }
    }

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

    /// <summary>
    /// 添加配置信息
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddConfiguration(string key, object value)
    {
        Configuration[key] = value;
    }

    /// <summary>
    /// 添加系统信息
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void AddSystemInfo(string key, object value)
    {
        SystemInfo[key] = value;
    }

    /// <summary>
    /// 刷新摘要信息
    /// </summary>
    public void RefreshSummary()
    {
        Summary = TestSummary.FromTestResults(Results);
    }

    /// <summary>
    /// 获取失败的测试
    /// </summary>
    /// <returns>失败的测试列表</returns>
    public List<TestResult> GetFailedTests()
    {
        return Results.Where(r => r.Status == TestStatus.Failed).ToList();
    }

    /// <summary>
    /// 获取通过的测试
    /// </summary>
    /// <returns>通过的测试列表</returns>
    public List<TestResult> GetPassedTests()
    {
        return Results.Where(r => r.Status == TestStatus.Passed).ToList();
    }

    /// <summary>
    /// 获取跳过的测试
    /// </summary>
    /// <returns>跳过的测试列表</returns>
    public List<TestResult> GetSkippedTests()
    {
        return Results.Where(r => r.Status == TestStatus.Skipped).ToList();
    }

    /// <summary>
    /// 按分类获取测试结果
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>指定分类的测试结果</returns>
    public List<TestResult> GetTestsByCategory(string category)
    {
        return Results.Where(r => r.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// 获取所有分类
    /// </summary>
    /// <returns>分类列表</returns>
    public List<string> GetAllCategories()
    {
        return Results.SelectMany(r => r.Categories).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// 获取报告摘要文本
    /// </summary>
    /// <returns>报告摘要文本</returns>
    public string GetReportSummary()
    {
        var duration = TestEndTime > TestStartTime ? TestEndTime - TestStartTime : TimeSpan.Zero;
        return $"测试报告: {ReportName}\n" +
               $"环境: {Environment}\n" +
               $"执行时间: {TestStartTime:yyyy-MM-dd HH:mm:ss} - {TestEndTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"总耗时: {duration.TotalSeconds:F1}秒\n" +
               $"{Summary.GetSummaryText()}";
    }

    /// <summary>
    /// 验证报告完整性
    /// </summary>
    /// <returns>验证结果</returns>
    public bool ValidateReport()
    {
        if (string.IsNullOrWhiteSpace(ReportName)) return false;
        if (string.IsNullOrWhiteSpace(Environment)) return false;
        if (TestStartTime == default) return false;
        if (TestEndTime == default) return false;
        if (TestEndTime < TestStartTime) return false;
        if (Summary == null) return false;
        if (Results == null) return false;

        return true;
    }

    /// <summary>
    /// 初始化系统信息
    /// </summary>
    public void InitializeSystemInfo()
    {
        AddSystemInfo("MachineName", System.Environment.MachineName);
        AddSystemInfo("UserName", System.Environment.UserName);
        AddSystemInfo("OSVersion", System.Environment.OSVersion.ToString());
        AddSystemInfo("ProcessorCount", System.Environment.ProcessorCount);
        AddSystemInfo("WorkingSet", System.Environment.WorkingSet);
        AddSystemInfo("CLRVersion", System.Environment.Version.ToString());
        AddSystemInfo("Is64BitOperatingSystem", System.Environment.Is64BitOperatingSystem);
        AddSystemInfo("Is64BitProcess", System.Environment.Is64BitProcess);
    }

    /// <summary>
    /// 创建报告构建器
    /// </summary>
    /// <param name="reportName">报告名称</param>
    /// <param name="environment">环境名称</param>
    /// <returns>报告构建器</returns>
    public static TestReportBuilder CreateBuilder(string reportName, string environment)
    {
        return new TestReportBuilder(reportName, environment);
    }
}

/// <summary>
/// 测试报告构建器
/// </summary>
public class TestReportBuilder
{
    private readonly TestReport _report;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="reportName">报告名称</param>
    /// <param name="environment">环境名称</param>
    public TestReportBuilder(string reportName, string environment)
    {
        _report = new TestReport
        {
            ReportName = reportName,
            Environment = environment,
            TestStartTime = DateTime.UtcNow
        };
        _report.InitializeSystemInfo();
    }

    /// <summary>
    /// 设置测试开始时间
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <returns>构建器实例</returns>
    public TestReportBuilder WithStartTime(DateTime startTime)
    {
        _report.TestStartTime = startTime;
        return this;
    }

    /// <summary>
    /// 设置测试结束时间
    /// </summary>
    /// <param name="endTime">结束时间</param>
    /// <returns>构建器实例</returns>
    public TestReportBuilder WithEndTime(DateTime endTime)
    {
        _report.TestEndTime = endTime;
        return this;
    }

    /// <summary>
    /// 添加测试结果
    /// </summary>
    /// <param name="testResults">测试结果列表</param>
    /// <returns>构建器实例</returns>
    public TestReportBuilder WithTestResults(IEnumerable<TestResult> testResults)
    {
        _report.AddTestResults(testResults);
        return this;
    }

    /// <summary>
    /// 添加元数据
    /// </summary>
    /// <param name="metadata">元数据字典</param>
    /// <returns>构建器实例</returns>
    public TestReportBuilder WithMetadata(Dictionary<string, object> metadata)
    {
        foreach (var kvp in metadata)
        {
            _report.AddMetadata(kvp.Key, kvp.Value);
        }
        return this;
    }

    /// <summary>
    /// 添加配置信息
    /// </summary>
    /// <param name="configuration">配置信息字典</param>
    /// <returns>构建器实例</returns>
    public TestReportBuilder WithConfiguration(Dictionary<string, object> configuration)
    {
        foreach (var kvp in configuration)
        {
            _report.AddConfiguration(kvp.Key, kvp.Value);
        }
        return this;
    }

    /// <summary>
    /// 构建报告
    /// </summary>
    /// <returns>测试报告</returns>
    public TestReport Build()
    {
        if (_report.TestEndTime == default)
        {
            _report.TestEndTime = DateTime.UtcNow;
        }
        
        _report.RefreshSummary();
        return _report;
    }
}