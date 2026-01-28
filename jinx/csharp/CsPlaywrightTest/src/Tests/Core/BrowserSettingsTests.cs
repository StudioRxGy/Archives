using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// BrowserSettings 单元测试
/// </summary>
public class BrowserSettingsTests
{
    [Theory]
    [InlineData("Chromium")]
    [InlineData("Firefox")]
    [InlineData("Webkit")]
    public void BrowserSettings_WithValidBrowserType_ShouldPassValidation(string browserType)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = browserType,
            Headless = false,
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = 30000
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
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void BrowserSettings_WithInvalidBrowserType_ShouldFailValidation(string? invalidType)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = invalidType!,
            Headless = false,
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("浏览器类型不能为空"));
    }

    [Theory]
    [InlineData("Chrome")]
    [InlineData("Edge")]
    [InlineData("Safari")]
    [InlineData("InvalidBrowser")]
    public void BrowserSettings_WithUnsupportedBrowserType_ShouldFailCustomValidation(string unsupportedType)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = unsupportedType,
            Headless = false,
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("浏览器类型必须是以下值之一"));
    }

    [Fact]
    public void BrowserSettings_WithCaseInsensitiveValidBrowserType_ShouldPassCustomValidation()
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "chromium", // lowercase
            Headless = false,
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-100)]
    public void BrowserSettings_WithInvalidViewportWidth_ShouldFailValidation(int invalidWidth)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = invalidWidth,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("视口宽度必须在100-4000像素之间"));
    }

    [Theory]
    [InlineData(4001)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void BrowserSettings_WithTooLargeViewportWidth_ShouldFailValidation(int tooLargeWidth)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = tooLargeWidth,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("视口宽度必须在100-4000像素之间"));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-100)]
    public void BrowserSettings_WithInvalidViewportHeight_ShouldFailValidation(int invalidHeight)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = 1920,
            ViewportHeight = invalidHeight,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("视口高度必须在100-3000像素之间"));
    }

    [Theory]
    [InlineData(3001)]
    [InlineData(4000)]
    [InlineData(5000)]
    public void BrowserSettings_WithTooLargeViewportHeight_ShouldFailValidation(int tooLargeHeight)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = 1920,
            ViewportHeight = tooLargeHeight,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("视口高度必须在100-3000像素之间"));
    }

    [Theory]
    [InlineData(999)]
    [InlineData(500)]
    [InlineData(0)]
    [InlineData(-1000)]
    public void BrowserSettings_WithInvalidTimeout_ShouldFailValidation(int invalidTimeout)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = invalidTimeout
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("超时时间必须在1000-300000毫秒之间"));
    }

    [Theory]
    [InlineData(300001)]
    [InlineData(400000)]
    [InlineData(1000000)]
    public void BrowserSettings_WithTooLargeTimeout_ShouldFailValidation(int tooLargeTimeout)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = tooLargeTimeout
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("超时时间必须在1000-300000毫秒之间"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrowserSettings_WithValidHeadlessValue_ShouldPassValidation(bool headlessValue)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            Headless = headlessValue,
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = 30000
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void BrowserSettings_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new BrowserSettings();

        // Assert
        settings.Type.Should().Be("Chromium");
        settings.Headless.Should().BeFalse();
        settings.ViewportWidth.Should().Be(1920);
        settings.ViewportHeight.Should().Be(1080);
        settings.Timeout.Should().Be(30000);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(1920, 1080)]
    [InlineData(4000, 3000)]
    public void BrowserSettings_WithValidViewportDimensions_ShouldPassValidation(int width, int height)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = width,
            ViewportHeight = height,
            Timeout = 30000
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
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(300000)]
    public void BrowserSettings_WithValidTimeout_ShouldPassValidation(int timeout)
    {
        // Arrange
        var settings = new BrowserSettings
        {
            Type = "Chromium",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            Timeout = timeout
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