using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using FluentAssertions;
using EnterpriseAutomationFramework.Pages;
using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Core.Attributes;

namespace EnterpriseAutomationFramework.Tests.Pages;

/// <summary>
/// HomePage 单元测试
/// </summary>
[UnitTest]
[TestCategory(TestCategory.PageObject)]
[TestPriority(TestPriority.High)]
public class HomePageTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private ILogger? _logger;
    private HomePage? _homePage;

    public async Task InitializeAsync()
    {
        // 创建日志记录器
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<HomePageTests>();

        // 初始化 Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();

        // 创建 HomePage 实例
        _homePage = new HomePage(_page, _logger);
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
        var homePage = new HomePage(_page!, _logger!);

        // Assert
        homePage.Should().NotBeNull();
    }

    [Fact]
    public async Task IsLoadedAsync_ShouldReturnTrueWhenSearchBoxIsVisible()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='搜索'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isLoaded = await _homePage!.IsLoadedAsync();

        // Assert
        isLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task IsLoadedAsync_ShouldReturnFalseWhenSearchBoxIsNotVisible()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div>No search box here</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isLoaded = await _homePage!.IsLoadedAsync();

        // Assert
        isLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task WaitForLoadAsync_ShouldCompleteWhenBothElementsArePresent()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='搜索'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert - Should not throw
        await _homePage!.WaitForLoadAsync();
    }

    [Fact]
    public async Task SearchAsync_ShouldFillSearchBoxAndClickButton()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='Search' onclick='document.getElementById(\"result\").innerText=\"Search Complete\"'><div id='result'></div></body></html>";
        await _page!.GotoAsync(testUrl);
        var searchQuery = "test search";

        // Act
        await _homePage!.SearchAsync(searchQuery);

        // Assert
        var inputValue = await _page.InputValueAsync("#kw");
        inputValue.Should().Be(searchQuery);
        
        var resultText = await _page.TextContentAsync("#result");
        resultText.Should().Be("Search Complete");
    }

    [Fact]
    public async Task SearchAsync_ShouldThrowArgumentExceptionForEmptyQuery()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='搜索'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchAsync("   "));
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchAsync(null!));
    }

    [Fact]
    public async Task SearchWithYamlAsync_ShouldUseYamlElementsForSearch()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='Search' onclick='document.getElementById(\"result\").innerText=\"YAML Search Complete\"'><div id='result'></div></body></html>";
        await _page!.GotoAsync(testUrl);
        var searchQuery = "YAML test search";
        var yamlFilePath = Path.Combine("TestData", "valid_elements.yaml");

        // Act
        await _homePage!.SearchWithYamlAsync(searchQuery, yamlFilePath);

        // Assert
        var inputValue = await _page.InputValueAsync("#kw");
        inputValue.Should().Be(searchQuery);
        
        var resultText = await _page.TextContentAsync("#result");
        resultText.Should().Be("YAML Search Complete");
    }

    [Fact]
    public async Task SearchWithYamlAsync_ShouldThrowArgumentExceptionForEmptyQuery()
    {
        // Arrange
        var yamlFilePath = Path.Combine("TestData", "valid_elements.yaml");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchWithYamlAsync("", yamlFilePath));
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchWithYamlAsync("   ", yamlFilePath));
        await Assert.ThrowsAsync<ArgumentException>(() => _homePage!.SearchWithYamlAsync(null!, yamlFilePath));
    }

    [Fact]
    public async Task GetSearchResultCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var testUrl = @"data:text/html,<html><body>
            <div class='result'><h3><a>结果1</a></h3></div>
            <div class='result'><h3><a>结果2</a></h3></div>
            <div class='result'><h3><a>结果3</a></h3></div>
        </body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var count = await _homePage!.GetSearchResultCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetSearchResultCountAsync_ShouldReturnZeroWhenNoResults()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div>没有搜索结果</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var count = await _homePage!.GetSearchResultCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetSearchResultsAsync_ShouldReturnResultTitles()
    {
        // Arrange
        var testUrl = @"data:text/html,<html><body>
            <div class='result'><h3><a>First Result</a></h3></div>
            <div class='result'><h3><a>Second Result</a></h3></div>
            <div class='result'><h3><a>Third Result</a></h3></div>
        </body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var results = await _homePage!.GetSearchResultsAsync();

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain("First Result");
        results.Should().Contain("Second Result");
        results.Should().Contain("Third Result");
    }

    [Fact]
    public async Task GetSearchResultsAsync_ShouldReturnEmptyListWhenNoResults()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div>没有搜索结果</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var results = await _homePage!.GetSearchResultsAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSearchResultCountWithYamlAsync_ShouldUseYamlConfiguration()
    {
        // Arrange
        var testUrl = @"data:text/html,<html><body>
            <div class='result'><h3><a>YAML结果1</a></h3></div>
            <div class='result'><h3><a>YAML结果2</a></h3></div>
        </body></html>";
        await _page!.GotoAsync(testUrl);
        var yamlFilePath = Path.Combine("TestData", "valid_elements.yaml");

        // Act
        var count = await _homePage!.GetSearchResultCountWithYamlAsync(yamlFilePath);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task IsSearchBoxAvailableAsync_ShouldReturnTrueWhenSearchBoxIsVisibleAndEnabled()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isAvailable = await _homePage!.IsSearchBoxAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsSearchBoxAvailableAsync_ShouldReturnFalseWhenSearchBoxIsDisabled()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text' disabled></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isAvailable = await _homePage!.IsSearchBoxAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task IsSearchBoxAvailableAsync_ShouldReturnFalseWhenSearchBoxIsNotVisible()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><div>没有搜索框</div></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isAvailable = await _homePage!.IsSearchBoxAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task IsSearchButtonAvailableAsync_ShouldReturnTrueWhenSearchButtonIsVisibleAndEnabled()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='su' type='submit' value='搜索'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isAvailable = await _homePage!.IsSearchButtonAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsSearchButtonAvailableAsync_ShouldReturnFalseWhenSearchButtonIsDisabled()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='su' type='submit' value='搜索' disabled></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var isAvailable = await _homePage!.IsSearchButtonAvailableAsync();

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetSearchBoxPlaceholderAsync_ShouldReturnPlaceholderText()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text' placeholder='Enter search keywords'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var placeholder = await _homePage!.GetSearchBoxPlaceholderAsync();

        // Assert
        placeholder.Should().Be("Enter search keywords");
    }

    [Fact]
    public async Task GetSearchBoxPlaceholderAsync_ShouldReturnEmptyStringWhenNoPlaceholder()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var placeholder = await _homePage!.GetSearchBoxPlaceholderAsync();

        // Assert
        placeholder.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSearchBoxValueAsync_ShouldReturnCurrentValue()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text' value='initial value'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var value = await _homePage!.GetSearchBoxValueAsync();

        // Assert
        value.Should().Be("initial value");
    }

    [Fact]
    public async Task GetSearchBoxValueAsync_ShouldReturnEmptyStringWhenNoValue()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        var value = await _homePage!.GetSearchBoxValueAsync();

        // Assert
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearSearchBoxAsync_ShouldClearSearchBoxContent()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text' value='content to clear'></body></html>";
        await _page!.GotoAsync(testUrl);

        // Act
        await _homePage!.ClearSearchBoxAsync();

        // Assert
        var value = await _page.InputValueAsync("#kw");
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task WaitForSearchResultsAsync_ShouldCompleteWhenResultsArePresent()
    {
        // Arrange
        var testUrl = @"data:text/html,<html><body>
            <div class='result'><h3><a>Search Result</a></h3></div>
        </body></html>";
        
        await _page!.GotoAsync(testUrl);

        // Act & Assert - Should not throw
        await _homePage!.WaitForSearchResultsAsync(5000);
    }

    [Fact]
    public async Task NavigateAsync_ShouldNavigateToUrlAndWaitForLoad()
    {
        // Arrange
        var testUrl = "data:text/html,<html><body><input id='kw' type='text'><input id='su' type='submit' value='搜索'></body></html>";

        // Act
        await _homePage!.NavigateAsync(testUrl);

        // Assert
        var currentUrl = _page!.Url;
        currentUrl.Should().StartWith("data:text/html");
        
        var isLoaded = await _homePage.IsLoadedAsync();
        isLoaded.Should().BeTrue();
    }
}