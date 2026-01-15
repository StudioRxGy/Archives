using EnterpriseAutomationFramework.Core.Models;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// TestResult 类的单元测试
/// </summary>
public class TestResultTests
{
    [Fact]
    public void TestResult_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var testResult = new TestResult();

        // Assert
        testResult.TestName.Should().Be(string.Empty);
        testResult.Status.Should().Be(TestStatus.Passed);
        testResult.Screenshots.Should().NotBeNull().And.BeEmpty();
        testResult.TestData.Should().NotBeNull().And.BeEmpty();
        testResult.Categories.Should().NotBeNull().And.BeEmpty();
        testResult.Tags.Should().NotBeNull().And.BeEmpty();
        testResult.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void TestResult_WithBasicProperties_ShouldSetCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(5);
        var duration = endTime - startTime;

        // Act
        var testResult = new TestResult
        {
            TestName = "TestMethod1",
            TestClass = "TestClass1",
            TestMethod = "Method1",
            Status = TestStatus.Passed,
            StartTime = startTime,
            EndTime = endTime,
            Duration = duration,
            ErrorMessage = null,
            StackTrace = null,
            Output = "Test output"
        };

        // Assert
        testResult.TestName.Should().Be("TestMethod1");
        testResult.TestClass.Should().Be("TestClass1");
        testResult.TestMethod.Should().Be("Method1");
        testResult.Status.Should().Be(TestStatus.Passed);
        testResult.StartTime.Should().Be(startTime);
        testResult.EndTime.Should().Be(endTime);
        testResult.Duration.Should().Be(duration);
        testResult.Output.Should().Be("Test output");
    }

    [Fact]
    public void AddScreenshot_WithValidPath_ShouldAddToList()
    {
        // Arrange
        var testResult = new TestResult();
        var screenshotPath = "screenshots/test1.png";

        // Act
        testResult.AddScreenshot(screenshotPath);

        // Assert
        testResult.Screenshots.Should().Contain(screenshotPath);
        testResult.Screenshots.Should().HaveCount(1);
    }

    [Fact]
    public void AddScreenshot_WithDuplicatePath_ShouldNotAddDuplicate()
    {
        // Arrange
        var testResult = new TestResult();
        var screenshotPath = "screenshots/test1.png";

        // Act
        testResult.AddScreenshot(screenshotPath);
        testResult.AddScreenshot(screenshotPath);

        // Assert
        testResult.Screenshots.Should().HaveCount(1);
        testResult.Screenshots.Should().Contain(screenshotPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddScreenshot_WithInvalidPath_ShouldNotAdd(string? screenshotPath)
    {
        // Arrange
        var testResult = new TestResult();

        // Act
        testResult.AddScreenshot(screenshotPath!);

        // Assert
        testResult.Screenshots.Should().BeEmpty();
    }

    [Fact]
    public void AddTestData_WithValidKeyValue_ShouldAddToCollection()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "inputData";
        var value = "test value";

        // Act
        testResult.AddTestData(key, value);

        // Assert
        testResult.TestData.Should().ContainKey(key);
        testResult.TestData[key].Should().Be(value);
    }

    [Fact]
    public void AddTestData_WithDuplicateKey_ShouldOverwriteValue()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "inputData";
        var originalValue = "original value";
        var newValue = "new value";

        // Act
        testResult.AddTestData(key, originalValue);
        testResult.AddTestData(key, newValue);

        // Assert
        testResult.TestData.Should().ContainKey(key);
        testResult.TestData[key].Should().Be(newValue);
        testResult.TestData.Should().HaveCount(1);
    }

    [Fact]
    public void AddMetadata_WithValidKeyValue_ShouldAddToCollection()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "browser";
        var value = "Chrome";

        // Act
        testResult.AddMetadata(key, value);

        // Assert
        testResult.Metadata.Should().ContainKey(key);
        testResult.Metadata[key].Should().Be(value);
    }

    [Fact]
    public void GetMetadata_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "browser";
        var value = "Chrome";
        testResult.AddMetadata(key, value);

        // Act
        var result = testResult.GetMetadata<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GetMetadata_WithNonExistingKey_ShouldReturnDefaultValue()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "nonexistent";
        var defaultValue = "default";

        // Act
        var result = testResult.GetMetadata(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void GetMetadata_WithWrongType_ShouldReturnDefaultValue()
    {
        // Arrange
        var testResult = new TestResult();
        var key = "number";
        testResult.AddMetadata(key, "not a number");

        // Act
        var result = testResult.GetMetadata<int>(key, 42);

        // Assert
        result.Should().Be(42);
    }

    [Theory]
    [InlineData(TestStatus.Passed, "通过")]
    [InlineData(TestStatus.Failed, "失败")]
    [InlineData(TestStatus.Skipped, "跳过")]
    [InlineData(TestStatus.Inconclusive, "不确定")]
    public void GetStatusDescription_WithDifferentStatuses_ShouldReturnCorrectDescription(TestStatus status, string expectedDescription)
    {
        // Arrange
        var testResult = new TestResult { Status = status };

        // Act
        var description = testResult.GetStatusDescription();

        // Assert
        description.Should().Be(expectedDescription);
    }

    [Theory]
    [InlineData(TestStatus.Failed, true)]
    [InlineData(TestStatus.Passed, false)]
    [InlineData(TestStatus.Skipped, false)]
    [InlineData(TestStatus.Inconclusive, false)]
    public void IsFailed_WithDifferentStatuses_ShouldReturnCorrectResult(TestStatus status, bool expectedResult)
    {
        // Arrange
        var testResult = new TestResult { Status = status };

        // Act
        var result = testResult.IsFailed();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(TestStatus.Passed, true)]
    [InlineData(TestStatus.Failed, false)]
    [InlineData(TestStatus.Skipped, false)]
    [InlineData(TestStatus.Inconclusive, false)]
    public void IsPassed_WithDifferentStatuses_ShouldReturnCorrectResult(TestStatus status, bool expectedResult)
    {
        // Arrange
        var testResult = new TestResult { Status = status };

        // Act
        var result = testResult.IsPassed();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(TestStatus.Skipped, true)]
    [InlineData(TestStatus.Passed, false)]
    [InlineData(TestStatus.Failed, false)]
    [InlineData(TestStatus.Inconclusive, false)]
    public void IsSkipped_WithDifferentStatuses_ShouldReturnCorrectResult(TestStatus status, bool expectedResult)
    {
        // Arrange
        var testResult = new TestResult { Status = status };

        // Act
        var result = testResult.IsSkipped();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void TestResult_WithFailedStatus_ShouldHaveErrorInformation()
    {
        // Arrange & Act
        var testResult = new TestResult
        {
            TestName = "FailedTest",
            Status = TestStatus.Failed,
            ErrorMessage = "Test assertion failed",
            StackTrace = "at TestMethod() line 42"
        };

        // Assert
        testResult.IsFailed().Should().BeTrue();
        testResult.ErrorMessage.Should().Be("Test assertion failed");
        testResult.StackTrace.Should().Be("at TestMethod() line 42");
    }

    [Fact]
    public void TestResult_WithCategoriesAndTags_ShouldStoreCorrectly()
    {
        // Arrange
        var testResult = new TestResult();
        var categories = new List<string> { "UI", "Smoke" };
        var tags = new List<string> { "Priority1", "Chrome" };

        // Act
        testResult.Categories.AddRange(categories);
        testResult.Tags.AddRange(tags);

        // Assert
        testResult.Categories.Should().BeEquivalentTo(categories);
        testResult.Tags.Should().BeEquivalentTo(tags);
    }
}