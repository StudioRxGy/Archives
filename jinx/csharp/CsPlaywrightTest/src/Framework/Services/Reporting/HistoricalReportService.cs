using System.Text.Json;
using EnterpriseAutomationFramework.Core.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseAutomationFramework.Services.Reporting;

/// <summary>
/// 历史报告服务
/// </summary>
public class HistoricalReportService
{
    private readonly ILogger<HistoricalReportService> _logger;
    private readonly string _historyDirectory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="historyDirectory">历史报告目录</param>
    public HistoricalReportService(ILogger<HistoricalReportService> logger, string historyDirectory = "Reports/History")
    {
        _logger = logger;
        _historyDirectory = historyDirectory;
        
        // 确保历史目录存在
        if (!Directory.Exists(_historyDirectory))
        {
            Directory.CreateDirectory(_historyDirectory);
        }
    }

    /// <summary>
    /// 保存历史报告
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>保存的历史报告路径</returns>
    public async Task<string> SaveHistoricalReportAsync(TestReport testReport)
    {
        try
        {
            _logger.LogInformation("保存历史报告: {ReportName}", testReport.ReportName);

            var historyData = CreateHistoryData(testReport);
            var fileName = $"{testReport.Environment}_{testReport.TestStartTime:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(_historyDirectory, fileName);

            var json = JsonSerializer.Serialize(historyData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("历史报告保存完成: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存历史报告失败: {ReportName}", testReport.ReportName);
            throw;
        }
    }

    /// <summary>
    /// 获取历史报告列表
    /// </summary>
    /// <param name="environment">环境名称（可选）</param>
    /// <param name="days">获取最近几天的报告（默认30天）</param>
    /// <returns>历史报告列表</returns>
    public async Task<List<HistoricalReportSummary>> GetHistoricalReportsAsync(string? environment = null, int days = 30)
    {
        try
        {
            _logger.LogInformation("获取历史报告列表: 环境={Environment}, 天数={Days}", environment ?? "全部", days);

            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var reports = new List<HistoricalReportSummary>();

            var files = Directory.GetFiles(_historyDirectory, "*.json")
                .Where(f => File.GetCreationTimeUtc(f) >= cutoffDate)
                .OrderByDescending(f => File.GetCreationTimeUtc(f));

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var historyData = JsonSerializer.Deserialize<HistoricalReportData>(json);
                    
                    if (historyData != null && 
                        (string.IsNullOrEmpty(environment) || 
                         historyData.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase)))
                    {
                        reports.Add(new HistoricalReportSummary
                        {
                            FilePath = file,
                            ReportId = historyData.ReportId,
                            ReportName = historyData.ReportName,
                            Environment = historyData.Environment,
                            TestStartTime = historyData.TestStartTime,
                            TestEndTime = historyData.TestEndTime,
                            TotalTests = historyData.Summary.TotalTests,
                            PassedTests = historyData.Summary.PassedTests,
                            FailedTests = historyData.Summary.FailedTests,
                            SkippedTests = historyData.Summary.SkippedTests,
                            PassRate = historyData.Summary.PassRate,
                            TotalDuration = historyData.Summary.TotalDuration
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "读取历史报告文件失败: {FilePath}", file);
                }
            }

            _logger.LogInformation("获取到 {Count} 个历史报告", reports.Count);
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取历史报告列表失败");
            throw;
        }
    }

    /// <summary>
    /// 比较两个报告
    /// </summary>
    /// <param name="currentReport">当前报告</param>
    /// <param name="previousReportId">之前报告的ID</param>
    /// <returns>报告比较结果</returns>
    public async Task<ReportComparison> CompareReportsAsync(TestReport currentReport, string previousReportId)
    {
        try
        {
            _logger.LogInformation("比较报告: 当前={CurrentId}, 之前={PreviousId}", 
                currentReport.ReportId, previousReportId);

            var previousReport = await LoadHistoricalReportAsync(previousReportId);
            if (previousReport == null)
            {
                throw new ArgumentException($"未找到历史报告: {previousReportId}");
            }

            var comparison = new ReportComparison
            {
                CurrentReport = CreateHistoryData(currentReport),
                PreviousReport = previousReport,
                ComparisonDate = DateTime.UtcNow
            };

            // 计算差异
            comparison.SummaryComparison = CompareSummaries(currentReport.Summary, previousReport.Summary);
            comparison.TestComparisons = CompareTestResults(currentReport.Results, previousReport.TestResults);
            comparison.OverallTrend = DetermineOverallTrend(comparison.SummaryComparison);

            _logger.LogInformation("报告比较完成");
            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "比较报告失败");
            throw;
        }
    }

    /// <summary>
    /// 生成趋势分析
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">分析天数</param>
    /// <returns>趋势分析结果</returns>
    public async Task<TrendAnalysis> GenerateTrendAnalysisAsync(string environment, int days = 30)
    {
        try
        {
            _logger.LogInformation("生成趋势分析: 环境={Environment}, 天数={Days}", environment, days);

            var reports = await GetHistoricalReportsAsync(environment, days);
            if (reports.Count < 2)
            {
                throw new InvalidOperationException("需要至少2个历史报告才能进行趋势分析");
            }

            var trendAnalysis = new TrendAnalysis
            {
                Environment = environment,
                AnalysisPeriod = days,
                StartDate = reports.Min(r => r.TestStartTime),
                EndDate = reports.Max(r => r.TestStartTime),
                TotalReports = reports.Count
            };

            // 计算趋势指标
            trendAnalysis.PassRateTrend = CalculatePassRateTrend(reports);
            trendAnalysis.TestCountTrend = CalculateTestCountTrend(reports);
            trendAnalysis.DurationTrend = CalculateDurationTrend(reports);
            trendAnalysis.StabilityMetrics = CalculateStabilityMetrics(reports);

            // 识别问题测试
            trendAnalysis.ProblematicTests = await IdentifyProblematicTestsAsync(environment, days);

            _logger.LogInformation("趋势分析完成");
            return trendAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成趋势分析失败");
            throw;
        }
    }

    /// <summary>
    /// 清理过期的历史报告
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的文件数量</returns>
    public async Task<int> CleanupHistoricalReportsAsync(int retentionDays = 90)
    {
        try
        {
            _logger.LogInformation("清理过期历史报告: 保留天数={RetentionDays}", retentionDays);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var files = Directory.GetFiles(_historyDirectory, "*.json")
                .Where(f => File.GetCreationTimeUtc(f) < cutoffDate)
                .ToList();

            var deletedCount = 0;
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                    _logger.LogDebug("删除过期报告文件: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除文件失败: {FilePath}", file);
                }
            }

            _logger.LogInformation("清理完成，删除了 {DeletedCount} 个过期报告文件", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理历史报告失败");
            throw;
        }
    }

    /// <summary>
    /// 创建历史数据
    /// </summary>
    /// <param name="testReport">测试报告</param>
    /// <returns>历史报告数据</returns>
    private HistoricalReportData CreateHistoryData(TestReport testReport)
    {
        return new HistoricalReportData
        {
            ReportId = testReport.ReportId,
            ReportName = testReport.ReportName,
            Environment = testReport.Environment,
            TestStartTime = testReport.TestStartTime,
            TestEndTime = testReport.TestEndTime,
            Summary = new HistoricalSummary
            {
                TotalTests = testReport.Summary.TotalTests,
                PassedTests = testReport.Summary.PassedTests,
                FailedTests = testReport.Summary.FailedTests,
                SkippedTests = testReport.Summary.SkippedTests,
                InconclusiveTests = testReport.Summary.InconclusiveTests,
                PassRate = testReport.Summary.PassRate,
                FailureRate = testReport.Summary.FailureRate,
                SkipRate = testReport.Summary.SkipRate,
                TotalDuration = testReport.Summary.TotalDuration,
                AverageDuration = testReport.Summary.AverageDuration,
                FastestTest = testReport.Summary.FastestTest,
                SlowestTest = testReport.Summary.SlowestTest
            },
            TestResults = testReport.Results.Select(r => new HistoricalTestResult
            {
                TestName = r.TestName,
                TestClass = r.TestClass,
                TestMethod = r.TestMethod,
                Status = r.Status,
                Duration = r.Duration,
                ErrorMessage = r.ErrorMessage,
                Categories = r.Categories,
                Tags = r.Tags
            }).ToList(),
            SystemInfo = testReport.SystemInfo,
            Configuration = testReport.Configuration
        };
    }

    /// <summary>
    /// 加载历史报告
    /// </summary>
    /// <param name="reportId">报告ID</param>
    /// <returns>历史报告数据</returns>
    private async Task<HistoricalReportData?> LoadHistoricalReportAsync(string reportId)
    {
        var files = Directory.GetFiles(_historyDirectory, "*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var historyData = JsonSerializer.Deserialize<HistoricalReportData>(json);
                
                if (historyData?.ReportId == reportId)
                {
                    return historyData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取历史报告文件失败: {FilePath}", file);
            }
        }

        return null;
    }

    /// <summary>
    /// 比较摘要信息
    /// </summary>
    /// <param name="current">当前摘要</param>
    /// <param name="previous">之前摘要</param>
    /// <returns>摘要比较结果</returns>
    private SummaryComparison CompareSummaries(TestSummary current, HistoricalSummary previous)
    {
        return new SummaryComparison
        {
            TotalTestsChange = current.TotalTests - previous.TotalTests,
            PassedTestsChange = current.PassedTests - previous.PassedTests,
            FailedTestsChange = current.FailedTests - previous.FailedTests,
            SkippedTestsChange = current.SkippedTests - previous.SkippedTests,
            PassRateChange = current.PassRate - previous.PassRate,
            DurationChange = current.TotalDuration - previous.TotalDuration
        };
    }

    /// <summary>
    /// 比较测试结果
    /// </summary>
    /// <param name="currentResults">当前测试结果</param>
    /// <param name="previousResults">之前测试结果</param>
    /// <returns>测试比较结果列表</returns>
    private List<TestComparison> CompareTestResults(List<TestResult> currentResults, List<HistoricalTestResult> previousResults)
    {
        var comparisons = new List<TestComparison>();
        var previousDict = previousResults.ToDictionary(r => r.TestName, r => r);

        foreach (var current in currentResults)
        {
            var comparison = new TestComparison
            {
                TestName = current.TestName,
                CurrentStatus = current.Status,
                CurrentDuration = current.Duration
            };

            if (previousDict.TryGetValue(current.TestName, out var previous))
            {
                comparison.PreviousStatus = previous.Status;
                comparison.PreviousDuration = previous.Duration;
                comparison.StatusChanged = current.Status != previous.Status;
                comparison.DurationChange = current.Duration - previous.Duration;
                comparison.ChangeType = DetermineChangeType(current.Status, previous.Status);
            }
            else
            {
                comparison.ChangeType = TestChangeType.New;
            }

            comparisons.Add(comparison);
        }

        // 查找已删除的测试
        var currentTestNames = currentResults.Select(r => r.TestName).ToHashSet();
        foreach (var previous in previousResults.Where(p => !currentTestNames.Contains(p.TestName)))
        {
            comparisons.Add(new TestComparison
            {
                TestName = previous.TestName,
                PreviousStatus = previous.Status,
                PreviousDuration = previous.Duration,
                ChangeType = TestChangeType.Removed
            });
        }

        return comparisons;
    }

    /// <summary>
    /// 确定变更类型
    /// </summary>
    /// <param name="currentStatus">当前状态</param>
    /// <param name="previousStatus">之前状态</param>
    /// <returns>变更类型</returns>
    private TestChangeType DetermineChangeType(TestStatus currentStatus, TestStatus previousStatus)
    {
        if (currentStatus == previousStatus)
            return TestChangeType.NoChange;

        if (previousStatus == TestStatus.Failed && currentStatus == TestStatus.Passed)
            return TestChangeType.Fixed;

        if (previousStatus == TestStatus.Passed && currentStatus == TestStatus.Failed)
            return TestChangeType.Regressed;

        return TestChangeType.StatusChanged;
    }

    /// <summary>
    /// 确定整体趋势
    /// </summary>
    /// <param name="summaryComparison">摘要比较</param>
    /// <returns>整体趋势</returns>
    private OverallTrend DetermineOverallTrend(SummaryComparison summaryComparison)
    {
        if (summaryComparison.PassRateChange > 5)
            return OverallTrend.Improving;
        
        if (summaryComparison.PassRateChange < -5)
            return OverallTrend.Declining;
        
        return OverallTrend.Stable;
    }

    /// <summary>
    /// 计算通过率趋势
    /// </summary>
    /// <param name="reports">历史报告列表</param>
    /// <returns>通过率趋势</returns>
    private TrendMetric CalculatePassRateTrend(List<HistoricalReportSummary> reports)
    {
        var values = reports.OrderBy(r => r.TestStartTime).Select(r => r.PassRate).ToList();
        return CalculateTrendMetric(values, "通过率");
    }

    /// <summary>
    /// 计算测试数量趋势
    /// </summary>
    /// <param name="reports">历史报告列表</param>
    /// <returns>测试数量趋势</returns>
    private TrendMetric CalculateTestCountTrend(List<HistoricalReportSummary> reports)
    {
        var values = reports.OrderBy(r => r.TestStartTime).Select(r => (double)r.TotalTests).ToList();
        return CalculateTrendMetric(values, "测试数量");
    }

    /// <summary>
    /// 计算执行时长趋势
    /// </summary>
    /// <param name="reports">历史报告列表</param>
    /// <returns>执行时长趋势</returns>
    private TrendMetric CalculateDurationTrend(List<HistoricalReportSummary> reports)
    {
        var values = reports.OrderBy(r => r.TestStartTime).Select(r => r.TotalDuration.TotalSeconds).ToList();
        return CalculateTrendMetric(values, "执行时长");
    }

    /// <summary>
    /// 计算趋势指标
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <param name="metricName">指标名称</param>
    /// <returns>趋势指标</returns>
    private TrendMetric CalculateTrendMetric(List<double> values, string metricName)
    {
        if (values.Count < 2)
        {
            return new TrendMetric
            {
                MetricName = metricName,
                Trend = TrendDirection.Stable,
                ChangePercentage = 0,
                Average = values.FirstOrDefault(),
                Min = values.FirstOrDefault(),
                Max = values.FirstOrDefault()
            };
        }

        var first = values.First();
        var last = values.Last();
        var changePercentage = first != 0 ? ((last - first) / first) * 100 : 0;

        var trend = changePercentage switch
        {
            > 5 => TrendDirection.Increasing,
            < -5 => TrendDirection.Decreasing,
            _ => TrendDirection.Stable
        };

        return new TrendMetric
        {
            MetricName = metricName,
            Trend = trend,
            ChangePercentage = changePercentage,
            Average = values.Average(),
            Min = values.Min(),
            Max = values.Max()
        };
    }

    /// <summary>
    /// 计算稳定性指标
    /// </summary>
    /// <param name="reports">历史报告列表</param>
    /// <returns>稳定性指标</returns>
    private StabilityMetrics CalculateStabilityMetrics(List<HistoricalReportSummary> reports)
    {
        var passRates = reports.Select(r => r.PassRate).ToList();
        var testCounts = reports.Select(r => (double)r.TotalTests).ToList();

        return new StabilityMetrics
        {
            PassRateStandardDeviation = CalculateStandardDeviation(passRates),
            TestCountStandardDeviation = CalculateStandardDeviation(testCounts),
            ConsistencyScore = CalculateConsistencyScore(passRates)
        };
    }

    /// <summary>
    /// 计算标准差
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <returns>标准差</returns>
    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;

        var average = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - average, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    /// <summary>
    /// 计算一致性分数
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <returns>一致性分数（0-100）</returns>
    private double CalculateConsistencyScore(List<double> values)
    {
        if (values.Count < 2) return 100;

        var standardDeviation = CalculateStandardDeviation(values);
        var average = values.Average();
        
        if (average == 0) return 100;

        var coefficientOfVariation = (standardDeviation / average) * 100;
        return Math.Max(0, 100 - coefficientOfVariation);
    }

    /// <summary>
    /// 识别问题测试
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="days">分析天数</param>
    /// <returns>问题测试列表</returns>
    private async Task<List<ProblematicTest>> IdentifyProblematicTestsAsync(string environment, int days)
    {
        var reports = await GetHistoricalReportsAsync(environment, days);
        var testFailureRates = new Dictionary<string, List<bool>>();

        // 收集每个测试的失败历史
        foreach (var report in reports)
        {
            var fullReport = await LoadHistoricalReportAsync(report.ReportId);
            if (fullReport?.TestResults != null)
            {
                foreach (var test in fullReport.TestResults)
                {
                    if (!testFailureRates.ContainsKey(test.TestName))
                    {
                        testFailureRates[test.TestName] = new List<bool>();
                    }
                    testFailureRates[test.TestName].Add(test.Status == TestStatus.Failed);
                }
            }
        }

        var problematicTests = new List<ProblematicTest>();

        foreach (var kvp in testFailureRates)
        {
            var testName = kvp.Key;
            var failures = kvp.Value;
            
            if (failures.Count >= 3) // 至少有3次执行记录
            {
                var failureRate = failures.Count(f => f) / (double)failures.Count * 100;
                var isFlaky = IsFlaky(failures);

                if (failureRate > 20 || isFlaky) // 失败率超过20%或者是不稳定测试
                {
                    problematicTests.Add(new ProblematicTest
                    {
                        TestName = testName,
                        FailureRate = failureRate,
                        IsFlaky = isFlaky,
                        TotalExecutions = failures.Count,
                        FailedExecutions = failures.Count(f => f)
                    });
                }
            }
        }

        return problematicTests.OrderByDescending(t => t.FailureRate).ToList();
    }

    /// <summary>
    /// 判断测试是否不稳定
    /// </summary>
    /// <param name="failures">失败历史</param>
    /// <returns>是否不稳定</returns>
    private bool IsFlaky(List<bool> failures)
    {
        if (failures.Count < 5) return false;

        // 检查是否有交替的成功和失败模式
        var changes = 0;
        for (int i = 1; i < failures.Count; i++)
        {
            if (failures[i] != failures[i - 1])
            {
                changes++;
            }
        }

        // 如果状态变化次数超过总次数的30%，认为是不稳定的
        return changes > failures.Count * 0.3;
    }
}

// 历史报告相关数据模型
public class HistoricalReportData
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public HistoricalSummary Summary { get; set; } = new();
    public List<HistoricalTestResult> TestResults { get; set; } = new();
    public Dictionary<string, object> SystemInfo { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class HistoricalSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public int InconclusiveTests { get; set; }
    public double PassRate { get; set; }
    public double FailureRate { get; set; }
    public double SkipRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan FastestTest { get; set; }
    public TimeSpan SlowestTest { get; set; }
}

public class HistoricalTestResult
{
    public string TestName { get; set; } = string.Empty;
    public string? TestClass { get; set; }
    public string? TestMethod { get; set; }
    public TestStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class HistoricalReportSummary
{
    public string FilePath { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public double PassRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class ReportComparison
{
    public HistoricalReportData CurrentReport { get; set; } = new();
    public HistoricalReportData PreviousReport { get; set; } = new();
    public DateTime ComparisonDate { get; set; }
    public SummaryComparison SummaryComparison { get; set; } = new();
    public List<TestComparison> TestComparisons { get; set; } = new();
    public OverallTrend OverallTrend { get; set; }
}

public class SummaryComparison
{
    public int TotalTestsChange { get; set; }
    public int PassedTestsChange { get; set; }
    public int FailedTestsChange { get; set; }
    public int SkippedTestsChange { get; set; }
    public double PassRateChange { get; set; }
    public TimeSpan DurationChange { get; set; }
}

public class TestComparison
{
    public string TestName { get; set; } = string.Empty;
    public TestStatus? CurrentStatus { get; set; }
    public TestStatus? PreviousStatus { get; set; }
    public TimeSpan? CurrentDuration { get; set; }
    public TimeSpan? PreviousDuration { get; set; }
    public bool StatusChanged { get; set; }
    public TimeSpan? DurationChange { get; set; }
    public TestChangeType ChangeType { get; set; }
}

public class TrendAnalysis
{
    public string Environment { get; set; } = string.Empty;
    public int AnalysisPeriod { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalReports { get; set; }
    public TrendMetric PassRateTrend { get; set; } = new();
    public TrendMetric TestCountTrend { get; set; } = new();
    public TrendMetric DurationTrend { get; set; } = new();
    public StabilityMetrics StabilityMetrics { get; set; } = new();
    public List<ProblematicTest> ProblematicTests { get; set; } = new();
}

public class TrendMetric
{
    public string MetricName { get; set; } = string.Empty;
    public TrendDirection Trend { get; set; }
    public double ChangePercentage { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

public class StabilityMetrics
{
    public double PassRateStandardDeviation { get; set; }
    public double TestCountStandardDeviation { get; set; }
    public double ConsistencyScore { get; set; }
}

public class ProblematicTest
{
    public string TestName { get; set; } = string.Empty;
    public double FailureRate { get; set; }
    public bool IsFlaky { get; set; }
    public int TotalExecutions { get; set; }
    public int FailedExecutions { get; set; }
}

public enum TestChangeType
{
    NoChange,
    New,
    Removed,
    Fixed,
    Regressed,
    StatusChanged
}

public enum OverallTrend
{
    Improving,
    Stable,
    Declining
}

public enum TrendDirection
{
    Increasing,
    Stable,
    Decreasing
}