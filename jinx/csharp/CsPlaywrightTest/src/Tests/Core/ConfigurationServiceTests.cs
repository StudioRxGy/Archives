using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// ConfigurationService 单元测试
/// </summary>
public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        // 创建临时测试目录
        _testBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);
        
        _configurationService = new ConfigurationService(_testBasePath);
        
        // 创建测试配置文件
        CreateTestConfigurationFiles();
    }

    [Fact]
    public void LoadConfiguration_WithValidEnvironment_ShouldReturnConfiguration()
    {
        // Act
        var config = _configurationService.LoadConfiguration("Development");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Development", config.Environment.Name);
        Assert.Equal("https://dev.example.com", config.Environment.BaseUrl);
        Assert.False(config.Browser.Headless);
        Assert.Equal("Debug", config.Logging.Level);
    }

    [Fact]
    public void LoadConfiguration_WithTestEnvironment_ShouldReturnTestConfiguration()
    {
        // Act
        var config = _configurationService.LoadConfiguration("Test");

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Test", config.Environment.Name);
        Assert.Equal("https://test.example.com", config.Environment.BaseUrl);
        Assert.True(config.Browser.Headless);
        Assert.Equal(1280, config.Browser.ViewportWidth);
        Assert.Equal("Information", config.Logging.Level);
    }

    [Fact]
    public void LoadConfiguration_WithInvalidEnvironment_ShouldThrowException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _configurationService.LoadConfiguration("InvalidEnvironment"));
        
        Assert.Contains("无效的环境名称", exception.Message);
    }

    [Fact]
    public void LoadConfiguration_WithEmptyEnvironment_ShouldThrowException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _configurationService.LoadConfiguration(""));
        
        Assert.Contains("环境名称不能为空", exception.Message);
    }

    [Fact]
    public void LoadConfiguration_WithNullEnvironment_ShouldThrowException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            _configurationService.LoadConfiguration(null!));
        
        Assert.Contains("环境名称不能为空", exception.Message);
    }

    [Fact]
    public void LoadConfiguration_SameEnvironmentTwice_ShouldReturnCachedInstance()
    {
        // Act
        var config1 = _configurationService.LoadConfiguration("Development");
        var config2 = _configurationService.LoadConfiguration("Development");

        // Assert
        Assert.Same(config1, config2);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithEnvironmentFlag_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "--environment", "Test" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Test", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithEnvFlag_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "--env", "Staging" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Staging", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithShortFlag_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "-e", "Production" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Production", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithEqualsFormat_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "--environment=Test" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Test", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithEnvEqualsFormat_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "--env=Staging" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Staging", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithShortEqualsFormat_ShouldReturnEnvironment()
    {
        // Arrange
        var args = new[] { "-e=Production" };

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Production", environment);
    }

    [Fact]
    public void GetEnvironmentFromCommandLine_WithNoArgs_ShouldReturnDevelopment()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var environment = _configurationService.GetEnvironmentFromCommandLine(args);

        // Assert
        Assert.Equal("Development", environment);
    }

    [Fact]
    public void LoadConfigurationFromCommandLine_WithValidArgs_ShouldReturnConfiguration()
    {
        // Arrange
        var args = new[] { "--environment", "Test" };

        // Act
        var config = _configurationService.LoadConfigurationFromCommandLine(args);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Test", config.Environment.Name);
    }

    [Fact]
    public void ConfigurationFileExists_WithExistingFile_ShouldReturnTrue()
    {
        // Act
        var exists = _configurationService.ConfigurationFileExists("Development");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void ConfigurationFileExists_WithNonExistingFile_ShouldReturnFalse()
    {
        // Act
        var exists = _configurationService.ConfigurationFileExists("NonExistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void GetAvailableEnvironments_ShouldReturnExistingEnvironments()
    {
        // Act
        var environments = _configurationService.GetAvailableEnvironments();

        // Assert
        Assert.Contains("Development", environments);
        Assert.Contains("Test", environments);
        Assert.DoesNotContain("Production", environments); // 我们没有创建这个文件
    }

    [Fact]
    public void ClearCache_ShouldClearConfigurationCache()
    {
        // Arrange
        var config1 = _configurationService.LoadConfiguration("Development");
        
        // Act
        _configurationService.ClearCache();
        var config2 = _configurationService.LoadConfiguration("Development");

        // Assert
        Assert.NotSame(config1, config2); // 应该是不同的实例
    }

    [Fact]
    public void ValidateConfiguration_WithValidConfiguration_ShouldReturnSuccess()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Development",
                BaseUrl = "https://example.com",
                ApiBaseUrl = "https://api.example.com"
            },
            Browser = new BrowserSettings
            {
                Type = "Chromium",
                ViewportWidth = 1920,
                ViewportHeight = 1080
            },
            Api = new ApiSettings
            {
                Timeout = 30000,
                RetryCount = 3,
                RetryDelay = 1000
            },
            Reporting = new ReportingSettings(),
            Logging = new LoggingSettings()
        };

        // Act
        var result = _configurationService.ValidateConfiguration(config);

        // Assert
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void ValidateConfiguration_WithNullConfiguration_ShouldReturnError()
    {
        // Act
        var result = _configurationService.ValidateConfiguration(null!);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("配置不能为空", result.ErrorMessage!);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidConfiguration_ShouldReturnError()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "InvalidEnvironment", // 无效的环境名称
                BaseUrl = "invalid-url", // 无效的URL
                ApiBaseUrl = "invalid-api-url" // 无效的URL
            }
        };

        // Act
        var result = _configurationService.ValidateConfiguration(config);

        // Assert
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.NotNull(result.ErrorMessage);
    }

    private void CreateTestConfigurationFiles()
    {
        // 创建 Development 环境配置
        var devConfig = @"{
  ""TestConfiguration"": {
    ""Environment"": {
      ""Name"": ""Development"",
      ""BaseUrl"": ""https://dev.example.com"",
      ""ApiBaseUrl"": ""https://dev-api.example.com"",
      ""Variables"": {
        ""DefaultTimeout"": ""30000"",
        ""RetryCount"": ""3""
      }
    },
    ""Browser"": {
      ""Type"": ""Chromium"",
      ""Headless"": false,
      ""ViewportWidth"": 1920,
      ""ViewportHeight"": 1080,
      ""Timeout"": 30000
    },
    ""Api"": {
      ""Timeout"": 30000,
      ""RetryCount"": 3,
      ""RetryDelay"": 1000
    },
    ""Reporting"": {
      ""OutputPath"": ""Reports/Development"",
      ""Format"": ""Html"",
      ""IncludeScreenshots"": true
    },
    ""Logging"": {
      ""Level"": ""Debug"",
      ""FilePath"": ""Logs/dev-test-{Date}.log""
    }
  }
}";

        // 创建 Test 环境配置
        var testConfig = @"{
  ""TestConfiguration"": {
    ""Environment"": {
      ""Name"": ""Test"",
      ""BaseUrl"": ""https://test.example.com"",
      ""ApiBaseUrl"": ""https://test-api.example.com"",
      ""Variables"": {
        ""DefaultTimeout"": ""20000"",
        ""RetryCount"": ""2""
      }
    },
    ""Browser"": {
      ""Type"": ""Chromium"",
      ""Headless"": true,
      ""ViewportWidth"": 1280,
      ""ViewportHeight"": 720,
      ""Timeout"": 20000
    },
    ""Api"": {
      ""Timeout"": 20000,
      ""RetryCount"": 2,
      ""RetryDelay"": 500
    },
    ""Reporting"": {
      ""OutputPath"": ""Reports/Test"",
      ""Format"": ""Html"",
      ""IncludeScreenshots"": false
    },
    ""Logging"": {
      ""Level"": ""Information"",
      ""FilePath"": ""Logs/test-{Date}.log""
    }
  }
}";

        File.WriteAllText(Path.Combine(_testBasePath, "appsettings.Development.json"), devConfig);
        File.WriteAllText(Path.Combine(_testBasePath, "appsettings.Test.json"), testConfig);
    }

    public void Dispose()
    {
        // 清理测试目录
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}