using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using EnterpriseAutomationFramework.Pages;
using EnterpriseAutomationFramework.Core.Fixtures;
using System.Threading.Tasks;

namespace EnterpriseAutomationFramework.Tests.Examples
{
    /// <summary>
    /// 百度首页测试示例
    /// 演示如何使用 BasePageObjectWithPlaywright 基类创建的页面对象
    /// </summary>
    [Trait("Type", "UI")]
    [Trait("Category", "Example")]
    public class BaiduHomePageTests : IClassFixture<BrowserFixture>
    {
        private readonly BrowserFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly BaiduHomePage _baiduHomePage;

        public BaiduHomePageTests(BrowserFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // 创建页面对象实例
            _baiduHomePage = new BaiduHomePage(_fixture.Page, _fixture.Logger);
        }

        /// <summary>
        /// 测试基本搜索功能
        /// </summary>
        [Fact]
        public async Task SearchFunctionality_ShouldReturnResults()
        {
            // Arrange
            var searchQuery = "Playwright 自动化测试";
            
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            await _baiduHomePage.SearchAsync(searchQuery);
            
            // Assert
            var resultCount = await _baiduHomePage.GetSearchResultCountAsync();
            Assert.True(resultCount > 0, "搜索结果应该大于0");
            
            // 验证页面标题
            var titleValidation = await _baiduHomePage.ValidatePageTitleAsync();
            Assert.Equal("pass", titleValidation);
            
            _output.WriteLine($"搜索 '{searchQuery}' 返回了 {resultCount} 个结果");
        }

        /// <summary>
        /// 测试使用回车键搜索
        /// </summary>
        [Fact]
        public async Task SearchWithEnter_ShouldReturnResults()
        {
            // Arrange
            var searchQuery = "C# Playwright";
            
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            await _baiduHomePage.SearchWithEnterAsync(searchQuery);
            
            // Assert
            var resultCount = await _baiduHomePage.GetSearchResultCountAsync();
            Assert.True(resultCount > 0, "使用回车键搜索应该返回结果");
            
            _output.WriteLine($"使用回车键搜索 '{searchQuery}' 返回了 {resultCount} 个结果");
        }

        /// <summary>
        /// 测试清除并重新搜索功能
        /// </summary>
        [Fact]
        public async Task ClearAndSearch_ShouldReturnNewResults()
        {
            // Arrange
            var firstQuery = "第一次搜索";
            var secondQuery = "第二次搜索";
            
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            // 第一次搜索
            await _baiduHomePage.SearchAsync(firstQuery);
            var firstResultCount = await _baiduHomePage.GetSearchResultCountAsync();
            
            // 清除并进行第二次搜索
            await _baiduHomePage.ClearAndSearchAsync(secondQuery);
            var secondResultCount = await _baiduHomePage.GetSearchResultCountAsync();
            
            // Assert
            Assert.True(firstResultCount > 0, "第一次搜索应该有结果");
            Assert.True(secondResultCount > 0, "第二次搜索应该有结果");
            
            _output.WriteLine($"第一次搜索结果: {firstResultCount}, 第二次搜索结果: {secondResultCount}");
        }

        /// <summary>
        /// 测试页面元素可见性
        /// </summary>
        [Fact]
        public async Task PageElements_ShouldBeVisible()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            // Assert
            var isSearchBoxVisible = await _baiduHomePage.IsSearchBoxVisibleAsync();
            Assert.True(isSearchBoxVisible, "搜索框应该可见");
            
            var isPageLoaded = await _baiduHomePage.IsLoadedAsync();
            Assert.True(isPageLoaded, "页面应该完全加载");
            
            _output.WriteLine("页面元素可见性验证通过");
        }

        /// <summary>
        /// 测试获取元素属性
        /// </summary>
        [Fact]
        public async Task GetElementAttribute_ShouldReturnValue()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            var placeholder = await _baiduHomePage.GetSearchBoxPlaceholderAsync();
            
            // Assert
            Assert.NotNull(placeholder);
            Assert.NotEmpty(placeholder);
            
            _output.WriteLine($"搜索框 placeholder: {placeholder}");
        }

        /// <summary>
        /// 测试鼠标悬停操作
        /// </summary>
        [Fact]
        public async Task HoverOperation_ShouldWork()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            // 悬停到Logo上（这个操作不会抛出异常就算成功）
            await _baiduHomePage.HoverOnLogoAsync();
            
            // Assert
            // 悬停操作通常不会有明显的断言，这里我们验证操作没有抛出异常
            Assert.True(true, "悬停操作应该成功执行");
            
            _output.WriteLine("鼠标悬停操作执行成功");
        }

        /// <summary>
        /// 测试JavaScript操作
        /// </summary>
        [Fact]
        public async Task JavaScriptOperations_ShouldWork()
        {
            // Arrange
            var searchQuery = "JavaScript 测试";
            
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            // 输入搜索关键词
            await _baiduHomePage.TypeAsync("#kw", searchQuery);
            
            // 使用JavaScript点击搜索按钮
            await _baiduHomePage.ClickSearchButtonByJavaScriptAsync();
            
            // 等待结果
            await Task.Delay(2000);
            
            // 滚动到底部
            await _baiduHomePage.ScrollToBottomAsync();
            
            // Assert
            var resultCount = await _baiduHomePage.GetSearchResultCountAsync();
            Assert.True(resultCount > 0, "JavaScript操作应该能正常搜索");
            
            _output.WriteLine($"JavaScript操作搜索返回了 {resultCount} 个结果");
        }

        /// <summary>
        /// 测试截图功能
        /// </summary>
        [Fact]
        public async Task TakeScreenshot_ShouldCreateFile()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            var screenshotPath = await _baiduHomePage.TakeScreenshotAsync("baidu_homepage_test.png");
            
            // Assert
            Assert.NotNull(screenshotPath);
            Assert.NotEmpty(screenshotPath);
            
            _output.WriteLine($"截图已保存到: {screenshotPath}");
        }

        /// <summary>
        /// 测试复合操作：搜索并验证结果
        /// </summary>
        [Theory]
        [InlineData("Playwright", 5)]
        [InlineData("自动化测试", 3)]
        [InlineData("C# 编程", 4)]
        public async Task SearchAndValidate_ShouldReturnExpectedResults(string searchQuery, int expectedMinResults)
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            var success = await _baiduHomePage.SearchAndValidateResultsAsync(searchQuery, expectedMinResults);
            
            // Assert
            Assert.True(success, $"搜索 '{searchQuery}' 应该返回至少 {expectedMinResults} 个结果");
            
            _output.WriteLine($"搜索 '{searchQuery}' 验证成功");
        }

        /// <summary>
        /// 测试断言功能
        /// </summary>
        [Fact]
        public async Task AssertionMethods_ShouldWork()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            var title = await _baiduHomePage.GetTitleAsync();
            
            // 测试相等断言
            var equalResult = await _baiduHomePage.AssertEqualAsync(title.Contains("百度"), true);
            Assert.Equal("pass", equalResult);
            
            // 测试不相等断言
            var notEqualResult = await _baiduHomePage.AssertNotEqualAsync(title, "");
            Assert.Equal("pass", notEqualResult);
            
            // 测试标题包含断言
            var titleContainsResult = await _baiduHomePage.IsTitleContainsAsync("百度");
            Assert.Equal("pass", titleContainsResult);
            
            _output.WriteLine("所有断言测试通过");
        }

        /// <summary>
        /// 测试统计功能
        /// </summary>
        [Fact]
        public async Task StatisticsMethods_ShouldTrackCounts()
        {
            // Act
            await _baiduHomePage.NavigateToBaiduAsync();
            await _baiduHomePage.WaitForLoadAsync();
            
            // 重置计数
            _baiduHomePage.ResetCounts();
            
            // 执行一些断言操作
            await _baiduHomePage.AssertEqualAsync(1, 1); // 应该通过
            await _baiduHomePage.AssertEqualAsync(1, 2); // 应该失败
            await _baiduHomePage.AssertEqualAsync(2, 2); // 应该通过
            
            // Assert
            var passCount = _baiduHomePage.GetPassCount();
            var failCount = _baiduHomePage.GetFailCount();
            
            Assert.Equal(2, passCount);
            Assert.Equal(1, failCount);
            
            _output.WriteLine($"通过: {passCount}, 失败: {failCount}");
        }
    }
}