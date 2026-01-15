using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Services.Data;
using EnterpriseAutomationFramework.Core.Exceptions;

namespace EnterpriseAutomationFramework.Core.Base;

/// <summary>
/// 页面对象基类
/// </summary>
public abstract class BasePageObject : IPageObject
{
    protected readonly IPage _page;
    protected readonly ILogger _logger;
    protected readonly YamlElementReader _elementReader;
    protected PageElementCollection? _elements;

    /// <summary>
    /// 页面名称，用于从YAML文件中获取元素
    /// </summary>
    protected abstract string PageName { get; }

    protected BasePageObject(IPage page, ILogger logger, YamlElementReader? elementReader = null)
    {
        _page = page;
        _logger = logger;
        _elementReader = elementReader ?? new YamlElementReader(logger as ILogger<YamlElementReader>);
    }

    /// <summary>
    /// 导航到指定URL
    /// </summary>
    public virtual async Task NavigateAsync(string url)
    {
        _logger.LogInformation($"导航到页面: {url}");
        await _page.GotoAsync(url);
        await WaitForLoadAsync();
    }

    /// <summary>
    /// 检查页面是否已加载
    /// </summary>
    public abstract Task<bool> IsLoadedAsync();

    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    public virtual async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        _logger.LogInformation("等待页面加载完成");
        
        try
        {
            // 等待网络空闲状态
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = timeoutMs });
            
            // 验证页面是否真正加载完成
            var isLoaded = await IsLoadedAsync();
            if (!isLoaded)
            {
                throw new TimeoutException($"页面在 {timeoutMs}ms 内未能完全加载");
            }
            
            _logger.LogInformation("页面加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "页面加载失败");
            throw;
        }
    }

    /// <summary>
    /// 加载页面元素配置
    /// </summary>
    /// <param name="yamlFilePath">YAML文件路径</param>
    protected virtual async Task LoadElementsAsync(string yamlFilePath)
    {
        try
        {
            _logger.LogInformation($"加载页面元素配置: {yamlFilePath}");
            _elements = _elementReader.LoadElements(yamlFilePath);
            _logger.LogInformation($"成功加载 {_elements.Count} 个页面元素");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"加载页面元素配置失败: {yamlFilePath}");
            throw;
        }
    }

    /// <summary>
    /// 获取YAML定义的页面元素
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <returns>页面元素</returns>
    /// <exception cref="InvalidOperationException">元素集合未加载时抛出</exception>
    /// <exception cref="KeyNotFoundException">元素不存在时抛出</exception>
    protected virtual PageElement GetYamlElement(string elementName)
    {
        if (_elements == null)
        {
            throw new InvalidOperationException("页面元素集合未加载，请先调用 LoadElementsAsync 方法");
        }

        try
        {
            return _elements.GetElement(PageName, elementName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError($"页面元素不存在: {PageName}.{elementName}");
            throw new ElementNotFoundException(GetType().Name, $"{PageName}.{elementName}", ex.Message);
        }
    }

    /// <summary>
    /// 检查YAML元素是否存在
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <returns>元素是否存在</returns>
    protected virtual bool HasYamlElement(string elementName)
    {
        return _elements?.ContainsElement(PageName, elementName) ?? false;
    }

    /// <summary>
    /// 等待元素可见
    /// </summary>
    protected async Task WaitForElementAsync(string selector, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogDebug($"等待元素可见: {selector}");
            await _page.WaitForSelectorAsync(selector, new() { Timeout = timeoutMs });
            _logger.LogDebug($"元素已可见: {selector}");
        }
        catch (TimeoutException)
        {
            _logger.LogError($"等待元素超时: {selector}");
            throw new ElementNotFoundException(GetType().Name, selector, $"元素在 {timeoutMs}ms 内未出现: {selector}");
        }
    }

    /// <summary>
    /// 等待YAML定义的元素可见
    /// </summary>
    /// <param name="elementName">元素名称</param>
    protected async Task WaitForYamlElementAsync(string elementName)
    {
        var element = GetYamlElement(elementName);
        await WaitForElementAsync(element.Selector, element.TimeoutMs);
    }

    /// <summary>
    /// 检查元素是否可见
    /// </summary>
    /// <param name="selector">元素选择器</param>
    /// <returns>元素是否可见</returns>
    protected async Task<bool> IsElementVisibleAsync(string selector)
    {
        try
        {
            return await _page.IsVisibleAsync(selector);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"检查元素可见性时发生异常: {selector}");
            return false;
        }
    }

    /// <summary>
    /// 检查YAML定义的元素是否可见
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <returns>元素是否可见</returns>
    protected async Task<bool> IsYamlElementVisibleAsync(string elementName)
    {
        var element = GetYamlElement(elementName);
        return await IsElementVisibleAsync(element.Selector);
    }

    /// <summary>
    /// 点击元素
    /// </summary>
    protected async Task ClickAsync(string selector)
    {
        try
        {
            _logger.LogInformation($"点击元素: {selector}");
            await WaitForElementAsync(selector);
            await _page.ClickAsync(selector);
            _logger.LogDebug($"成功点击元素: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"点击元素失败: {selector}");
            throw;
        }
    }

    /// <summary>
    /// 点击YAML定义的元素
    /// </summary>
    /// <param name="elementName">元素名称</param>
    protected async Task ClickYamlElementAsync(string elementName)
    {
        var element = GetYamlElement(elementName);
        await ClickAsync(element.Selector);
    }

    /// <summary>
    /// 输入文本
    /// </summary>
    protected async Task FillAsync(string selector, string text)
    {
        try
        {
            _logger.LogInformation($"在 {selector} 中输入文本");
            await WaitForElementAsync(selector);
            await _page.FillAsync(selector, text);
            _logger.LogDebug($"成功输入文本到元素: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"输入文本失败: {selector}");
            throw;
        }
    }

    /// <summary>
    /// 在YAML定义的元素中输入文本
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="text">输入文本</param>
    protected async Task FillYamlElementAsync(string elementName, string text)
    {
        var element = GetYamlElement(elementName);
        await FillAsync(element.Selector, text);
    }

    /// <summary>
    /// 获取元素文本
    /// </summary>
    protected async Task<string> GetTextAsync(string selector)
    {
        try
        {
            await WaitForElementAsync(selector);
            var text = await _page.TextContentAsync(selector) ?? string.Empty;
            _logger.LogDebug($"获取元素文本: {selector} = '{text}'");
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取元素文本失败: {selector}");
            throw;
        }
    }

    /// <summary>
    /// 获取YAML定义元素的文本
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <returns>元素文本</returns>
    protected async Task<string> GetYamlElementTextAsync(string elementName)
    {
        var element = GetYamlElement(elementName);
        return await GetTextAsync(element.Selector);
    }

    /// <summary>
    /// 获取元素属性值
    /// </summary>
    /// <param name="selector">元素选择器</param>
    /// <param name="attributeName">属性名称</param>
    /// <returns>属性值</returns>
    protected async Task<string?> GetAttributeAsync(string selector, string attributeName)
    {
        try
        {
            await WaitForElementAsync(selector);
            var value = await _page.GetAttributeAsync(selector, attributeName);
            _logger.LogDebug($"获取元素属性: {selector}.{attributeName} = '{value}'");
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取元素属性失败: {selector}.{attributeName}");
            throw;
        }
    }

    /// <summary>
    /// 获取YAML定义元素的属性值
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="attributeName">属性名称</param>
    /// <returns>属性值</returns>
    protected async Task<string?> GetYamlElementAttributeAsync(string elementName, string attributeName)
    {
        var element = GetYamlElement(elementName);
        return await GetAttributeAsync(element.Selector, attributeName);
    }

    /// <summary>
    /// 等待页面标题包含指定文本
    /// </summary>
    /// <param name="expectedTitle">期望的标题文本</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    protected async Task WaitForTitleContainsAsync(string expectedTitle, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogDebug($"等待页面标题包含: {expectedTitle}");
            await _page.WaitForFunctionAsync($"document.title.includes('{expectedTitle}')", 
                new PageWaitForFunctionOptions { Timeout = timeoutMs });
            _logger.LogDebug($"页面标题验证成功: {expectedTitle}");
        }
        catch (TimeoutException)
        {
            var currentTitle = await _page.TitleAsync();
            _logger.LogError($"等待页面标题超时，当前标题: '{currentTitle}', 期望包含: '{expectedTitle}'");
            throw new TimeoutException($"页面标题在 {timeoutMs}ms 内未包含期望文本 '{expectedTitle}'，当前标题: '{currentTitle}'");
        }
    }

    /// <summary>
    /// 等待URL包含指定文本
    /// </summary>
    /// <param name="expectedUrlPart">期望的URL部分</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    protected async Task WaitForUrlContainsAsync(string expectedUrlPart, int timeoutMs = 30000)
    {
        try
        {
            _logger.LogDebug($"等待URL包含: {expectedUrlPart}");
            await _page.WaitForURLAsync($"**/*{expectedUrlPart}*", new PageWaitForURLOptions { Timeout = timeoutMs });
            _logger.LogDebug($"URL验证成功: {expectedUrlPart}");
        }
        catch (TimeoutException)
        {
            var currentUrl = _page.Url;
            _logger.LogError($"等待URL超时，当前URL: '{currentUrl}', 期望包含: '{expectedUrlPart}'");
            throw new TimeoutException($"URL在 {timeoutMs}ms 内未包含期望文本 '{expectedUrlPart}'，当前URL: '{currentUrl}'");
        }
    }
}