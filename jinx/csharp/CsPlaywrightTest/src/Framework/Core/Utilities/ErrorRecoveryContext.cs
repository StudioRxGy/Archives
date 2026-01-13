using Microsoft.Playwright;
using EnterpriseAutomationFramework.Core.Interfaces;
using EnterpriseAutomationFramework.Core.Configuration;

namespace EnterpriseAutomationFramework.Core.Utilities;

/// <summary>
/// 错误恢复上下文
/// 包含执行错误恢复所需的所有资源
/// </summary>
public class ErrorRecoveryContext
{
    /// <summary>
    /// 页面实例
    /// </summary>
    public IPage? Page { get; set; }

    /// <summary>
    /// 浏览器服务
    /// </summary>
    public IBrowserService? BrowserService { get; set; }

    /// <summary>
    /// 浏览器设置
    /// </summary>
    public BrowserSettings? BrowserSettings { get; set; }

    /// <summary>
    /// API客户端
    /// </summary>
    public IApiClient? ApiClient { get; set; }

    /// <summary>
    /// 测试名称
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    /// 组件名称
    /// </summary>
    public string? ComponentName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// 额外的上下文数据
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public ErrorRecoveryContext()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    public ErrorRecoveryContext(IPage page, string? testName = null, string? operationName = null)
    {
        Page = page;
        TestName = testName;
        OperationName = operationName;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    public ErrorRecoveryContext(
        IBrowserService browserService, 
        BrowserSettings browserSettings, 
        string? testName = null, 
        string? operationName = null)
    {
        BrowserService = browserService;
        BrowserSettings = browserSettings;
        TestName = testName;
        OperationName = operationName;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    public ErrorRecoveryContext(IApiClient apiClient, string? testName = null, string? operationName = null)
    {
        ApiClient = apiClient;
        TestName = testName;
        OperationName = operationName;
    }

    /// <summary>
    /// 完整构造函数
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <param name="apiClient">API客户端</param>
    /// <param name="testName">测试名称</param>
    /// <param name="componentName">组件名称</param>
    /// <param name="operationName">操作名称</param>
    public ErrorRecoveryContext(
        IPage? page = null,
        IBrowserService? browserService = null,
        BrowserSettings? browserSettings = null,
        IApiClient? apiClient = null,
        string? testName = null,
        string? componentName = null,
        string? operationName = null)
    {
        Page = page;
        BrowserService = browserService;
        BrowserSettings = browserSettings;
        ApiClient = apiClient;
        TestName = testName;
        ComponentName = componentName;
        OperationName = operationName;
    }

    /// <summary>
    /// 检查是否具有页面恢复能力
    /// </summary>
    /// <returns>是否具有页面恢复能力</returns>
    public bool HasPageRecoveryCapability()
    {
        return Page != null && !Page.IsClosed;
    }

    /// <summary>
    /// 检查是否具有浏览器重启能力
    /// </summary>
    /// <returns>是否具有浏览器重启能力</returns>
    public bool HasBrowserRestartCapability()
    {
        return BrowserService != null && BrowserSettings != null;
    }

    /// <summary>
    /// 检查是否具有API重试能力
    /// </summary>
    /// <returns>是否具有API重试能力</returns>
    public bool HasApiRetryCapability()
    {
        return ApiClient != null;
    }

    /// <summary>
    /// 添加额外数据
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns>当前上下文实例</returns>
    public ErrorRecoveryContext WithAdditionalData(string key, object value)
    {
        AdditionalData[key] = value;
        return this;
    }

    /// <summary>
    /// 获取额外数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>数据值</returns>
    public T? GetAdditionalData<T>(string key, T? defaultValue = default)
    {
        if (AdditionalData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置测试信息
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="componentName">组件名称</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>当前上下文实例</returns>
    public ErrorRecoveryContext WithTestInfo(string? testName, string? componentName = null, string? operationName = null)
    {
        TestName = testName;
        ComponentName = componentName;
        OperationName = operationName;
        return this;
    }

    /// <summary>
    /// 设置页面信息
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <returns>当前上下文实例</returns>
    public ErrorRecoveryContext WithPage(IPage page)
    {
        Page = page;
        return this;
    }

    /// <summary>
    /// 设置浏览器信息
    /// </summary>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <returns>当前上下文实例</returns>
    public ErrorRecoveryContext WithBrowser(IBrowserService browserService, BrowserSettings browserSettings)
    {
        BrowserService = browserService;
        BrowserSettings = browserSettings;
        return this;
    }

    /// <summary>
    /// 设置API客户端
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <returns>当前上下文实例</returns>
    public ErrorRecoveryContext WithApiClient(IApiClient apiClient)
    {
        ApiClient = apiClient;
        return this;
    }

    /// <summary>
    /// 克隆上下文
    /// </summary>
    /// <returns>克隆的上下文</returns>
    public ErrorRecoveryContext Clone()
    {
        return new ErrorRecoveryContext
        {
            Page = Page,
            BrowserService = BrowserService,
            BrowserSettings = BrowserSettings,
            ApiClient = ApiClient,
            TestName = TestName,
            ComponentName = ComponentName,
            OperationName = OperationName,
            AdditionalData = new Dictionary<string, object>(AdditionalData)
        };
    }

    /// <summary>
    /// 获取上下文描述
    /// </summary>
    /// <returns>上下文描述</returns>
    public string GetDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(TestName))
            parts.Add($"Test: {TestName}");

        if (!string.IsNullOrEmpty(ComponentName))
            parts.Add($"Component: {ComponentName}");

        if (!string.IsNullOrEmpty(OperationName))
            parts.Add($"Operation: {OperationName}");

        if (HasPageRecoveryCapability())
            parts.Add("Page: Available");

        if (HasBrowserRestartCapability())
            parts.Add("Browser: Available");

        if (HasApiRetryCapability())
            parts.Add("API: Available");

        return string.Join(", ", parts);
    }

    /// <summary>
    /// 创建页面专用上下文
    /// </summary>
    /// <param name="page">页面实例</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>页面上下文</returns>
    public static ErrorRecoveryContext ForPage(IPage page, string? testName = null, string? operationName = null)
    {
        return new ErrorRecoveryContext(page, testName, operationName);
    }

    /// <summary>
    /// 创建浏览器专用上下文
    /// </summary>
    /// <param name="browserService">浏览器服务</param>
    /// <param name="browserSettings">浏览器设置</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>浏览器上下文</returns>
    public static ErrorRecoveryContext ForBrowser(
        IBrowserService browserService, 
        BrowserSettings browserSettings, 
        string? testName = null, 
        string? operationName = null)
    {
        return new ErrorRecoveryContext(browserService, browserSettings, testName, operationName);
    }

    /// <summary>
    /// 创建API专用上下文
    /// </summary>
    /// <param name="apiClient">API客户端</param>
    /// <param name="testName">测试名称</param>
    /// <param name="operationName">操作名称</param>
    /// <returns>API上下文</returns>
    public static ErrorRecoveryContext ForApi(IApiClient apiClient, string? testName = null, string? operationName = null)
    {
        return new ErrorRecoveryContext(apiClient, testName, operationName);
    }
}