using Xunit;
using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// BaseTestFixture 单元测试
/// </summary>
public class BaseTestFixtureTests
{
    private class TestFixture : BaseTestFixture
    {
        public TestFixture() : base(CreateLogger()) { }
        
        private static ILogger CreateLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<TestFixture>();
        }
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreatePlaywrightInstances()
    {
        // Arrange
        var fixture = new TestFixture();

        // Act
        await fixture.InitializeAsync();

        // Assert
        Assert.NotNull(fixture.Playwright);
        Assert.NotNull(fixture.Browser);
        Assert.NotNull(fixture.Context);
        Assert.NotNull(fixture.Page);
        Assert.NotNull(fixture.Configuration);

        // Cleanup
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var fixture = new TestFixture();
        await fixture.InitializeAsync();

        // Act & Assert - Should not throw
        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task Configuration_ShouldHaveDefaultValues()
    {
        // Arrange
        var fixture = new TestFixture();
        await fixture.InitializeAsync();

        // Act
        var config = fixture.Configuration;

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Environment);
        Assert.NotNull(config.Browser);
        Assert.NotNull(config.Api);
        Assert.NotNull(config.Reporting);
        Assert.NotNull(config.Logging);
        
        // Cleanup
        await fixture.DisposeAsync();
    }
}