using EnterpriseAutomationFramework.Core.Models;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// TestSummary 类的单元测试
/// </summary>
public class TestSummaryTests
{
    [Fact]
    public void TestSummary_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var summary = new TestSummary();

        // Assert
        summary.TotalTests.Should().Be(0);
        summary.PassedTests.Should().Be(0);
        summary.FailedTests.Should().Be(0);
        summary.SkippedTests.Should().Be(0);
        summary.InconclusiveTests.Should().Be(0);
        summary.TotalDuration.Should().Be(TimeSpan.Zero);
        summary.AverageDuration.Should().Be(TimeSpan.Zero);
        summary.FastestTest.Should().Be(TimeSpan.Zero);
        summary.SlowestTest.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void PassRate_WithValidData_ShouldCalculateCorrectly()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            PassedTests = 8
        };

        // Act
        var passRate = summary.PassRate;

        // Assert
        passRate.Should().Be(80.0);
    }

    [Fact]
    public void PassRate_WithZeroTests_ShouldReturnZero()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 0,
            PassedTests = 0
        };

        // Act
        var passRate = summary.PassRate;

        // Assert
        passRate.Should().Be(0.0);
    }

    [Fact]
    public void FailureRate_WithValidData_ShouldCalculateCorrectly()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            FailedTests = 2
        };

        // Act
        var failureRate = summary.FailureRate;

        // Assert
        failureRate.Should().Be(20.0);
    }

    [Fact]
    public void SkipRate_WithValidData_ShouldCalculateCorrectly()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            SkippedTests = 1
        };

        // Act
        var skipRate = summary.SkipRate;

        // Assert
        skipRate.Should().Be(10.0);
    }

    [Fact]
    public void TestsPerHour_WithValidData_ShouldCalculateCorrectly()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 120,
            TotalDuration = TimeSpan.FromHours(2)
        };

        // Act
        var testsPerHour = summary.TestsPerHour;

        // Assert
        testsPerHour.Should().Be(60.0);
    }

    [Fact]
    public void TestsPerHour_WithZeroDuration_ShouldReturnZero()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            TotalDuration = TimeSpan.Zero
        };

        // Act
        var testsPerHour = summary.TestsPerHour;

        // Assert
        testsPerHour.Should().Be(0.0);
    }

    [Fact]
    public void FromTestResults_WithEmptyList_ShouldReturnEmptySummary()
    {
        // Arrange
        var testResults = new List<TestResult>();

        // Act
        var summary = TestSummary.FromTestResults(testResults);

        // Assert
        summary.TotalTests.Should().Be(0);
        summary.PassedTests.Should().Be(0);
        summary.FailedTests.Should().Be(0);
        summary.SkippedTests.Should().Be(0);
        summary.InconclusiveTests.Should().Be(0);
    }

    [Fact]
    public void FromTestResults_WithMixedResults_ShouldCalculateCorrectly()
    {
        // Arrange
        var testResults = new List<TestResult>
        {
            new() { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(1) },
            new() { Status = TestStatus.Passed, Duration = TimeSpan.FromSeconds(2) },
            new() { Status = TestStatus.Failed, Duration = TimeSpan.FromSeconds(3) },
            new() { Status = TestStatus.Skipped, Duration = TimeSpan.FromSeconds(0.5) },
            new() { Status = TestStatus.Inconclusive, Duration = TimeSpan.FromSeconds(1.5) }
        };

        // Act
        var summary = TestSummary.FromTestResults(testResults);

        // Assert
        summary.TotalTests.Should().Be(5);
        summary.PassedTests.Should().Be(2);
        summary.FailedTests.Should().Be(1);
        summary.SkippedTests.Should().Be(1);
        summary.InconclusiveTests.Should().Be(1);
        summary.TotalDuration.Should().Be(TimeSpan.FromSeconds(8));
        summary.AverageDuration.Should().Be(TimeSpan.FromSeconds(1.6));
        summary.FastestTest.Should().Be(TimeSpan.FromSeconds(0.5));
        summary.SlowestTest.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void FromTestResults_WithSingleResult_ShouldCalculateCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(2.5);
        var testResults = new List<TestResult>
        {
            new() { Status = TestStatus.Passed, Duration = duration }
        };

        // Act
        var summary = TestSummary.FromTestResults(testResults);

        // Assert
        summary.TotalTests.Should().Be(1);
        summary.PassedTests.Should().Be(1);
        summary.FailedTests.Should().Be(0);
        summary.SkippedTests.Should().Be(0);
        summary.InconclusiveTests.Should().Be(0);
        summary.TotalDuration.Should().Be(duration);
        summary.AverageDuration.Should().Be(duration);
        summary.FastestTest.Should().Be(duration);
        summary.SlowestTest.Should().Be(duration);
    }

    [Fact]
    public void GetSummaryText_WithValidData_ShouldReturnFormattedString()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            PassedTests = 8,
            FailedTests = 1,
            SkippedTests = 1,
            TotalDuration = TimeSpan.FromSeconds(25.5)
        };

        // Act
        var summaryText = summary.GetSummaryText();

        // Assert
        summaryText.Should().Be("总计: 10, 通过: 8, 失败: 1, 跳过: 1, 通过率: 80.0%, 总耗时: 25.5s");
    }

    [Fact]
    public void GetDetailedStatistics_WithValidData_ShouldReturnCorrectDictionary()
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = 10,
            PassedTests = 8,
            FailedTests = 1,
            SkippedTests = 1,
            InconclusiveTests = 0,
            TotalDuration = TimeSpan.FromSeconds(25.5),
            AverageDuration = TimeSpan.FromSeconds(2.55),
            FastestTest = TimeSpan.FromSeconds(1.0),
            SlowestTest = TimeSpan.FromSeconds(5.0)
        };

        // Act
        var statistics = summary.GetDetailedStatistics();

        // Assert
        statistics.Should().ContainKey("TotalTests").WhoseValue.Should().Be(10);
        statistics.Should().ContainKey("PassedTests").WhoseValue.Should().Be(8);
        statistics.Should().ContainKey("FailedTests").WhoseValue.Should().Be(1);
        statistics.Should().ContainKey("SkippedTests").WhoseValue.Should().Be(1);
        statistics.Should().ContainKey("InconclusiveTests").WhoseValue.Should().Be(0);
        statistics.Should().ContainKey("PassRate").WhoseValue.Should().Be(80.0);
        statistics.Should().ContainKey("FailureRate").WhoseValue.Should().Be(10.0);
        statistics.Should().ContainKey("SkipRate").WhoseValue.Should().Be(10.0);
        statistics.Should().ContainKey("TotalDurationSeconds").WhoseValue.Should().Be(25.5);
        statistics.Should().ContainKey("AverageDurationSeconds").WhoseValue.Should().Be(2.55);
        statistics.Should().ContainKey("FastestTestSeconds").WhoseValue.Should().Be(1.0);
        statistics.Should().ContainKey("SlowestTestSeconds").WhoseValue.Should().Be(5.0);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(10, 5, true)]
    public void HasFailures_WithDifferentFailureCounts_ShouldReturnCorrectResult(int totalTests, int failedTests, bool expectedResult)
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = totalTests,
            FailedTests = failedTests
        };

        // Act
        var hasFailures = summary.HasFailures();

        // Assert
        hasFailures.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(5, 5, true)]
    [InlineData(5, 4, false)]
    [InlineData(10, 10, true)]
    public void AllTestsPassed_WithDifferentPassCounts_ShouldReturnCorrectResult(int totalTests, int passedTests, bool expectedResult)
    {
        // Arrange
        var summary = new TestSummary
        {
            TotalTests = totalTests,
            PassedTests = passedTests
        };

        // Act
        var allPassed = summary.AllTestsPassed();

        // Assert
        allPassed.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(5, false)]
    public void IsSuccessful_WithDifferentFailureCounts_ShouldReturnCorrectResult(int failedTests, bool expectedResult)
    {
        // Arrange
        var summary = new TestSummary
        {
            FailedTests = failedTests
        };

        // Act
        var isSuccessful = summary.IsSuccessful();

        // Assert
        isSuccessful.Should().Be(expectedResult);
    }

    [Fact]
    public void TestSummary_WithComplexScenario_ShouldCalculateAllMetricsCorrectly()
    {
        // Arrange
        var testResults = new List<TestResult>();
        
        // Add 50 passed tests with varying durations
        for (int i = 0; i < 50; i++)
        {
            testResults.Add(new TestResult 
            { 
                Status = TestStatus.Passed, 
                Duration = TimeSpan.FromSeconds(1 + i * 0.1) 
            });
        }
        
        // Add 10 failed tests
        for (int i = 0; i < 10; i++)
        {
            testResults.Add(new TestResult 
            { 
                Status = TestStatus.Failed, 
                Duration = TimeSpan.FromSeconds(2 + i * 0.2) 
            });
        }
        
        // Add 5 skipped tests
        for (int i = 0; i < 5; i++)
        {
            testResults.Add(new TestResult 
            { 
                Status = TestStatus.Skipped, 
                Duration = TimeSpan.FromSeconds(0.1) 
            });
        }

        // Act
        var summary = TestSummary.FromTestResults(testResults);

        // Assert
        summary.TotalTests.Should().Be(65);
        summary.PassedTests.Should().Be(50);
        summary.FailedTests.Should().Be(10);
        summary.SkippedTests.Should().Be(5);
        summary.InconclusiveTests.Should().Be(0);
        summary.PassRate.Should().BeApproximately(76.92, 0.01);
        summary.FailureRate.Should().BeApproximately(15.38, 0.01);
        summary.SkipRate.Should().BeApproximately(7.69, 0.01);
        summary.HasFailures().Should().BeTrue();
        summary.AllTestsPassed().Should().BeFalse();
        summary.IsSuccessful().Should().BeFalse();
    }
}