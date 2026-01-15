using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using FluentAssertions;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Tests.Core;

/// <summary>
/// BasePageObject 单元测试
/// </summary>
public class BasePageObjectTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private ILogger? _logger;
    private TestPageObject? _testPageObject;

    /// <summary>
    /// 测试用的页面对象实现
    /// </summary>
    private class TestPageObject : BasePageObject
    {
        protected override string PageName => "TestPage";
        
        public TestPageObject(IPage page, ILogger logger, YamlElementReader? elementReader = null)
            : base(page, logger, elementReader) { }

        public override async Task<bool> IsLoadedAsync()
        {
            // 简单的加载检查 - 检查页面标题是否存在
            var title = await _page.TitleAsync();
            return !string.IsNullOrEmpty(title);
        }

        // 公开受保护的方法以便测试
        public new async Task WaitForElementAsync(string selector, int timeoutMs = 30000)
        {
            await base.WaitForElementAsync(selector, timeoutMs);
        }

        public new async Task<bool> IsElementVisibleAsync(string selector)
        {
            return await base.IsElementVisibleAsync(selector);
        }

        public new async Task ClickAsync(string selector)
        {
            await base.ClickAsync(selector);
        }

        public new async Task FillAsync(string selector, string text)
        {
            await base.FillAsync(selector, text);
        }

        public new async Task<string> GetTextAsync(string selector)
        {
            return await base.GetTextAsync(selector);
        }

        public new async Task<string?> GetAttributeAsync(string selector, string attributeName)
        {
            return await base.GetAttributeAsync(selector, attributeName);
        }

        public new async Task LoadElementsAsync(string yamlFilePath)
        {
            await base.LoadElementsAsync(yamlFilePath);
        }

        public new PageElement GetYamlElement(string elementName)
        {
            return base.GetYamlElement(elementName);
        }

        public new bool HasYamlElement(string elementName)
        {
            return base.HasYamlElement(elementName);
        }

        public new async Task WaitForYamlElementAsync(string elementName)
        {
            await base.WaitForYamlElementAsync(elementName);
        }

        public new async Task<bool> IsYamlElementVisibleAsync(string elementName)
        {
            return await base.IsYamlElementVisibleAsync(elementName);
        }

        public new async Task ClickYamlElementAsync(string elementName)
        {
            await base.ClickYamlElementAsync(elementName);
        }

        public new async Task FillYamlElementAsync(string elementName, string text)
        {
            await base.FillYamlElementAsync(elementName, text);
        }

        public new async Task<string> GetYamlElementTextAsync(string elementName)
        {
            return await base.GetYamlElementTextAsync(elementName);
        }

        public new async Task<string?> GetYamlElementAttributeAsync(string elementName, string attributeName)
        {
            return await base.GetYamlElementAttributeAsync(elementName, attributeName);
        }

        public new async Task WaitForTitleContainsAsync(string expectedTitle, int timeoutMs = 30000)
        {
            await base.WaitForTitleContainsAsync(expectedTitle, timeoutMs);
        }

        public new async Task WaitForUrlContainsAsync(string expectedUrlPart, int timeoutMs = 30000)
        {
            await base.WaitForUrlContainsAsync(expectedUrlPart, timeoutMs);
        }
    }

    public async Task InitializeAsync()
    {
        // 创建日志记录器
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<BasePageObjectTests>();

        // 初始化 Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();

        // 创建测试页面对象
        _testPageObject = new TestPageObject(_page, _logger);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_browser != null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var pageObject = new TestPageObject(_page!, _logger!);

        // Assert
        pageObject.Should().NotBeNull();
    }

    [Fact]
    public async Task NavigateAsync_ShouldNavigateToUrl()
    {
        // Arrange
        var testUrl = "data:text/html,<html><head><title>Test Page</title></head><body><h1>Test</h1></body></html>";

        // Act
        await _testPageObject!.NavigateAsync(testUrl);

        // Assert
        var currentUrl = _page!.Url;
        currentUrl.Should().StartWith("data:text/html");
        
        var title = await _page.TitleAsync();
        title.Should().Be("Test Page");
    }

    [Fact]
    public async Task IsLoadedAsync_ShouldReturnTrueWhenPageHasTitle()
    {
        // Arrange
        var testUrl = "data:text/html,<html><head><title>Test Page</title></head><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isLoaded = await _testPageObject!.IsLoadedAsync();

        // Assert
        isLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForLoadAsync_ShouldCompleteWhenPageIsLoaded()
    {
        // Arrange
        var testUrl = "data:text/html,<html><head><title>Test Page</title></head><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert - Should not throw
        await _testPageObject!.WaitForLoadAsync();
    }

    [Fact]
    public async Task IsElementVisibleAsync_ShouldReturnTrueForVisibleElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='test'>Visible Element</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isVisible = await _testPageObject!.IsElementVisibleAsync("#test");

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public async Task IsElementVisibleAsync_ShouldReturnFalseForNonExistentElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isVisible = await _testPageObject!.IsElementVisibleAsync("#nonexistent");

        // Assert
        isVisible.Should().BeFalse();
    }

    [Fact]
    public async Task ClickAsync_ShouldClickElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><button id='btn' onclick='this.innerText=\"Clicked\"'>Click Me</button></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        await _testPageObject!.ClickAsync("#btn");

        // Assert
        var buttonText = await _page.TextContentAsync("#btn");
        buttonText.Should().Be("Clicked");
    }

    [Fact]
    public async Task FillAsync_ShouldFillInputElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='input' type='text'></body></html>";
        await _page!.GotoAsync(testUrl);
        var testText = "Test Input";

        // Act
        await _testPageObject!.FillAsync("#input", testText);

        // Assert
        var inputValue = await _page.InputValueAsync("#input");
        inputValue.Should().Be(testText);
    }

    [Fact]
    public async Task GetTextAsync_ShouldReturnElementText()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='text'>Hello World</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var text = await _testPageObject!.GetTextAsync("#text");

        // Assert
        text.Should().Be("Hello World");
    }

    [Fact]
    public async Task GetAttributeAsync_ShouldReturnAttributeValue()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='test' data-value='test-attribute'>Test</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var attributeValue = await _testPageObject!.GetAttributeAsync("#test", "data-value");

        // Assert
        attributeValue.Should().Be("test-attribute");
    }

    [Fact]
    public async Task WaitForElementAsync_ShouldThrowElementNotFoundExceptionForNonExistentElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert
        await Assert.ThrowsAsync<ElementNotFoundException>(
            () => _testPageObject!.WaitForElementAsync("#nonexistent", 1000));
    }

    [Fact]
    public async Task WaitForTitleContainsAsync_ShouldCompleteWhenTitleMatches()
    {
        // Arrange
        var testUrl = "data:text/html,<html><head><title>Test Page Title</title></head><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert - Should not throw
        await _testPageObject!.WaitForTitleContainsAsync("Test Page");
    }

    [Fact]
    public async Task WaitForTitleContainsAsync_ShouldThrowTimeoutExceptionWhenTitleDoesNotMatch()
    {
        // Arrange
        var testUrl = "data:text/html,<html><head><title>Different Title</title></head><body></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => _testPageObject!.WaitForTitleContainsAsync("Expected Title", 1000));
    }

    [Fact]
    public void GetYamlElement_ShouldThrowInvalidOperationExceptionWhenElementsNotLoaded()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => _testPageObject!.GetYamlElement("TestElement"));
    }

    [Fact]
    public void HasYamlElement_ShouldReturnFalseWhenElementsNotLoaded()
    {
        // Act
        var hasElement = _testPageObject!.HasYamlElement("TestElement");

        // Assert
        hasElement.Should().BeFalse();
    }

    [Fact]
    public async Task LoadElementsAsync_ShouldLoadYamlElements()
    {
        // Arrange
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");

        // Act
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Assert
        _testPageObject.HasYamlElement("TestButton").Should().BeTrue();
        _testPageObject.HasYamlElement("TestInput").Should().BeTrue();
        _testPageObject.HasYamlElement("TestText").Should().BeTrue();
        _testPageObject.HasYamlElement("NonExistentElement").Should().BeFalse();
    }

    [Fact]
    public async Task GetYamlElement_ShouldReturnCorrectElement()
    {
        // Arrange
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act
        var element = _testPageObject.GetYamlElement("TestButton");

        // Assert
        element.Should().NotBeNull();
        element.Name.Should().Be("TestButton");
        element.Selector.Should().Be("#test-button");
        element.Type.Should().Be(ElementType.Button);
        element.TimeoutMs.Should().Be(5000);
    }

    [Fact]
    public async Task GetYamlElement_ShouldThrowElementNotFoundExceptionForNonExistentElement()
    {
        // Arrange
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act & Assert
        Assert.Throws<ElementNotFoundException>(
            () => _testPageObject.GetYamlElement("NonExistentElement"));
    }

    [Fact]
    public async Task ClickYamlElementAsync_ShouldClickYamlDefinedElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><button id='test-button' onclick='this.innerText=\"Clicked\"'>Click Me</button></body></html>";
        await _page!.GotoAsync(testUrl);
        
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act
        await _testPageObject.ClickYamlElementAsync("TestButton");

        // Assert
        var buttonText = await _page.TextContentAsync("#test-button");
        buttonText.Should().Be("Clicked");
    }

    [Fact]
    public async Task FillYamlElementAsync_ShouldFillYamlDefinedElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='test-input' type='text'></body></html>";
        await _page!.GotoAsync(testUrl);
        
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);
        var testText = "Test Input";

        // Act
        await _testPageObject.FillYamlElementAsync("TestInput", testText);

        // Assert
        var inputValue = await _page.InputValueAsync("#test-input");
        inputValue.Should().Be(testText);
    }

    [Fact]
    public async Task GetYamlElementTextAsync_ShouldReturnYamlElementText()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='test-text'>Hello YAML</div></body></html>";
        await _page!.GotoAsync(testUrl);
        
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act
        var text = await _testPageObject.GetYamlElementTextAsync("TestText");

        // Assert
        text.Should().Be("Hello YAML");
    }

    [Fact]
    public async Task IsYamlElementVisibleAsync_ShouldReturnTrueForVisibleYamlElement()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='test-text'>Visible YAML Element</div></body></html>";
        await _page!.GotoAsync(testUrl);
        
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act
        var isVisible = await _testPageObject.IsYamlElementVisibleAsync("TestText");

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    public async Task GetYamlElementAttributeAsync_ShouldReturnYamlElementAttribute()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div id='test-text' class='test-class' data-test='test-value'>Test</div></body></html>";
        await _page!.GotoAsync(testUrl);
        
        var yamlFilePath = Path.Combine("TestData", "test_elements.yaml");
        await _testPageObject!.LoadElementsAsync(yamlFilePath);

        // Act
        var classValue = await _testPageObject.GetYamlElementAttributeAsync("TestText", "class");
        var dataValue = await _testPageObject.GetYamlElementAttributeAsync("TestText", "data-test");

        // Assert
        classValue.Should().Be("test-class");
        dataValue.Should().Be("test-value");
    }
}