using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using CsPlaywrightXun.src.playwright.Core.Base;
using CsPlaywrightXun.src.playwright.Services.Data;

namespace CsPlaywrightXun.src.docs.examples
{
    /// <summary>
    /// 示例页面对象，展示 Page Object 模式的最佳实践
    /// </summary>
    public class ExamplePage : BasePageObjectWithPlaywright
    {
        // 页面元素选择器常量
        private const string SearchInputSelector = "#search-input";
        private const string SearchButtonSelector = "#search-button";
        private const string SearchResultsSelector = ".search-results";
        private const string SearchResultItemSelector = ".search-result-item";
        private const string FilterDropdownSelector = "#filter-dropdown";
        private const string LoadingIndicatorSelector = ".loading-spinner";
        private const string ErrorMessageSelector = ".error-message";
        private const string NoResultsMessageSelector = ".no-results";
        
        public ExamplePage(IPage page, ILogger logger, YamlElementReader elementReader = null) 
            : base(page, logger, elementReader)
        {
        }
        
        #region 基础页面操作
        
        /// <summary>
        /// 检查页面是否已加载
        /// </summary>
        public override async Task<bool> IsLoadedAsync()
        {
            try
            {
                return await IsElementExistAsync(SearchInputSelector) && 
                       await IsElementExistAsync(SearchButtonSelector);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"检查页面加载状态时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 等待页面加载完成
        /// </summary>
        public override async Task WaitForLoadAsync(int timeoutMs = 30000)
        {
            Logger.LogInformation("等待页面加载完成");
            
            try
            {
                await WaitForElementAsync(SearchInputSelector, timeoutMs);
                await WaitForElementAsync(SearchButtonSelector, timeoutMs);
                
                // 等待可能的加载指示器消失
                var hasLoadingIndicator = await IsElementExistAsync(LoadingIndicatorSelector, 1000);
                if (hasLoadingIndicator)
                {
                    await WaitForElementToDisappearAsync(LoadingIndicatorSelector, timeoutMs);
                }
                
                Logger.LogInformation("页面加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "页面加载超时");
                throw;
            }
        }
        
        #endregion
        
        #region 搜索功能
        
        /// <summary>
        /// 执行搜索操作
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        public async Task SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("搜索关键词不能为空", nameof(searchTerm));
            }
            
            Logger.LogInformation($"执行搜索: {searchTerm}");
            
            try
            {
                // 清除并输入搜索关键词
                await ClearAndTypeAsync(SearchInputSelector, searchTerm);
                
                // 点击搜索按钮
                await ClickAsync(SearchButtonSelector);
                
                Logger.LogInformation("搜索操作完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"搜索操作失败: {searchTerm}");
                throw;
            }
        }
        
        /// <summary>
        /// 等待搜索结果加载
        /// </summary>
        /// <param name="timeoutMs">超时时间</param>
        public async Task WaitForSearchResultsAsync(int timeoutMs = 10000)
        {
            Logger.LogInformation("等待搜索结果加载");
            
            try
            {
                // 等待加载指示器消失
                var hasLoadingIndicator = await IsElementExistAsync(LoadingIndicatorSelector, 1000);
                if (hasLoadingIndicator)
                {
                    await WaitForElementToDisappearAsync(LoadingIndicatorSelector, timeoutMs);
                }
                
                // 等待结果容器出现
                await WaitForElementAsync(SearchResultsSelector, timeoutMs);
                
                Logger.LogInformation("搜索结果加载完成");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"等待搜索结果超时: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 获取搜索结果数量
        /// </summary>
        /// <returns>结果数量</returns>
        public async Task<int> GetSearchResultCountAsync()
        {
            try
            {
                var elements = await _page.QuerySelectorAllAsync(SearchResultItemSelector);
                var count = elements.Count;
                
                Logger.LogInformation($"找到 {count} 个搜索结果");
                return count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取搜索结果数量失败");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取搜索结果列表
        /// </summary>
        /// <returns>搜索结果列表</returns>
        public async Task<List<SearchResult>> GetSearchResultsAsync()
        {
            var results = new List<SearchResult>();
            
            try
            {
                var resultElements = await _page.QuerySelectorAllAsync(SearchResultItemSelector);
                
                foreach (var element in resultElements)
                {
                    var title = await GetElementTextSafeAsync(element, ".result-title");
                    var description = await GetElementTextSafeAsync(element, ".result-description");
                    var url = await GetElementAttributeSafeAsync(element, ".result-link", "href");
                    
                    results.Add(new SearchResult
                    {
                        Title = title,
                        Description = description,
                        Url = url
                    });
                }
                
                Logger.LogInformation($"获取到 {results.Count} 个搜索结果详情");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取搜索结果详情失败");
            }
            
            return results;
        }
        
        /// <summary>
        /// 检查是否有搜索结果
        /// </summary>
        /// <returns>是否有结果</returns>
        public async Task<bool> HasSearchResultsAsync()
        {
            try
            {
                var hasResults = await IsElementExistAsync(SearchResultItemSelector, 2000);
                var hasNoResultsMessage = await IsElementExistAsync(NoResultsMessageSelector, 1000);
                
                return hasResults && !hasNoResultsMessage;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"检查搜索结果状态失败: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region 过滤功能
        
        /// <summary>
        /// 应用搜索过滤器
        /// </summary>
        /// <param name="filterValue">过滤器值</param>
        public async Task ApplyFilterAsync(string filterValue)
        {
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                throw new ArgumentException("过滤器值不能为空", nameof(filterValue));
            }
            
            Logger.LogInformation($"应用过滤器: {filterValue}");
            
            try
            {
                // 点击过滤器下拉框
                await ClickAsync(FilterDropdownSelector);
                
                // 等待下拉选项出现
                await Task.Delay(500);
                
                // 选择过滤器选项
                await ClickAsync($"text={filterValue}");
                
                // 等待过滤结果加载
                await WaitForSearchResultsAsync();
                
                Logger.LogInformation($"过滤器应用完成: {filterValue}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"应用过滤器失败: {filterValue}");
                throw;
            }
        }
        
        /// <summary>
        /// 清除所有过滤器
        /// </summary>
        public async Task ClearFiltersAsync()
        {
            Logger.LogInformation("清除所有过滤器");
            
            try
            {
                var clearButton = await _page.QuerySelectorAsync(".clear-filters");
                if (clearButton != null)
                {
                    await clearButton.ClickAsync();
                    await WaitForSearchResultsAsync();
                    Logger.LogInformation("过滤器已清除");
                }
                else
                {
                    Logger.LogInformation("未找到清除过滤器按钮");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "清除过滤器失败");
                throw;
            }
        }
        
        #endregion
        
        #region 错误处理
        
        /// <summary>
        /// 检查是否有错误消息
        /// </summary>
        /// <returns>是否有错误消息</returns>
        public async Task<bool> HasErrorMessageAsync()
        {
            try
            {
                return await IsElementExistAsync(ErrorMessageSelector, 2000);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"检查错误消息状态失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取错误消息文本
        /// </summary>
        /// <returns>错误消息</returns>
        public async Task<string> GetErrorMessageAsync()
        {
            try
            {
                var hasError = await HasErrorMessageAsync();
                if (hasError)
                {
                    return await GetTextAsync(ErrorMessageSelector);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取错误消息失败");
                return string.Empty;
            }
        }
        
        #endregion
        
        #region UI 状态检查
        
        /// <summary>
        /// 检查搜索框是否可见
        /// </summary>
        /// <returns>搜索框是否可见</returns>
        public async Task<bool> IsSearchBoxVisibleAsync()
        {
            try
            {
                var element = await _page.QuerySelectorAsync(SearchInputSelector);
                return element != null && await element.IsVisibleAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"检查搜索框可见性失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查页面是否正在加载
        /// </summary>
        /// <returns>是否正在加载</returns>
        public async Task<bool> IsLoadingAsync()
        {
            try
            {
                return await IsElementExistAsync(LoadingIndicatorSelector, 1000);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"检查加载状态失败: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 等待元素消失
        /// </summary>
        /// <param name="selector">元素选择器</param>
        /// <param name="timeoutMs">超时时间</param>
        private async Task WaitForElementToDisappearAsync(string selector, int timeoutMs = 10000)
        {
            var startTime = DateTime.Now;
            
            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                var exists = await IsElementExistAsync(selector, 100);
                if (!exists)
                {
                    return;
                }
                
                await Task.Delay(200);
            }
            
            Logger.LogWarning($"元素在 {timeoutMs}ms 内未消失: {selector}");
        }
        
        /// <summary>
        /// 安全获取元素文本
        /// </summary>
        /// <param name="parentElement">父元素</param>
        /// <param name="childSelector">子元素选择器</param>
        /// <returns>元素文本</returns>
        private async Task<string> GetElementTextSafeAsync(IElementHandle parentElement, string childSelector)
        {
            try
            {
                var childElement = await parentElement.QuerySelectorAsync(childSelector);
                return childElement != null ? await childElement.InnerTextAsync() : string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"获取元素文本失败 {childSelector}: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 安全获取元素属性
        /// </summary>
        /// <param name="parentElement">父元素</param>
        /// <param name="childSelector">子元素选择器</param>
        /// <param name="attributeName">属性名</param>
        /// <returns>属性值</returns>
        private async Task<string> GetElementAttributeSafeAsync(IElementHandle parentElement, string childSelector, string attributeName)
        {
            try
            {
                var childElement = await parentElement.QuerySelectorAsync(childSelector);
                return childElement != null ? await childElement.GetAttributeAsync(attributeName) : string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"获取元素属性失败 {childSelector}.{attributeName}: {ex.Message}");
                return string.Empty;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 搜索结果数据模型
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        
        public override string ToString()
        {
            return $"Title: {Title}, Description: {Description}, Url: {Url}";
        }
    }
}