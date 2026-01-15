using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using CsPlaywrightXun.src.playwright.Core.Exceptions;
using CsPlaywrightXun.src.playwright.Core.Configuration;
using CsPlaywrightXun.src.playwright.Core.Interfaces;

namespace CsPlaywrightXun.src.playwright.Core.Utilities;

/// <summary>
/// 错误恢复策略
/// </summary>
public class ErrorRecoveryStrategy
{
    private readonly ILogger<ErrorRecoveryStrategy> _logger;
    private readonly RetryExecutor _retryExecutor;
    private readonly ErrorRecoverySettings _settings;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="retryExecutor">重试执行器</param>
    /// <param name="settings">错误恢复设置</param>
    public ErrorRecoveryStrategy(
        ILogger<ErrorRecoveryStrategy> logger,
        RetryExecutor retryExecutor,
        ErrorRecoverySettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryExecutor = retryExecutor ?? throw new ArgumentNullException(nameof(retryExecutor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// 页面刷新恢复策略
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteWithPageRefreshRecoveryAsync<T>(
        IPage page,
        Func<Task<T>> operation,
        string operationName = "PageOperation")
    {
        if (page == null)
            throw new ArgumentNullException(nameof(page));
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await _retryExecutor.ExecuteAsync(async () =>
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsPageRefreshableError(ex))
            {
                _logger.LogWarning(ex, "检测到可通过页面刷新恢复的错误，正在刷新页面: {OperationName}", operationName);
                
                // 刷新页面
                await RefreshPageAsync(page);
                
                // 等待页面加载
                await WaitForPageLoadAsync(page);
                
                // 重新执行操作
                _logger.LogInformation("页面刷新完成，重新执行操作: {OperationName}", operationName);
                return await operation();
            }
        }, $"PageRefreshRecovery_{operationName}");
    }

    /// <summary>
    /// 页面刷新恢复策略（无返回值）
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    public async Task ExecuteWithPageRefreshRecoveryAsync(
        IPage page,
        Func<Task> operation,
        string operationName = "PageOperation")
    {
        await ExecuteWithPageRefreshRecoveryAsync(page, async () =>
        {
            await operation();
            return true;
        }, operationName);
    }

    /// <summary>
    /// 浏览器重启恢复策略
    /// </summary>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteWithBrowserRestartRecoveryAsync<T>(
        IBrowserService browserService,
        BrowserSettings browserSettings,
        Func<IPage, Task<T>> operation,
        string operationName = "BrowserOperation")
    {
        if (browserService == null)
            throw new ArgumentNullException(nameof(browserService));
        if (browserSettings == null)
            throw new ArgumentNullException(nameof(browserSettings));
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        IPage? currentPage = null;
        
        return await _retryExecutor.ExecuteAsync(async () =>
        {
            try
            {
                // 如果没有当前页面或页面已关闭，创建新页面
                if (currentPage == null || currentPage.IsClosed)
                {
                    currentPage = await browserService.CreatePageAsync(browserSettings);
                }
                
                return await operation(currentPage);
            }
            catch (Exception ex) when (IsBrowserRestartableError(ex))
            {
                _logger.LogWarning(ex, "检测到需要浏览器重启的错误，正在重启浏览器: {OperationName}", operationName);
                
                // 关闭当前浏览器服务
                await browserService.CloseAsync();
                
                // 等待一段时间让资源释放
                await Task.Delay(_settings.BrowserRestartDelayTimeSpan);
                
                // 创建新的页面实例
                currentPage = await browserService.CreatePageAsync(browserSettings);
                
                _logger.LogInformation("浏览器重启完成，重新执行操作: {OperationName}", operationName);
                return await operation(currentPage);
            }
        }, $"BrowserRestartRecovery_{operationName}");
    }

    /// <summary>
    /// 浏览器重启恢复策略（无返回值）
    /// </summary>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    public async Task ExecuteWithBrowserRestartRecoveryAsync(
        IBrowserService browserService,
        BrowserSettings browserSettings,
        Func<IPage, Task> operation,
        string operationName = "BrowserOperation")
    {
        await ExecuteWithBrowserRestartRecoveryAsync(browserService, browserSettings, async (page) =>
        {
            await operation(page);
            return true;
        }, operationName);
    }

    /// <summary>
    /// API重试恢复策略
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="operation">要执行的API操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteWithApiRetryRecoveryAsync<T>(
        IApiClient apiClient,
        Func<Task<T>> operation,
        string operationName = "ApiOperation")
    {
        if (apiClient == null)
            throw new ArgumentNullException(nameof(apiClient));
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var apiRetryPolicy = CreateApiRetryPolicy();
        var apiRetryExecutor = new RetryExecutor(apiRetryPolicy, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RetryExecutor>());

        return await apiRetryExecutor.ExecuteAsync(async () =>
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsApiRetryableError(ex))
            {
                _logger.LogWarning(ex, "检测到可重试的API错误: {OperationName}", operationName);
                
                // 等待一段时间再重试
                await Task.Delay(_settings.ApiRetryDelayTimeSpan);
                
                throw; // 让重试执行器处理重试逻辑
            }
        }, $"ApiRetryRecovery_{operationName}");
    }

    /// <summary>
    /// API重试恢复策略（无返回值）
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="operation">要执行的API操作</param>
    /// <param name="operationName">操作名称</param>
    public async Task ExecuteWithApiRetryRecoveryAsync(
        IApiClient apiClient,
        Func<Task> operation,
        string operationName = "ApiOperation")
    {
        await ExecuteWithApiRetryRecoveryAsync(apiClient, async () =>
        {
            await operation();
            return true;
        }, operationName);
    }

    /// <summary>
    /// 综合错误恢复策略
    /// 结合页面刷新、浏览器重启和API重试
    /// </summary>
    /// <param name="context">恢复上下文</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteWithComprehensiveRecoveryAsync<T>(
        ErrorRecoveryContext context,
        Func<Task<T>> operation,
        string operationName = "ComprehensiveOperation")
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        return await _retryExecutor.ExecuteAsync(async () =>
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "操作失败，正在分析错误类型并选择恢复策略: {OperationName}", operationName);
                
                // 根据错误类型选择恢复策略
                if (context.Page != null && IsPageRefreshableError(ex))
                {
                    _logger.LogInformation("使用页面刷新恢复策略: {OperationName}", operationName);
                    await RefreshPageAsync(context.Page);
                    await WaitForPageLoadAsync(context.Page);
                }
                else if (context.BrowserService != null && context.BrowserSettings != null && IsBrowserRestartableError(ex))
                {
                    _logger.LogInformation("使用浏览器重启恢复策略: {OperationName}", operationName);
                    await context.BrowserService.CloseAsync();
                    await Task.Delay(_settings.BrowserRestartDelayTimeSpan);
                    context.Page = await context.BrowserService.CreatePageAsync(context.BrowserSettings);
                }
                else if (context.ApiClient != null && IsApiRetryableError(ex))
                {
                    _logger.LogInformation("使用API重试恢复策略: {OperationName}", operationName);
                    await Task.Delay(_settings.ApiRetryDelayTimeSpan);
                }
                
                throw; // 让重试执行器处理重试逻辑
            }
        }, $"ComprehensiveRecovery_{operationName}");
    }

    /// <summary>
    /// 刷新页面
    /// </summary>
    /// <param name="page">页面实例</param>
    private async Task RefreshPageAsync(IPage page)
    {
        try
        {
            _logger.LogDebug("正在刷新页面");
            await page.ReloadAsync(new PageReloadOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = _settings.PageRefreshTimeout
            });
            _logger.LogDebug("页面刷新完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "页面刷新失败");
            throw new TestFrameworkException("ErrorRecoveryStrategy", "ErrorRecoveryStrategy", 
                $"页面刷新失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 等待页面加载完成
    /// </summary>
    /// <param name="page">页面实例</param>
    private async Task WaitForPageLoadAsync(IPage page)
    {
        try
        {
            _logger.LogDebug("等待页面加载完成");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = _settings.PageLoadTimeout
            });
            _logger.LogDebug("页面加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "等待页面加载超时，继续执行");
            // 不抛出异常，允许操作继续
        }
    }

    /// <summary>
    /// 判断是否为可通过页面刷新恢复的错误
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否可通过页面刷新恢复</returns>
    private bool IsPageRefreshableError(Exception exception)
    {
        return exception switch
        {
            ElementNotFoundException => true,
            TimeoutException => true,
            PlaywrightException pwEx when pwEx.Message.Contains("Element is not attached") => true,
            PlaywrightException pwEx when pwEx.Message.Contains("Element is not visible") => true,
            PlaywrightException pwEx when pwEx.Message.Contains("waiting for selector") => true,
            InvalidOperationException ioEx when ioEx.Message.Contains("Page is closed") => false, // 需要浏览器重启
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为需要浏览器重启的错误
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否需要浏览器重启</returns>
    private bool IsBrowserRestartableError(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException ioEx when ioEx.Message.Contains("Page is closed") => true,
            InvalidOperationException ioEx when ioEx.Message.Contains("Browser is closed") => true,
            InvalidOperationException ioEx when ioEx.Message.Contains("Context is closed") => true,
            PlaywrightException pwEx when pwEx.Message.Contains("Browser has been closed") => true,
            PlaywrightException pwEx when pwEx.Message.Contains("Target page, context or browser has been closed") => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为可重试的API错误
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否为可重试的API错误</returns>
    private bool IsApiRetryableError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            ApiException apiEx when apiEx.StatusCode >= 500 => true, // 服务器错误
            ApiException apiEx when apiEx.StatusCode == 429 => true, // 请求过多
            ApiException apiEx when apiEx.StatusCode == 408 => true, // 请求超时
            _ => false
        };
    }

    /// <summary>
    /// 创建API重试策略
    /// </summary>
    /// <returns>API重试策略</returns>
    private RetryPolicy CreateApiRetryPolicy()
    {
        return new RetryPolicy
        {
            MaxAttempts = _settings.ApiMaxRetryAttempts,
            DelayBetweenAttempts = _settings.ApiRetryDelayTimeSpan,
            UseExponentialBackoff = _settings.UseExponentialBackoff,
            ExponentialBackoffMultiplier = _settings.ExponentialBackoffMultiplier,
            MaxDelay = _settings.MaxRetryDelayTimeSpan,
            RetryCondition = IsApiRetryableError
        };
    }

    /// <summary>
    /// 创建默认错误恢复策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>错误恢复策略</returns>
    public static ErrorRecoveryStrategy CreateDefault(ILogger<ErrorRecoveryStrategy> logger)
    {
        var settings = ErrorRecoverySettings.CreateDefault();
        var retryPolicy = RetryPolicy.CreateDefaultUiPolicy();
        var retryLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RetryExecutor>();
        var retryExecutor = new RetryExecutor(retryPolicy, retryLogger);
        
        return new ErrorRecoveryStrategy(logger, retryExecutor, settings);
    }

    /// <summary>
    /// 创建API专用错误恢复策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>API错误恢复策略</returns>
    public static ErrorRecoveryStrategy CreateForApi(ILogger<ErrorRecoveryStrategy> logger)
    {
        var settings = ErrorRecoverySettings.CreateForApi();
        var retryPolicy = RetryPolicy.CreateDefaultApiPolicy();
        var retryLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RetryExecutor>();
        var retryExecutor = new RetryExecutor(retryPolicy, retryLogger);
        
        return new ErrorRecoveryStrategy(logger, retryExecutor, settings);
    }

    /// <summary>
    /// 创建UI专用错误恢复策略
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <returns>UI错误恢复策略</returns>
    public static ErrorRecoveryStrategy CreateForUi(ILogger<ErrorRecoveryStrategy> logger)
    {
        var settings = ErrorRecoverySettings.CreateForUi();
        var retryPolicy = RetryPolicy.CreateDefaultUiPolicy();
        var retryLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<RetryExecutor>();
        var retryExecutor = new RetryExecutor(retryPolicy, retryLogger);
        
        return new ErrorRecoveryStrategy(logger, retryExecutor, settings);
    }
}