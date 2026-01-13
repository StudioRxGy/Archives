using Xunit;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// ConfigurationManager 单元测试
/// </summary>
public class ConfigurationManagerTests
{
    [Fact]
    public void GetConfiguration_ShouldReturnValidConfiguration()
    {
        // Act
        var config = ConfigurationManager.GetConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Environment);
        Assert.NotNull(config.Browser);
        Assert.NotNull(config.Api);
        Assert.NotNull(config.Reporting);
        Assert.NotNull(config.Logging);
    }

    [Fact]
    public void GetConfiguration_ShouldReturnSameInstance()
    {
        // Act
        var config1 = ConfigurationManager.GetConfiguration();
        var config2 = ConfigurationManager.GetConfiguration();

        // Assert
        Assert.Same(config1, config2);
    }

    [Fact]
    public void SaveToFile_And_LoadFromFile_ShouldWork()
    {
        // Arrange
        var originalConfig = new TestConfiguration
        {
            Environment = new EnvironmentSettings
            {
                Name = "Test",
                BaseUrl = "https://test.example.com"
            }
        };
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            ConfigurationManager.SaveToFile(originalConfig, tempFile);
            var loadedConfig = ConfigurationManager.LoadFromFile(tempFile);

            // Assert
            Assert.Equal(originalConfig.Environment.Name, loadedConfig.Environment.Name);
            Assert.Equal(originalConfig.Environment.BaseUrl, loadedConfig.Environment.BaseUrl);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromFile_WithNonExistentFile_ShouldThrowException()
    {
        // Arrange
        var nonExistentFile = "non-existent-file.json";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            ConfigurationManager.LoadFromFile(nonExistentFile));
    }
}