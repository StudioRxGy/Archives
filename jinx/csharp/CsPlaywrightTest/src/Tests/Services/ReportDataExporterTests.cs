using EnterpriseAutomationFramework.Core.Models;
using EnterpriseAutomationFramework.Services.Reporting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// 报告数据导出器单元测试
/// </summary>
[Trait("Category", "Unit")]
[Trait("Type", "Service")]
public class ReportDataExporterTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ReportDataExporter> _logger;
    private readonly ReportDataExporter _exporter;
    private readonly string _testOutputDirectory;

    public ReportDataExporterTests(ITestOutputHelper output)
    {
        _output = output;
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ReportDataExporter>();
        _exporter = new ReportDataExporter(_logger);

        _testOutputDirectory = Path.Combine(Path.GetTempPath(), "ExporterTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDirectory);
    }

    [Fact]
    public async Task ExportToJsonAsync_WithValidTestReport_ShouldCreateValidJsonFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "test-report.json");

        // Act
        var result = await _exporter.ExportToJsonAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.False(string.IsNullOrWhiteSpace(content));

        // 验证JSON格式
        var jsonDocument = JsonDocument.Parse(content);
        var root = jsonDocument.RootElement;

        Assert.True(root.TryGetProperty("reportInfo", out var reportInfo));
        Assert.True(reportInfo.TryGetProperty("reportName", out var reportName));
        Assert.Equal("SampleReport", reportName.GetString());

        Assert.True(root.TryGetProperty("summary", out var summary));
        Assert.True(summary.TryGetProperty("totalTests", out var totalTests));
        Assert.True(totalTests.GetInt32() > 0);

        Assert.True(root.TryGetProperty("testResults", out var testResults));
        Assert.True(testResults.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ExportToXmlAsync_WithValidTestReport_ShouldCreateValidXmlFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "test-report.xml");

        // Act
        var result = await _exporter.ExportToXmlAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.False(string.IsNullOrWhiteSpace(content));

        // 验证XML格式
        var xmlDocument = XDocument.Parse(content);
        var root = xmlDocument.Root;

        Assert.NotNull(root);
        Assert.Equal("TestReport", root.Name.LocalName);
        Assert.Equal("SampleReport", root.Attribute("reportName")?.Value);

        var summary = root.Element("Summary");
        Assert.NotNull(summary);
        Assert.True(int.Parse(summary.Element("TotalTests")?.Value ?? "0") > 0);

        var testResults = root.Element("TestResults");
        Assert.NotNull(testResults);
        Assert.True(testResults.Elements("TestResult").Any());
    }

    [Fact]
    public async Task ExportToCsvAsync_WithValidTestReport_ShouldCreateValidCsvFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "test-report.csv");

        // Act
        var result = await _exporter.ExportToCsvAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var lines = await File.ReadAllLinesAsync(outputPath);
        Assert.True(lines.Length > 1); // 至少有标题行和一行数据

        // 验证CSV格式
        var header = lines[0];
        Assert.Contains("TestName", header);
        Assert.Contains("Status", header);
        Assert.Contains("Duration(s)", header);

        var dataLine = lines[1];
        Assert.Contains("SampleTest", dataLine);
        Assert.Contains("Passed", dataLine);
    }

    [Fact]
    public async Task ExportSummaryAsync_WithValidTestReport_ShouldCreateSummaryFile()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "test-summary.txt");

        // Act
        var result = await _exporter.ExportSummaryAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.False(string.IsNullOrWhiteSpace(content));

        // 验证摘要内容
        Assert.Contains("测试报告摘要", content);
        Assert.Contains("SampleReport", content);
        Assert.Contains("总测试数", content);
        Assert.Contains("通过", content);
        Assert.Contains("失败", content);
    }

    [Fact]
    public async Task ExportMultipleFormatsAsync_WithAllFormats_ShouldCreateAllFiles()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var formats = new[] { "json", "xml", "csv", "txt" };

        // Act
        var result = await _exporter.ExportMultipleFormatsAsync(testReport, _testOutputDirectory, formats);

        // Assert
        Assert.Equal(formats.Length, result.Count);

        foreach (var format in formats)
        {
            var file = result.FirstOrDefault(f => f.EndsWith($".{format}"));
            Assert.NotNull(file);
            Assert.True(File.Exists(file));
            Assert.True(new FileInfo(file).Length > 0);
        }
    }

    [Fact]
    public async Task ExportToJsonAsync_WithEmptyTestReport_ShouldCreateValidJsonFile()
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

        var outputPath = Path.Combine(_testOutputDirectory, "empty-report.json");

        // Act
        var result = await _exporter.ExportToJsonAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        var jsonDocument = JsonDocument.Parse(content);
        var root = jsonDocument.RootElement;

        Assert.True(root.TryGetProperty("reportInfo", out var reportInfo));
        Assert.True(root.TryGetProperty("summary", out var summary));
        Assert.True(root.TryGetProperty("testResults", out var testResults));
        Assert.Equal(0, testResults.GetArrayLength());
    }

    [Fact]
    public async Task ExportToCsvAsync_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var testReport = CreateTestReportWithSpecialCharacters();
        var outputPath = Path.Combine(_testOutputDirectory, "special-chars.csv");

        // Act
        var result = await _exporter.ExportToCsvAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        
        // 验证特殊字符被正确转义
        Assert.Contains("\"Test with, comma\"", content);
        Assert.Contains("\"Test with \"\"quotes\"\"\"", content);
        Assert.Contains("\"Test with\nline break\"", content);
    }

    [Fact]
    public async Task ExportToXmlAsync_WithComplexTestData_ShouldIncludeAllElements()
    {
        // Arrange
        var testReport = CreateComplexTestReport();
        var outputPath = Path.Combine(_testOutputDirectory, "complex-report.xml");

        // Act
        var result = await _exporter.ExportToXmlAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var xmlDocument = XDocument.Parse(await File.ReadAllTextAsync(outputPath));
        var root = xmlDocument.Root;

        // 验证复杂数据结构
        var testResults = root?.Element("TestResults");
        Assert.NotNull(testResults);

        var testResult = testResults.Elements("TestResult").First();
        
        // 验证分类
        var categories = testResult.Element("Categories");
        Assert.NotNull(categories);
        Assert.True(categories.Elements("Category").Any());

        // 验证标签
        var tags = testResult.Element("Tags");
        Assert.NotNull(tags);
        Assert.True(tags.Elements("Tag").Any());

        // 验证截图
        var screenshots = testResult.Element("Screenshots");
        if (screenshots != null)
        {
            Assert.True(screenshots.Elements("Screenshot").Any());
        }
    }

    [Fact]
    public async Task ExportSummaryAsync_WithFailedTests_ShouldIncludeFailureDetails()
    {
        // Arrange
        var testReport = CreateTestReportWithFailures();
        var outputPath = Path.Combine(_testOutputDirectory, "failed-summary.txt");

        // Act
        var result = await _exporter.ExportSummaryAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        
        // 验证失败测试详情
        Assert.Contains("失败测试详情", content);
        Assert.Contains("FailedTest", content);
        Assert.Contains("Test failed with assertion error", content);
    }

    [Fact]
    public async Task ExportSummaryAsync_WithTestCategories_ShouldIncludeCategoryStatistics()
    {
        // Arrange
        var testReport = CreateTestReportWithCategories();
        var outputPath = Path.Combine(_testOutputDirectory, "category-summary.txt");

        // Act
        var result = await _exporter.ExportSummaryAsync(testReport, outputPath);

        // Assert
        Assert.Equal(outputPath, result);
        Assert.True(File.Exists(outputPath));

        var content = await File.ReadAllTextAsync(outputPath);
        
        // 验证分类统计
        Assert.Contains("测试分类统计", content);
        Assert.Contains("UI", content);
        Assert.Contains("API", content);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("xml")]
    [InlineData("csv")]
    [InlineData("txt")]
    public async Task ExportMultipleFormatsAsync_WithSingleFormat_ShouldCreateCorrectFile(string format)
    {
        // Arrange
        var testReport = CreateSampleTestReport();

        // Act
        var result = await _exporter.ExportMultipleFormatsAsync(testReport, _testOutputDirectory, format);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].EndsWith($".{format}"));
        Assert.True(File.Exists(result[0]));
    }

    [Fact]
    public async Task ExportMultipleFormatsAsync_WithUnsupportedFormat_ShouldSkipUnsupportedFormat()
    {
        // Arrange
        var testReport = CreateSampleTestReport();
        var formats = new[] { "json", "unsupported", "xml" };

        // Act
        var result = await _exporter.ExportMultipleFormatsAsync(testReport, _testOutputDirectory, formats);

        // Assert
        Assert.Equal(2, result.Count); // 只有json和xml应该成功
        Assert.Contains(result, f => f.EndsWith(".json"));
        Assert.Contains(result, f => f.EndsWith(".xml"));
        Assert.DoesNotContain(result, f => f.EndsWith(".unsupported"));
    }

    /// <summary>
    /// 创建示例测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateSampleTestReport()
    {
        var report = TestReport.CreateBuilder("SampleReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-10))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "SampleTest",
            TestClass = "SampleTestClass",
            TestMethod = "TestMethod",
            Status = TestStatus.Passed,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddMinutes(-9),
            Duration = TimeSpan.FromMinutes(1),
            Categories = new List<string> { "Unit" },
            Tags = new List<string> { "Smoke" }
        });

        return report;
    }

    /// <summary>
    /// 创建包含特殊字符的测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithSpecialCharacters()
    {
        var report = TestReport.CreateBuilder("SpecialCharsReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-10))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        var testResults = new[]
        {
            new TestResult
            {
                TestName = "Test with, comma",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-9),
                Duration = TimeSpan.FromMinutes(1)
            },
            new TestResult
            {
                TestName = "Test with \"quotes\"",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-9),
                EndTime = DateTime.UtcNow.AddMinutes(-8),
                Duration = TimeSpan.FromMinutes(1)
            },
            new TestResult
            {
                TestName = "Test with\nline break",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-8),
                EndTime = DateTime.UtcNow.AddMinutes(-7),
                Duration = TimeSpan.FromMinutes(1)
            }
        };

        report.AddTestResults(testResults);
        return report;
    }

    /// <summary>
    /// 创建复杂测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateComplexTestReport()
    {
        var report = TestReport.CreateBuilder("ComplexReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-10))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        report.AddTestResult(new TestResult
        {
            TestName = "ComplexTest",
            TestClass = "ComplexTestClass",
            TestMethod = "TestComplexMethod",
            Status = TestStatus.Passed,
            StartTime = DateTime.UtcNow.AddMinutes(-10),
            EndTime = DateTime.UtcNow.AddMinutes(-9),
            Duration = TimeSpan.FromMinutes(1),
            Categories = new List<string> { "UI", "Integration" },
            Tags = new List<string> { "Critical", "Regression" },
            Screenshots = new List<string> { "screenshot1.png", "screenshot2.png" },
            TestData = new Dictionary<string, object>
            {
                ["UserId"] = 12345,
                ["UserName"] = "testuser"
            },
            Metadata = new Dictionary<string, object>
            {
                ["TestCaseId"] = "TC001",
                ["Priority"] = "High"
            }
        });

        return report;
    }

    /// <summary>
    /// 创建包含失败测试的报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithFailures()
    {
        var report = TestReport.CreateBuilder("FailedReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-10))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        var testResults = new[]
        {
            new TestResult
            {
                TestName = "PassedTest",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-9),
                Duration = TimeSpan.FromMinutes(1)
            },
            new TestResult
            {
                TestName = "FailedTest",
                Status = TestStatus.Failed,
                StartTime = DateTime.UtcNow.AddMinutes(-9),
                EndTime = DateTime.UtcNow.AddMinutes(-8),
                Duration = TimeSpan.FromMinutes(1),
                ErrorMessage = "Test failed with assertion error",
                StackTrace = "at TestClass.TestMethod() line 42"
            }
        };

        report.AddTestResults(testResults);
        return report;
    }

    /// <summary>
    /// 创建包含分类的测试报告
    /// </summary>
    /// <returns>测试报告</returns>
    private TestReport CreateTestReportWithCategories()
    {
        var report = TestReport.CreateBuilder("CategoryReport", "Test")
            .WithStartTime(DateTime.UtcNow.AddMinutes(-10))
            .WithEndTime(DateTime.UtcNow)
            .Build();

        var testResults = new[]
        {
            new TestResult
            {
                TestName = "UITest1",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                EndTime = DateTime.UtcNow.AddMinutes(-9),
                Duration = TimeSpan.FromMinutes(1),
                Categories = new List<string> { "UI" }
            },
            new TestResult
            {
                TestName = "UITest2",
                Status = TestStatus.Failed,
                StartTime = DateTime.UtcNow.AddMinutes(-9),
                EndTime = DateTime.UtcNow.AddMinutes(-8),
                Duration = TimeSpan.FromMinutes(1),
                Categories = new List<string> { "UI" }
            },
            new TestResult
            {
                TestName = "APITest1",
                Status = TestStatus.Passed,
                StartTime = DateTime.UtcNow.AddMinutes(-8),
                EndTime = DateTime.UtcNow.AddMinutes(-7),
                Duration = TimeSpan.FromMinutes(1),
                Categories = new List<string> { "API" }
            }
        };

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