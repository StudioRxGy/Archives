using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// ConfigurationService 集成测试
/// </summary>
public class ConfigurationServiceIntegrationTests
{
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceIntegrationTests()
    {
        // 使用实际的框架目录作为基础路径
        var frameworkPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Framework");
        _configurationService = new ConfigurationService(frameworkPath);
    }

    [Fact]
    public void LoadConfiguration_WithActualDevelopmentConfig_ShouldWork()
    {
        // Act
        var config = _configurationService.LoadConfiguration("Development");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Development", config.Environment.Name);
        Assert.Equal("https://dev.example.com", config.Environment.BaseUrl);
        Assert.Equal("https://dev-api.example.com", config.Environment.ApiBaseUrl);
        Assert.False(config.Browser.Headless);
        Assert.Equal("Debug", config.Logging.Level);
        Assert.Equal("Reports/Development", config.Reporting.OutputPath);
    }

    [Fact]
    public void LoadConfiguration_WithActualTestConfig_ShouldWork()
    {
        // Act
        var config = _configurationService.LoadConfiguration("Test");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Test", config.Environment.Name);
        Assert.Equal("https://test.example.com", config.Environment.BaseUrl);
        Assert.Equal("https://test-api.example.com", config.Environment.ApiBaseUrl);
        Assert.True(config.Browser.Headless);
        Assert.Equal(1280, config.Browser.ViewportWidth);
        Assert.Equal(720, config.Browser.ViewportHeight);
        Assert.Equal("Information", config.Logging.Level);
        Assert.False(config.Reporting.IncludeScreenshots);
    }

    [Fact]
    public void LoadConfiguration_WithActualStagingConfig_ShouldWork()
    {
        // Act
        var config = _configurationService.LoadConfiguration("Staging");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Staging", config.Environment.Name);
        Assert.Equal("https://staging.example.com", config.Environment.BaseUrl);
        Assert.Equal("https://staging-api.example.com", config.Environment.ApiBaseUrl);
        Assert.True(config.Browser.Headless);
        Assert.Equal(45000, config.Api.Timeout);
        Assert.Equal(5, config.Api.RetryCount);
        Assert.Equal("Warning", config.Logging.Level);
    }

    [Fact]
    public void GetAvailableEnvironments_WithActualFiles_ShouldReturnCorrectEnvironments()
    {
        // Act
        var environments = _configurationService.GetAvailableEnvironments();

        // Assert
        Assert.Contains("Development", environments);
        Assert.Contains("Test", environments);
        Assert.Contains("Staging", environments);
        // Production 文件不存在，所以不应该在列表中
        Assert.DoesNotContain("Production", environments);
    }

    [Theory]
    [InlineData("--environment", "Development")]
    [InlineData("--env", "Test")]
    [InlineData("-e", "Staging")]
    [InlineData("--environment=Development")]
    [InlineData("--env=Test")]
    [InlineData("-e=Staging")]
    public void LoadConfigurationFromCommandLine_WithVariousFormats_ShouldWork(params string[] args)
    {
        // Act
        var config = _configurationService.LoadConfigurationFromCommandLine(args);

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Environment);
        Assert.NotEmpty(config.Environment.Name);
    }

    [Fact]
    public void ConfigurationService_ShouldSupportCaching()
    {
        // Act
        var config1 = _configurationService.LoadConfiguration("Development");
        var config2 = _configurationService.LoadConfiguration("Development");

        // Assert
        Assert.Same(config1, config2); // 应该是同一个实例（缓存）

        // 清除缓存后应该是不同实例
        _configurationService.ClearCache();
        var config3 = _configurationService.LoadConfiguration("Development");
        
        Assert.NotSame(config1, config3); // 应该是不同实例
    }
}