using Xunit;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Fixtures;
using CsPlaywrightXun.src.playwright.Core.Attributes;
using CsPlaywrightXun.src.playwright.Services.Data;
using CsPlaywrightXun.src.playwright.Pages.UI.example;
using CsPlaywrightXun.src.playwright.Flows.example;

namespace CsPlaywrightXun.src.docs.examples
{
    /// <summary>
    /// 完整的测试示例，展示框架的各种功能
    /// </summary>
    [UITest]
    [TestCategory(TestCategory.PageObject)]
    [TestPriority(TestPriority.High)]
    public class CompleteTestExample : IClassFixture<BrowserFixture>
    {
        private readonly BrowserFixture _fixture;
        private readonly ExamplePage _examplePage;
        private readonly SearchFlow _searchFlow;
        private readonly ILogger _logger;
        
        public CompleteTestExample(BrowserFixture fixture)
        {
            _fixture = fixture;
            _logger = _fixture.Logger;
            _examplePage = new ExamplePage(_fixture.Page, _logger);
            _searchFlow = new SearchFlow(_examplePage, _logger);
        }
        
        /// <summary>
        /// 数据驱动测试示例
        /// </summary>
        [Theory]
        [CsvData("src/config/date/UI/search_test_data.csv")]
        [TestTag("DataDriven")]
        public async Task SearchFunctionality_WithVariousData_ShouldWork(SearchTestData data)
        {
            // Arrange - 准备阶段
            _logger.LogInformation($"开始执行搜索测试：{data.TestName}");
            
            await _examplePage.NavigateAsync(data.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Act - 执行阶段
            var parameters = new Dictionary<string, object>
            {
                ["searchTerm"] = data.SearchTerm,
                ["expectedMinResults"] = data.ExpectedMinResults
            };
            
            await _searchFlow.ExecuteAsync(parameters);
            
            // Assert - 断言阶段
            var resultCount = await _examplePage.GetSearchResultCountAsync();
            
            Assert.True(resultCount >= data.ExpectedMinResults, 
                $"期望至少 {data.ExpectedMinResults} 个结果，实际得到 {resultCount} 个");
            
            // 使用框架内置的断言方法
            var assertResult = await _examplePage.AssertEqualAsync(
                resultCount >= data.ExpectedMinResults, 
                true
            );
            Assert.Equal("pass", assertResult);
            
            _logger.LogInformation($"搜索测试完成：{data.TestName}，结果数量：{resultCount}");
        }
        
        /// <summary>
        /// 冒烟测试示例
        /// </summary>
        [Fact]
        [SmokeTest]
        [FastTest]
        [TestTag("Critical")]
        public async Task HomePage_SmokeTest_ShouldLoadSuccessfully()
        {
            // Arrange
            var baseUrl = _fixture.Configuration.Environment.BaseUrl;
            
            // Act
            await _examplePage.NavigateAsync(baseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Assert
            var isLoaded = await _examplePage.IsLoadedAsync();
            Assert.True(isLoaded, "页面应该成功加载");
            
            var title = await _examplePage.GetTitleAsync();
            Assert.NotEmpty(title);
            
            _logger.LogInformation($"冒烟测试通过，页面标题：{title}");
        }
        
        /// <summary>
        /// 错误处理测试示例
        /// </summary>
        [Fact]
        [TestTag("ErrorHandling")]
        [TestPriority(TestPriority.Medium)]
        public async Task SearchWithInvalidInput_ShouldHandleGracefully()
        {
            // Arrange
            await _examplePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Act - 使用无效输入
            var invalidSearchTerms = new[] { "", "   ", "!@#$%^&*()", "很长很长很长的搜索词".PadRight(1000, '长') };
            
            foreach (var invalidTerm in invalidSearchTerms)
            {
                try
                {
                    await _examplePage.SearchAsync(invalidTerm);
                    
                    // 验证系统是否正确处理了无效输入
                    var hasErrorMessage = await _examplePage.HasErrorMessageAsync();
                    var hasResults = await _examplePage.GetSearchResultCountAsync() > 0;
                    
                    // 系统应该要么显示错误消息，要么显示空结果，但不应该崩溃
                    Assert.True(hasErrorMessage || !hasResults, 
                        $"无效输入 '{invalidTerm}' 应该被正确处理");
                    
                    _logger.LogInformation($"无效输入 '{invalidTerm}' 处理正确");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"处理无效输入 '{invalidTerm}' 时发生异常");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// 性能测试示例
        /// </summary>
        [Fact]
        [TestTag("Performance")]
        [SlowTest]
        public async Task SearchPerformance_ShouldMeetRequirements()
        {
            // Arrange
            await _examplePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            var searchTerm = "performance test";
            var maxResponseTimeMs = 5000; // 5秒最大响应时间
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            await _examplePage.SearchAsync(searchTerm);
            await _examplePage.WaitForSearchResultsAsync();
            
            stopwatch.Stop();
            
            // Assert
            var responseTime = stopwatch.ElapsedMilliseconds;
            
            Assert.True(responseTime <= maxResponseTimeMs, 
                $"搜索响应时间 {responseTime}ms 超过了最大限制 {maxResponseTimeMs}ms");
            
            _logger.LogInformation($"搜索性能测试通过，响应时间：{responseTime}ms");
        }
        
        /// <summary>
        /// 多步骤业务流程测试示例
        /// </summary>
        [Fact]
        [TestTag("BusinessFlow")]
        [TestPriority(TestPriority.High)]
        public async Task CompleteUserJourney_ShouldWorkEndToEnd()
        {
            // Arrange
            var testData = new
            {
                SearchTerm = "自动化测试",
                ExpectedResultCount = 5,
                FilterCategory = "技术文档"
            };
            
            // Act & Assert - 步骤1：导航到首页
            await _examplePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            var isHomePage = await _examplePage.IsLoadedAsync();
            Assert.True(isHomePage, "应该成功导航到首页");
            
            // Act & Assert - 步骤2：执行搜索
            await _examplePage.SearchAsync(testData.SearchTerm);
            await _examplePage.WaitForSearchResultsAsync();
            
            var initialResults = await _examplePage.GetSearchResultCountAsync();
            Assert.True(initialResults >= testData.ExpectedResultCount, 
                $"初始搜索结果应该至少有 {testData.ExpectedResultCount} 个");
            
            // Act & Assert - 步骤3：应用过滤器
            await _examplePage.ApplyFilterAsync(testData.FilterCategory);
            await _examplePage.WaitForSearchResultsAsync();
            
            var filteredResults = await _examplePage.GetSearchResultCountAsync();
            Assert.True(filteredResults <= initialResults, 
                "过滤后的结果数量应该小于或等于初始结果");
            
            // Act & Assert - 步骤4：验证结果质量
            var results = await _examplePage.GetSearchResultsAsync();
            Assert.All(results, result => 
            {
                Assert.NotEmpty(result.Title);
                Assert.NotEmpty(result.Description);
                Assert.Contains(testData.SearchTerm, result.Title + " " + result.Description, 
                    StringComparison.OrdinalIgnoreCase);
            });
            
            _logger.LogInformation($"完整用户旅程测试通过，最终结果数量：{filteredResults}");
        }
        
        /// <summary>
        /// 响应式设计测试示例
        /// </summary>
        [Theory]
        [InlineData(1920, 1080, "Desktop")]
        [InlineData(768, 1024, "Tablet")]
        [InlineData(375, 667, "Mobile")]
        [TestTag("Responsive")]
        public async Task ResponsiveDesign_ShouldWorkOnDifferentViewports(int width, int height, string deviceType)
        {
            // Arrange
            await _fixture.Page.SetViewportSizeAsync(width, height);
            await _examplePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Act & Assert
            var isLoaded = await _examplePage.IsLoadedAsync();
            Assert.True(isLoaded, $"页面应该在 {deviceType} 视口下正常加载");
            
            // 检查关键元素是否可见
            var isSearchBoxVisible = await _examplePage.IsSearchBoxVisibleAsync();
            Assert.True(isSearchBoxVisible, $"搜索框应该在 {deviceType} 视口下可见");
            
            // 执行基本功能测试
            await _examplePage.SearchAsync("responsive test");
            var hasResults = await _examplePage.GetSearchResultCountAsync() > 0;
            Assert.True(hasResults, $"搜索功能应该在 {deviceType} 视口下正常工作");
            
            _logger.LogInformation($"响应式设计测试通过：{deviceType} ({width}x{height})");
        }
        
        /// <summary>
        /// 可访问性测试示例
        /// </summary>
        [Fact]
        [TestTag("Accessibility")]
        [TestPriority(TestPriority.Medium)]
        public async Task Accessibility_ShouldMeetBasicRequirements()
        {
            // Arrange
            await _examplePage.NavigateAsync(_fixture.Configuration.Environment.BaseUrl);
            await _examplePage.WaitForLoadAsync();
            
            // Act & Assert - 检查页面标题
            var title = await _examplePage.GetTitleAsync();
            Assert.NotEmpty(title);
            Assert.True(title.Length <= 60, "页面标题长度应该适中");
            
            // Act & Assert - 检查主要标题
            var mainHeading = await _fixture.Page.QuerySelectorAsync("h1");
            Assert.NotNull(mainHeading);
            
            var headingText = await mainHeading.InnerTextAsync();
            Assert.NotEmpty(headingText);
            
            // Act & Assert - 检查表单标签
            var searchInput = await _fixture.Page.QuerySelectorAsync("input[type='search'], input[type='text']");
            if (searchInput != null)
            {
                var hasLabel = await _fixture.Page.QuerySelectorAsync("label[for]") != null ||
                              await searchInput.GetAttributeAsync("aria-label") != null ||
                              await searchInput.GetAttributeAsync("placeholder") != null;
                
                Assert.True(hasLabel, "搜索输入框应该有适当的标签或描述");
            }
            
            // Act & Assert - 检查键盘导航
            await _fixture.Page.PressAsync("body", "Tab");
            var focusedElement = await _fixture.Page.EvaluateAsync<string>("document.activeElement.tagName");
            Assert.NotEqual("BODY", focusedElement);
            
            _logger.LogInformation("基本可访问性检查通过");
        }
    }
    
    /// <summary>
    /// 搜索测试数据模型
    /// </summary>
    public class SearchTestData
    {
        public string TestName { get; set; }
        public string SearchTerm { get; set; }
        public int ExpectedMinResults { get; set; }
        public string BaseUrl { get; set; }
        public string Category { get; set; }
    }
    
    /// <summary>
    /// 搜索结果模型
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}