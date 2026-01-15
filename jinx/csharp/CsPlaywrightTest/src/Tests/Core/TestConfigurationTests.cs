using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// TestConfiguration 单元测试
/// </summary>
public class TestConfigurationTests
{
    [Fact]
    public void TestConfiguration_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Development",
                BaseUrl = "https://www.baidu.com",
                ApiBaseUrl = "https://www.baidu.com/api"
            },
            Browser = new BrowserSettings
            {
                Type = "Chromium",
                Headless = false,
                ViewportWidth = 1920,
                ViewportHeight = 1080,
                Timeout = 30000
            },
            Api = new ApiSettings
            {
                Timeout = 30000,
                RetryCount = 3,
                RetryDelay = 1000
            },
            Reporting = new ReportingSettings
            {
                OutputPath = "Reports",
                Format = "Html",
                IncludeScreenshots = true
            },
            Logging = new LoggingSettings
            {
                Level = "Information",
                FilePath = "Logs/test-{Date}.log"
            }
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void TestConfiguration_WithNullEnvironment_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = null!
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("环境设置不能为空");
    }

    [Fact]
    public void TestConfiguration_WithNullBrowser_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Browser = null!
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("浏览器设置不能为空");
    }

    [Fact]
    public void TestConfiguration_WithNullApi_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Api = null!
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("API设置不能为空");
    }

    [Fact]
    public void TestConfiguration_WithNullReporting_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Reporting = null!
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("报告设置不能为空");
    }

    [Fact]
    public void TestConfiguration_WithNullLogging_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Logging = null!
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain("日志设置不能为空");
    }

    [Fact]
    public void TestConfiguration_WithInvalidNestedObjects_ShouldFailValidation()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "", // Invalid: empty name
                BaseUrl = "invalid-url", // Invalid: not a valid URL
                ApiBaseUrl = "invalid-api-url" // Invalid: not a valid URL
            },
            Browser = new BrowserSettings
            {
                Type = "InvalidBrowser", // Invalid: not supported browser type
                ViewportWidth = 50, // Invalid: too small
                ViewportHeight = 50, // Invalid: too small
                Timeout = 500 // Invalid: too small
            },
            Api = new ApiSettings
            {
                Timeout = 500, // Invalid: too small
                RetryCount = 15, // Invalid: too large
                RetryDelay = 50 // Invalid: too small
            }
        };

        // Act
        var isValid = config.IsValid();
        var errors = config.GetValidationErrors();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("环境名称长度必须在1-50个字符之间"));
        errors.Should().Contain(e => e.Contains("基础URL格式不正确"));
        errors.Should().Contain(e => e.Contains("API基础URL格式不正确"));
        errors.Should().Contain(e => e.Contains("浏览器类型必须是以下值之一"));
        errors.Should().Contain(e => e.Contains("视口宽度必须在100-4000像素之间"));
        errors.Should().Contain(e => e.Contains("视口高度必须在100-3000像素之间"));
        errors.Should().Contain(e => e.Contains("超时时间必须在1000-300000毫秒之间"));
        errors.Should().Contain(e => e.Contains("API超时时间必须在1000-300000毫秒之间"));
        errors.Should().Contain(e => e.Contains("重试次数必须在0-10次之间"));
        errors.Should().Contain(e => e.Contains("重试延迟必须在100-60000毫秒之间"));
    }

    [Fact]
    public void Validate_ShouldReturnValidationResults()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "InvalidEnvironment",
                BaseUrl = "invalid-url",
                ApiBaseUrl = "invalid-api-url"
            }
        };

        // Act
        var validationResults = config.Validate(new ValidationContext(config)).ToList();

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("环境名称必须是以下值之一"));
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("基础URL格式不正确"));
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("API基础URL格式不正确"));
    }
}