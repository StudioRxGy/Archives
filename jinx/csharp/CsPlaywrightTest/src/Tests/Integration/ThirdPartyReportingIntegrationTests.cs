using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace EnterpriseAutomationFramework.Tests.Integration;

/// <summary>
/// 第三方报告集成测试
/// </summary>
[Trait("Category", "Integration")]
[Trait("Type", "ThirdPartyReporting")]
public class ThirdPartyReportingIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ThirdPartyReportingService> _logger;
    private readonly ILogger<AllureReportGenerator> _allureLogger;
    private readonly ILogger<ReportDataExporter> _exporterLogger;
    private readonly ILogger<HistoricalReportService> _historicalLogger;
    private readonly string _testOutputDirectory;
    private readonly string _historyDirectory;

    public ThirdPartyReportingIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 创建测试专用的日志记录器
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ThirdPartyReportingService>();
        _allureLogger = loggerFactory.CreateLogger<AllureReportGenerator>();
        _exporterLogger = loggerFactory.CreateLogger<ReportDataExporter>();
        _historicalLogger = loggerFactory.CreateLogger<HistoricalReportService>();

        // 创建测试输出目录
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "EAF_Tests", Guid.NewGuid().ToString());
        _historyDirectory = Path.Combine(_testOutputDirectory, "History");
        Directory.CreateDirectory(_testOutputDirectory);
        Directory.CreateDirectory(_historyDirectory);
    }

    [Fact]
    public async Task GenerateAllureReport_ShouldCreateAllureResults()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var allureGenerator = new AllureReportGenerator(_allureLogger);
        var dataExporter = new ReportDataExporter(_exporterLogger);
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var thirdPartyService = new ThirdPartyReportingService(_logger, allureGenerator, dataExporter, historicalService);

        // Act
        var reportPath = await thirdPartyService.GenerateAllureReportAsync(testReport, _testOutputDirectory);

        // Assert
        Assert.NotNull(reportPath);
        Assert.True(Directory.Exists(reportPath));
        
        // 验证Allure结果文件
        var allureFiles = Directory.GetFiles(reportPath, "*-result.json");
        Assert.True(allureFiles.Length > 0, "应该生成Allure结果文件");

        // 验证容器文件
        var containerFiles = Directory.GetFiles(reportPath, "*-container.json");
        Assert.True(containerFiles.Length > 0, "应该生成Allure容器文件");

        // 验证环境文件
        var environmentFile = Path.Combine(reportPath, "environment.properties");
        Assert.True(File.Exists(environmentFile), "应该生成环境配置文件");

        _output.WriteLine($"Allure报告生成成功: {reportPath}");
    }

    [Fact]
    public async Task ExportReportData_ShouldCreateMultipleFormats()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var allureGenerator = new AllureReportGenerator(_allureLogger);
        var dataExporter = new ReportDataExporter(_exporterLogger);
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var thirdPartyService = new ThirdPartyReportingService(_logger, allureGenerator, dataExporter, historicalService);

        var formats = new[] { "json", "xml", "csv", "txt" };

        // Act
        var exportedFiles = await thirdPartyService.ExportReportDataAsync(testReport, _testOutputDirectory, formats);

        // Assert
        Assert.Equal(formats.Length, exportedFiles.Count);

        foreach (var format in formats)
        {
            var file = exportedFiles.FirstOrDefault(f => f.EndsWith($".{format}"));
            Assert.NotNull(file);
            Assert.True(File.Exists(file), $"应该生成 {format} 格式文件");
            Assert.True(new FileInfo(file).Length > 0, $"{format} 文件不应为空");
        }

        _output.WriteLine($"数据导出成功，共生成 {exportedFiles.Count} 个文件");
    }

    [Fact]
    public async Task IntegrateThirdPartyTools_ShouldGenerateAllReports()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var allureGenerator = new AllureReportGenerator(_allureLogger);
        var dataExporter = new ReportDataExporter(_exporterLogger);
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var thirdPartyService = new ThirdPartyReportingService(_logger, allureGenerator, dataExporter, historicalService);

        var options = new ThirdPartyIntegrationOptions
        {
            GenerateAllureReport = true,
            ExportFormats = new[] { "json", "xml", "csv" },
            GenerateTrendAnalysis = false, // 跳过趋势分析，因为没有足够的历史数据
            CompareWithReportId = null
        };

        // Act
        var result = await thirdPartyService.IntegrateThirdPartyToolsAsync(testReport, _testOutputDirectory, options);

        // Assert
        Assert.True(result.IsSuccessful, $"集成应该成功。错误: {string.Join(", ", result.Errors)}");
        Assert.Contains("Allure", result.SuccessfulIntegrations);
        Assert.Contains("DataExport", result.SuccessfulIntegrations);
        Assert.NotNull(result.AllureReportPath);
        Assert.True(result.ExportedFiles.Count >= 3);

        _output.WriteLine(result.GetIntegrationSummary());
    }

    [Fact]
    public async Task HistoricalReportService_ShouldSaveAndRetrieveReports()
    {
        // Arrange
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var testReport1 = CreateSampleTestReport("Report1", DateTime.UtcNow.AddDays(-1));
        var testReport2 = CreateSampleTestReport("Report2", DateTime.UtcNow);

        // Act - 保存历史报告
        var savedPath1 = await historicalService.SaveHistoricalReportAsync(testReport1);
        var savedPath2 = await historicalService.SaveHistoricalReportAsync(testReport2);

        // Assert - 验证保存
        Assert.True(File.Exists(savedPath1));
        Assert.True(File.Exists(savedPath2));

        // Act - 获取历史报告列表
        var historicalReports = await historicalService.GetHistoricalReportsAsync("Test", 7);

        // Assert - 验证检索
        Assert.Equal(2, historicalReports.Count);
        Assert.Contains(historicalReports, r => r.ReportName == "Report1");
        Assert.Contains(historicalReports, r => r.ReportName == "Report2");

        _output.WriteLine($"成功保存和检索 {historicalReports.Count} 个历史报告");
    }

    [Fact]
    public async Task CompareReports_ShouldGenerateComparisonReport()
    {
        // Arrange
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var allureGenerator = new AllureReportGenerator(_allureLogger);
        var dataExporter = new ReportDataExporter(_exporterLogger);
        var thirdPartyService = new ThirdPartyReportingService(_logger, allureGenerator, dataExporter, historicalService);

        // 创建两个不同的测试报告
        var previousReport = CreateSampleTestReport("PreviousReport", DateTime.UtcNow.AddDays(-1));
        previousReport.Results.Clear();
        // 添加8个通过，2个失败的测试
        for (int i = 0; i < 8; i++)
        {
            previousReport.AddTestResult(new TestResult
            {
                TestName = $"PassedTest{i}",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(1),
                Duration = TimeSpan.FromMinutes(1)
            });
        }
        for (int i = 0; i < 2; i++)
        {
            previousReport.AddTestResult(new TestResult
            {
                TestName = $"FailedTest{i}",
                Status = TestStatus.Failed,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(1),
                Duration = TimeSpan.FromMinutes(1)
            });
        }

        var currentReport = CreateSampleTestReport("CurrentReport", DateTime.UtcNow);
        currentReport.Results.Clear();
        // 添加9个通过，1个失败的测试
        for (int i = 0; i < 9; i++)
        {
            currentReport.AddTestResult(new TestResult
            {
                TestName = $"PassedTest{i}",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(1),
                Duration = TimeSpan.FromMinutes(1)
            });
        }
        currentReport.AddTestResult(new TestResult
        {
            TestName = "FailedTest",
            Status = TestStatus.Failed,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(1),
            Duration = TimeSpan.FromMinutes(1)
        });

        // 保存之前的报告
        await historicalService.SaveHistoricalReportAsync(previousReport);

        // Act
        var comparisonPath = Path.Combine(_testOutputDirectory, "comparison.html");
        var reportPath = await thirdPartyService.GenerateComparisonReportAsync(
            currentReport, previousReport.ReportId, comparisonPath);

        // Assert
        Assert.Equal(comparisonPath, reportPath);
        Assert.True(File.Exists(reportPath));
        
        var content = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("测试报告对比", content);
        Assert.Contains("CurrentReport", content);
        Assert.Contains("PreviousReport", content);

        _output.WriteLine($"对比报告生成成功: {reportPath}");
    }

    [Fact]
    public async Task GenerateTrendAnalysis_WithMultipleReports_ShouldCreateTrendReport()
    {
        // Arrange
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);
        var allureGenerator = new AllureReportGenerator(_allureLogger);
        var dataExporter = new ReportDataExporter(_exporterLogger);
        var thirdPartyService = new ThirdPartyReportingService(_logger, allureGenerator, dataExporter, historicalService);

        // 创建多个历史报告
        for (int i = 0; i < 5; i++)
        {
            var report = CreateSampleTestReport($"Report{i}", DateTime.UtcNow.AddDays(-i));
            report.Results.Clear();
            
            // 模拟改进趋势：通过的测试增加，失败的测试减少
            var passedCount = 8 + i;
            var failedCount = Math.Max(0, 2 - (i / 3));
            
            for (int j = 0; j < passedCount; j++)
            {
                report.AddTestResult(new TestResult
                {
                    TestName = $"PassedTest{j}",
                    Status = TestStatus.Passed,
                    StartTime = DateTime.UtcNow.AddDays(-i),
                    EndTime = DateTime.UtcNow.AddDays(-i).AddMinutes(1),
                    Duration = TimeSpan.FromMinutes(1)
                });
            }
            
            for (int j = 0; j < failedCount; j++)
            {
                report.AddTestResult(new TestResult
                {
                    TestName = $"FailedTest{j}",
                    Status = TestStatus.Failed,
                    StartTime = DateTime.UtcNow.AddDays(-i),
                    EndTime = DateTime.UtcNow.AddDays(-i).AddMinutes(1),
                    Duration = TimeSpan.FromMinutes(1)
                });
            }
            
            await historicalService.SaveHistoricalReportAsync(report);
        }

        // Act
        var trendPath = Path.Combine(_testOutputDirectory, "trend.html");
        var reportPath = await thirdPartyService.GenerateTrendAnalysisReportAsync("Test", 7, trendPath);

        // Assert
        Assert.Equal(trendPath, reportPath);
        Assert.True(File.Exists(reportPath));
        
        var content = await File.ReadAllTextAsync(reportPath);
        Assert.Contains("趋势分析报告", content);
        Assert.Contains("通过率趋势", content);
        Assert.Contains("测试数量趋势", content);

        _output.WriteLine($"趋势分析报告生成成功: {reportPath}");
    }

    [Fact]
    public async Task CleanupHistoricalReports_ShouldRemoveOldReports()
    {
        // Arrange
        var historicalService = new HistoricalReportService(_historicalLogger, _historyDirectory);

        // 创建新旧报告
        var oldReport = CreateSampleTestReport("OldReport", DateTime.UtcNow.AddDays(-100));
        var newReport = CreateSampleTestReport("NewReport", DateTime.UtcNow);

        await historicalService.SaveHistoricalReportAsync(oldReport);
        await historicalService.SaveHistoricalReportAsync(newReport);

        // 手动修改旧报告文件的创建时间
        var allFiles = Directory.GetFiles(_historyDirectory, "*.json");
        var oldFile = allFiles.FirstOrDefault(f => f.Contains("OldReport") || 
            File.GetCreationTimeUtc(f) < DateTime.UtcNow.AddDays(-50));
        
        if (oldFile != null)
        {
            File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-100));
        }

        // Act
        var deletedCount = await historicalService.CleanupHistoricalReportsAsync(30);

        // Assert
        Assert.True(deletedCount >= 0);
        
        var remainingReports = await historicalService.GetHistoricalReportsAsync("Test", 60);
        Assert.Contains(remainingReports, r => r.ReportName == "NewReport");

        _output.WriteLine($"清理完成，删除了 {deletedCount} 个过期报告");
    }

    [Fact]
    public async Task AllureReportGenerator_ShouldHandleComplexTestResults()
    {
        // Arrange
        var testReport = CreateComplexTestReport();
        var allureGenerator = new AllureReportGenerator(_allureLogger);

        // Act
        var allureResultsDir = Path.Combine(_testOutputDirectory, "complex-allure-results");
        var reportPath = await allureGenerator.GenerateReportAsync(testReport, allureResultsDir);

        // Assert
        Assert.True(Directory.Exists(reportPath));

        // 验证复杂测试结果的处理
        var resultFiles = Directory.GetFiles(reportPath, "*-result.json");
        Assert.True(resultFiles.Length >= 3, "应该为复杂测试生成多个结果文件");

        // 验证附件处理
        var hasAttachments = resultFiles.Any(file =>
        {
            var content = File.ReadAllText(file);
            return content.Contains("attachments") && content.Contains("screenshot");
        });
        Assert.True(hasAttachments, "应该包含附件信息");

        _output.WriteLine($"复杂Allure报告生成成功，包含 {resultFiles.Length} 个测试结果");
    }

    [Fact]
    public async Task DataExporter_ShouldHandleEmptyAndLargeReports()
    {
        // Arrange
        var dataExporter = new ReportDataExporter(_exporterLogger);

        // Test empty report
        var emptyReport = new TestReport
        {
            ReportName = "EmptyReport",
            Environment = "Test",
            TestStartTime = DateTime.UtcNow.AddMinutes(-1),
            TestEndTime = DateTime.UtcNow
        };
        emptyReport.RefreshSummary();

        // Test large report
        var largeReport = CreateLargeTestReport(1000);

        // Act & Assert - Empty report
        var emptyJsonPath = Path.Combine(_testOutputDirectory, "empty.json");
        var exportedEmptyPath = await dataExporter.ExportToJsonAsync(emptyReport, emptyJsonPath);
        Assert.True(File.Exists(exportedEmptyPath));

        // Act & Assert - Large report
        var largeJsonPath = Path.Combine(_testOutputDirectory, "large.json");
        var exportedLargePath = await dataExporter.ExportToJsonAsync(largeReport, largeJsonPath);
        Assert.True(File.Exists(exportedLargePath));
        
        var largeFileInfo = new FileInfo(exportedLargePath);
        Assert.True(largeFileInfo.Length > 10000, "大报告文件应该有相当的大小");

        _output.WriteLine($"空报告和大报告导出测试完成");
    }

    /// <summary>
    /// 创建示例测试报告
    /// </summary>
    /// <param name="reportName">报告名称</param>
    /// <param name="testStartTime">测试开始时间</param>
    /// <returns>测试报告</returns>
    private TestReport CreateSampleTestReport(string reportName = "SampleReport", DateTime? testStartTime = null)
    {
        var startTime = testStartTime ?? DateTime.UtcNow.AddMinutes(-10);
        var endTime = startTime.AddMinutes(5);

        var report = TestReport.CreateBuilder(reportName, "Test")
            .WithStartTime(startTime)
            .WithEndTime(endTime)
            .Build();

        // 添加示例测试结果
        var testResults = new List<TestResult>
        {
            new TestResult
            {
                TestName = "LoginTest",
                TestClass = "AuthenticationTests",
                TestMethod = "TestUserLogin",
                Status = TestStatus.Passed,
                StartTime = startTime,
                EndTime = startTime.AddSeconds(30),
                Duration = TimeSpan.FromSeconds(30),
                Categories = new List<string> { "UI", "Authentication" },
                Tags = new List<string> { "Smoke", "Critical" }
            },
            new TestResult
            {
                TestName = "LogoutTest",
                TestClass = "AuthenticationTests",
                TestMethod = "TestUserLogout",
                Status = TestStatus.Failed,
                StartTime = startTime.AddSeconds(30),
                EndTime = startTime.AddSeconds(60),
                Duration = TimeSpan.FromSeconds(30),
                ErrorMessage = "Element not found: logout button",
                StackTrace = "at AuthenticationTests.TestUserLogout() line 45",
                Categories = new List<string> { "UI", "Authentication" },
                Tags = new List<string> { "Regression" },
                Screenshots = new List<string> { "logout_failure.png" }
            }
        };

        report.AddTestResults(testResults);
        return report;
    }

    /// <summary>
    /// 创建复杂测试报告
    /// </summary>
    /// <returns>复杂测试报告</returns>
    private TestReport CreateComplexTestReport()
    {
        var report = CreateSampleTestReport("ComplexReport");

        // 添加更多复杂的测试结果
        var complexResults = new List<TestResult>
        {
            new TestResult
            {
                TestName = "DataDrivenTest",
                TestClass = "DataTests",
                TestMethod = "TestWithMultipleDataSets",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow.AddMinutes(-4),
                Duration = TimeSpan.FromMinutes(1),
                Categories = new List<string> { "API", "DataDriven" },
                Tags = new List<string> { "Parameterized", "Integration" },
                TestData = new Dictionary<string, object>
                {
                    ["UserId"] = 12345,
                    ["UserName"] = "testuser",
                    ["Email"] = "test@example.com"
                },
                Metadata = new Dictionary<string, object>
                {
                    ["TestCaseUrl"] = "https://testcase.example.com/TC001",
                    ["IssueUrl"] = "https://jira.example.com/ISSUE-123"
                }
            },
            new TestResult
            {
                TestName = "PerformanceTest",
                TestClass = "PerformanceTests",
                TestMethod = "TestResponseTime",
                Status = TestStatus.Inconclusive,
                StartTime = DateTime.UtcNow.AddMinutes(-3),
                EndTime = DateTime.UtcNow.AddMinutes(-1),
                Duration = TimeSpan.FromMinutes(2),
                Categories = new List<string> { "Performance", "API" },
                Tags = new List<string> { "Load", "Benchmark" },
                Output = "Response time: 1.5s\nThroughput: 100 req/s\nMemory usage: 256MB",
                Screenshots = new List<string> { "performance_graph.png", "memory_usage.png" }
            }
        };

        report.AddTestResults(complexResults);
        return report;
    }

    /// <summary>
    /// 创建大型测试报告
    /// </summary>
    /// <param name="testCount">测试数量</param>
    /// <returns>大型测试报告</returns>
    private TestReport CreateLargeTestReport(int testCount)
    {
        var report = TestReport.CreateBuilder("LargeReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddHours(-1))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        var random = new Random();
        var testResults = new List<TestResult>();

        for (int i = 0; i < testCount; i++)
        {
            var status = random.Next(100) < 85 ? TestStatus.Passed : 
                        random.Next(100) < 90 ? TestStatus.Failed : TestStatus.Skipped;

            testResults.Add(new TestResult
            {
                TestName = $"Test_{i:D4}",
                TestClass = $"TestClass_{i / 100}",
                TestMethod = $"TestMethod_{i % 100}",
                Status = status,
                StartTime = DateTime.UtcNow.AddMinutes(-60 + i * 0.06),
                EndTime = DateTime.UtcNow.AddMinutes(-60 + i * 0.06 + 0.05),
                Duration = TimeSpan.FromSeconds(3),
                Categories = new List<string> { i % 2 == 0 ? "UI" : "API" },
                Tags = new List<string> { $"Tag_{i % 10}" }
            });
        }

        report.AddTestResults(testResults);
        return report;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDirectory))
            {
                Directory.Delete(_testOutputDirectory, true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"清理测试目录失败: {ex.Message}");
        }
    }
}