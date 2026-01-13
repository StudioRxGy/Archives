using EnterpriseAutomationFramework.Core.Models;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// TestReport 类的单元测试
/// </summary>
public class TestReportTests
{
    [Fact]
    public void TestReport_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var report = new TestReport();

        // Assert
        report.ReportId.Should().NotBeNullOrEmpty();
        report.ReportName.Should().Be(string.Empty);
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        report.Environment.Should().Be(string.Empty);
        report.Summary.Should().NotBeNull();
        report.Results.Should().NotBeNull().And.BeEmpty();
        report.Screenshots.Should().NotBeNull().And.BeEmpty();
        report.OutputFiles.Should().NotBeNull().And.BeEmpty();
        report.Metadata.Should().NotBeNull().And.BeEmpty();
        report.Configuration.Should().NotBeNull().And.BeEmpty();
        report.SystemInfo.Should().NotBeNull().And.BeEmpty();
        report.ReportVersion.Should().Be("1.0");
    }

    [Fact]
    public void TestReport_WithBasicProperties_ShouldSetCorrectly()
    {
        // Arrange
        var reportName = "Integration Test Report";
        var environment = "Development";
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var endTime = DateTime.UtcNow;

        // Act
        var report = new TestReport
        {
            ReportName = reportName,
            Environment = environment,
            TestStartTime = startTime,
            TestEndTime = endTime
        };

        // Assert
        report.ReportName.Should().Be(reportName);
        report.Environment.Should().Be(environment);
        report.TestStartTime.Should().Be(startTime);
        report.TestEndTime.Should().Be(endTime);
    }

    [Fact]
    public void AddTestResult_WithValidResult_ShouldAddToCollection()
    {
        // Arrange
        var report = new TestReport();
        var testResult = new TestResult
        {
            TestName = "Test1",
            Status = TestStatus.Passed,
            Duration = TimeSpan.FromSeconds(2)
        };

        // Act
        report.AddTestResult(testResult);

        // Assert
        report.Results.Should().Contain(testResult);
        report.Results.Should().HaveCount(1);
        report.Summary.TotalTests.Should().Be(1);
        report.Summary.PassedTests.Should().Be(1);
    }

    [Fact]
    public void AddTestResult_WithNull_ShouldNotAdd()
    {
        // Arrange
        var report = new TestReport();

        // Act
        report.AddTestResult(null!);

        // Assert
        report.Results.Should().BeEmpty();
    }

    [Fact]
    public void AddTestResult_WithScreenshots_ShouldCollectScreenshots()
    {
        // Arrange
        var report = new TestReport();
        var testResult = new TestResult
        {
            TestName = "Test1",
            Status = TestStatus.Failed
        };
        testResult.AddScreenshot("screenshot1.png");
        testResult.AddScreenshot("screenshot2.png");

        // Act
        report.AddTestResult(testResult);

        // Assert
        report.Screenshots.Should().Contain("screenshot1.png");
        report.Screenshots.Should().Contain("screenshot2.png");
        report.Screenshots.Should().HaveCount(2);
    }

    [Fact]
    public void AddTestResults_WithMultipleResults_ShouldAddAll()
    {
        // Arrange
        var report = new TestReport();
        var testResults = new List<TestResult>
        {
            new() { TestName = "Test1", Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) },
            new() { TestName = "Test2", Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(2) },
            new() { TestName = "Test3", Status = TestStatus.Skipped, Duration = TimeSpan.FromSeconds(0) }
        };

        // Act
        report.AddTestResults(testResults);

        // Assert
        report.Results.Should().HaveCount(3);
        report.Summary.TotalTests.Should().Be(3);
        report.Summary.PassedTests.Should().Be(1);
        report.Summary.FailedTests.Should().Be(1);
        report.Summary.SkippedTests.Should().Be(1);
    }

    [Fact]
    public void AddScreenshot_WithValidPath_ShouldAddToList()
    {
        // Arrange
        var report = new TestReport();
        var screenshotPath = "screenshots/test1.png";

        // Act
        report.AddScreenshot(screenshotPath);

        // Assert
        report.Screenshots.Should().Contain(screenshotPath);
        report.Screenshots.Should().HaveCount(1);
    }

    [Fact]
    public void AddScreenshot_WithDuplicatePath_ShouldNotAddDuplicate()
    {
        // Arrange
        var report = new TestReport();
        var screenshotPath = "screenshots/test1.png";

        // Act
        report.AddScreenshot(screenshotPath);
        report.AddScreenshot(screenshotPath);

        // Assert
        report.Screenshots.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddScreenshot_WithInvalidPath_ShouldNotAdd(string? screenshotPath)
    {
        // Arrange
        var report = new TestReport();

        // Act
        report.AddScreenshot(screenshotPath!);

        // Assert
        report.Screenshots.Should().BeEmpty();
    }

    [Fact]
    public void AddOutputFile_WithValidPath_ShouldAddToList()
    {
        // Arrange
        var report = new TestReport();
        var filePath = "reports/test-results.xml";

        // Act
        report.AddOutputFile(filePath);

        // Assert
        report.OutputFiles.Should().Contain(filePath);
        report.OutputFiles.Should().HaveCount(1);
    }

    [Fact]
    public void AddMetadata_WithValidKeyValue_ShouldAddToCollection()
    {
        // Arrange
        var report = new TestReport();
        var key = "browser";
        var value = "Chrome";

        // Act
        report.AddMetadata(key, value);

        // Assert
        report.Metadata.Should().ContainKey(key);
        report.Metadata[key].Should().Be(value);
    }

    [Fact]
    public void GetMetadata_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var report = new TestReport();
        var key = "browser";
        var value = "Chrome";
        report.AddMetadata(key, value);

        // Act
        var result = report.GetMetadata<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GetMetadata_WithNonExistingKey_ShouldReturnDefaultValue()
    {
        // Arrange
        var report = new TestReport();
        var key = "nonexistent";
        var defaultValue = "default";

        // Act
        var result = report.GetMetadata(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void AddConfiguration_WithValidKeyValue_ShouldAddToCollection()
    {
        // Arrange
        var report = new TestReport();
        var key = "timeout";
        var value = 30000;

        // Act
        report.AddConfiguration(key, value);

        // Assert
        report.Configuration.Should().ContainKey(key);
        report.Configuration[key].Should().Be(value);
    }

    [Fact]
    public void AddSystemInfo_WithValidKeyValue_ShouldAddToCollection()
    {
        // Arrange
        var report = new TestReport();
        var key = "os";
        var value = "Windows 11";

        // Act
        report.AddSystemInfo(key, value);

        // Assert
        report.SystemInfo.Should().ContainKey(key);
        report.SystemInfo[key].Should().Be(value);
    }

    [Fact]
    public void RefreshSummary_WithTestResults_ShouldUpdateSummary()
    {
        // Arrange
        var report = new TestReport();
        report.Results.Add(new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) });
        report.Results.Add(new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(2) });

        // Act
        report.RefreshSummary();

        // Assert
        report.Summary.TotalTests.Should().Be(2);
        report.Summary.PassedTests.Should().Be(1);
        report.Summary.FailedTests.Should().Be(1);
    }

    [Fact]
    public void GetFailedTests_WithMixedResults_ShouldReturnOnlyFailedTests()
    {
        // Arrange
        var report = new TestReport();
        var passedTest = new TestResult { TestName = "PassedTest", Status = TestStatus.Passed };
        var failedTest1 = new TestResult { TestName = "FailedTest1", Status = TestStatus.Failed };
        var failedTest2 = new TestResult { TestName = "FailedTest2", Status = TestStatus.Failed };
        var skippedTest = new TestResult { TestName = "SkippedTest", Status = TestStatus.Skipped };

        report.AddTestResult(passedTest);
        report.AddTestResult(failedTest1);
        report.AddTestResult(failedTest2);
        report.AddTestResult(skippedTest);

        // Act
        var failedTests = report.GetFailedTests();

        // Assert
        failedTests.Should().HaveCount(2);
        failedTests.Should().Contain(failedTest1);
        failedTests.Should().Contain(failedTest2);
    }

    [Fact]
    public void GetPassedTests_WithMixedResults_ShouldReturnOnlyPassedTests()
    {
        // Arrange
        var report = new TestReport();
        var passedTest1 = new TestResult { TestName = "PassedTest1", Status = TestStatus.Passed };
        var passedTest2 = new TestResult { TestName = "PassedTest2", Status = TestStatus.Passed };
        var failedTest = new TestResult { TestName = "FailedTest", Status = TestStatus.Failed };

        report.AddTestResult(passedTest1);
        report.AddTestResult(passedTest2);
        report.AddTestResult(failedTest);

        // Act
        var passedTests = report.GetPassedTests();

        // Assert
        passedTests.Should().HaveCount(2);
        passedTests.Should().Contain(passedTest1);
        passedTests.Should().Contain(passedTest2);
    }

    [Fact]
    public void GetSkippedTests_WithMixedResults_ShouldReturnOnlySkippedTests()
    {
        // Arrange
        var report = new TestReport();
        var passedTest = new TestResult { TestName = "PassedTest", Status = TestStatus.Passed };
        var skippedTest1 = new TestResult { TestName = "SkippedTest1", Status = TestStatus.Skipped };
        var skippedTest2 = new TestResult { TestName = "SkippedTest2", Status = TestStatus.Skipped };

        report.AddTestResult(passedTest);
        report.AddTestResult(skippedTest1);
        report.AddTestResult(skippedTest2);

        // Act
        var skippedTests = report.GetSkippedTests();

        // Assert
        skippedTests.Should().HaveCount(2);
        skippedTests.Should().Contain(skippedTest1);
        skippedTests.Should().Contain(skippedTest2);
    }

    [Fact]
    public void GetTestsByCategory_WithCategorizedTests_ShouldReturnMatchingTests()
    {
        // Arrange
        var report = new TestReport();
        var uiTest1 = new TestResult { TestName = "UITest1" };
        uiTest1.Categories.Add("UI");
        var uiTest2 = new TestResult { TestName = "UITest2" };
        uiTest2.Categories.Add("UI");
        var apiTest = new TestResult { TestName = "APITest" };
        apiTest.Categories.Add("API");

        report.AddTestResult(uiTest1);
        report.AddTestResult(uiTest2);
        report.AddTestResult(apiTest);

        // Act
        var uiTests = report.GetTestsByCategory("UI");

        // Assert
        uiTests.Should().HaveCount(2);
        uiTests.Should().Contain(uiTest1);
        uiTests.Should().Contain(uiTest2);
    }

    [Fact]
    public void GetAllCategories_WithCategorizedTests_ShouldReturnUniqueCategories()
    {
        // Arrange
        var report = new TestReport();
        var test1 = new TestResult { TestName = "Test1" };
        test1.Categories.AddRange(new[] { "UI", "Smoke" });
        var test2 = new TestResult { TestName = "Test2" };
        test2.Categories.AddRange(new[] { "API", "Smoke" });
        var test3 = new TestResult { TestName = "Test3" };
        test3.Categories.Add("Integration");

        report.AddTestResult(test1);
        report.AddTestResult(test2);
        report.AddTestResult(test3);

        // Act
        var categories = report.GetAllCategories();

        // Assert
        categories.Should().HaveCount(4);
        categories.Should().Contain("UI");
        categories.Should().Contain("API");
        categories.Should().Contain("Smoke");
        categories.Should().Contain("Integration");
    }

    [Fact]
    public void GetReportSummary_WithValidData_ShouldReturnFormattedString()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 1, 1, 10, 5, 30, DateTimeKind.Utc);
        var report = new TestReport
        {
            ReportName = "Test Report",
            Environment = "Development",
            TestStartTime = startTime,
            TestEndTime = endTime
        };
        report.AddTestResult(new TestResult { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) });
        report.AddTestResult(new TestResult { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(2) });

        // Act
        var summary = report.GetReportSummary();

        // Assert
        summary.Should().Contain("测试报告: Test Report");
        summary.Should().Contain("环境: Development");
        summary.Should().Contain("执行时间: 2024-01-01 10:00:00 - 2024-01-01 10:05:30");
        summary.Should().Contain("总耗时: 330.0秒");
        summary.Should().Contain("总计: 2, 通过: 1, 失败: 1, 跳过: 0");
    }

    [Theory]
    [InlineData("", "Development", false)]
    [InlineData("Test Report", "", false)]
    [InlineData("Test Report", "Development", true)]
    public void ValidateReport_WithDifferentData_ShouldReturnCorrectResult(string reportName, string environment, bool expectedValid)
    {
        // Arrange
        var report = new TestReport
        {
            ReportName = reportName,
            Environment = environment,
            TestStartTime = DateTime.UtcNow.AddMinutes(-5),
            TestEndTime = DateTime.UtcNow
        };

        // Act
        var isValid = report.ValidateReport();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidateReport_WithInvalidTimeRange_ShouldReturnFalse()
    {
        // Arrange
        var report = new TestReport
        {
            ReportName = "Test Report",
            Environment = "Development",
            TestStartTime = DateTime.UtcNow,
            TestEndTime = DateTime.UtcNow.AddMinutes(-5) // End time before start time
        };

        // Act
        var isValid = report.ValidateReport();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void InitializeSystemInfo_ShouldAddSystemInformation()
    {
        // Arrange
        var report = new TestReport();

        // Act
        report.InitializeSystemInfo();

        // Assert
        report.SystemInfo.Should().ContainKey("MachineName");
        report.SystemInfo.Should().ContainKey("UserName");
        report.SystemInfo.Should().ContainKey("OSVersion");
        report.SystemInfo.Should().ContainKey("ProcessorCount");
        report.SystemInfo.Should().ContainKey("CLRVersion");
        report.SystemInfo.Should().ContainKey("Is64BitOperatingSystem");
        report.SystemInfo.Should().ContainKey("Is64BitProcess");
    }

    [Fact]
    public void CreateBuilder_WithValidParameters_ShouldReturnBuilder()
    {
        // Arrange
        var reportName = "Test Report";
        var environment = "Development";

        // Act
        var builder = TestReport.CreateBuilder(reportName, environment);

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<TestReportBuilder>();
    }
}

/// <summary>
/// TestReportBuilder 类的单元测试
/// </summary>
public class TestReportBuilderTests
{
    [Fact]
    public void TestReportBuilder_Constructor_ShouldInitializeReport()
    {
        // Arrange
        var reportName = "Test Report";
        var environment = "Development";

        // Act
        var builder = new TestReportBuilder(reportName, environment);
        var report = builder.Build();

        // Assert
        report.ReportName.Should().Be(reportName);
        report.Environment.Should().Be(environment);
        report.TestStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        report.SystemInfo.Should().NotBeEmpty();
    }

    [Fact]
    public void WithStartTime_ShouldSetStartTime()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var startTime = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var result = builder.WithStartTime(startTime);
        var report = result.Build();

        // Assert
        result.Should().BeSameAs(builder);
        report.TestStartTime.Should().Be(startTime);
    }

    [Fact]
    public void WithEndTime_ShouldSetEndTime()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var endTime = DateTime.UtcNow;

        // Act
        var result = builder.WithEndTime(endTime);
        var report = result.Build();

        // Assert
        result.Should().BeSameAs(builder);
        report.TestEndTime.Should().Be(endTime);
    }

    [Fact]
    public void WithTestResults_ShouldAddTestResults()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var testResults = new List<TestResult>
        {
            new() { TestName = "Test1", Status = TestStatus.Passed },
            new() { TestName = "Test2", Status = TestStatus.Failed }
        };

        // Act
        var result = builder.WithTestResults(testResults);
        var report = result.Build();

        // Assert
        result.Should().BeSameAs(builder);
        report.Results.Should().HaveCount(2);
        report.Summary.TotalTests.Should().Be(2);
    }

    [Fact]
    public void WithMetadata_ShouldAddMetadata()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var metadata = new Dictionary<string, object>
        {
            ["browser"] = "Chrome",
            ["version"] = "1.0"
        };

        // Act
        var result = builder.WithMetadata(metadata);
        var report = result.Build();

        // Assert
        result.Should().BeSameAs(builder);
        report.Metadata.Should().ContainKey("browser");
        report.Metadata.Should().ContainKey("version");
    }

    [Fact]
    public void WithConfiguration_ShouldAddConfiguration()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var configuration = new Dictionary<string, object>
        {
            ["timeout"] = 30000,
            ["headless"] = true
        };

        // Act
        var result = builder.WithConfiguration(configuration);
        var report = result.Build();

        // Assert
        result.Should().BeSameAs(builder);
        report.Configuration.Should().ContainKey("timeout");
        report.Configuration.Should().ContainKey("headless");
    }

    [Fact]
    public void Build_WithoutEndTime_ShouldSetEndTimeToNow()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var beforeBuild = DateTime.UtcNow;

        // Act
        var report = builder.Build();

        // Assert
        report.TestEndTime.Should().BeOnOrAfter(beforeBuild);
        report.TestEndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Build_ShouldRefreshSummary()
    {
        // Arrange
        var builder = new TestReportBuilder("Test", "Dev");
        var testResults = new List<TestResult>
        {
            new() { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) },
            new() { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(2) }
        };

        // Act
        var report = builder
            .WithTestResults(testResults)
            .Build();

        // Assert
        report.Summary.TotalTests.Should().Be(2);
        report.Summary.PassedTests.Should().Be(1);
        report.Summary.FailedTests.Should().Be(1);
    }
}