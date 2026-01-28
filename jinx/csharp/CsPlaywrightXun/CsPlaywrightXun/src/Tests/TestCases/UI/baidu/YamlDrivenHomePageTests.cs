using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Pages.UI.baidu;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Flows.UI.baidu;

namespace CsPlaywrightXun.src.playwright.Tests.UI.baidu;

/// <summary>
/// YAML驱动的首页UI测试类
/// 演示使用YAML配置文件进行元素管理的测试
/// 验证混合定位器管理策略
/// 需求: 3.1, 3.2, 3.4, 3.5
/// </summary>
[Trait("Type", "UI")]
[Trait("Category", "YamlDriven")]
[Trait("Priority", "Medium")]
public class YamlDrivenHomePageTests : IClassFixture<BrowserFixture>, IAsyncLifetime
{
    private readonly BrowserFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    private IBrowserContext? _isolatedContext;
    private IPage? _isolatedPage;
    private HomePage? _homePage;
    private SearchFlow? _searchFlow;
    private const string YamlFilePath = "CsPlaywrightXun/src/config/elements/comprehensive_elements.yaml";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fixture">浏览器固件</param>
    /// <param name="output">测试输出助手</param>
    public YamlDrivenHomePageTests(BrowserFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<YamlDrivenHomePageTests>();
    }

    /// <summary>
    /// 测试初始化
    /// </summary>
    public async Task InitializeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在初始化YAML驱动测试环境...");
        
        await _fixture.InitializeAsync();
        (_isolatedContext, _isolatedPage) = await _fixture.GetIsolatedBrowserAsync();
        
        _homePage = new HomePage(_isolatedPage, _logger);
        _searchFlow = new SearchFlow(_fixture, _logger);
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] YAML驱动测试环境初始化完成");
    }

    /// <summary>
    /// 测试清理
    /// </summary>
    public async Task DisposeAsync()
    {
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 正在清理YAML驱动测试环境...");
        
        if (_isolatedContext != null)
        {
            await _isolatedContext.CloseAsync();
        }
        
        _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] YAML驱动测试环境清理完成");
    }

    #region YAML元素加载测试

    /// <summary>
    /// 测试YAML元素配置加载
    /// </summary>
    [Fact]
    [Trait("TestType", "Configuration")]
    public async Task YamlElements_LoadConfiguration_ShouldSucceed()
    {
        _output.WriteLine("开始测试YAML元素配置加载");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 加载YAML元素配置
        await _homePage.LoadElementsAsync(YamlFilePath);
        
        // Assert - 验证元素是否正确加载
        Assert.True(_homePage.HasYamlElement("SearchBox"), "应该加载SearchBox元素");
        Assert.True(_homePage.HasYamlElement("SearchButton"), "应该加载SearchButton元素");
        Assert.True(_homePage.HasYamlElement("SearchResults"), "应该加载SearchResults元素");
        
        // 验证元素属性
        var searchBoxElement = _homePage.GetYamlElement("SearchBox");
        Assert.Equal("#kw", searchBoxElement.Selector);
        Assert.Equal("Input", searchBoxElement.Type.ToString());
        Assert.Equal(5000, searchBoxElement.TimeoutMs);
        
        _output.WriteLine("YAML元素配置加载测试通过");
    }

    /// <summary>
    /// 测试使用YAML元素进行页面操作
    /// </summary>
    [Fact]
    [Trait("TestType", "Functional")]
    public async Task YamlElements_PageOperations_ShouldWork()
    {
        _output.WriteLine("开始测试使用YAML元素进行页面操作");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        const string searchQuery = "YAML驱动测试";
        
        // Act - 使用YAML配置执行搜索
        await _homePage.SearchWithYamlAsync(searchQuery, YamlFilePath);
        
        // 等待页面跳转
        await Task.Delay(2000);
        
        // Assert - 验证搜索操作结果
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("wd="), "应该导航到搜索结果页面");
        Assert.True(currentUrl.Contains("YAML驱动测试") || currentUrl.Contains(Uri.EscapeDataString("YAML驱动测试")), 
            "URL应该包含搜索关键词");
        
        _output.WriteLine($"YAML元素页面操作测试通过 - 搜索URL: {currentUrl}");
    }

    #endregion

    #region YAML数据驱动测试

    /// <summary>
    /// 使用YAML配置的数据驱动搜索测试
    /// </summary>
    /// <param name="testData">测试数据</param>
    [Theory]
    [CsvData("CsPlaywrightXun/src/config/date/UI/comprehensive_test_data.csv")]
    [Trait("TestType", "DataDriven")]
    [Trait("ElementSource", "YAML")]
    public async Task YamlDrivenSearch_WithCsvData_ShouldReturnResults(Dictionary<string, object> testData)
    {
        // 跳过禁用的测试
        if (testData.ContainsKey("IsEnabled") && !Convert.ToBoolean(testData["IsEnabled"]))
        {
            _output.WriteLine($"跳过禁用的YAML驱动测试: {testData["TestName"]}");
            return;
        }
        
        var testName = testData["TestName"].ToString()!;
        var searchQuery = testData["SearchQuery"].ToString()!;
        var expectedResultCount = Convert.ToInt32(testData["ExpectedResultCount"]);
        
        _output.WriteLine($"开始执行YAML驱动数据测试: {testName}");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act - 使用YAML配置执行搜索
        await _homePage.SearchWithYamlAsync(searchQuery, YamlFilePath);
        await _homePage.WaitForSearchResultsAsync();
        
        // Assert - 验证搜索结果
        var actualResultCount = await _homePage.GetSearchResultCountWithYamlAsync(YamlFilePath);
        Assert.True(actualResultCount >= expectedResultCount, 
            $"YAML驱动搜索结果数量不足，期望至少 {expectedResultCount} 个，实际 {actualResultCount} 个");
        
        // Assert - 验证页面状态
        var currentUrl = _isolatedPage!.Url;
        Assert.True(currentUrl.Contains("wd="), "应该导航到搜索结果页面");
        
        _output.WriteLine($"YAML驱动数据测试通过: {testName} - 搜索词: {searchQuery}, 结果数: {actualResultCount}");
    }

    #endregion

    #region SearchFlow与YAML集成测试

    /// <summary>
    /// 测试SearchFlow与YAML配置的集成
    /// </summary>
    [Fact]
    [Trait("TestType", "Integration")]
    [Trait("Component", "Flow")]
    public async Task SearchFlow_WithYamlConfiguration_ShouldExecuteSuccessfully()
    {
        _output.WriteLine("开始测试SearchFlow与YAML配置的集成");
        
        const string searchQuery = "SearchFlow YAML集成测试";
        
        // Act - 使用SearchFlow执行YAML配置的搜索
        await _searchFlow!.ExecuteYamlSearchAsync(searchQuery, YamlFilePath, true);
        
        // Assert - 验证流程执行结果
        var actualResultCount = await _homePage!.GetSearchResultCountWithYamlAsync(YamlFilePath);
        Assert.True(actualResultCount > 0, "SearchFlow YAML集成应该返回搜索结果");
        
        // Assert - 验证页面状态
        var currentUrl = _fixture.Page.Url;
        Assert.True(currentUrl.Contains("wd="), "SearchFlow应该导航到搜索结果页面");
        
        _output.WriteLine($"SearchFlow YAML集成测试通过 - 搜索词: {searchQuery}, 结果数: {actualResultCount}");
    }

    #endregion

    #region 混合定位器策略测试

    /// <summary>
    /// 测试混合定位器管理策略
    /// 验证代码中定义的定位器和YAML定位器的协同工作
    /// </summary>
    [Fact]
    [Trait("TestType", "Strategy")]
    [Trait("Category", "LocatorManagement")]
    public async Task HybridLocatorStrategy_CodeAndYaml_ShouldWorkTogether()
    {
        _output.WriteLine("开始测试混合定位器管理策略");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        const string searchQuery = "混合定位器测试";
        
        // Act & Assert - 使用代码中定义的定位器
        _output.WriteLine("使用代码中定义的定位器执行搜索");
        await _homePage.SearchAsync(searchQuery);
        await Task.Delay(2000);
        
        var codeBasedUrl = _isolatedPage!.Url;
        Assert.True(codeBasedUrl.Contains("wd="), "代码定位器应该正常工作");
        
        // 返回首页
        await _homePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        // Act & Assert - 使用YAML定义的定位器
        _output.WriteLine("使用YAML定义的定位器执行搜索");
        await _homePage.SearchWithYamlAsync(searchQuery + "_YAML", YamlFilePath);
        await Task.Delay(2000);
        
        var yamlBasedUrl = _isolatedPage.Url;
        Assert.True(yamlBasedUrl.Contains("wd="), "YAML定位器应该正常工作");
        Assert.True(yamlBasedUrl.Contains("YAML"), "YAML定位器搜索应该包含YAML标识");
        
        // Assert - 验证两种方式都能正常工作
        Assert.NotEqual(codeBasedUrl, yamlBasedUrl);
        
        _output.WriteLine($"混合定位器策略测试通过 - 代码URL: {codeBasedUrl}, YAML URL: {yamlBasedUrl}");
    }

    /// <summary>
    /// 测试YAML元素的类型安全访问
    /// </summary>
    [Fact]
    [Trait("TestType", "TypeSafety")]
    public async Task YamlElements_TypeSafeAccess_ShouldProvideCorrectTypes()
    {
        _output.WriteLine("开始测试YAML元素的类型安全访问");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.LoadElementsAsync(YamlFilePath);
        
        // Act & Assert - 验证不同类型的元素
        var searchBoxElement = _homePage.GetYamlElement("SearchBox");
        Assert.Equal("Input", searchBoxElement.Type.ToString());
        Assert.Equal("#kw", searchBoxElement.Selector);
        
        var searchButtonElement = _homePage.GetYamlElement("SearchButton");
        Assert.Equal("Button", searchButtonElement.Type.ToString());
        Assert.Equal("#su", searchButtonElement.Selector);
        
        var searchResultsElement = _homePage.GetYamlElement("SearchResults");
        Assert.Equal("Text", searchResultsElement.Type.ToString());
        Assert.Equal(".result", searchResultsElement.Selector);
        
        // 验证元素属性
        Assert.True(searchBoxElement.Attributes.ContainsKey("placeholder"));
        Assert.True(searchButtonElement.Attributes.ContainsKey("value"));
        Assert.True(searchResultsElement.Attributes.ContainsKey("class"));
        
        _output.WriteLine("YAML元素类型安全访问测试通过");
    }

    #endregion

    #region 错误处理测试

    /// <summary>
    /// 测试YAML文件不存在时的错误处理
    /// </summary>
    [Fact]
    [Trait("TestType", "ErrorHandling")]
    public async Task YamlElements_FileNotFound_ShouldHandleGracefully()
    {
        _output.WriteLine("开始测试YAML文件不存在时的错误处理");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        
        const string nonExistentYamlPath = "non_existent_file.yaml";
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _homePage.LoadElementsAsync(nonExistentYamlPath);
        });
        
        Assert.Contains("non_existent_file.yaml", exception.Message);
        
        _output.WriteLine("YAML文件不存在错误处理测试通过");
    }

    /// <summary>
    /// 测试访问不存在的YAML元素时的错误处理
    /// </summary>
    [Fact]
    [Trait("TestType", "ErrorHandling")]
    public async Task YamlElements_ElementNotFound_ShouldHandleGracefully()
    {
        _output.WriteLine("开始测试访问不存在的YAML元素时的错误处理");
        
        // Arrange
        await _homePage!.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
        await _homePage.WaitForLoadAsync();
        await _homePage.LoadElementsAsync(YamlFilePath);
        
        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() =>
        {
            _homePage.GetYamlElement("NonExistentElement");
        });
        
        Assert.Contains("NonExistentElement", exception.Message);
        
        _output.WriteLine("访问不存在YAML元素错误处理测试通过");
    }

    #endregion
}