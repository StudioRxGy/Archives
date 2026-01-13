using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// ReportingSettings 单元测试
/// </summary>
public class ReportingSettingsTests
{
    [Fact]
    public void ReportingSettings_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new ReportingSettings();

        // Assert
        settings.OutputPath.Should().Be("Reports");
        settings.Format.Should().Be("Html");
        settings.IncludeScreenshots.Should().BeTrue();
    }

    [Theory]
    [InlineData("Reports")]
    [InlineData("TestResults")]
    [InlineData("Output/Reports")]
    [InlineData("C:\\TestReports")]
    [InlineData("/var/reports")]
    public void ReportingSettings_WithValidOutputPath_ShouldSetCorrectly(string outputPath)
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            OutputPath = outputPath
        };

        // Assert
        settings.OutputPath.Should().Be(outputPath);
    }

    [Theory]
    [InlineData("Html")]
    [InlineData("Json")]
    [InlineData("Xml")]
    [InlineData("Allure")]
    public void ReportingSettings_WithValidFormat_ShouldSetCorrectly(string format)
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            Format = format
        };

        // Assert
        settings.Format.Should().Be(format);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReportingSettings_WithValidIncludeScreenshots_ShouldSetCorrectly(bool includeScreenshots)
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            IncludeScreenshots = includeScreenshots
        };

        // Assert
        settings.IncludeScreenshots.Should().Be(includeScreenshots);
    }

    [Fact]
    public void ReportingSettings_WithAllProperties_ShouldSetCorrectly()
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            OutputPath = "CustomReports",
            Format = "Json",
            IncludeScreenshots = false
        };

        // Assert
        settings.OutputPath.Should().Be("CustomReports");
        settings.Format.Should().Be("Json");
        settings.IncludeScreenshots.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ReportingSettings_WithEmptyOrNullOutputPath_ShouldSetValue(string? outputPath)
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            OutputPath = outputPath!
        };

        // Assert
        settings.OutputPath.Should().Be(outputPath ?? string.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ReportingSettings_WithEmptyOrNullFormat_ShouldSetValue(string? format)
    {
        // Arrange & Act
        var settings = new ReportingSettings
        {
            Format = format!
        };

        // Assert
        settings.Format.Should().Be(format ?? string.Empty);
    }
}