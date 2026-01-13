using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Services.Data;
using System.Threading.Tasks;

namespace EnterpriseAutomationFramework.Pages
{
    /// <summary>
    /// 百度首页页面对象示例
    /// 演示如何使用 BasePageObjectWithPlaywright 基类
    /// </summary>
    public class BaiduHomePage : BasePageObjectWithPlaywright
    {
        // 页面元素选择器
        private const string SearchBoxSelector = "#kw";
        private const string SearchButtonSelector = "#su";
        private const string ResultsSelector = ".result";
        private const string LogoSelector = "#lg";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="page">Playwright页面实例</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="elementReader">元素读取器</param>
        public BaiduHomePage(IPage page, ILogger logger, YamlElementReader elementReader = null) 
            : base(page, logger, elementReader)
        {
        }

        /// <summary>
        /// 导航到百度首页
        /// </summary>
        public async Task NavigateToBaiduAsync()
        {
            await NavigateAsync("https://www.baidu.com");
        }

        /// <summary>
        /// 执行搜索操作
        /// </summary>
        /// <param name="searchQuery">搜索关键词</param>
        public async Task SearchAsync(string searchQuery)
        {
            // 输入搜索关键词
            await TypeAsync(SearchBoxSelector, searchQuery);
            
            // 点击搜索按钮
            await ClickAsync(SearchButtonSelector);
            
            // 等待搜索结果加载
            await WaitForElementAsync(ResultsSelector);
        }

        /// <summary>
        /// 执行搜索并按回车
        /// </summary>
        /// <param name="searchQuery">搜索关键词</param>
        public async Task SearchWithEnterAsync(string searchQuery)
        {
            // 输入搜索关键词并按回车
            await TypeAndEnterAsync(SearchBoxSelector, searchQuery);
            
            // 等待搜索结果加载
            await WaitForElementAsync(ResultsSelector);
        }

        /// <summary>
        /// 清除搜索框并重新搜索
        /// </summary>
        /// <param name="searchQuery">搜索关键词</param>
        public async Task ClearAndSearchAsync(string searchQuery)
        {
            // 清除并输入新的搜索关键词
            await ClearAndTypeAsync(SearchBoxSelector, searchQuery);
            
            // 点击搜索按钮
            await ClickAsync(SearchButtonSelector);
            
            // 等待搜索结果加载
            await WaitForElementAsync(ResultsSelector);
        }

        /// <summary>
        /// 获取搜索结果数量
        /// </summary>
        /// <returns>搜索结果数量</returns>
        public async Task<int> GetSearchResultCountAsync()
        {
            var results = await FindElementsAsync(ResultsSelector);
            return results.Count;
        }

        /// <summary>
        /// 获取第一个搜索结果的标题
        /// </summary>
        /// <returns>第一个搜索结果标题</returns>
        public async Task<string> GetFirstResultTitleAsync()
        {
            var firstResultTitleSelector = ".result:first-child h3 a";
            return await GetTextAsync(firstResultTitleSelector);
        }

        /// <summary>
        /// 检查搜索框是否可见
        /// </summary>
        /// <returns>搜索框可见性</returns>
        public async Task<bool> IsSearchBoxVisibleAsync()
        {
            return await IsElementExistAsync(SearchBoxSelector);
        }

        /// <summary>
        /// 验证页面标题包含"百度"
        /// </summary>
        /// <returns>验证结果</returns>
        public async Task<string> ValidatePageTitleAsync()
        {
            return await IsTitleContainsAsync("百度");
        }

        /// <summary>
        /// 验证搜索结果包含指定关键词
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>验证结果</returns>
        public async Task<string> ValidateSearchResultsContainKeywordAsync(string keyword)
        {
            var firstResultTitle = await GetFirstResultTitleAsync();
            return await AssertEqualAsync(firstResultTitle.Contains(keyword), true);
        }

        /// <summary>
        /// 悬停到百度Logo
        /// </summary>
        public async Task HoverOnLogoAsync()
        {
            await HoverAsync(LogoSelector);
        }

        /// <summary>
        /// 通过JavaScript点击搜索按钮
        /// </summary>
        public async Task ClickSearchButtonByJavaScriptAsync()
        {
            await ClickByJavaScriptAsync(SearchButtonSelector);
        }

        /// <summary>
        /// 滚动到页面底部
        /// </summary>
        public async Task ScrollToBottomAsync()
        {
            await ScrollToAsync(0, 1000);
        }

        /// <summary>
        /// 获取搜索框的placeholder属性
        /// </summary>
        /// <returns>placeholder属性值</returns>
        public async Task<string> GetSearchBoxPlaceholderAsync()
        {
            return await GetAttributeAsync(SearchBoxSelector, "placeholder");
        }

        /// <summary>
        /// 检查页面是否已加载（实现抽象方法）
        /// </summary>
        /// <returns>页面加载状态</returns>
        public override async Task<bool> IsLoadedAsync()
        {
            try
            {
                // 检查关键元素是否存在
                return await IsElementExistAsync(SearchBoxSelector, 5000) && 
                       await IsElementExistAsync(SearchButtonSelector, 5000);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 等待页面加载完成（实现抽象方法）
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        public override async Task WaitForLoadAsync(int timeoutMs = 30000)
        {
            // 等待关键元素加载
            await WaitForElementAsync(SearchBoxSelector, timeoutMs);
            await WaitForElementAsync(SearchButtonSelector, timeoutMs);
            
            // 可以添加更多的加载完成条件
            // 例如等待页面完全加载
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = timeoutMs });
        }

        /// <summary>
        /// 执行复合操作示例：搜索并验证结果
        /// </summary>
        /// <param name="searchQuery">搜索关键词</param>
        /// <param name="expectedMinResults">期望的最少结果数量</param>
        /// <returns>操作结果</returns>
        public async Task<bool> SearchAndValidateResultsAsync(string searchQuery, int expectedMinResults = 1)
        {
            try
            {
                // 执行搜索
                await SearchAsync(searchQuery);
                
                // 等待结果加载
                await Task.Delay(2000);
                
                // 获取结果数量
                var resultCount = await GetSearchResultCountAsync();
                
                // 验证结果数量
                var validationResult = await AssertEqualAsync(resultCount >= expectedMinResults, true);
                
                // 截图记录
                await TakeScreenshotAsync($"search_results_{searchQuery}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                
                return validationResult == "pass";
            }
            catch (Exception ex)
            {
                _logger.LogError("搜索并验证结果失败: {Error}", ex.Message);
                return false;
            }
        }
    }
}