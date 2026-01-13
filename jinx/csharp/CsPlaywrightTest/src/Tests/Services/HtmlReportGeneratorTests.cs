using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// HTML报告生成器测试
/// </summary>
public class HtmlReportGeneratorTests : IDisposable
{
    private readonly Mock<ILogger<HtmlReportGenerator>> _mockLogger;
    private readonly HtmlTemplateProvider _templateProvider;
    private readonly HtmlReportGenerator _generator;
    private readonly string _tempDirectory;

    public HtmlReportGeneratorTests()
    {
        _mockLogger = new Mock<ILogger<HtmlReportGenerator>>();
        _templateProvider = new HtmlTemplateProvider();
        _generator = new HtmlReportGenerator(_mockLogger.Object, _templateProvider);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task GenerateReportAsync_WithValidTestReport_ShouldCreateHtmlFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, "test-report.html");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
        
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains(testReport.ReportName, htmlContent);
        Assert.Contains(testReport.Environment, htmlContent);
        Assert.Contains("测试报告", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithValidTestReport_ShouldCreateAssetsDirectory()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, "test-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var assetsDir = Path.Combine(_tempDirectory, "assets");
        Assert.True(Directory.Exists(assetsDir));
        Assert.True(File.Exists(Path.Combine(assetsDir, "report.css")));
        Assert.True(File.Exists(Path.Combine(assetsDir, "report.js")));
    }

    [Fact]
    public async Task GenerateReportAsync_WithFailedTests_ShouldIncludeFailureDetails()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddTestResult(new TestResult
        {
            TestName = "FailedTest",
            Status = TestStatus.Failed,
            ErrorMessage = "Test failed due to assertion error",
            StackTrace = "at TestMethod() line 42",
            Duration = TimeSpan.FromSeconds(2.5)
        });

        var outputPath = Path.Combine(_tempDirectory, "failed-test-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("FailedTest", htmlContent);
        Assert.Contains("Test failed due to assertion error", htmlContent);
        Assert.Contains("失败测试详情", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithScreenshots_ShouldIncludeScreenshotGallery()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddScreenshot("screenshot1.png");
        testReport.AddScreenshot("screenshot2.png");

        var outputPath = Path.Combine(_tempDirectory, "screenshot-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("screenshot1.png", htmlContent);
        Assert.Contains("screenshot2.png", htmlContent);
        Assert.Contains("测试截图", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithSystemInfo_ShouldIncludeSystemInfoSection()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddSystemInfo("OS", "Windows 11");
        testReport.AddSystemInfo("Browser", "Chrome 120");

        var outputPath = Path.Combine(_tempDirectory, "system-info-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Windows 11", htmlContent);
        Assert.Contains("Chrome 120", htmlContent);
        Assert.Contains("系统信息", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithConfiguration_ShouldIncludeConfigurationSection()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddConfiguration("BaseUrl", "https://example.com");
        testReport.AddConfiguration("Timeout", "30000");

        var outputPath = Path.Combine(_tempDirectory, "config-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("https://example.com", htmlContent);
        Assert.Contains("30000", htmlContent);
        Assert.Contains("测试配置", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithMetadata_ShouldIncludeMetadataSection()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddMetadata("BuildNumber", "1.2.3");
        testReport.AddMetadata("Branch", "main");

        var outputPath = Path.Combine(_tempDirectory, "metadata-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("1.2.3", htmlContent);
        Assert.Contains("main", htmlContent);
        Assert.Contains("元数据", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyTestReport_ShouldGenerateBasicReport()
    {
        // Arrange
        var testReport = new TestReport
        {
            ReportName = "Empty Report",
            Environment = "Test",
            TestStartTime = DateTime.UtcNow.AddMinutes(-5),
            TestEndTime = DateTime.UtcNow
        };

        var outputPath = Path.Combine(_tempDirectory, "empty-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Empty Report", htmlContent);
        Assert.Contains("没有失败的测试", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithNonExistentDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var nonExistentDir = Path.Combine(_tempDirectory, "new-directory");
        var outputPath = Path.Combine(nonExistentDir, "test-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.True(Directory.Exists(nonExistentDir));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenerateReportAsync_WithSpecialCharacters_ShouldEscapeHtml()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddTestResult(new TestResult
        {
            TestName = "Test with <special> & \"characters\"",
            Status = TestStatus.Failed,
            ErrorMessage = "Error with <tags> & 'quotes'",
            Duration = TimeSpan.FromSeconds(1)
        });

        var outputPath = Path.Combine(_tempDirectory, "special-chars-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("&lt;special&gt;", htmlContent);
        Assert.Contains("&amp;", htmlContent);
        Assert.Contains("&quot;", htmlContent);
        Assert.DoesNotContain("<special>", htmlContent);
    }

    [Fact]
    public async Task GenerateReportAsync_WithLargeTestSuite_ShouldHandlePerformance()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        
        // 添加大量测试结果
        for (int i = 0; i < 1000; i++)
        {
            testReport.AddTestResult(new TestResult
            {
                TestName = $"Test_{i:D4}",
                Status = i % 10 == 0 ? TestStatus.Failed : TestStatus.Passed,
                Duration = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 5000)),
                StartTime = DateTime.UtcNow.AddMinutes(-i),
                EndTime = DateTime.UtcNow.AddMinutes(-i).AddMilliseconds(Random.Shared.Next(100, 5000))
            });
        }

        var outputPath = Path.Combine(_tempDirectory, "large-report.html");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _generator.GenerateReportAsync(testReport, outputPath);
        stopwatch.Stop();

        // Assert
        Assert.True(File.Exists(outputPath));
        Assert.True(stopwatch.ElapsedMilliseconds < 10000); // 应该在10秒内完成
        
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("1000", htmlContent); // 应该包含总测试数
    }

    [Theory]
    [InlineData(TestStatus.Passed, "✅")]
    [InlineData(TestStatus.Failed, "❌")]
    [InlineData(TestStatus.Skipped, "⏭️")]
    [InlineData(TestStatus.Inconclusive, "❓")]
    public async Task GenerateReportAsync_WithDifferentTestStatuses_ShouldShowCorrectIcons(TestStatus status, string expectedIcon)
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddTestResult(new TestResult
        {
            TestName = $"Test_{status}",
            Status = status,
            Duration = TimeSpan.FromSeconds(1)
        });

        var outputPath = Path.Combine(_tempDirectory, $"status-{status}-report.html");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var htmlContent = await File.ReadAllTextAsync(outputPath);
        Assert.Contains(expectedIcon, htmlContent);
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
            TestStartTime = DateTime.UtcNow.AddMinutes(-10),
            TestEndTime = DateTime.UtcNow
        };

        // 添加一些示例测试结果
        report.AddTestResult(new TestResult
        {
            TestName = "PassedTest1",
            Status = TestStatus.Passed,
            Duration = TimeSpan.FromSeconds(1.5),
            StartTime = DateTime.UtcNow.AddMinutes(-9),
            EndTime = DateTime.UtcNow.AddMinutes(-9).AddSeconds(1.5)
        });

        report.AddTestResult(new TestResult
        {
            TestName = "PassedTest2",
            Status = TestStatus.Passed,
            Duration = TimeSpan.FromSeconds(2.1),
            StartTime = DateTime.UtcNow.AddMinutes(-8),
            EndTime = DateTime.UtcNow.AddMinutes(-8).AddSeconds(2.1)
        });

        report.AddTestResult(new TestResult
        {
            TestName = "SkippedTest",
            Status = TestStatus.Skipped,
            Duration = TimeSpan.Zero,
            StartTime = DateTime.UtcNow.AddMinutes(-7),
            EndTime = DateTime.UtcNow.AddMinutes(-7)
        });

        return report;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}