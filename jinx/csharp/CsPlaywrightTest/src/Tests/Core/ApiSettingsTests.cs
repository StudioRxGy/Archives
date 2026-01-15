using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// ApiSettings 单元测试
/// </summary>
public class ApiSettingsTests
{
    [Fact]
    public void ApiSettings_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(999)]
    [InlineData(500)]
    [InlineData(0)]
    [InlineData(-1000)]
    public void ApiSettings_WithInvalidTimeout_ShouldFailValidation(int invalidTimeout)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = invalidTimeout,
            RetryCount = 3,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("API超时时间必须在1000-300000毫秒之间"));
    }

    [Theory]
    [InlineData(300001)]
    [InlineData(400000)]
    [InlineData(1000000)]
    public void ApiSettings_WithTooLargeTimeout_ShouldFailValidation(int tooLargeTimeout)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = tooLargeTimeout,
            RetryCount = 3,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("API超时时间必须在1000-300000毫秒之间"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-100)]
    public void ApiSettings_WithNegativeRetryCount_ShouldFailValidation(int negativeRetryCount)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = negativeRetryCount,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("重试次数必须在0-10次之间"));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(100)]
    public void ApiSettings_WithTooLargeRetryCount_ShouldFailValidation(int tooLargeRetryCount)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = tooLargeRetryCount,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("重试次数必须在0-10次之间"));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-100)]
    public void ApiSettings_WithInvalidRetryDelay_ShouldFailValidation(int invalidRetryDelay)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = invalidRetryDelay
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("重试延迟必须在100-60000毫秒之间"));
    }

    [Theory]
    [InlineData(60001)]
    [InlineData(100000)]
    [InlineData(1000000)]
    public void ApiSettings_WithTooLargeRetryDelay_ShouldFailValidation(int tooLargeRetryDelay)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = tooLargeRetryDelay
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("重试延迟必须在100-60000毫秒之间"));
    }

    [Fact]
    public void ApiSettings_WithRetryCountZeroAndAnyRetryDelay_ShouldPassCustomValidation()
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 0,
            RetryDelay = 0 // This should be valid when RetryCount is 0
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ApiSettings_WithRetryCountGreaterThanZeroAndZeroRetryDelay_ShouldFailCustomValidation()
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = 0 // This should fail when RetryCount > 0
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("当重试次数大于0时，重试延迟必须大于0"));
    }

    [Fact]
    public void ApiSettings_WithRetryCountGreaterThanZeroAndNegativeRetryDelay_ShouldFailCustomValidation()
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = -100 // This should fail when RetryCount > 0
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("当重试次数大于0时，重试延迟必须大于0"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 100)]
    [InlineData(0, 1000)]
    public void ApiSettings_WithRetryCountZero_ShouldPassCustomValidationRegardlessOfRetryDelay(int retryCount, int retryDelay)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = retryCount,
            RetryDelay = retryDelay
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(3, 1000)]
    [InlineData(5, 2000)]
    [InlineData(10, 60000)]
    public void ApiSettings_WithValidRetryCountAndDelay_ShouldPassCustomValidation(int retryCount, int retryDelay)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = retryCount,
            RetryDelay = retryDelay
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ApiSettings_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new ApiSettings();

        // Assert
        settings.Timeout.Should().Be(30000);
        settings.RetryCount.Should().Be(3);
        settings.RetryDelay.Should().Be(1000);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(300000)]
    public void ApiSettings_WithValidTimeout_ShouldPassValidation(int timeout)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = timeout,
            RetryCount = 3,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void ApiSettings_WithValidRetryCount_ShouldPassValidation(int retryCount)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = retryCount,
            RetryDelay = 1000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(60000)]
    public void ApiSettings_WithValidRetryDelay_ShouldPassValidation(int retryDelay)
    {
        // Arrange
        var settings = new ApiSettings
        {
            Timeout = 30000,
            RetryCount = 3,
            RetryDelay = retryDelay
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}