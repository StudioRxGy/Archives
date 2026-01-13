using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Base;
using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Pages;

/// <summary>
/// 首页页面对象
/// </summary>
public class HomePage : BasePageObject
{
    /// <summary>
    /// 页面名称，用于从YAML文件中获取元素
    /// </summary>
    protected override string PageName => "HomePage";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="elementReader">元素读取器</param>
    public HomePage(IPage page, ILogger logger, YamlElementReader? elementReader = null)
        : base(page, logger, elementReader)
    {
    }

    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    /// <returns>页面加载状态</returns>
    public override async Task<bool> IsLoadedAsync()
    {
        try
        {
            // 检查搜索框是否可见作为页面加载的标志
            return await IsElementVisibleAsync("#kw");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查首页加载状态时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        try
        {
            _logger.LogInformation("等待首页加载完成");
            
            // 等待搜索框出现
            await WaitForElementAsync("#kw", timeoutMs);
            
            // 等待搜索按钮出现
            await WaitForElementAsync("#su", timeoutMs);
            
            _logger.LogInformation("首页加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "首页加载失败");
            throw;
        }
    }

    /// <summary>
    /// 执行搜索操作
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <exception cref="ArgumentException">搜索关键词为空时抛出</exception>
    public async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("搜索关键词不能为空", nameof(query));
        }

        try
        {
            _logger.LogInformation($"执行搜索操作，关键词：{query}");
            
            // 清空搜索框并输入关键词
            await FillAsync("#kw", query);
            
            // 点击搜索按钮
            await ClickAsync("#su");
            
            _logger.LogInformation("搜索操作执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"搜索操作失败，关键词：{query}");
            throw;
        }
    }

    /// <summary>
    /// 使用YAML配置执行搜索操作
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="yamlFilePath">YAML配置文件路径</param>
    /// <exception cref="ArgumentException">搜索关键词为空时抛出</exception>
    public async Task SearchWithYamlAsync(string query, string yamlFilePath)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("搜索关键词不能为空", nameof(query));
        }

        try
        {
            _logger.LogInformation($"使用YAML配置执行搜索操作，关键词：{query}");
            
            // 加载YAML元素配置
            await LoadElementsAsync(yamlFilePath);
            
            // 使用YAML定义的元素执行搜索
            await FillYamlElementAsync("SearchBox", query);
            await ClickYamlElementAsync("SearchButton");
            
            _logger.LogInformation("YAML配置搜索操作执行完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"YAML配置搜索操作失败，关键词：{query}");
            throw;
        }
    }

    /// <summary>
    /// 获取搜索结果数量
    /// </summary>
    /// <returns>搜索结果数量</returns>
    public async Task<int> GetSearchResultCountAsync()
    {
        try
        {
            _logger.LogInformation("获取搜索结果数量");
            
            // 等待搜索结果加载
            await WaitForElementAsync(".result", 10000);
            
            // 获取所有搜索结果元素
            var resultElements = await _page.QuerySelectorAllAsync(".result");
            var count = resultElements.Count;
            
            _logger.LogInformation($"搜索结果数量：{count}");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取搜索结果数量失败");
            return 0;
        }
    }

    /// <summary>
    /// 获取搜索结果列表
    /// </summary>
    /// <returns>搜索结果标题列表</returns>
    public async Task<List<string>> GetSearchResultsAsync()
    {
        try
        {
            _logger.LogInformation("获取搜索结果列表");
            
            // 等待搜索结果加载
            await WaitForElementAsync(".result", 10000);
            
            // 获取所有搜索结果标题
            var results = new List<string>();
            var resultElements = await _page.QuerySelectorAllAsync(".result h3 a");
            
            foreach (var element in resultElements)
            {
                var title = await element.TextContentAsync();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    results.Add(title.Trim());
                }
            }
            
            _logger.LogInformation($"获取到 {results.Count} 个搜索结果");
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取搜索结果列表失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 使用YAML配置获取搜索结果数量
    /// </summary>
    /// <param name="yamlFilePath">YAML配置文件路径</param>
    /// <returns>搜索结果数量</returns>
    public async Task<int> GetSearchResultCountWithYamlAsync(string yamlFilePath)
    {
        try
        {
            _logger.LogInformation("使用YAML配置获取搜索结果数量");
            
            // 确保YAML元素已加载
            if (!HasYamlElement("SearchResults"))
            {
                await LoadElementsAsync(yamlFilePath);
            }
            
            // 等待搜索结果元素出现
            await WaitForYamlElementAsync("SearchResults");
            
            // 获取搜索结果元素
            var searchResultsElement = GetYamlElement("SearchResults");
            var resultElements = await _page.QuerySelectorAllAsync(searchResultsElement.Selector);
            var count = resultElements.Count;
            
            _logger.LogInformation($"YAML配置搜索结果数量：{count}");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "使用YAML配置获取搜索结果数量失败");
            return 0;
        }
    }

    /// <summary>
    /// 检查搜索框是否可用
    /// </summary>
    /// <returns>搜索框是否可用</returns>
    public async Task<bool> IsSearchBoxAvailableAsync()
    {
        try
        {
            var isVisible = await IsElementVisibleAsync("#kw");
            if (!isVisible) return false;
            
            var isEnabled = await _page.IsEnabledAsync("#kw");
            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查搜索框可用性时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 检查搜索按钮是否可用
    /// </summary>
    /// <returns>搜索按钮是否可用</returns>
    public async Task<bool> IsSearchButtonAvailableAsync()
    {
        try
        {
            var isVisible = await IsElementVisibleAsync("#su");
            if (!isVisible) return false;
            
            var isEnabled = await _page.IsEnabledAsync("#su");
            return isEnabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查搜索按钮可用性时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取搜索框的占位符文本
    /// </summary>
    /// <returns>占位符文本</returns>
    public async Task<string> GetSearchBoxPlaceholderAsync()
    {
        try
        {
            var placeholder = await GetAttributeAsync("#kw", "placeholder");
            return placeholder ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取搜索框占位符文本失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取当前搜索框中的文本
    /// </summary>
    /// <returns>搜索框中的文本</returns>
    public async Task<string> GetSearchBoxValueAsync()
    {
        try
        {
            var value = await _page.InputValueAsync("#kw");
            return value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取搜索框文本失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 清空搜索框
    /// </summary>
    public async Task ClearSearchBoxAsync()
    {
        try
        {
            _logger.LogInformation("清空搜索框");
            await _page.FillAsync("#kw", "");
            _logger.LogDebug("搜索框已清空");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空搜索框失败");
            throw;
        }
    }

    /// <summary>
    /// 等待搜索结果页面加载
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public async Task WaitForSearchResultsAsync(int timeoutMs = 10000)
    {
        try
        {
            _logger.LogInformation("等待搜索结果页面加载");
            
            // 等待搜索结果元素出现
            await WaitForElementAsync(".result", timeoutMs);
            
            _logger.LogInformation("搜索结果页面加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "等待搜索结果页面加载失败");
            throw;
        }
    }
}