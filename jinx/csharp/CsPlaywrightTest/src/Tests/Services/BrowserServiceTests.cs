using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using FluentAssertions;
using Xunit;
using EnterpriseAutomationFramework.Services.Browser;
using EnterpriseAutomationFramework.Core.Configuration;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// BrowserService 单元测试
/// </summary>
public class BrowserServiceTests : IAsyncDisposable
{
    private readonly ILogger<BrowserService> _logger;
    private readonly BrowserService _browserService;
    private readonly BrowserSettings _defaultSettings;

    public BrowserServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BrowserService>();
        _browserService = new BrowserService(_logger);
        
        _defaultSettings = new BrowserSettings
        {
            Type = "Chromium",
            Headless = true, // 使用无头模式进行测试
            ViewportWidth = 1280,
            ViewportHeight = 720,
            Timeout = 30000
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new BrowserService(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitializeSuccessfully()
    {
        // Act
        var service = new BrowserService(_logger);

        // Assert
        service.Should().NotBeNull();
        service.Playwright.Should().BeNull(); // 初始状态下应该为空
        service.Browser.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetBrowserAsync_WithInvalidBrowserType_ShouldThrowArgumentException(string browserType)
    {
        // Act & Assert
        var action = async () => await _browserService.GetBrowserAsync(browserType);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("browserType");
    }

    [Theory]
    [InlineData("InvalidBrowser")]
    [InlineData("Chrome")]
    [InlineData("Edge")]
    public async Task GetBrowserAsync_WithUnsupportedBrowserType_ShouldThrowTestFrameworkException(string browserType)
    {
        // Act & Assert
        var action = async () => await _browserService.GetBrowserAsync(browserType);
        await action.Should().ThrowAsync<TestFrameworkException>()
            .WithMessage($"不支持的浏览器类型: {browserType}");
    }

    [Theory]
    [InlineData("Chromium")]
    [InlineData("chromium")]
    [InlineData("CHROMIUM")]
    public async Task GetBrowserAsync_WithValidBrowserType_ShouldReturnBrowser(string browserType)
    {
        // Act
        var browser = await _browserService.GetBrowserAsync(browserType);

        // Assert
        browser.Should().NotBeNull();
        browser.IsConnected.Should().BeTrue();
        _browserService.Playwright.Should().NotBeNull();
        _browserService.Browser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBrowserAsync_CalledTwiceWithSameBrowserType_ShouldReturnSameBrowser()
    {
        // Act
        var browser1 = await _browserService.GetBrowserAsync("Chromium");
        var browser2 = await _browserService.GetBrowserAsync("Chromium");

        // Assert
        browser1.Should().BeSameAs(browser2);
    }

    [Fact]
    public async Task CreateContextAsync_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _browserService.CreateContextAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task CreateContextAsync_WithValidSettings_ShouldReturnContext()
    {
        // Act
        var context = await _browserService.CreateContextAsync(_defaultSettings);

        // Assert
        context.Should().NotBeNull();
        context.Browser.Should().NotBeNull();
        context.Browser.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task CreatePageAsync_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _browserService.CreatePageAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public async Task CreatePageAsync_WithValidSettings_ShouldReturnPage()
    {
        // Act
        var page = await _browserService.CreatePageAsync(_defaultSettings);

        // Assert
        page.Should().NotBeNull();
        page.Context.Should().NotBeNull();
        page.Context.Browser.Should().NotBeNull();
    }

    [Fact]
    public async Task TakeScreenshotAsync_WithNullPage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _browserService.TakeScreenshotAsync(null!, "test.png");
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("page");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task TakeScreenshotAsync_WithInvalidFileName_ShouldThrowArgumentException(string fileName)
    {
        // Arrange
        var page = await _browserService.CreatePageAsync(_defaultSettings);

        // Act & Assert
        var action = async () => await _browserService.TakeScreenshotAsync(page, fileName);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("fileName");
    }

    [Fact]
    public async Task TakeScreenshotAsync_WithValidPageAndFileName_ShouldReturnScreenshotBytes()
    {
        // Arrange
        var page = await _browserService.CreatePageAsync(_defaultSettings);
        await page.GotoAsync("data:text/html,<html><body><h1>Test Page</h1></body></html>");

        // Act
        var screenshot = await _browserService.TakeScreenshotAsync(page, "test.png");

        // Assert
        screenshot.Should().NotBeNull();
        screenshot.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TakeScreenshotToFileAsync_WithNullPage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _browserService.TakeScreenshotToFileAsync(null!, "test.png");
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("page");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task TakeScreenshotToFileAsync_WithInvalidFilePath_ShouldThrowArgumentException(string filePath)
    {
        // Arrange
        var page = await _browserService.CreatePageAsync(_defaultSettings);

        // Act & Assert
        var action = async () => await _browserService.TakeScreenshotToFileAsync(page, filePath);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public async Task TakeScreenshotToFileAsync_WithValidPageAndFilePath_ShouldCreateFile()
    {
        // Arrange
        var page = await _browserService.CreatePageAsync(_defaultSettings);
        await page.GotoAsync("data:text/html,<html><body><h1>Test Page</h1></body></html>");
        var tempDir = Path.Combine(Path.GetTempPath(), "BrowserServiceTests");
        var filePath = Path.Combine(tempDir, "test_screenshot.png");

        try
        {
            // Act
            await _browserService.TakeScreenshotToFileAsync(page, filePath);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var fileInfo = new FileInfo(filePath);
            fileInfo.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task CloseAsync_ShouldCloseAllBrowsersAndPlaywright()
    {
        // Arrange
        var browser = await _browserService.GetBrowserAsync("Chromium");
        browser.IsConnected.Should().BeTrue();

        // Act
        await _browserService.CloseAsync();

        // Assert
        _browserService.Playwright.Should().BeNull();
        _browserService.Browser.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCloseServiceProperly()
    {
        // Arrange
        await _browserService.GetBrowserAsync("Chromium");

        // Act
        await _browserService.DisposeAsync();

        // Assert
        _browserService.Playwright.Should().BeNull();
        _browserService.Browser.Should().BeNull();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        await _browserService.GetBrowserAsync("Chromium");

        // Act & Assert
        await _browserService.DisposeAsync();
        var action = async () => await _browserService.DisposeAsync();
        await action.Should().NotThrowAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _browserService.DisposeAsync();
    }
}