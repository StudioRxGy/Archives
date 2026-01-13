using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// 趋势分析服务测试
/// </summary>
public class TrendAnalysisServiceTests : IDisposable
{
    private readonly Mock<ILogger<TrendAnalysisService>> _mockLogger;
    private readonly TrendAnalysisService _trendService;
    private readonly string _tempDirectory;

    public TrendAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<TrendAnalysisService>>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _trendService = new TrendAnalysisService(_mockLogger.Object, _tempDirectory);
    }

    [Fact]
    public async Task SaveReportHistoryAsync_WithValidReport_ShouldCreateHistoryFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();

        // Act
        var historyFile = await _trendService.SaveReportHistoryAsync(testReport);

        // Assert
        Assert.True(File.Exists(historyFile));
        Assert.Contains(testReport.Environment, historyFile);
        
        var content = await File.ReadAllTextAsync(historyFile);
        Assert.Contains(testReport.ReportName, content);
        Assert.Contains(testReport.Environment, content);
    }

    [Fact]
    public async Task GetTrendDataAsync_WithNoHistoryFiles_ShouldReturnEmptyTrendData()
    {
        // Act
        var trendData = await _trendService.GetTrendDataAsync("NonExistentEnvironment");

        // Assert
        Assert.Equal(0, trendData.DataPoints);
        Assert.Empty(trendData.PassRateTrend);
        Assert.Empty(trendData.DurationTrend);
        Assert.Empty(trendData.FailuresTrend);
    }

    [Fact]
    public async Task GetTrendDataAsync_WithHistoryFiles_ShouldReturnTrendData()
    {
        // Arrange
        var environment = "TestEnv";
        var reports = CreateMultipleTestReports(environment, 5);
        
        // 保存历史数据
        foreach (var report in reports)
        {
            await _trendService.SaveReportHistoryAsync(report);
        }

        // Act
        var trendData = await _trendService.GetTrendDataAsync(environment);

        // Assert
        Assert.Equal(environment, trendData.Environment);
        Assert.Equal(5, trendData.DataPoints);
        Assert.Equal(5, trendData.PassRateTrend.Count);
        Assert.Equal(5, trendData.DurationTrend.Count);
        Assert.Equal(5, trendData.FailuresTrend.Count);
        Assert.True(trendData.AveragePassRate > 0);
    }

    [Fact]
    public async Task GetTrendDataAsync_WithDaysFilter_ShouldFilterByDate()
    {
        // Arrange
        var environment = "TestEnv";
        var oldReport = CreateSampleTestReport();
        oldReport.Environment = environment;
        oldReport.GeneratedAt = DateTime.UtcNow.AddDays(-40); // 超过30天
        
        var recentReport = CreateSampleTestReport();
        recentReport.Environment = environment;
        recentReport.GeneratedAt = DateTime.UtcNow.AddDays(-10); // 在30天内

        await _trendService.SaveReportHistoryAsync(oldReport);
        await _trendService.SaveReportHistoryAsync(recentReport);

        // Act
        var trendData = await _trendService.GetTrendDataAsync(environment, 30);

        // Assert
        Assert.Equal(1, trendData.DataPoints); // 只有最近的报告
    }

    [Fact]
    public async Task GenerateTrendReportAsync_WithValidData_ShouldReturnHtmlContent()
    {
        // Arrange
        var environment = "TestEnv";
        var reports = CreateMultipleTestReports(environment, 3);
        
        foreach (var report in reports)
        {
            await _trendService.SaveReportHistoryAsync(report);
        }

        // Act
        var htmlContent = await _trendService.GenerateTrendReportAsync(environment);

        // Assert
        Assert.Contains("趋势分析", htmlContent);
        Assert.Contains(environment, htmlContent);
        Assert.Contains("通过率趋势", htmlContent);
        Assert.Contains("执行时间趋势", htmlContent);
        Assert.Contains("失败测试趋势", htmlContent);
    }

    [Fact]
    public void CompareReports_WithTwoReports_ShouldReturnComparison()
    {
        // Arrange
        var currentReport = CreateSampleTestReport();
        currentReport.Summary = TestSummary.FromTestResults(new[]
        {
            new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) },
            new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(2) },
            new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(1) }
        });

        var previousReport = CreateSampleTestReport();
        previousReport.Summary = TestSummary.FromTestResults(new[]
        {
            new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) },
            new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(1) },
            new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(1) }
        });

        // Act
        var comparison = _trendService.CompareReports(currentReport, previousReport);

        // Assert
        Assert.Equal(0, comparison.TotalTestsChange); // 3 - 3 = 0
        Assert.Equal(1, comparison.PassedTestsChange); // 2 - 1 = 1
        Assert.Equal(-1, comparison.FailedTestsChange); // 1 - 2 = -1
        Assert.True(comparison.PassRateChange > 0); // 通过率提高了
    }

    [Theory]
    [InlineData(new double[] { 80, 85, 90, 95 }, TrendDirection.Increasing)]
    [InlineData(new double[] { 95, 90, 85, 80 }, TrendDirection.Decreasing)]
    [InlineData(new double[] { 85, 86, 85, 86 }, TrendDirection.Stable)]
    public async Task GetTrendDataAsync_WithDifferentTrends_ShouldDetectCorrectDirection(double[] passRates, TrendDirection expectedDirection)
    {
        // Arrange
        var environment = "TestEnv";
        var reports = new List<TestReport>();

        for (int i = 0; i < passRates.Length; i++)
        {
            var report = CreateSampleTestReport();
            report.Environment = environment;
            report.GeneratedAt = DateTime.UtcNow.AddDays(-passRates.Length + i);
            
            // 创建测试结果以达到指定的通过率
            var totalTests = 100;
            var passedTests = (int)(totalTests * passRates[i] / 100);
            var failedTests = totalTests - passedTests;

            var testResults = new List<TestResult>();
            for (int j = 0; j < passedTests; j++)
            {
                testResults.Add(new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) });
            }
            for (int j = 0; j < failedTests; j++)
            {
                testResults.Add(new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(1) });
            }

            report.AddTestResults(testResults);
            reports.Add(report);
        }

        foreach (var report in reports)
        {
            await _trendService.SaveReportHistoryAsync(report);
        }

        // Act
        var trendData = await _trendService.GetTrendDataAsync(environment);

        // Assert
        Assert.Equal(expectedDirection, trendData.PassRateTrendDirection);
    }

    [Fact]
    public async Task SaveReportHistoryAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidService = new TrendAnalysisService(_mockLogger.Object, "/invalid/path/that/does/not/exist");
        var testReport = CreateSampleTestReport();

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            invalidService.SaveReportHistoryAsync(testReport));
    }

    [Fact]
    public async Task GetTrendDataAsync_WithCorruptedHistoryFile_ShouldSkipCorruptedFile()
    {
        // Arrange
        var environment = "TestEnv";
        var validReport = CreateSampleTestReport();
        validReport.Environment = environment;

        // 创建一个有效的历史文件
        await _trendService.SaveReportHistoryAsync(validReport);

        // 创建一个损坏的历史文件
        var corruptedFile = Path.Combine(_tempDirectory, $"{environment}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_corrupted.json");
        await File.WriteAllTextAsync(corruptedFile, "{ invalid json content");

        // Act
        var trendData = await _trendService.GetTrendDataAsync(environment);

        // Assert
        Assert.Equal(1, trendData.DataPoints); // 只有有效的文件被处理
    }

    [Fact]
    public async Task GetTrendDataAsync_WithEmptyEnvironment_ShouldReturnEmptyTrendData()
    {
        // Act
        var trendData = await _trendService.GetTrendDataAsync("");

        // Assert
        Assert.Equal(0, trendData.DataPoints);
        Assert.Empty(trendData.PassRateTrend);
    }

    [Fact]
    public async Task SaveReportHistoryAsync_WithNullReport_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _trendService.SaveReportHistoryAsync(null!));
    }

    /// <summary>
    /// 创建示例测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateSampleTestReport()
    {
        var report = new TestReport
        {
            ReportName = "Sample Test Report",
            Environment = "Development",
            GeneratedAt = DateTime.UtcNow,
            TestStartTime = DateTime.UtcNow.AddMinutes(-10),
            TestEndTime = DateTime.UtcNow
        };

        // 添加一些示例测试结果
        report.AddTestResult(new TestResult
        {
            TestName = "PassedTest1",
            Status = TestStatus.Passed,
            Duration = TimeSpan.FromSeconds(1.5)
        });

        report.AddTestResult(new TestResult
        {
            TestName = "PassedTest2",
            Status = TestStatus.Passed,
            Duration = TimeSpan.FromSeconds(2.1)
        });

        report.AddTestResult(new TestResult
        {
            TestName = "SkippedTest",
            Status = TestStatus.Skipped,
            Duration = TimeSpan.Zero
        });

        return report;
    }

    /// <summary>
    /// 创建多个测试报告
    /// </summary>
    /// <param name="environment">环境名称</param>
    /// <param name="count">报告数量</param>
    /// <returns>测试报告列表</returns>
    private List<TestReport> CreateMultipleTestReports(string environment, int count)
    {
        var reports = new List<TestReport>();

        for (int i = 0; i < count; i++)
        {
            var report = CreateSampleTestReport();
            report.Environment = environment;
            report.GeneratedAt = DateTime.UtcNow.AddDays(-count + i);
            report.ReportName = $"Report_{i + 1}";

            // 添加一些变化的测试结果
            if (i % 2 == 0)
            {
                report.AddTestResult(new TestResult
                {
                    TestName = $"FailedTest_{i}",
                    Status = TestStatus.Failed,
                    Duration = TimeSpan.FromSeconds(1)
                });
            }

            reports.Add(report);
        }

        return reports;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}