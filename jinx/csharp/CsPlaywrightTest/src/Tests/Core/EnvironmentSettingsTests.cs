using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// EnvironmentSettings 单元测试
/// </summary>
public class EnvironmentSettingsTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void EnvironmentSettings_WithValidEnvironmentName_ShouldPassValidation(string environmentName)
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = environmentName,
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api"
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
    public void EnvironmentSettings_WithInvalidName_ShouldFailValidation(string? invalidName)
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = invalidName!,
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api"
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("环境名称不能为空") || 
                                      vr.ErrorMessage!.Contains("环境名称长度必须在1-50个字符之间"));
    }

    [Fact]
    public void EnvironmentSettings_WithTooLongName_ShouldFailValidation()
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = new string('A', 51), // 51 characters, exceeds limit
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api"
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("环境名称长度必须在1-50个字符之间"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-url")]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid-protocol.com")]
    public void EnvironmentSettings_WithInvalidBaseUrl_ShouldFailValidation(string? invalidUrl)
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = "Development",
            BaseUrl = invalidUrl!,
            ApiBaseUrl = "https://www.baidu.com/api"
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("基础URL不能为空") || 
                                      vr.ErrorMessage!.Contains("基础URL格式不正确"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid-api-url")]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid-protocol.com")]
    public void EnvironmentSettings_WithInvalidApiBaseUrl_ShouldFailValidation(string? invalidUrl)
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = "Development",
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = invalidUrl!
        };

        // Act
        var context = new ValidationContext(settings);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(settings, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(vr => vr.ErrorMessage!.Contains("API基础URL不能为空") || 
                                      vr.ErrorMessage!.Contains("API基础URL格式不正确"));
    }

    [Theory]
    [InlineData("InvalidEnvironment")]
    [InlineData("Custom")]
    [InlineData("Local")]
    [InlineData("UAT")]
    public void EnvironmentSettings_WithUnsupportedEnvironmentName_ShouldFailCustomValidation(string unsupportedName)
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = unsupportedName,
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api"
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("环境名称必须是以下值之一"));
    }

    [Fact]
    public void EnvironmentSettings_WithCaseInsensitiveValidEnvironmentName_ShouldPassCustomValidation()
    {
        // Arrange
        var settings = new EnvironmentSettings
        {
            Name = "development", // lowercase
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api"
        };

        // Act
        var validationResults = settings.Validate(new ValidationContext(settings)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void EnvironmentSettings_WithVariables_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var settings = new EnvironmentSettings
        {
            Name = "Development",
            BaseUrl = "https://www.baidu.com",
            ApiBaseUrl = "https://www.baidu.com/api",
            Variables = new Dictionary<string, string>
            {
                { "DB_CONNECTION", "Server=localhost;Database=TestDB;" },
                { "API_KEY", "test-api-key" }
            }
        };

        // Assert
        settings.Variables.Should().NotBeNull();
        settings.Variables.Should().HaveCount(2);
        settings.Variables["DB_CONNECTION"].Should().Be("Server=localhost;Database=TestDB;");
        settings.Variables["API_KEY"].Should().Be("test-api-key");
    }

    [Fact]
    public void EnvironmentSettings_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var settings = new EnvironmentSettings();

        // Assert
        settings.Name.Should().Be("Development");
        settings.BaseUrl.Should().Be(string.Empty);
        settings.ApiBaseUrl.Should().Be(string.Empty);
        settings.Variables.Should().NotBeNull();
        settings.Variables.Should().BeEmpty();
    }
}