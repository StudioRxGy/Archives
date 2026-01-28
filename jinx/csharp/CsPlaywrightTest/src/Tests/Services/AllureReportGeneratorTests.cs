using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// Allure报告生成器单元测试
/// </summary>
[Trait("Category", "Unit")]
[Trait("Type", "Service")]
public class AllureReportGeneratorTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<AllureReportGenerator> _logger;
    private readonly AllureReportGenerator _generator;
    private readonly string _testOutputDirectory;

    public AllureReportGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AllureReportGenerator>();
        _generator = new AllureReportGenerator(_logger);

        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "AllureTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);
    }

    [Fact]
    public async Task GenerateReportAsync_WithValidTestReport_ShouldCreateAllureFiles()
    {
        // Arrange
        var testReport = CreateTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(Directory.Exists(outputPath));

        // 验证生成的文件
        var resultFiles = Directory.GetFiles(outputPath, "*-result.json");
        Assert.True(resultFiles.Length > 0, "应该生成测试结果文件");

        var containerFiles = Directory.GetFiles(outputPath, "*-container.json");
        Assert.True(containerFiles.Length > 0, "应该生成容器文件");

        var environmentFile = Path.Combine(outputPath, "environment.properties");
        Assert.True(File.Exists(environmentFile), "应该生成环境文件");

        var categoriesFile = Path.Combine(outputPath, "categories.json");
        Assert.True(File.Exists(categoriesFile), "应该生成分类文件");

        var executorFile = Path.Combine(outputPath, "executor.json");
        Assert.True(File.Exists(executorFile), "应该生成执行器文件");
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyTestReport_ShouldCreateBasicFiles()
    {
        // Arrange
        var testReport = new TestReport
        {
            ReportName = "EmptyReport",
            Environment = "Test",
            TestStartTime = DateTime.UtcNow.AddMinutes(-1),
            TestEndTime = DateTime.UtcNow
        };
        testReport.RefreshSummary();

        var outputPath = Path.Combine(_testOutputDirectory, "empty-allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(Directory.Exists(outputPath));

        // 即使没有测试结果，也应该生成基本文件
        var environmentFile = Path.Combine(outputPath, "environment.properties");
        Assert.True(File.Exists(environmentFile));

        var categoriesFile = Path.Combine(outputPath, "categories.json");
        Assert.True(File.Exists(categoriesFile));
    }

    [Fact]
    public async Task GenerateReportAsync_WithFailedTests_ShouldIncludeErrorDetails()
    {
        // Arrange
        var testReport = CreateTestReportWithFailures();
        var outputPath = Path.Combine(_testOutputDirectory, "failed-allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);

        var resultFiles = Directory.GetFiles(outputPath, "*-result.json");
        Assert.True(resultFiles.Length > 0);

        // 验证失败测试的错误信息
        var hasFailedTest = false;
        foreach (var file in resultFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("\"status\":\"failed\""))
            {
                hasFailedTest = true;
                Assert.Contains("statusDetails", content);
                Assert.Contains("Test failed with assertion error", content);
                break;
            }
        }

        Assert.True(hasFailedTest, "应该包含失败测试的详细信息");
    }

    [Fact]
    public async Task GenerateReportAsync_WithTestCategories_ShouldIncludeLabels()
    {
        // Arrange
        var testReport = CreateTestReportWithCategories();
        var outputPath = Path.Combine(_testOutputDirectory, "categories-allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);

        var resultFiles = Directory.GetFiles(outputPath, "*-result.json");
        Assert.True(resultFiles.Length > 0);

        // 验证标签信息
        var hasLabels = false;
        foreach (var file in resultFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("labels"))
            {
                hasLabels = true;
                Assert.Contains("UI", content);
                Assert.Contains("API", content);
                Assert.Contains("Critical", content);
                break;
            }
        }

        Assert.True(hasLabels, "应该包含测试分类标签");
    }

    [Fact]
    public async Task GenerateReportAsync_WithScreenshots_ShouldIncludeAttachments()
    {
        // Arrange
        var testReport = CreateTestReportWithScreenshots();
        var outputPath = Path.Combine(_testOutputDirectory, "screenshots-allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);

        var resultFiles = Directory.GetFiles(outputPath, "*-result.json");
        Assert.True(resultFiles.Length > 0);

        // 验证附件信息
        var hasAttachments = false;
        foreach (var file in resultFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("attachments"))
            {
                hasAttachments = true;
                Assert.Contains("Screenshot", content);
                Assert.Contains("image/png", content);
                break;
            }
        }

        Assert.True(hasAttachments, "应该包含截图附件");
    }

    [Fact]
    public async Task GenerateReportAsync_ShouldCreateValidEnvironmentProperties()
    {
        // Arrange
        var testReport = CreateTestReport();
        testReport.AddSystemInfo("OS", "Windows 11");
        testReport.AddSystemInfo("Browser", "Chrome 120");
        testReport.AddConfiguration("Timeout", "30000");
        testReport.AddConfiguration("Headless", "false");

        var outputPath = Path.Combine(_testOutputDirectory, "env-allure-results");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var environmentFile = Path.Combine(outputPath, "environment.properties");
        Assert.True(File.Exists(environmentFile));

        var content = await File.ReadAllTextAsync(environmentFile);
        Assert.Contains("Environment=Test", content);
        Assert.Contains("System.OS=Windows 11", content);
        Assert.Contains("System.Browser=Chrome 120", content);
        Assert.Contains("Config.Timeout=30000", content);
        Assert.Contains("Config.Headless=false", content);
    }

    [Theory]
    [InlineData(TestStatus.Passed, "passed")]
    [InlineData(TestStatus.Failed, "failed")]
    [InlineData(TestStatus.Skipped, "skipped")]
    [InlineData(TestStatus.Inconclusive, "broken")]
    public async Task GenerateReportAsync_ShouldMapTestStatusCorrectly(TestStatus inputStatus, string expectedAllureStatus)
    {
        // Arrange
        var testReport = new TestReport
        {
            ReportName = "StatusTest",
            Environment = "Test",
            TestStartTime = DateTime.UtcNow.AddMinutes(-1),
            TestEndTime = DateTime.UtcNow
        };

        testReport.AddTestResult(new TestResult
        {
            TestName = "StatusTestCase",
            Status = inputStatus,
            StartTime = DateTime.UtcNow.AddMinutes(-1),
            EndTime = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(1)
        });

        var outputPath = Path.Combine(_testOutputDirectory, $"status-{inputStatus}-allure-results");

        // Act
        await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        var resultFiles = Directory.GetFiles(outputPath, "*-result.json");
        Assert.Single(resultFiles);

        var content = await File.ReadAllTextAsync(resultFiles[0]);
        Assert.Contains($"\"status\":\"{expectedAllureStatus}\"", content);
    }

    [Fact]
    public async Task GenerateReportAsync_WithInvalidOutputPath_ShouldCreateDirectory()
    {
        // Arrange
        var testReport = CreateTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "nested", "deep", "path", "allure-results");

        // Act
        var result = await _generator.GenerateReportAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(Directory.Exists(outputPath));
    }

    /// <summary>
    /// 创建测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReport()
    {
        var report = TestReport.CreateBuilder("TestReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-5))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "SampleTest",
            TestClass = "SampleTestClass",
            TestMethod = "TestMethod",
            Status = TestStatus.Passed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-4),
            Duration = TimeSpan.FromMinutes(1)
        });

        return report;
    }

    /// <summary>
    /// 创建包含失败测试的报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithFailures()
    {
        var report = TestReport.CreateBuilder("FailedTestReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-5))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "FailedTest",
            TestClass = "FailedTestClass",
            TestMethod = "TestFailedMethod",
            Status = TestStatus.Failed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-4),
            Duration = TimeSpan.FromMinutes(1),
            ErrorMessage = "Test failed with assertion error",
            StackTrace = "at FailedTestClass.TestFailedMethod() line 42"
        });

        return report;
    }

    /// <summary>
    /// 创建包含分类的测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithCategories()
    {
        var report = TestReport.CreateBuilder("CategorizedTestReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-5))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "UITest",
            TestClass = "UITestClass",
            TestMethod = "TestUIMethod",
            Status = TestStatus.Passed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-4),
            Duration = TimeSpan.FromMinutes(1),
            Categories = new List<string> { "UI", "Smoke" },
            Tags = new List<string> { "Critical", "Regression" }
        });

        report.AddTestResult(new TestResult
        {
            TestName = "APITest",
            TestClass = "APITestClass",
            TestMethod = "TestAPIMethod",
            Status = TestStatus.Passed,
            StartTime = DateTime.UtcNow.AddMinutes(-4),
            EndTime = DateTime.UtcNow.AddMinutes(-3),
            Duration = TimeSpan.FromMinutes(1),
            Categories = new List<string> { "API", "Integration" },
            Tags = new List<string> { "Medium", "Functional" }
        });

        return report;
    }

    /// <summary>
    /// 创建包含截图的测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithScreenshots()
    {
        var report = TestReport.CreateBuilder("ScreenshotTestReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-5))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "TestWithScreenshot",
            TestClass = "ScreenshotTestClass",
            TestMethod = "TestScreenshotMethod",
            Status = TestStatus.Failed,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddMinutes(-4),
            Duration = TimeSpan.FromMinutes(1),
            Screenshots = new List<string> { "screenshot1.png", "screenshot2.png" },
            Output = "Test output with detailed information"
        });

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