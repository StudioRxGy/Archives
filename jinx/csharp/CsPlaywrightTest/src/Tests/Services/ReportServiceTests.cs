using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// 报告服务测试
/// </summary>
public class ReportServiceTests : IDisposable
{
    private readonly Mock<ILogger<ReportService>> _mockLogger;
    private readonly Mock<ILogger<HtmlReportGenerator>> _mockHtmlLogger;
    private readonly HtmlTemplateProvider _templateProvider;
    private readonly HtmlReportGenerator _htmlGenerator;
    private readonly ReportService _reportService;
    private readonly string _tempDirectory;

    public ReportServiceTests()
    {
        _mockLogger = new Mock<ILogger<ReportService>>();
        _mockHtmlLogger = new Mock<ILogger<HtmlReportGenerator>>();
        _templateProvider = new HtmlTemplateProvider();
        _htmlGenerator = new HtmlReportGenerator(_mockHtmlLogger.Object, _templateProvider);
        var mockAllureLogger = new Mock<ILogger<AllureReportGenerator>>();
        var allureGenerator = new AllureReportGenerator(mockAllureLogger.Object);
        _reportService = new ReportService(_mockLogger.Object, _htmlGenerator, allureGenerator);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task GenerateReportAsync_WithHtmlFormat_ShouldGenerateHtmlReport()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, "test-report.html");

        // Act
        var result = await _reportService.GenerateReportAsync(testReport, outputPath, "html");

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task GenerateReportAsync_WithUnsupportedFormat_ShouldThrowNotSupportedException()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, "test-report.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _reportService.GenerateReportAsync(testReport, outputPath, "pdf"));
    }

    [Fact]
    public async Task GenerateMultipleReportsAsync_WithMultipleFormats_ShouldGenerateAllFormats()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var formats = new[] { "html", "htm" };

        // Act
        var results = await _reportService.GenerateMultipleReportsAsync(testReport, _tempDirectory, formats);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, path => Assert.True(File.Exists(path)));
    }

    [Fact]
    public void GetSupportedFormats_ShouldReturnSupportedFormats()
    {
        // Act
        var formats = _reportService.GetSupportedFormats();

        // Assert
        Assert.Contains("html", formats);
        Assert.Contains("htm", formats);
    }

    [Fact]
    public void GenerateReportSummary_WithValidReport_ShouldReturnCorrectSummary()
    {
        // Arrange
        var testReport = CreateSampleTestReport();

        // Act
        var summary = _reportService.GenerateReportSummary(testReport);

        // Assert
        Assert.Equal(testReport.ReportName, summary.ReportName);
        Assert.Equal(testReport.Environment, summary.Environment);
        Assert.Equal(testReport.Summary.TotalTests, summary.TotalTests);
        Assert.Equal(testReport.Summary.PassedTests, summary.PassedTests);
        Assert.Equal(testReport.Summary.FailedTests, summary.FailedTests);
        Assert.Equal(testReport.Summary.PassRate, summary.PassRate);
    }

    [Fact]
    public void GenerateReportSummary_WithFailedTests_ShouldIndicateFailures()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        testReport.AddTestResult(new TestResult
        {
            TestName = "FailedTest",
            Status = TestStatus.Failed,
            Duration = TimeSpan.FromSeconds(1)
        });

        // Act
        var summary = _reportService.GenerateReportSummary(testReport);

        // Assert
        Assert.True(summary.HasFailures);
        Assert.Equal("失败", summary.GetStatusDescription());
    }

    [Fact]
    public void GenerateReportSummary_WithOnlyPassedTests_ShouldIndicateSuccess()
    {
        // Arrange
        var testReport = CreateSampleTestReport();

        // Act
        var summary = _reportService.GenerateReportSummary(testReport);

        // Assert
        Assert.False(summary.HasFailures);
        Assert.Equal("成功", summary.GetStatusDescription());
    }

    [Fact]
    public void GenerateReportSummary_ShouldGenerateCorrectBriefDescription()
    {
        // Arrange
        var testReport = CreateSampleTestReport();

        // Act
        var summary = _reportService.GenerateReportSummary(testReport);

        // Assert
        var description = summary.GetBriefDescription();
        Assert.Contains(summary.TotalTests.ToString(), description);
        Assert.Contains(summary.PassedTests.ToString(), description);
        Assert.Contains(summary.FailedTests.ToString(), description);
        Assert.Contains($"{summary.PassRate:F1}%", description);
    }

    [Fact]
    public async Task GenerateReportAsync_WithNullTestReport_ShouldThrowArgumentNullException()
    {
        // Arrange
        TestReport? testReport = null;
        var outputPath = Path.Combine(_tempDirectory, "test-report.html");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _reportService.GenerateReportAsync(testReport!, outputPath));
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyOutputPath_ShouldThrowArgumentException()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _reportService.GenerateReportAsync(testReport, outputPath));
    }

    [Fact]
    public async Task GenerateMultipleReportsAsync_WithInvalidFormat_ShouldSkipInvalidFormat()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var formats = new[] { "html", "invalid", "htm" };

        // Act
        var results = await _reportService.GenerateMultipleReportsAsync(testReport, _tempDirectory, formats);

        // Assert
        Assert.Equal(2, results.Count); // 只有html和htm格式成功
        Assert.All(results, path => Assert.True(File.Exists(path)));
    }

    [Fact]
    public void RegisterGenerator_WithNewFormat_ShouldAddToSupportedFormats()
    {
        // Arrange
        var mockGenerator = new Mock<EnterpriseAutomationFramework.Core.Interfaces.IReportGenerator>();
        var newFormat = "xml";

        // Act
        _reportService.RegisterGenerator(newFormat, mockGenerator.Object);
        var formats = _reportService.GetSupportedFormats();

        // Assert
        Assert.Contains(newFormat, formats);
    }

    [Fact]
    public async Task GenerateReportAsync_WithRegisteredGenerator_ShouldUseRegisteredGenerator()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, "test-report.xml");
        var mockGenerator = new Mock<EnterpriseAutomationFramework.Core.Interfaces.IReportGenerator>();
        mockGenerator.Setup(g => g.GenerateReportAsync(It.IsAny<TestReport>(), It.IsAny<string>()))
                   .ReturnsAsync(outputPath);

        _reportService.RegisterGenerator("xml", mockGenerator.Object);

        // Act
        var result = await _reportService.GenerateReportAsync(testReport, outputPath, "xml");

        // Assert
        Assert.Equal(outputPath, result);
        mockGenerator.Verify(g => g.GenerateReportAsync(testReport, outputPath), Times.Once);
    }

    [Theory]
    [InlineData("HTML")]
    [InlineData("Html")]
    [InlineData("HTM")]
    [InlineData("Htm")]
    public async Task GenerateReportAsync_WithCaseInsensitiveFormat_ShouldWork(string format)
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_tempDirectory, $"test-report.{format.ToLower()}");

        // Act
        var result = await _reportService.GenerateReportAsync(testReport, outputPath, format);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}