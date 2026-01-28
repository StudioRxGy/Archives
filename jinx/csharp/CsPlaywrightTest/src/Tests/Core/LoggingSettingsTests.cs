using System.ComponentModel.DataAnnotations;
using EnterpriseAutomationFramework.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// 日志设置测试类
/// </summary>
public class LoggingSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var settings = new LoggingSettings();

        // Assert
        settings.Level.Should().Be("Information");
        settings.FilePath.Should().Be("Logs/test-{Date}.log");
        settings.EnableConsole.Should().BeTrue();
        settings.EnableFile.Should().BeTrue();
        settings.EnableStructuredLogging.Should().BeTrue();
        settings.FileSizeLimitMB.Should().Be(100);
        settings.RetainedFileCount.Should().Be(30);
        settings.EnableTestContext.Should().BeTrue();
    }

    [Theory]
    [InlineData("Verbose")]
    [InlineData("Debug")]
    [InlineData("Information")]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Fatal")]
    public void Validate_WithValidLogLevel_ShouldPass(string level)
    {
        // Arrange
        var settings = new LoggingSettings { Level = level };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act - Validate both data annotations and IValidatableObject
        var isValid = Validator.TryValidateObject(settings, context, results, true);
        if (isValid)
        {
            var customResults = settings.Validate(context);
            results.AddRange(customResults);
            isValid = !customResults.Any();
        }

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("TRACE")]
    public void Validate_WithInvalidLogLevel_ShouldFail(string level)
    {
        // Arrange
        var settings = new LoggingSettings { Level = level };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act - Validate both data annotations and IValidatableObject
        var isValid = Validator.TryValidateObject(settings, context, results, true);
        if (isValid)
        {
            // If data annotations pass, check custom validation
            var customResults = settings.Validate(context);
            results.AddRange(customResults);
            isValid = !customResults.Any();
        }

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.ErrorMessage!.Contains("日志级别必须是以下值之一"));
    }

    [Fact]
    public void Validate_WithEmptyLogLevel_ShouldFailWithRequiredMessage()
    {
        // Arrange
        var settings = new LoggingSettings { Level = "" };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.ErrorMessage!.Contains("日志级别不能为空"));
    }

    [Fact]
    public void Validate_WithBothOutputsDisabled_ShouldFail()
    {
        // Arrange
        var settings = new LoggingSettings
        {
            EnableConsole = false,
            EnableFile = false
        };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act - Validate both data annotations and IValidatableObject
        var isValid = Validator.TryValidateObject(settings, context, results, true);
        if (isValid)
        {
            var customResults = settings.Validate(context);
            results.AddRange(customResults);
            isValid = !customResults.Any();
        }

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.ErrorMessage!.Contains("必须至少启用控制台输出或文件输出中的一种"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Validate_WithAtLeastOneOutputEnabled_ShouldPass(bool enableConsole, bool enableFile)
    {
        // Arrange
        var settings = new LoggingSettings
        {
            EnableConsole = enableConsole,
            EnableFile = enableFile
        };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act - Validate both data annotations and IValidatableObject
        var isValid = Validator.TryValidateObject(settings, context, results, true);
        if (isValid)
        {
            var customResults = settings.Validate(context);
            results.AddRange(customResults);
            isValid = !customResults.Any();
        }

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithFileEnabledAndEmptyPath_ShouldFail(string? filePath)
    {
        // Arrange
        var settings = new LoggingSettings
        {
            EnableFile = true,
            FilePath = filePath!
        };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act - Validate both data annotations and IValidatableObject
        var isValid = Validator.TryValidateObject(settings, context, results, true);
        if (isValid)
        {
            // If data annotations pass, check custom validation
            var customResults = settings.Validate(context);
            results.AddRange(customResults);
            isValid = !customResults.Any();
        }

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.ErrorMessage!.Contains("启用文件输出时，文件路径不能为空") || r.ErrorMessage!.Contains("日志文件路径不能为空"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Validate_WithInvalidFileSizeLimit_ShouldFail(int fileSizeLimit)
    {
        // Arrange
        var settings = new LoggingSettings { FileSizeLimitMB = fileSizeLimit };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.ErrorMessage!.Contains("文件大小限制必须在1-1000MB之间"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void Validate_WithInvalidRetainedFileCount_ShouldFail(int retainedFileCount)
    {
        // Arrange
        var settings = new LoggingSettings { RetainedFileCount = retainedFileCount };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.ErrorMessage!.Contains("保留文件数量必须在1-365之间"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(1000)]
    public void Validate_WithValidFileSizeLimit_ShouldPass(int fileSizeLimit)
    {
        // Arrange
        var settings = new LoggingSettings { FileSizeLimitMB = fileSizeLimit };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void Validate_WithValidRetainedFileCount_ShouldPass(int retainedFileCount)
    {
        // Arrange
        var settings = new LoggingSettings { RetainedFileCount = retainedFileCount };
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}