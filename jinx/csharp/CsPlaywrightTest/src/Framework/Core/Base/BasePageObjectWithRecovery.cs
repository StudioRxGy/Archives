using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Core.Utilities;
using EnterpriseAutomationFramework.Services.Data;

namespace EnterpriseAutomationFramework.Core.Base;

/// <summary>
/// 带错误恢复功能的页面对象基类
/// </summary>
public abstract class BasePageObjectWithRecovery : BasePageObject
{
    protected readonly ErrorRecoveryStrategy _errorRecoveryStrategy;
    protected readonly ErrorRecoveryContext _recoveryContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="elementReader">元素读取器</param>
    /// <param name="errorRecoveryStrategy">错误恢复策略</param>
    protected BasePageObjectWithRecovery(
        IPage page,
        ILogger logger,
        YamlElementReader? elementReader = null,
        ErrorRecoveryStrategy? errorRecoveryStrategy = null)
        : base(page, logger, elementReader)
    {
        _errorRecoveryStrategy = errorRecoveryStrategy ?? ErrorRecoveryStrategy.CreateForUi(
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ErrorRecoveryStrategy>());
        
        _recoveryContext = ErrorRecoveryContext.ForPage(page, GetType().Name);
    }

    /// <summary>
    /// 带错误恢复的点击操作
    /// </summary>
    /// <param name="selector">选择器</param>
    /// <param name="options">点击选项</param>
    protected async Task ClickWithRecoveryAsync(string selector, PageClickOptions? options = null)
    {
        await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => await _page.ClickAsync(selector, options),
            $"Click_{selector}");
    }

    /// <summary>
    /// 带错误恢复的填充操作
    /// </summary>
    /// <param name="selector">选择器</param>
    /// <param name="value">值</param>
    /// <param name="options">填充选项</param>
    protected async Task FillWithRecoveryAsync(string selector, string value, PageFillOptions? options = null)
    {
        await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => await _page.FillAsync(selector, value, options),
            $"Fill_{selector}");
    }

    /// <summary>
    /// 带错误恢复的等待选择器操作
    /// </summary>
    /// <param name="selector">选择器</param>
    /// <param name="options">等待选项</param>
    /// <returns>元素句柄</returns>
    protected async Task<IElementHandle?> WaitForSelectorWithRecoveryAsync(string selector, PageWaitForSelectorOptions? options = null)
    {
        return await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => await _page.WaitForSelectorAsync(selector, options),
            $"WaitForSelector_{selector}");
    }

    /// <summary>
    /// 带错误恢复的获取文本操作
    /// </summary>
    /// <param name="selector">选择器</param>
    /// <param name="options">文本内容选项</param>
    /// <returns>文本内容</returns>
    protected async Task<string> GetTextWithRecoveryAsync(string selector, PageTextContentOptions? options = null)
    {
        return await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => await _page.TextContentAsync(selector, options) ?? string.Empty,
            $"GetText_{selector}");
    }

    /// <summary>
    /// 带错误恢复的检查元素可见性操作
    /// </summary>
    /// <param name="selector">选择器</param>
    /// <param name="options">可见性选项</param>
    /// <returns>是否可见</returns>
    protected async Task<bool> IsVisibleWithRecoveryAsync(string selector, PageIsVisibleOptions? options = null)
    {
        return await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => await _page.IsVisibleAsync(selector, options),
            $"IsVisible_{selector}");
    }

    /// <summary>
    /// 带错误恢复的导航操作
    /// </summary>
    /// <param name="url">目标URL</param>
    public override async Task NavigateAsync(string url)
    {
        await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => 
            {
                await _page.GotoAsync(url);
                await WaitForLoadAsync();
            },
            $"Navigate_{url}");
    }

    /// <summary>
    /// 带错误恢复的页面加载等待
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    public override async Task WaitForLoadAsync(int timeoutMs = 30000)
    {
        await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            async () => 
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = timeoutMs
                });
            },
            "WaitForLoad");
    }

    /// <summary>
    /// 使用元素名称进行带错误恢复的点击操作
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="options">点击选项</param>
    protected async Task ClickElementWithRecoveryAsync(string elementName, PageClickOptions? options = null)
    {
        var element = GetYamlElement(elementName);
        await ClickWithRecoveryAsync(element.Selector, options);
    }

    /// <summary>
    /// 使用元素名称进行带错误恢复的填充操作
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="value">值</param>
    /// <param name="options">填充选项</param>
    protected async Task FillElementWithRecoveryAsync(string elementName, string value, PageFillOptions? options = null)
    {
        var element = GetYamlElement(elementName);
        await FillWithRecoveryAsync(element.Selector, value, options);
    }

    /// <summary>
    /// 使用元素名称进行带错误恢复的文本获取操作
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="options">文本内容选项</param>
    /// <returns>文本内容</returns>
    protected async Task<string> GetElementTextWithRecoveryAsync(string elementName, PageTextContentOptions? options = null)
    {
        var element = GetYamlElement(elementName);
        return await GetTextWithRecoveryAsync(element.Selector, options);
    }

    /// <summary>
    /// 使用元素名称进行带错误恢复的可见性检查操作
    /// </summary>
    /// <param name="elementName">元素名称</param>
    /// <param name="options">可见性选项</param>
    /// <returns>是否可见</returns>
    protected async Task<bool> IsElementVisibleWithRecoveryAsync(string elementName, PageIsVisibleOptions? options = null)
    {
        var element = GetYamlElement(elementName);
        return await IsVisibleWithRecoveryAsync(element.Selector, options);
    }

    /// <summary>
    /// 执行自定义操作并带错误恢复
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    protected async Task<T> ExecuteWithRecoveryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        return await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            operation,
            operationName);
    }

    /// <summary>
    /// 执行自定义操作并带错误恢复（无返回值）
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    protected async Task ExecuteWithRecoveryAsync(Func<Task> operation, string operationName)
    {
        await _errorRecoveryStrategy.ExecuteWithPageRefreshRecoveryAsync(
            _page,
            operation,
            operationName);
    }

    /// <summary>
    /// 更新恢复上下文
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    protected void UpdateRecoveryContext(string? testName = null, string? operationName = null)
    {
        if (!string.IsNullOrEmpty(testName))
            _recoveryContext.TestName = testName;
        
        if (!string.IsNullOrEmpty(operationName))
            _recoveryContext.OperationName = operationName;
    }
}